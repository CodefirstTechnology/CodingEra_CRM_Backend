using CRM.DATA;
using CRM.DTO;
using CRM.Helpers;
using CRM.models;
using Microsoft.EntityFrameworkCore;

namespace CRM.Services
{
    public interface IQuotationService
    {
        Task<QuotationSettingsDto> GetSettingsAsync(CancellationToken ct = default);
        Task<QuotationSettingsDto> UpdateSettingsAsync(QuotationSettingsDto dto, CancellationToken ct = default);
        Task<QuotationNextNumberDto> PeekNextNumberAsync(string? companyCode, DateTime? asOf, CancellationToken ct = default);
        Task ReserveNextNumberAsync(Quotation entity, bool forceNewSequence, CancellationToken ct = default);
    }

    public class QuotationService : IQuotationService
    {
        private readonly TaskDbcontext _db;

        public QuotationService(TaskDbcontext db)
        {
            _db = db;
        }

        public async Task<QuotationSettingsDto> GetSettingsAsync(CancellationToken ct = default)
        {
            var row = await EnsureSettingsRowAsync(ct);
            return new QuotationSettingsDto
            {
                CompanyCode = row.CompanyCode,
                DocumentTypeCode = row.DocumentTypeCode,
            };
        }

        public async Task<QuotationSettingsDto> UpdateSettingsAsync(QuotationSettingsDto dto, CancellationToken ct = default)
        {
            var row = await EnsureSettingsRowAsync(ct);
            row.CompanyCode = (dto.CompanyCode ?? string.Empty).Trim();
            row.DocumentTypeCode = string.IsNullOrWhiteSpace(dto.DocumentTypeCode)
                ? "QTN"
                : dto.DocumentTypeCode.Trim();
            row.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync(ct);
            return await GetSettingsAsync(ct);
        }

        public async Task<QuotationNextNumberDto> PeekNextNumberAsync(
            string? companyCode,
            DateTime? asOf,
            CancellationToken ct = default)
        {
            var settings = await EnsureSettingsRowAsync(ct);
            var cc = string.IsNullOrWhiteSpace(companyCode) ? settings.CompanyCode : companyCode.Trim();
            var doc = settings.DocumentTypeCode;
            var date = asOf.HasValue ? DateTimeUtcHelper.ToUtc(asOf.Value) : DateTime.UtcNow;
            var fy = QuotationNumberHelper.FiscalYearLabelFor(date);
            var nextSeq = await GetNextSequenceValueAsync(cc, fy, peekOnly: true, ct);
            return BuildNextDto(cc, doc, fy, nextSeq, date);
        }

        public async Task ReserveNextNumberAsync(Quotation entity, bool forceNewSequence, CancellationToken ct = default)
        {
            var settings = await EnsureSettingsRowAsync(ct);
            if (string.IsNullOrWhiteSpace(entity.CompanyCode))
            {
                entity.CompanyCode = settings.CompanyCode;
            }

            if (string.IsNullOrWhiteSpace(entity.DocumentTypeCode))
            {
                entity.DocumentTypeCode = settings.DocumentTypeCode;
            }

            if (string.IsNullOrWhiteSpace(entity.FiscalYearLabel))
            {
                entity.FiscalYearLabel = QuotationNumberHelper.FiscalYearLabelFor(entity.QuotationDate);
            }

            if (entity.SequenceNumber > 0 && !string.IsNullOrWhiteSpace(entity.QuotationNumber) && !forceNewSequence)
            {
                entity.GrandTotal = QuotationMappingHelper.ComputeGrandTotal(entity.LineItems);
                return;
            }

            var seq = await GetNextSequenceValueAsync(entity.CompanyCode, entity.FiscalYearLabel, peekOnly: false, ct);
            entity.SequenceNumber = seq;
            entity.QuotationNumber = QuotationNumberHelper.FormatNumber(
                entity.CompanyCode,
                entity.DocumentTypeCode,
                entity.FiscalYearLabel,
                entity.SequenceNumber);
            entity.GrandTotal = QuotationMappingHelper.ComputeGrandTotal(entity.LineItems);
        }

        private async Task<QuotationSettings> EnsureSettingsRowAsync(CancellationToken ct)
        {
            var row = await _db.QuotationSettings.FirstOrDefaultAsync(s => s.Id == 1, ct);
            if (row != null)
            {
                return row;
            }

            row = new QuotationSettings
            {
                Id = 1,
                CompanyCode = string.Empty,
                DocumentTypeCode = "QTN",
                UpdatedAt = DateTime.UtcNow,
            };
            _db.QuotationSettings.Add(row);
            await _db.SaveChangesAsync(ct);
            return row;
        }

        private async Task<int> GetNextSequenceValueAsync(
            string companyCode,
            string fiscalYear,
            bool peekOnly,
            CancellationToken ct)
        {
            var cc = (companyCode ?? string.Empty).Trim();
            var fy = (fiscalYear ?? string.Empty).Trim();

            var row = await _db.QuotationFiscalSequences
                .FirstOrDefaultAsync(s => s.CompanyCode == cc && s.FiscalYearLabel == fy, ct);

            if (row == null)
            {
                if (peekOnly)
                {
                    var used = await _db.Quotations.AsNoTracking()
                        .Where(q => q.CompanyCode == cc && q.FiscalYearLabel == fy)
                        .MaxAsync(q => (int?)q.SequenceNumber, ct);
                    return (used ?? 0) + 1;
                }

                row = new QuotationFiscalSequence
                {
                    CompanyCode = cc,
                    FiscalYearLabel = fy,
                    LastSequence = 0,
                };
                _db.QuotationFiscalSequences.Add(row);
            }

            var next = row.LastSequence + 1;
            if (!peekOnly)
            {
                row.LastSequence = next;
            }

            return next;
        }

        private static QuotationNextNumberDto BuildNextDto(
            string companyCode,
            string docType,
            string fiscalYear,
            int sequence,
            DateTime date)
        {
            return new QuotationNextNumberDto
            {
                CompanyCode = companyCode,
                DocumentTypeCode = docType,
                FiscalYearLabel = fiscalYear,
                SequenceNumber = sequence,
                QuotationNumber = QuotationNumberHelper.FormatNumber(companyCode, docType, fiscalYear, sequence),
                QuotationDate = DateTimeUtcHelper.ToUtc(date),
            };
        }
    }
}
