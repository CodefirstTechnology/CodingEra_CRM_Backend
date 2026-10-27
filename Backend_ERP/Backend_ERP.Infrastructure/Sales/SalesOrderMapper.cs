using System;
using System.Linq;
using ERP.Application.Sales.Dtos;
using ERP.Domain.Sales;
using ERP.Shared.Helpers;

namespace ERP.Infrastructure.Sales
{
    internal static class SalesOrderMapper
    {
        // Delegated to shared DateHelper
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

        public static SalesOrderListItemDto ToListItem(SalesOrder entity)
        {
            var totalOrdered = entity.Items.Sum(i => i.Quantity);
            var totalAllocated = entity.Items.Sum(i => i.AllocatedQuantity);
            var totalDispatched = entity.Items.Sum(i => i.DispatchedQuantity);

            return new SalesOrderListItemDto
            {
                Id = entity.Id,
                SalesOrderNumber = entity.SalesOrderNumber,
                QuotationNumber = entity.QuotationNumber,
                CustomerId = entity.CustomerId,
                CustomerName = entity.CustomerName,
                OrderDate = FormatDate(entity.OrderDate),
                Amount = entity.GrandTotal,
                GrandTotal = entity.GrandTotal,
                AllocatedPercentage = totalOrdered > 0 ? Math.Round((totalAllocated / totalOrdered) * 100m, 1) : 0m,
                DispatchedPercentage = totalOrdered > 0 ? Math.Round((totalDispatched / totalOrdered) * 100m, 1) : 0m,
                IsProductionReleased = entity.IsProductionReleased,
                IsDispatchReleased = entity.IsDispatchReleased,
                CreditHoldReason = entity.CreditHoldReason,
                Status = entity.Status,
                CreatedBy = entity.CreatedBy
            };
        }

        public static SalesOrderDto ToDto(SalesOrder entity) => new()
        {
            Id = entity.Id,
            SalesOrderNumber = entity.SalesOrderNumber,
            QuotationId = entity.QuotationId,
            QuotationNumber = entity.QuotationNumber,
            SourceType = entity.SourceType,
            CustomerId = entity.CustomerId,
            CustomerCategory = entity.CustomerCategory,
            PlaceOfSupplyStateCode = entity.PlaceOfSupplyStateCode,
            Customer = new SalesOrderCustomerDto
            {
                CustomerId = entity.CustomerId,
                CustomerName = entity.CustomerName,
                CustomerCategory = entity.CustomerCategory,
                ContactPerson = entity.ContactPerson,
                BillingAddress = entity.BillingAddress,
                ShippingAddress = entity.ShippingAddress,
                PlaceOfSupplyStateCode = entity.PlaceOfSupplyStateCode,
                CustomerEmail = entity.CustomerEmail,
                CustomerPhone = entity.CustomerPhone
            },
            SalesPerson = entity.SalesPerson,
            SalesPersonUserId = entity.SalesPersonUserId,
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
                    ItemId = i.ItemId,
                    WarehouseId = i.WarehouseId,
                    ItemCode = i.ItemCode,
                    ItemName = i.ItemName,
                    Description = i.Description,
                    Quantity = i.Quantity,
                    AllocatedQuantity = i.AllocatedQuantity,
                    DispatchedQuantity = i.DispatchedQuantity,
                    InvoicedQuantity = i.InvoicedQuantity,
                    ProformaInvoicedQuantity = i.ProformaInvoicedQuantity,
                    Unit = i.Unit,
                    BaseStockUom = i.BaseStockUom,
                    Rate = i.Rate,
                    Discount = i.Discount,
                    Gst = i.Gst,
                    Amount = i.Amount,
                    ResolvedPriceListId = i.ResolvedPriceListId,
                    PriceListNumberSnapshot = i.PriceListNumberSnapshot,
                    OriginalSellingRate = i.OriginalSellingRate,
                    FloorPriceSnapshot = i.FloorPriceSnapshot,
                    MaxDiscountPctSnapshot = i.MaxDiscountPctSnapshot,
                    TaxSlabPercentSnapshot = i.TaxSlabPercentSnapshot,
                    PlaceOfSupplyStateCode = i.PlaceOfSupplyStateCode
                })
                .ToList(),
            Amount = entity.GrandTotal,
            Subtotal = entity.Subtotal,
            DiscountTotal = entity.DiscountTotal,
            GstTotal = entity.GstTotal,
            GrandTotal = entity.GrandTotal,
            AdvanceAllocatedAmount = entity.AdvanceAllocatedAmount,
            ProformaInvoicedAmount = entity.ProformaInvoicedAmount,
            FinalInvoicedAmount = entity.FinalInvoicedAmount,

            // Release Gates
            IsProductionReleased = entity.IsProductionReleased,
            ProductionReleasedDate = entity.ProductionReleasedDate.HasValue ? FormatDateTime(entity.ProductionReleasedDate.Value) : null,
            ProductionReleasedBy = entity.ProductionReleasedBy,
            IsDispatchReleased = entity.IsDispatchReleased,
            DispatchReleasedDate = entity.DispatchReleasedDate.HasValue ? FormatDateTime(entity.DispatchReleasedDate.Value) : null,
            DispatchReleasedBy = entity.DispatchReleasedBy,

            // Credit Management
            Status = entity.Status,
            CreditHoldReason = entity.CreditHoldReason,
            CreditApprovedDate = entity.CreditApprovedDate.HasValue ? FormatDateTime(entity.CreditApprovedDate.Value) : null,
            CreditApprovedBy = entity.CreditApprovedBy,
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

        public static SalesOrderAllocationDto ToAllocationDto(SalesOrderAllocation a) => new()
        {
            Id = a.Id,
            SalesOrderId = a.SalesOrderId,
            SalesOrderItemId = a.SalesOrderItemId,
            ItemCode = a.SalesOrderItem?.ItemCode ?? string.Empty,
            ItemName = a.SalesOrderItem?.ItemName ?? string.Empty,
            FinishedGoodId = a.FinishedGoodId,
            WarehouseId = a.WarehouseId,
            WarehouseName = a.Warehouse?.Name ?? string.Empty,
            BatchNumber = a.BatchNumber,
            AllocatedQuantity = a.AllocatedQuantity,
            AllocationStatus = a.AllocationStatus.ToString(),
            AllocatedAt = FormatDateTime(a.AllocatedAt),
            AllocatedBy = a.AllocatedBy,
            ReleasedDate = a.ReleasedDate.HasValue ? FormatDateTime(a.ReleasedDate.Value) : null,
            ReleaseRemarks = a.ReleaseRemarks
        };
    }
}
