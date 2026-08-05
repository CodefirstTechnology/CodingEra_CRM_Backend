using ERP.Application.Sales.Dtos;
using ERP.Domain.Sales;

namespace ERP.Infrastructure.Sales
{
    internal static class AdvancePaymentMapper
    {
        public static string FormatDate(DateOnly date) => date.ToString("yyyy-MM-dd");

        public static string FormatDateTime(DateTimeOffset value) =>
            value.UtcDateTime.ToString("O");

        public static string? FormatOptionalDateTime(DateTimeOffset? value) =>
            value is null ? null : FormatDateTime(value.Value);

        public static DateOnly ParseDate(string value, DateOnly fallback)
        {
            if (DateOnly.TryParse(value, out var date))
            {
                return date;
            }

            if (DateTimeOffset.TryParse(value, out var dto))
            {
                return DateOnly.FromDateTime(dto.UtcDateTime);
            }

            return fallback;
        }

        public static DateOnly? ParseOptionalDate(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return null;
            }

            if (DateOnly.TryParse(value, out var date))
            {
                return date;
            }

            if (DateTimeOffset.TryParse(value, out var dto))
            {
                return DateOnly.FromDateTime(dto.UtcDateTime);
            }

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
