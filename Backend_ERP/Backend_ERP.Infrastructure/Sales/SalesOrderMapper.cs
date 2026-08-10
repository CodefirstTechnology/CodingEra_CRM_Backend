using ERP.Application.Sales.Dtos;
using ERP.Domain.Sales;
using ERP.Shared.Helpers;

namespace ERP.Infrastructure.Sales
{
    internal static class SalesOrderMapper
    {
        // Delegated to shared DateHelper — kept for backward-compat callsite in this class
        private static string FormatDate(DateOnly date) => DateHelper.FormatDate(date);
        private static string FormatDateTime(DateTimeOffset value) => DateHelper.FormatDateTime(value);
        public static DateOnly ParseDate(string value, DateOnly fallback) => DateHelper.ParseDate(value, fallback);

        public static DateOnly? ParseOptionalDate(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return null;
            if (DateOnly.TryParse(value, out var date))
                return date;
            if (DateTimeOffset.TryParse(value, out var dto))
                return DateOnly.FromDateTime(dto.UtcDateTime);
            return null;
        }

        public static SalesOrderListItemDto ToListItem(SalesOrder entity) => new()
        {
            Id = entity.Id,
            SalesOrderNumber = entity.SalesOrderNumber,
            QuotationNumber = entity.QuotationNumber,
            CustomerName = entity.CustomerName,
            OrderDate = FormatDate(entity.OrderDate),
            Amount = entity.GrandTotal,
            Status = entity.Status,
            CreatedBy = entity.CreatedBy
        };

        public static SalesOrderDto ToDto(SalesOrder entity) => new()
        {
            Id = entity.Id,
            SalesOrderNumber = entity.SalesOrderNumber,
            QuotationId = entity.QuotationId,
            QuotationNumber = entity.QuotationNumber,
            SourceType = entity.SourceType,
            Customer = new SalesOrderCustomerDto
            {
                CustomerName = entity.CustomerName,
                ContactPerson = entity.ContactPerson,
                BillingAddress = entity.BillingAddress,
                ShippingAddress = entity.ShippingAddress,
                CustomerEmail = entity.CustomerEmail,
                CustomerPhone = entity.CustomerPhone
            },
            SalesPerson = entity.SalesPerson,
            Notes = entity.Notes,
            OrderDate = FormatDate(entity.OrderDate),
            ExpectedDeliveryDate = entity.ExpectedDeliveryDate is null
                ? null
                : FormatDate(entity.ExpectedDeliveryDate.Value),
            PaymentTerms = entity.PaymentTerms,
            DeliveryTerms = entity.DeliveryTerms,
            Items = entity.Items
                .OrderBy(i => i.SortIndex)
                .Select(i => new SalesOrderItemDto
                {
                    Id = i.LineKey,
                    ItemName = i.ItemName,
                    Description = i.Description,
                    Quantity = i.Quantity,
                    Unit = i.Unit,
                    Rate = i.Rate,
                    Discount = i.Discount,
                    Gst = i.Gst,
                    Amount = i.Amount
                })
                .ToList(),
            Amount = entity.GrandTotal,
            Subtotal = entity.Subtotal,
            DiscountTotal = entity.DiscountTotal,
            GstTotal = entity.GstTotal,
            GrandTotal = entity.GrandTotal,
            Status = entity.Status,
            Remarks = entity.Remarks,
            StatusHistory = entity.StatusHistory
                .OrderBy(h => h.Date)
                .Select(h => new SalesOrderStatusHistoryDto
                {
                    Id = h.EntryKey,
                    Status = h.Status,
                    Date = FormatDateTime(h.Date),
                    User = h.User,
                    Remarks = h.Remarks,
                    Label = h.Label
                })
                .ToList(),
            CreatedBy = entity.CreatedBy,
            CreatedDate = FormatDateTime(entity.CreatedDate),
            UpdatedBy = entity.UpdatedBy,
            UpdatedDate = FormatDateTime(entity.UpdatedDate),
            PdfGeneratedDate = entity.PdfGeneratedDate is null
                ? null
                : FormatDateTime(entity.PdfGeneratedDate.Value),
            LastCommunicationDate = entity.LastCommunicationDate is null
                ? null
                : FormatDateTime(entity.LastCommunicationDate.Value),
            EmailHistory = entity.EmailHistory
                .OrderBy(e => e.SentDate)
                .Select(e => new SalesOrderEmailHistoryDto
                {
                    Id = e.EntryKey,
                    SalesOrderId = entity.Id,
                    Recipient = e.Recipient,
                    SentDate = FormatDateTime(e.SentDate),
                    Action = e.Action,
                    Status = e.Status
                })
                .ToList(),
            AuditCount = entity.StatusHistory.Count,
            LastModified = FormatDateTime(entity.UpdatedDate)
        };
    }
}
