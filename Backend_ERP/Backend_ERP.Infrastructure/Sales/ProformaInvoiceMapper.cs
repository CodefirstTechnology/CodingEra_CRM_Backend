using ERP.Application.Sales.Dtos;
using ERP.Domain.Sales;

namespace ERP.Infrastructure.Sales
{
    internal static class ProformaInvoiceMapper
    {
        public static string FormatDate(DateOnly date) => date.ToString("yyyy-MM-dd");

        public static string FormatDateTime(DateTimeOffset value) =>
            value.UtcDateTime.ToString("O");

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

        public static ProformaInvoiceListItemDto ToListItem(ProformaInvoice e) => new()
        {
            Id = e.Id,
            PiNumber = e.PiNumber,
            CustomerName = e.CustomerName,
            QuotationNumber = e.QuotationNumber,
            SalesOrderNumber = e.SalesOrderNumber,
            InvoiceDate = FormatDate(e.InvoiceDate),
            ValidUntil = FormatDate(e.ValidUntil),
            SalesPerson = e.SalesPerson,
            Currency = e.Currency,
            GrandTotal = e.GrandTotal,
            Status = e.Status
        };

        public static ProformaInvoiceDto ToDto(ProformaInvoice e) => new()
        {
            Id = e.Id,
            PiNumber = e.PiNumber,
            InvoiceDate = FormatDate(e.InvoiceDate),
            ValidUntil = FormatDate(e.ValidUntil),
            Customer = new ProformaInvoiceCustomerDto
            {
                CustomerId = e.CustomerId,
                CustomerName = e.CustomerName,
                ContactPerson = e.ContactPerson,
                BillingAddress = e.BillingAddress,
                ShippingAddress = e.ShippingAddress
            },
            SalesPersonId = e.SalesPersonUserId.ToString(),
            SalesPerson = e.SalesPerson,
            Currency = e.Currency,
            ExchangeRate = e.ExchangeRate,
            QuotationId = e.QuotationId,
            QuotationNumber = e.QuotationNumber,
            SalesOrderId = e.SalesOrderId,
            SalesOrderNumber = e.SalesOrderNumber,
            PaymentTerms = e.PaymentTerms,
            DeliveryTerms = e.DeliveryTerms,
            CustomerNotes = e.CustomerNotes,
            InternalNotes = e.InternalNotes,
            Items = e.Items.OrderBy(i => i.SortOrder).Select(i => new ProformaInvoiceItemDto
            {
                Id = i.LineKey,
                ItemName = i.ItemName,
                Description = i.Description,
                Quantity = i.Quantity,
                Unit = i.Unit,
                Rate = i.Rate,
                Discount = i.Discount,
                Gst = i.Gst,
                TaxAmount = i.TaxAmount,
                Amount = i.Amount
            }).ToList(),
            Subtotal = e.Subtotal,
            DiscountTotal = e.DiscountTotal,
            TaxTotal = e.TaxTotal,
            GrandTotal = e.GrandTotal,
            Status = e.Status,
            Remarks = e.Remarks,
            StatusHistory = e.StatusHistory.OrderBy(h => h.ChangedOn).Select(h => new ProformaInvoiceStatusHistoryDto
            {
                Id = h.EntryKey,
                OldStatus = h.OldStatus,
                NewStatus = h.NewStatus,
                Status = h.NewStatus,
                Date = FormatDateTime(h.ChangedOn),
                User = h.ChangedBy,
                Remarks = h.Remarks
            }).ToList(),
            ApprovalHistory = e.ApprovalHistory.OrderBy(h => h.ApprovedOn).Select(h => new ProformaInvoiceApprovalHistoryDto
            {
                Id = h.EntryKey,
                ApprovalLevel = h.ApprovalLevel,
                Decision = h.Decision,
                Action = h.Decision,
                Remarks = h.Remarks,
                ApprovedBy = h.ApprovedBy,
                PerformedBy = h.ApprovedBy,
                Date = FormatDateTime(h.ApprovedOn)
            }).ToList(),
            Conversion = string.IsNullOrWhiteSpace(e.ConvertedInvoiceNumber)
                ? null
                : new ProformaConversionDto
                {
                    SalesInvoiceNumber = e.ConvertedInvoiceNumber,
                    ConvertedOn = e.ConvertedOn is null ? null : FormatDateTime(e.ConvertedOn.Value),
                    Remarks = e.Remarks
                },
            CreatedBy = e.CreatedBy,
            CreatedDate = FormatDateTime(e.CreatedDate),
            UpdatedBy = e.UpdatedBy,
            UpdatedDate = FormatDateTime(e.UpdatedDate)
        };
    }
}
