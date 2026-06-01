using CRM.DTO;
using CRM.models;

namespace CRM.Helpers
{
    public static class QuotationMappingHelper
    {
        public static void ApplyHeader(Quotation entity, QuotationUpsertDto dto)
        {
            entity.DealId = dto.DealId;
            entity.Salutation = (dto.Salutation ?? string.Empty).Trim();
            entity.FirstName = (dto.FirstName ?? string.Empty).Trim();
            entity.LastName = (dto.LastName ?? string.Empty).Trim();
            entity.Gender = (dto.Gender ?? string.Empty).Trim();
            entity.Employees = (dto.Employees ?? string.Empty).Trim();
            entity.AnnualRevenue = dto.AnnualRevenue;
            entity.Website = (dto.Website ?? string.Empty).Trim();
            entity.Gst = (dto.Gst ?? string.Empty).Trim();
            entity.Territory = (dto.Territory ?? string.Empty).Trim();
            entity.Industry = (dto.Industry ?? string.Empty).Trim();

            var firstLast = string.Join(" ", new[] { entity.FirstName, entity.LastName }.Where(s => !string.IsNullOrWhiteSpace(s)));
            entity.CustomerName = !string.IsNullOrWhiteSpace(firstLast)
                ? firstLast
                : (dto.CustomerName ?? string.Empty).Trim();
            entity.CompanyName = (dto.CompanyName ?? string.Empty).Trim();

            var contactParts = new[] { entity.Salutation, entity.FirstName, entity.LastName }
                .Where(s => !string.IsNullOrWhiteSpace(s));
            entity.ContactPerson = !string.IsNullOrWhiteSpace(dto.ContactPerson)
                ? dto.ContactPerson.Trim()
                : string.Join(" ", contactParts);
            entity.MobileNumber = (dto.MobileNumber ?? string.Empty).Trim();
            entity.EmailAddress = (dto.EmailAddress ?? string.Empty).Trim();
            entity.OfficeAddress = (dto.OfficeAddress ?? string.Empty).Trim();
            entity.SiteAddress = (dto.SiteAddress ?? string.Empty).Trim();
            entity.ReferenceNumber = (dto.ReferenceNumber ?? string.Empty).Trim();
            entity.ReferenceDate = DateTimeUtcHelper.ToUtcOrNull(dto.ReferenceDate);
            entity.CompanyCode = (dto.CompanyCode ?? string.Empty).Trim();
            entity.DocumentTypeCode = string.IsNullOrWhiteSpace(dto.DocumentTypeCode)
                ? "QTN"
                : dto.DocumentTypeCode.Trim();
            entity.FiscalYearLabel = (dto.FiscalYearLabel ?? string.Empty).Trim();
            entity.SequenceNumber = dto.SequenceNumber;
            entity.QuotationNumber = (dto.QuotationNumber ?? string.Empty).Trim();
            entity.QuotationDate = dto.QuotationDate.HasValue
                ? DateTimeUtcHelper.ToUtc(dto.QuotationDate.Value)
                : DateTime.UtcNow;
            entity.Remarks = (dto.Remarks ?? string.Empty).Trim();

            var status = (dto.Status ?? QuotationStatuses.Draft).Trim();
            entity.Status = QuotationStatuses.All.Contains(status, StringComparer.OrdinalIgnoreCase)
                ? QuotationStatuses.All.First(s => s.Equals(status, StringComparison.OrdinalIgnoreCase))
                : QuotationStatuses.Draft;
        }

        public static List<QuotationLineItem> MapLineItems(int quotationId, IEnumerable<QuotationLineItemDto>? items)
        {
            var list = new List<QuotationLineItem>();
            if (items == null)
            {
                return list;
            }

            var idx = 0;
            foreach (var dto in items.OrderBy(x => x.LineIndex).ThenBy(x => x.Id))
            {
                var qty = dto.Quantity <= 0 ? 1 : dto.Quantity;
                var rate = dto.Rate;
                var amount = dto.Amount > 0 ? dto.Amount : Math.Round(qty * rate, 2, MidpointRounding.AwayFromZero);
                list.Add(new QuotationLineItem
                {
                    Id = 0,
                    QuotationId = quotationId,
                    LineIndex = dto.LineIndex > 0 ? dto.LineIndex : idx,
                    ItemCode = (dto.ItemCode ?? string.Empty).Trim(),
                    Description = (dto.Description ?? string.Empty).Trim(),
                    Quantity = qty,
                    Uom = (dto.Uom ?? string.Empty).Trim(),
                    Rate = rate,
                    Amount = amount,
                });
                idx++;
            }

            return list;
        }

        public static decimal ComputeGrandTotal(IEnumerable<QuotationLineItem> lines) =>
            lines.Sum(l => l.Amount);

        public static QuotationUpsertDto ToUpsertDto(Quotation q)
        {
            return new QuotationUpsertDto
            {
                Id = q.Id,
                DealId = q.DealId,
                Salutation = q.Salutation,
                FirstName = q.FirstName,
                LastName = q.LastName,
                Gender = q.Gender,
                CustomerName = q.CustomerName,
                CompanyName = q.CompanyName,
                Employees = q.Employees,
                AnnualRevenue = q.AnnualRevenue,
                Website = q.Website,
                Gst = q.Gst,
                Territory = q.Territory,
                Industry = q.Industry,
                ContactPerson = q.ContactPerson,
                MobileNumber = q.MobileNumber,
                EmailAddress = q.EmailAddress,
                OfficeAddress = q.OfficeAddress,
                SiteAddress = q.SiteAddress,
                ReferenceNumber = q.ReferenceNumber,
                ReferenceDate = q.ReferenceDate,
                CompanyCode = q.CompanyCode,
                DocumentTypeCode = q.DocumentTypeCode,
                FiscalYearLabel = q.FiscalYearLabel,
                SequenceNumber = q.SequenceNumber,
                QuotationNumber = q.QuotationNumber,
                QuotationDate = q.QuotationDate,
                Status = q.Status,
                Remarks = q.Remarks,
                LineItems = q.LineItems
                    .OrderBy(l => l.LineIndex)
                    .Select(l => new QuotationLineItemDto
                    {
                        Id = l.Id,
                        LineIndex = l.LineIndex,
                        ItemCode = l.ItemCode,
                        Description = l.Description,
                        Quantity = l.Quantity,
                        Uom = l.Uom,
                        Rate = l.Rate,
                        Amount = l.Amount,
                    })
                    .ToList(),
            };
        }
    }
}
