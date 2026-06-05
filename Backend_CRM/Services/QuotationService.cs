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
        Task<QuotationGridColumnsDto> GetGridColumnsForUserAsync(int userId, CancellationToken ct = default);
        Task<QuotationGridColumnsDto> SaveUserGridColumnsAsync(int userId, QuotationGridColumnsDto dto, CancellationToken ct = default);
        Task<QuotationGridColumnsDto> GetGridDefaultColumnsAsync(CancellationToken ct = default);
        Task<QuotationGridColumnsDto> SaveGridDefaultColumnsAsync(int adminUserId, QuotationGridColumnsDto dto, CancellationToken ct = default);
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
                QuotationMappingHelper.ApplyTotals(entity, entity.LineItems);
                return;
            }

            var seq = await GetNextSequenceValueAsync(entity.CompanyCode, entity.FiscalYearLabel, peekOnly: false, ct);
            entity.SequenceNumber = seq;
            entity.QuotationNumber = QuotationNumberHelper.FormatNumber(
                entity.CompanyCode,
                entity.DocumentTypeCode,
                entity.FiscalYearLabel,
                entity.SequenceNumber);
            QuotationMappingHelper.ApplyTotals(entity, entity.LineItems);
        }

        public async Task<QuotationGridColumnsDto> GetGridColumnsForUserAsync(int userId, CancellationToken ct = default)
        {
            var userPref = await _db.QuotationItemGridUserPreferences
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.UserId == userId, ct);

            if (userPref != null && !string.IsNullOrWhiteSpace(userPref.ColumnsJson))
            {
                return new QuotationGridColumnsDto
                {
                    Columns = QuotationGridColumnHelper.ParseColumns(userPref.ColumnsJson),
                };
            }

            return await GetGridDefaultColumnsAsync(ct);
        }

        public async Task<QuotationGridColumnsDto> SaveUserGridColumnsAsync(
            int userId,
            QuotationGridColumnsDto dto,
            CancellationToken ct = default)
        {
            var merged = QuotationGridColumnHelper.MergeWithDefaults(dto.Columns ?? new List<QuotationGridColumnDto>());
            var json = QuotationGridColumnHelper.SerializeColumns(merged);

            var row = await _db.QuotationItemGridUserPreferences.FirstOrDefaultAsync(p => p.UserId == userId, ct);
            if (row == null)
            {
                row = new QuotationItemGridUserPreference { UserId = userId };
                _db.QuotationItemGridUserPreferences.Add(row);
            }

            row.ColumnsJson = json;
            row.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync(ct);

            return new QuotationGridColumnsDto { Columns = merged };
        }

        public async Task<QuotationGridColumnsDto> GetGridDefaultColumnsAsync(CancellationToken ct = default)
        {
            var row = await EnsureGridDefaultRowAsync(ct);
            return new QuotationGridColumnsDto
            {
                Columns = QuotationGridColumnHelper.ParseColumns(row.ColumnsJson),
            };
        }

        public async Task<QuotationGridColumnsDto> SaveGridDefaultColumnsAsync(
            int adminUserId,
            QuotationGridColumnsDto dto,
            CancellationToken ct = default)
        {
            var merged = QuotationGridColumnHelper.MergeWithDefaults(dto.Columns ?? new List<QuotationGridColumnDto>());
            var json = QuotationGridColumnHelper.SerializeColumns(merged);
            var row = await EnsureGridDefaultRowAsync(ct);
            row.ColumnsJson = json;
            row.UpdatedAt = DateTime.UtcNow;
            row.UpdatedBy = adminUserId;
            await _db.SaveChangesAsync(ct);
            return new QuotationGridColumnsDto { Columns = merged };
        }

        private async Task<QuotationItemGridDefault> EnsureGridDefaultRowAsync(CancellationToken ct)
        {
            var row = await _db.QuotationItemGridDefaults.FirstOrDefaultAsync(d => d.Id == 1, ct);
            if (row != null)
            {
                if (string.IsNullOrWhiteSpace(row.ColumnsJson))
                {
                    row.ColumnsJson = QuotationGridColumnDefaults.DefaultColumnsJson;
                    row.UpdatedAt = DateTime.UtcNow;
                    await _db.SaveChangesAsync(ct);
                }

                return row;
            }

            row = new QuotationItemGridDefault
            {
                Id = 1,
                ColumnsJson = QuotationGridColumnDefaults.DefaultColumnsJson,
                UpdatedAt = DateTime.UtcNow,
            };
            _db.QuotationItemGridDefaults.Add(row);
            await _db.SaveChangesAsync(ct);
            return row;
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

            var maxUsedInQuotations = await _db.Quotations.AsNoTracking()
                .Where(q => q.CompanyCode == cc && q.FiscalYearLabel == fy)
                .MaxAsync(q => (int?)q.SequenceNumber, ct) ?? 0;

            var row = await _db.QuotationFiscalSequences
                .FirstOrDefaultAsync(s => s.CompanyCode == cc && s.FiscalYearLabel == fy, ct);

            if (row == null)
            {
                if (peekOnly)
                {
                    return maxUsedInQuotations + 1;
                }

                row = new QuotationFiscalSequence
                {
                    CompanyCode = cc,
                    FiscalYearLabel = fy,
                    LastSequence = maxUsedInQuotations,
                };
                _db.QuotationFiscalSequences.Add(row);
            }

            var baseline = Math.Max(row.LastSequence, maxUsedInQuotations);
            var next = baseline + 1;
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
