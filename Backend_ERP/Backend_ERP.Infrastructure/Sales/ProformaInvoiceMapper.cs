using ERP.Application.Sales.Dtos;
using ERP.Domain.Sales;
using ERP.Shared.Helpers;

namespace ERP.Infrastructure.Sales
{
    internal static class ProformaInvoiceMapper
    {
        private static string FormatDate(DateOnly date) => DateHelper.FormatDate(date);
        private static string FormatDateTime(DateTimeOffset value) => DateHelper.FormatDateTime(value);
        public static DateOnly ParseDate(string value, DateOnly fallback) => DateHelper.ParseDate(value, fallback);

        public static DateOnly? ParseOptionalDate(string? value)
        {
            if (string.IsNullOrWhiteSpace(value)) return null;
            if (DateOnly.TryParse(value, out var date)) return date;
            if (DateTimeOffset.TryParse(value, out var dto)) return DateOnly.FromDateTime(dto.UtcDateTime);
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
            AdvanceReceivedAmount = e.AdvanceReceivedAmount,
            PaymentStatus = e.PaymentStatus,
            BillingType = e.BillingType,
            MilestonePercentage = e.MilestonePercentage,
            RequiresFinanceCreditReview = e.RequiresFinanceCreditReview,
            IsProductionReleased = e.IsProductionReleased,
            IsDispatchReleased = e.IsDispatchReleased,
            Status = e.Status,
            PlaceOfSupplyStateCode = e.PlaceOfSupplyStateCode
        };

        public static ProformaInvoiceDto ToDto(ProformaInvoice e) => new()
        {
            Id = e.Id,
            PiNumber = e.PiNumber,
            InvoiceDate = FormatDate(e.InvoiceDate),
            ValidUntil = FormatDate(e.ValidUntil),
            BillingType = e.BillingType,
            MilestonePercentage = e.MilestonePercentage,
            RequiresFinanceCreditReview = e.RequiresFinanceCreditReview,
            CreditReviewReason = e.CreditReviewReason,
            AdvanceReceivedAmount = e.AdvanceReceivedAmount,
            PaymentStatus = e.PaymentStatus,
            Incoterm = e.Incoterm,
            Customer = new ProformaInvoiceCustomerDto
            {
                CustomerId = e.CustomerId,
                CustomerName = e.CustomerName,
                CustomerGstin = e.CustomerGstin,
                CustomerCategory = e.CustomerCategory,
                PlaceOfSupplyStateCode = e.PlaceOfSupplyStateCode,
                ContactPerson = e.ContactPerson,
                BillingAddress = e.BillingAddress,
                ShippingAddress = e.ShippingAddress
            },
            SalesPersonId = e.SalesPersonUserId.ToString(),
            SalesPerson = e.SalesPerson,
            Currency = e.Currency,
            ExchangeRate = e.ExchangeRate,
            ExchangeRateDate = e.ExchangeRateDate.HasValue ? FormatDate(e.ExchangeRateDate.Value) : null,
            BaseCurrencyEquivalentAmount = e.BaseCurrencyEquivalentAmount,
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
                SalesOrderItemId = i.SalesOrderItemId,
                QuotationItemId = i.QuotationItemId,
                LineKey = i.LineKey,
                ItemCode = i.ItemCode,
                ItemName = i.ItemName,
                Description = i.Description,
                HsnCode = i.HsnCode,
                Quantity = i.Quantity,
                Unit = i.Unit,
                Rate = i.Rate,
                Discount = i.Discount,
                DiscountAmount = i.DiscountAmount,
                Gst = i.Gst,
                TaxAmount = i.TaxAmount,
                Amount = i.Amount,
                OriginalSellingRate = i.OriginalSellingRate,
                FloorPriceSnapshot = i.FloorPriceSnapshot,
                MaxDiscountPctSnapshot = i.MaxDiscountPctSnapshot,
                TaxSlabPercentSnapshot = i.TaxSlabPercentSnapshot,
                PlaceOfSupplyStateCode = i.PlaceOfSupplyStateCode,
                ConvertedQuantity = i.ConvertedQuantity,
                RemainingQuantity = i.RemainingQuantity
            }).ToList(),
            Subtotal = e.Subtotal,
            DiscountTotal = e.DiscountTotal,
            TaxTotal = e.TaxTotal,
            GrandTotal = e.GrandTotal,
            Status = e.Status,
            Remarks = e.Remarks,
            ApprovalToken = e.ApprovalToken,
            ApprovedBy = e.ApprovedBy,
            ApprovedDate = e.ApprovedDate.HasValue ? FormatDateTime(e.ApprovedDate.Value) : null,
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
                ApproverUserId = h.ApproverUserId,
                ApprovalToken = h.ApprovalToken,
                IpAddress = h.IpAddress,
                PerformedBy = h.ApprovedBy,
                Date = FormatDateTime(h.ApprovedOn)
            }).ToList(),
            SettlementAllocations = e.SettlementAllocations.OrderByDescending(s => s.AllocatedOn).Select(s => new ProformaInvoiceSettlementAllocationDto
            {
                Id = s.Id,
                ProformaInvoiceId = s.ProformaInvoiceId,
                AdvancePaymentId = s.AdvancePaymentId,
                AllocatedAmount = s.AllocatedAmount,
                ExchangeRateAtSettlement = s.ExchangeRateAtSettlement,
                BaseCurrencyAllocatedAmount = s.BaseCurrencyAllocatedAmount,
                ReceiptVoucherNumber = s.ReceiptVoucherNumber,
                AllocatedOn = FormatDateTime(s.AllocatedOn),
                AllocatedBy = s.AllocatedBy,
                Remarks = s.Remarks,
                IsReversed = s.IsReversed
            }).ToList(),
            Conversion = string.IsNullOrWhiteSpace(e.ConvertedInvoiceNumber)
                ? null
                : new ProformaConversionDto
                {
                    SalesInvoiceNumber = e.ConvertedInvoiceNumber,
                    ConvertedOn = e.ConvertedOn.HasValue ? FormatDateTime(e.ConvertedOn.Value) : null,
                    Remarks = "Converted to Sales Invoice"
                },
            IsProductionReleased = e.IsProductionReleased,
            ProductionReleasedOn = e.ProductionReleasedOn.HasValue ? FormatDateTime(e.ProductionReleasedOn.Value) : null,
            ProductionReleasedBy = e.ProductionReleasedBy,
            IsDispatchReleased = e.IsDispatchReleased,
            DispatchReleasedOn = e.DispatchReleasedOn.HasValue ? FormatDateTime(e.DispatchReleasedOn.Value) : null,
            DispatchReleasedBy = e.DispatchReleasedBy,
            CreatedBy = e.CreatedBy,
            CreatedDate = FormatDateTime(e.CreatedDate),
            UpdatedBy = e.UpdatedBy,
            UpdatedDate = FormatDateTime(e.UpdatedDate)
        };
    }
}
