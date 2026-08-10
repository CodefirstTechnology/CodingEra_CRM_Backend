using ERP.Application.Sales.Dtos;
using ERP.Domain.Sales;
using ERP.Shared.Helpers;

namespace ERP.Infrastructure.Sales
{
    internal static class AdvancePaymentMapper
    {
        // Delegated to shared DateHelper — kept for backward-compat callsites in this class
        private static string FormatDate(DateOnly date) => DateHelper.FormatDate(date);
        private static string FormatDateTime(DateTimeOffset value) => DateHelper.FormatDateTime(value);
        public static string? FormatOptionalDateTime(DateTimeOffset? value) =>
            value is null ? null : DateHelper.FormatDateTime(value.Value);
        public static DateOnly ParseDate(string value, DateOnly fallback) => DateHelper.ParseDate(value, fallback);

        public static DateOnly? ParseOptionalDate(string? value)
        {
            if (string.IsNullOrWhiteSpace(value)) return null;
            if (DateOnly.TryParse(value, out var date)) return date;
            if (DateTimeOffset.TryParse(value, out var dto)) return DateOnly.FromDateTime(dto.UtcDateTime);
            return null;
        }

        public static AdvancePaymentListItemDto ToListItem(AdvancePayment e) => new()
        {
            Id = e.Id,
            PaymentNumber = e.PaymentNumber,
            CustomerName = e.CustomerName,
            SalesOrderNumber = e.SalesOrderNumber,
            QuotationNumber = e.QuotationNumber,
            PaymentDate = FormatDate(e.PaymentDate),
            PaymentMode = e.PaymentMode,
            Currency = e.Currency,
            AdvanceAmount = e.AdvanceAmount,
            AppliedAmount = e.AppliedAmount,
            RemainingAmount = e.RemainingAmount,
            Status = e.Status,
            ReferenceNumber = e.ReferenceNumber
        };

        public static AdvancePaymentLedgerDto ToLedgerItem(AdvancePayment e) => new()
        {
            Id = e.Id,
            PaymentNumber = e.PaymentNumber,
            CustomerName = e.CustomerName,
            PaymentDate = FormatDate(e.PaymentDate),
            PaymentMode = e.PaymentMode,
            Currency = e.Currency,
            AdvanceAmount = e.AdvanceAmount,
            AppliedAmount = e.AppliedAmount,
            RemainingAmount = e.RemainingAmount,
            Status = e.Status,
            ReferenceNumber = e.ReferenceNumber,
            SalesOrderNumber = e.SalesOrderNumber,
            QuotationNumber = e.QuotationNumber
        };

        public static AdvancePaymentTimelineDto ToTimelineDto(AdvancePaymentTimeline t) => new()
        {
            Id = t.Id,
            Action = t.Action,
            Remarks = t.Remarks,
            PerformedBy = t.PerformedBy,
            PerformedOn = FormatDateTime(t.PerformedOn)
        };

        public static AdvancePaymentDto ToDto(AdvancePayment e) => new()
        {
            Id = e.Id,
            PaymentNumber = e.PaymentNumber,
            CustomerId = e.CustomerId,
            CustomerName = e.CustomerName,
            SalesOrderId = e.SalesOrderId,
            SalesOrderNumber = e.SalesOrderNumber,
            QuotationId = e.QuotationId,
            QuotationNumber = e.QuotationNumber,
            PaymentDate = FormatDate(e.PaymentDate),
            PaymentMode = e.PaymentMode,
            ReferenceNumber = e.ReferenceNumber,
            Currency = e.Currency,
            ExchangeRate = e.ExchangeRate,
            AdvanceAmount = e.AdvanceAmount,
            AppliedAmount = e.AppliedAmount,
            RemainingAmount = e.RemainingAmount,
            Status = e.Status,
            Remarks = e.Remarks,
            AttachmentName = e.AttachmentName,
            VerifiedBy = e.VerifiedBy,
            VerifiedOn = FormatOptionalDateTime(e.VerifiedOn),
            ReceivedBy = e.ReceivedBy,
            ReceivedOn = FormatOptionalDateTime(e.ReceivedOn),
            CreatedBy = e.CreatedBy,
            CreatedDate = FormatDateTime(e.CreatedDate),
            UpdatedBy = e.UpdatedBy,
            UpdatedDate = FormatDateTime(e.UpdatedDate),
            Applications = e.Applications
                .OrderBy(a => a.AppliedOn)
                .Select(a => new AdvancePaymentApplicationDto
                {
                    Id = a.Id,
                    SalesOrderId = a.SalesOrderId,
                    SalesOrderNumber = a.SalesOrderNumber,
                    ApplyAmount = a.ApplyAmount,
                    Remarks = a.Remarks,
                    AppliedBy = a.AppliedBy,
                    AppliedOn = FormatDateTime(a.AppliedOn)
                }).ToList(),
            Timeline = e.Timeline
                .OrderBy(t => t.PerformedOn)
                .Select(ToTimelineDto)
                .ToList()
        };
    }
}
