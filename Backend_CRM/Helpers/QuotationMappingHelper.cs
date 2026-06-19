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
            entity.GstPercent = dto.GstPercent < 0 ? 0 : dto.GstPercent;
            entity.TransportationCharges = dto.TransportationCharges < 0 ? 0 : dto.TransportationCharges;
            entity.LoadingCharges = dto.LoadingCharges < 0 ? 0 : dto.LoadingCharges;
            entity.ServiceCharges = dto.ServiceCharges < 0 ? 0 : dto.ServiceCharges;

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
                var steelRate = dto.SteelRate < 0 ? 0 : dto.SteelRate;
                var unitWeight = dto.UnitWeight < 0 ? 0 : dto.UnitWeight;
                var weight = dto.Weight < 0 ? 0 : dto.Weight;
                var disc = dto.DiscountPercent < 0 ? 0 : dto.DiscountPercent;
                var rate = QuotationLineCalculator.ResolveUnitRate(unitWeight, steelRate, dto.Rate);

                var calc = QuotationLineCalculator.CalculateLine(qty, rate, disc, weight, unitWeight);

                list.Add(new QuotationLineItem
                {
                    Id = 0,
                    QuotationId = quotationId,
                    LineIndex = dto.LineIndex >= 0 ? dto.LineIndex : idx,
                    ItemId = dto.ItemId,
                    ItemCode = (dto.ItemCode ?? string.Empty).Trim(),
                    ItemName = (dto.ItemName ?? string.Empty).Trim(),
                    Description = (dto.Description ?? string.Empty).Trim(),
                    Quantity = qty,
                    Uom = (dto.Uom ?? string.Empty).Trim(),
                    Weight = weight,
                    UnitWeight = unitWeight,
                    Rate = rate,
                    SteelRate = steelRate,
                    ItemSnapshotJson = dto.ItemSnapshotJson ?? string.Empty,
                    DiscountPercent = disc,
                    GstPercent = 0,
                    Amount = calc.Amount,
                    TaxAmount = 0,
                    LineTotal = calc.LineTotal,
                });
                idx++;
            }

            return list;
        }

        public static List<QuotationAdditionalCharge> MapCustomCharges(
            int quotationId,
            IEnumerable<QuotationAdditionalChargeDto>? items)
        {
            var list = new List<QuotationAdditionalCharge>();
            if (items == null)
            {
                return list;
            }

            var idx = 0;
            foreach (var dto in items.OrderBy(x => x.SortIndex).ThenBy(x => x.Id))
            {
                var name = (dto.ChargeName ?? string.Empty).Trim();
                var amount = dto.Amount < 0 ? 0 : dto.Amount;
                if (string.IsNullOrWhiteSpace(name) && amount <= 0)
                {
                    continue;
                }

                list.Add(new QuotationAdditionalCharge
                {
                    Id = 0,
                    QuotationId = quotationId,
                    SortIndex = dto.SortIndex >= 0 ? dto.SortIndex : idx,
                    ChargeName = name,
                    Amount = amount,
                });
                idx++;
            }

            return list;
        }

        public static decimal ComputeAdditionalChargesTotal(Quotation entity) =>
            QuotationLineCalculator.SumAdditionalCharges(
                entity.TransportationCharges,
                entity.LoadingCharges,
                entity.ServiceCharges,
                entity.AdditionalCharges.Select(c => c.Amount));

        public static decimal ComputeAdditionalChargesTotal(QuotationUpsertDto dto) =>
            QuotationLineCalculator.SumAdditionalCharges(
                dto.TransportationCharges,
                dto.LoadingCharges,
                dto.ServiceCharges,
                dto.CustomCharges?.Select(c => c.Amount < 0 ? 0 : c.Amount));

        public static QuotationLineCalculator.QuotationTotals ComputeTotals(
            IEnumerable<QuotationLineItem> lines,
            decimal headerGstPercent,
            decimal additionalChargesTotal)
        {
            var rows = lines.Select(l =>
            {
                var calc = l.GstPercent > 0
                    ? QuotationLineCalculator.CalculateLineLegacy(
                        l.Quantity, l.Rate, l.DiscountPercent, l.GstPercent, l.Weight, l.UnitWeight)
                    : QuotationLineCalculator.CalculateLine(
                        l.Quantity, l.Rate, l.DiscountPercent, l.Weight, l.UnitWeight);
                return (l.Quantity, calc);
            });
            return QuotationLineCalculator.AggregateLines(rows, headerGstPercent, additionalChargesTotal);
        }

        public static void ApplyTotals(Quotation entity, IEnumerable<QuotationLineItem> lines)
        {
            var additionalTotal = ComputeAdditionalChargesTotal(entity);
            var totals = ComputeTotals(lines, entity.GstPercent, additionalTotal);
            entity.Subtotal = totals.Subtotal;
            entity.TaxTotal = totals.TaxTotal;
            entity.GrandTotal = totals.GrandTotal;
            entity.TotalQuantity = totals.TotalQuantity;
            entity.TotalWeight = totals.TotalWeight;
        }

        public static QuotationUpsertDto ToUpsertDto(Quotation q)
        {
            var additionalTotal = ComputeAdditionalChargesTotal(q);
            var totals = ComputeTotals(q.LineItems, q.GstPercent, additionalTotal);
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
                Subtotal = totals.Subtotal,
                TaxTotal = totals.TaxTotal,
                GstPercent = q.GstPercent,
                GrandTotal = totals.GrandTotal,
                TotalQuantity = totals.TotalQuantity,
                TotalWeight = totals.TotalWeight,
                TransportationCharges = q.TransportationCharges,
                LoadingCharges = q.LoadingCharges,
                ServiceCharges = q.ServiceCharges,
                CustomCharges = q.AdditionalCharges
                    .OrderBy(c => c.SortIndex)
                    .Select(c => new QuotationAdditionalChargeDto
                    {
                        Id = c.Id,
                        SortIndex = c.SortIndex,
                        ChargeName = c.ChargeName,
                        Amount = c.Amount,
                    })
                    .ToList(),
                LineItems = q.LineItems
                    .OrderBy(l => l.LineIndex)
                    .Select(l =>
                    {
                        var calc = l.GstPercent > 0
                            ? QuotationLineCalculator.CalculateLineLegacy(
                                l.Quantity, l.Rate, l.DiscountPercent, l.GstPercent, l.Weight, l.UnitWeight)
                            : QuotationLineCalculator.CalculateLine(
                                l.Quantity, l.Rate, l.DiscountPercent, l.Weight, l.UnitWeight);
                        return new QuotationLineItemDto
                        {
                            Id = l.Id,
                            LineIndex = l.LineIndex,
                            ItemId = l.ItemId,
                            ItemCode = l.ItemCode,
                            ItemName = l.ItemName,
                            Description = l.Description,
                            Quantity = l.Quantity,
                            Uom = l.Uom,
                            Weight = l.Weight,
                            UnitWeight = l.UnitWeight,
                            Rate = l.Rate,
                            SteelRate = l.SteelRate,
                            ItemSnapshotJson = l.ItemSnapshotJson,
                            DiscountPercent = l.DiscountPercent,
                            GstPercent = l.GstPercent,
                            Amount = calc.Amount,
                            TaxAmount = calc.TaxAmount,
                            LineTotal = calc.LineTotal,
                        };
                    })
                    .ToList(),
            };
        }
    }
}
