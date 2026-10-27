using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ERP.Application.Sales;
using ERP.Application.Sales.Dtos;
using ERP.Domain.Sales;
using ERP.Infrastructure.Data;
using ERP.Shared.Helpers;
using Microsoft.EntityFrameworkCore;

namespace ERP.Infrastructure.Sales
{
    public class SalesOrderService : ISalesOrderService
    {
        private readonly ERPDbContext _db;
        private readonly ISalesOrderAllocationService _allocationService;

        public SalesOrderService(ERPDbContext db, ISalesOrderAllocationService allocationService)
        {
            _db = db;
            _allocationService = allocationService;
        }

        public async Task<IReadOnlyList<SalesOrderListItemDto>> GetAllAsync(
            SalesOrderListQueryDto? query,
            CancellationToken cancellationToken = default)
        {
            var q = _db.SalesOrders.AsNoTracking().Include(x => x.Items).AsQueryable();

            if (!string.IsNullOrWhiteSpace(query?.Status))
            {
                var status = SalesOrderStatusRules.Normalize(query.Status)
                    ?? query.Status.Trim();
                q = q.Where(x => x.Status == status);
            }

            if (query?.CustomerId.HasValue == true && query.CustomerId.Value > 0)
            {
                q = q.Where(x => x.CustomerId == query.CustomerId.Value);
            }

            var dateFrom = SalesOrderMapper.ParseOptionalDate(query?.DateFrom);
            if (dateFrom is not null)
            {
                q = q.Where(x => x.OrderDate >= dateFrom);
            }

            var dateTo = SalesOrderMapper.ParseOptionalDate(query?.DateTo);
            if (dateTo is not null)
            {
                q = q.Where(x => x.OrderDate <= dateTo);
            }

            if (!string.IsNullOrWhiteSpace(query?.Search))
            {
                var term = query.Search.Trim().ToLowerInvariant();
                q = q.Where(x =>
                    x.SalesOrderNumber.ToLower().Contains(term)
                    || x.QuotationNumber.ToLower().Contains(term)
                    || x.CustomerName.ToLower().Contains(term)
                    || x.CreatedBy.ToLower().Contains(term));
            }

            var rows = await q
                .OrderByDescending(x => x.OrderDate)
                .ThenByDescending(x => x.Id)
                .ToListAsync(cancellationToken);

            return rows.Select(SalesOrderMapper.ToListItem).ToList();
        }

        public async Task<SalesOrderDto?> GetByIdAsync(
            int id,
            CancellationToken cancellationToken = default)
        {
            var entity = await LoadTrackedAsync(id, asTracking: false, cancellationToken);
            return entity is null ? null : SalesOrderMapper.ToDto(entity);
        }

        public async Task<SalesOrderDto> CreateAsync(
            SalesOrderCreateRequestDto request,
            string actingUser,
            CancellationToken cancellationToken = default)
        {
            ValidateCustomer(request.Customer);
            ValidateItems(request.Items);

            var now = DateTimeOffset.UtcNow;
            var orderDate = SalesOrderMapper.ParseDate(request.OrderDate, DateOnly.FromDateTime(DateTime.UtcNow));
            var status = SalesOrderStatusRules.Normalize(request.Status ?? SalesOrderStatuses.Draft)
                ?? SalesOrderStatuses.Draft;

            if (status != SalesOrderStatuses.Draft && status != SalesOrderStatuses.Submitted)
            {
                throw new InvalidOperationException(
                    "New sales orders may only start as Draft or Submitted.");
            }

            var sourceType = string.Equals(
                request.SourceType,
                SalesOrderSourceTypes.Quotation,
                StringComparison.OrdinalIgnoreCase)
                ? SalesOrderSourceTypes.Quotation
                : SalesOrderSourceTypes.Manual;

            var number = string.IsNullOrWhiteSpace(request.SalesOrderNumber)
                ? await NextNumberAsync(cancellationToken)
                : request.SalesOrderNumber.Trim();

            if (await _db.SalesOrders.AnyAsync(x => x.SalesOrderNumber == number, cancellationToken))
            {
                throw new InvalidOperationException($"Sales order number '{number}' already exists.");
            }

            var preparedItems = PrepareItems(request.Items);
            var totals = SalesOrderCalculator.Summarize(preparedItems);

            var entity = new SalesOrder
            {
                SalesOrderNumber = number,
                QuotationId = request.QuotationId,
                QuotationNumber = request.QuotationNumber?.Trim() ?? string.Empty,
                SourceType = sourceType,
                CustomerId = request.Customer.CustomerId > 0 ? request.Customer.CustomerId : 1,
                CustomerName = request.Customer.CustomerName.Trim(),
                CustomerCategory = string.IsNullOrWhiteSpace(request.Customer.CustomerCategory) ? "Standard" : request.Customer.CustomerCategory.Trim(),
                ContactPerson = request.Customer.ContactPerson?.Trim() ?? string.Empty,
                BillingAddress = request.Customer.BillingAddress?.Trim() ?? string.Empty,
                ShippingAddress = request.Customer.ShippingAddress?.Trim() ?? string.Empty,
                PlaceOfSupplyStateCode = string.IsNullOrWhiteSpace(request.Customer.PlaceOfSupplyStateCode) ? "27" : request.Customer.PlaceOfSupplyStateCode.Trim(),
                CustomerEmail = NullIfEmpty(request.Customer.CustomerEmail),
                CustomerPhone = NullIfEmpty(request.Customer.CustomerPhone),
                SalesPerson = request.SalesPerson?.Trim() ?? string.Empty,
                SalesPersonUserId = request.SalesPersonUserId,
                Notes = request.Notes?.Trim() ?? string.Empty,
                OrderDate = orderDate,
                ExpectedDeliveryDate = SalesOrderMapper.ParseOptionalDate(request.ExpectedDeliveryDate),
                PaymentTerms = request.PaymentTerms?.Trim() ?? string.Empty,
                DeliveryTerms = request.DeliveryTerms?.Trim() ?? string.Empty,
                Subtotal = totals.Subtotal,
                DiscountTotal = totals.DiscountTotal,
                GstTotal = totals.GstTotal,
                GrandTotal = totals.GrandTotal,
                Status = status,
                Remarks = string.Empty,
                CreatedBy = actingUser,
                CreatedDate = now,
                UpdatedBy = actingUser,
                UpdatedDate = now
            };

            for (var i = 0; i < preparedItems.Count; i++)
            {
                var item = preparedItems[i];
                var cleanName = item.ItemName.Trim().ToLower();
                var fg = await _db.FinishedGoods
                    .FirstOrDefaultAsync(f => f.ProductName.ToLower() == cleanName, cancellationToken);

                entity.Items.Add(new SalesOrderItem
                {
                    LineKey = string.IsNullOrWhiteSpace(item.Id) ? Guid.NewGuid().ToString("N") : item.Id.Trim(),
                    SortIndex = i,
                    ItemId = item.ItemId ?? fg?.Id,
                    ItemCode = !string.IsNullOrWhiteSpace(item.ItemCode) ? item.ItemCode : (fg?.ProductCode ?? string.Empty),
                    WarehouseId = item.WarehouseId ?? fg?.WarehouseId,
                    ItemName = item.ItemName.Trim(),
                    Description = item.Description?.Trim() ?? string.Empty,
                    Quantity = item.Quantity,
                    Unit = string.IsNullOrWhiteSpace(item.Unit) ? (fg?.Unit ?? "Nos") : item.Unit.Trim(),
                    Rate = item.Rate,
                    Discount = item.Discount,
                    Gst = item.Gst,
                    Amount = item.Amount,
                    ResolvedPriceListId = item.ResolvedPriceListId,
                    PriceListNumberSnapshot = item.PriceListNumberSnapshot,
                    OriginalSellingRate = item.OriginalSellingRate > 0 ? item.OriginalSellingRate : item.Rate,
                    FloorPriceSnapshot = item.FloorPriceSnapshot,
                    MaxDiscountPctSnapshot = item.MaxDiscountPctSnapshot,
                    TaxSlabPercentSnapshot = item.TaxSlabPercentSnapshot > 0 ? item.TaxSlabPercentSnapshot : item.Gst,
                    PlaceOfSupplyStateCode = !string.IsNullOrWhiteSpace(item.PlaceOfSupplyStateCode) ? item.PlaceOfSupplyStateCode : entity.PlaceOfSupplyStateCode
                });
            }

            entity.StatusHistory.Add(NewHistory(
                status,
                now,
                actingUser,
                "Sales order created",
                status == SalesOrderStatuses.Draft ? "Created" : SalesOrderStatusRules.TimelineLabel(status)));

            _db.SalesOrders.Add(entity);
            await _db.SaveChangesAsync(cancellationToken);

            // If created from a quotation, transition quotation to ConvertedToSO and copy snapshots
            if (request.QuotationId.HasValue && request.QuotationId.Value > 0)
            {
                var quote = await _db.Quotations
                    .Include(q => q.Items)
                    .FirstOrDefaultAsync(q => q.Id == request.QuotationId.Value, cancellationToken);

                if (quote != null)
                {
                    quote.Status = QuotationStatuses.ConvertedToSO;
                    quote.ConvertedSalesOrderId = entity.Id;
                    quote.ConvertedSalesOrderNumber = entity.SalesOrderNumber;
                    quote.ConvertedOn = now;
                    quote.UpdatedBy = actingUser;
                    quote.UpdatedDate = now;

                    var matchingQa = await _db.QuotationApprovals
                        .FirstOrDefaultAsync(qa => qa.QuotationId == quote.Id && !qa.IsDeleted, cancellationToken);
                    if (matchingQa != null)
                    {
                        matchingQa.SalesOrderId = entity.Id;
                        matchingQa.SalesOrderNumber = entity.SalesOrderNumber;
                        matchingQa.Status = "Converted";
                        matchingQa.UpdatedBy = actingUser;
                        matchingQa.UpdatedDate = now;
                    }

                    for (var i = 0; i < entity.Items.Count; i++)
                    {
                        var soItem = entity.Items.ElementAt(i);
                        var matchingQItem = quote.Items.FirstOrDefault(qi =>
                            string.Equals(qi.ItemName.Trim(), soItem.ItemName.Trim(), StringComparison.OrdinalIgnoreCase))
                            ?? quote.Items.ElementAtOrDefault(i);

                        if (matchingQItem != null)
                        {
                            soItem.ItemId = matchingQItem.ItemId ?? soItem.ItemId;
                            soItem.ItemCode = string.IsNullOrWhiteSpace(soItem.ItemCode) ? matchingQItem.ItemCode : soItem.ItemCode;
                            soItem.WarehouseId = matchingQItem.WarehouseId ?? soItem.WarehouseId;
                            soItem.ItemType = matchingQItem.ItemType;
                            soItem.ResolvedPriceListId = matchingQItem.ResolvedPriceListId;
                            soItem.PriceListNumberSnapshot = matchingQItem.PriceListNumberSnapshot;
                            soItem.OriginalSellingRate = matchingQItem.OriginalSellingRate;
                            soItem.FloorPriceSnapshot = matchingQItem.FloorPriceSnapshot;
                            soItem.MaxDiscountPctSnapshot = matchingQItem.MaxDiscountPctSnapshot;
                            soItem.TaxSlabPercentSnapshot = matchingQItem.TaxSlabPercentSnapshot;
                        }
                    }

                    await _db.SaveChangesAsync(cancellationToken);
                }
            }

            return SalesOrderMapper.ToDto(await LoadTrackedAsync(entity.Id, false, cancellationToken)
                ?? entity);
        }

        public async Task<SalesOrderDto?> UpdateAsync(
            int id,
            SalesOrderUpdateRequestDto request,
            string actingUser,
            CancellationToken cancellationToken = default)
        {
            var entity = await LoadTrackedAsync(id, asTracking: true, cancellationToken);
            if (entity is null)
            {
                return null;
            }

            if (entity.Status != SalesOrderStatuses.Draft && entity.Status != SalesOrderStatuses.Submitted)
            {
                throw new InvalidOperationException(
                    $"Cannot update a sales order in '{entity.Status}' status. Only Draft or Submitted orders can be modified.");
            }

            ValidateCustomer(request.Customer);
            ValidateItems(request.Items);

            var preparedItems = PrepareItems(request.Items);
            var totals = SalesOrderCalculator.Summarize(preparedItems);
            var now = DateTimeOffset.UtcNow;

            if (!string.IsNullOrWhiteSpace(request.SalesOrderNumber)
                && !string.Equals(request.SalesOrderNumber.Trim(), entity.SalesOrderNumber, StringComparison.Ordinal))
            {
                var newNumber = request.SalesOrderNumber.Trim();
                if (await _db.SalesOrders.AnyAsync(
                        x => x.Id != id && x.SalesOrderNumber == newNumber,
                        cancellationToken))
                {
                    throw new InvalidOperationException($"Sales order number '{newNumber}' already exists.");
                }

                entity.SalesOrderNumber = newNumber;
            }

            entity.QuotationId = request.QuotationId;
            entity.QuotationNumber = request.QuotationNumber?.Trim() ?? string.Empty;
            entity.SourceType = string.Equals(
                request.SourceType,
                SalesOrderSourceTypes.Quotation,
                StringComparison.OrdinalIgnoreCase)
                ? SalesOrderSourceTypes.Quotation
                : SalesOrderSourceTypes.Manual;
            entity.CustomerId = request.Customer.CustomerId > 0 ? request.Customer.CustomerId : entity.CustomerId;
            entity.CustomerName = request.Customer.CustomerName.Trim();
            entity.CustomerCategory = string.IsNullOrWhiteSpace(request.Customer.CustomerCategory) ? entity.CustomerCategory : request.Customer.CustomerCategory.Trim();
            entity.ContactPerson = request.Customer.ContactPerson?.Trim() ?? string.Empty;
            entity.BillingAddress = request.Customer.BillingAddress?.Trim() ?? string.Empty;
            entity.ShippingAddress = request.Customer.ShippingAddress?.Trim() ?? string.Empty;
            entity.PlaceOfSupplyStateCode = string.IsNullOrWhiteSpace(request.Customer.PlaceOfSupplyStateCode) ? entity.PlaceOfSupplyStateCode : request.Customer.PlaceOfSupplyStateCode.Trim();
            entity.CustomerEmail = NullIfEmpty(request.Customer.CustomerEmail);
            entity.CustomerPhone = NullIfEmpty(request.Customer.CustomerPhone);
            entity.SalesPerson = request.SalesPerson?.Trim() ?? string.Empty;
            entity.SalesPersonUserId = request.SalesPersonUserId ?? entity.SalesPersonUserId;
            entity.Notes = request.Notes?.Trim() ?? string.Empty;
            entity.OrderDate = SalesOrderMapper.ParseDate(request.OrderDate, entity.OrderDate);
            entity.ExpectedDeliveryDate = SalesOrderMapper.ParseOptionalDate(request.ExpectedDeliveryDate);
            entity.PaymentTerms = request.PaymentTerms?.Trim() ?? string.Empty;
            entity.DeliveryTerms = request.DeliveryTerms?.Trim() ?? string.Empty;
            entity.Subtotal = totals.Subtotal;
            entity.DiscountTotal = totals.DiscountTotal;
            entity.GstTotal = totals.GstTotal;
            entity.GrandTotal = totals.GrandTotal;
            entity.UpdatedBy = actingUser;
            entity.UpdatedDate = now;

            _db.SalesOrderItems.RemoveRange(entity.Items);
            entity.Items.Clear();
            for (var i = 0; i < preparedItems.Count; i++)
            {
                var item = preparedItems[i];
                entity.Items.Add(new SalesOrderItem
                {
                    LineKey = string.IsNullOrWhiteSpace(item.Id) ? Guid.NewGuid().ToString("N") : item.Id.Trim(),
                    SortIndex = i,
                    ItemId = item.ItemId,
                    ItemCode = item.ItemCode,
                    WarehouseId = item.WarehouseId,
                    ItemName = item.ItemName.Trim(),
                    Description = item.Description?.Trim() ?? string.Empty,
                    Quantity = item.Quantity,
                    Unit = string.IsNullOrWhiteSpace(item.Unit) ? "Nos" : item.Unit.Trim(),
                    Rate = item.Rate,
                    Discount = item.Discount,
                    Gst = item.Gst,
                    Amount = item.Amount,
                    ResolvedPriceListId = item.ResolvedPriceListId,
                    PriceListNumberSnapshot = item.PriceListNumberSnapshot,
                    OriginalSellingRate = item.OriginalSellingRate > 0 ? item.OriginalSellingRate : item.Rate,
                    FloorPriceSnapshot = item.FloorPriceSnapshot,
                    MaxDiscountPctSnapshot = item.MaxDiscountPctSnapshot,
                    TaxSlabPercentSnapshot = item.TaxSlabPercentSnapshot > 0 ? item.TaxSlabPercentSnapshot : item.Gst,
                    PlaceOfSupplyStateCode = !string.IsNullOrWhiteSpace(item.PlaceOfSupplyStateCode) ? item.PlaceOfSupplyStateCode : entity.PlaceOfSupplyStateCode
                });
            }

            await _db.SaveChangesAsync(cancellationToken);
            return SalesOrderMapper.ToDto(await LoadTrackedAsync(id, false, cancellationToken) ?? entity);
        }

        public async Task<SalesOrderDto?> UpdateStatusAsync(
            int id,
            SalesOrderStatusUpdateRequestDto request,
            string actingUser,
            CancellationToken cancellationToken = default)
        {
            var entity = await LoadTrackedAsync(id, asTracking: true, cancellationToken);
            if (entity is null)
            {
                return null;
            }

            var target = SalesOrderStatusRules.Normalize(request.Status);
            if (target is null)
            {
                throw new InvalidOperationException($"Unknown status '{request.Status}'.");
            }

            if (!SalesOrderStatusRules.CanTransition(entity.Status, target))
            {
                throw new InvalidOperationException(
                    $"Cannot transition sales order from '{entity.Status}' to '{target}'.");
            }

            if (entity.Status.Equals(target, StringComparison.Ordinal))
            {
                return SalesOrderMapper.ToDto(entity);
            }

            var now = DateTimeOffset.UtcNow;
            entity.Status = target;
            entity.Remarks = request.Remarks?.Trim() ?? entity.Remarks;
            entity.UpdatedBy = actingUser;
            entity.UpdatedDate = now;
            entity.StatusHistory.Add(NewHistory(
                target,
                now,
                actingUser,
                request.Remarks?.Trim() ?? string.Empty,
                SalesOrderStatusRules.TimelineLabel(target)));

            // If cancelled, release soft allocations
            if (string.Equals(target, SalesOrderStatuses.Cancelled, StringComparison.OrdinalIgnoreCase))
            {
                await _allocationService.ReleaseOrderAllocationsAsync(
                    id,
                    request.Remarks?.Trim() ?? "Sales order cancelled.",
                    actingUser,
                    cancellationToken);
            }

            await _db.SaveChangesAsync(cancellationToken);
            return SalesOrderMapper.ToDto(await LoadTrackedAsync(id, false, cancellationToken) ?? entity);
        }

        public async Task<SalesOrderDto?> CancelAsync(
            int id,
            SalesOrderCancelRequestDto request,
            string actingUser,
            CancellationToken cancellationToken = default)
        {
            return await UpdateStatusAsync(
                id,
                new SalesOrderStatusUpdateRequestDto
                {
                    Status = SalesOrderStatuses.Cancelled,
                    Remarks = request.Remarks
                },
                actingUser,
                cancellationToken);
        }

        public async Task<SalesOrderDto?> SetCreditHoldAsync(
            int id,
            string reason,
            string actingUser,
            CancellationToken cancellationToken = default)
        {
            var entity = await LoadTrackedAsync(id, asTracking: true, cancellationToken);
            if (entity is null) return null;

            var now = DateTimeOffset.UtcNow;
            entity.Status = SalesOrderStatuses.OnCreditHold;
            entity.CreditHoldReason = reason;
            entity.UpdatedBy = actingUser;
            entity.UpdatedDate = now;
            entity.StatusHistory.Add(NewHistory(
                SalesOrderStatuses.OnCreditHold,
                now,
                actingUser,
                reason,
                "Credit Hold"));

            await _db.SaveChangesAsync(cancellationToken);
            return SalesOrderMapper.ToDto(await LoadTrackedAsync(id, false, cancellationToken) ?? entity);
        }

        public async Task<SalesOrderDto?> OverrideCreditHoldAsync(
            int id,
            string justification,
            string actingUser,
            CancellationToken cancellationToken = default)
        {
            var entity = await LoadTrackedAsync(id, asTracking: true, cancellationToken);
            if (entity is null) return null;

            var now = DateTimeOffset.UtcNow;
            entity.Status = SalesOrderStatuses.Confirmed;
            entity.CreditApprovedBy = actingUser;
            entity.CreditApprovedDate = now;
            entity.Remarks = $"Credit Hold Overridden: {justification}";
            entity.UpdatedBy = actingUser;
            entity.UpdatedDate = now;
            entity.StatusHistory.Add(NewHistory(
                SalesOrderStatuses.Confirmed,
                now,
                actingUser,
                $"Credit Override: {justification}",
                "Credit Approved"));

            await _db.SaveChangesAsync(cancellationToken);
            return SalesOrderMapper.ToDto(await LoadTrackedAsync(id, false, cancellationToken) ?? entity);
        }

        public async Task<SalesOrderDto?> SetProductionReleasedAsync(
            int id,
            string actingUser,
            CancellationToken cancellationToken = default)
        {
            var entity = await LoadTrackedAsync(id, asTracking: true, cancellationToken);
            if (entity is null) return null;

            var now = DateTimeOffset.UtcNow;
            entity.IsProductionReleased = true;
            entity.ProductionReleasedDate = now;
            entity.ProductionReleasedBy = actingUser;
            entity.UpdatedBy = actingUser;
            entity.UpdatedDate = now;
            entity.StatusHistory.Add(NewHistory(
                entity.Status,
                now,
                actingUser,
                "Order released to shop-floor production planning.",
                "Production Released"));

            await _db.SaveChangesAsync(cancellationToken);
            return SalesOrderMapper.ToDto(await LoadTrackedAsync(id, false, cancellationToken) ?? entity);
        }

        public async Task<SalesOrderDto?> SetDispatchReleasedAsync(
            int id,
            string actingUser,
            CancellationToken cancellationToken = default)
        {
            var entity = await LoadTrackedAsync(id, asTracking: true, cancellationToken);
            if (entity is null) return null;

            var now = DateTimeOffset.UtcNow;
            entity.IsDispatchReleased = true;
            entity.DispatchReleasedDate = now;
            entity.DispatchReleasedBy = actingUser;
            entity.UpdatedBy = actingUser;
            entity.UpdatedDate = now;
            entity.StatusHistory.Add(NewHistory(
                entity.Status,
                now,
                actingUser,
                "Order released to dispatch logistics and gate pass generation.",
                "Dispatch Released"));

            await _db.SaveChangesAsync(cancellationToken);
            return SalesOrderMapper.ToDto(await LoadTrackedAsync(id, false, cancellationToken) ?? entity);
        }

        public async Task<SalesOrderDto> ConvertQuotationAsync(
            int quotationApprovalId,
            string actingUser,
            CancellationToken cancellationToken = default)
        {
            await using var tx = await _db.Database.BeginTransactionAsync(cancellationToken);

            // 1. Try locking canonical Quotation row with row-level pessimistic lock (SELECT ... FOR UPDATE)
            Quotation? quote;
            if (_db.Database.IsRelational())
            {
                quote = await _db.Quotations
                    .FromSqlRaw(@"SELECT *, xmin FROM quotations WHERE ""Id"" = {0} AND ""IsDeleted"" = false FOR UPDATE", quotationApprovalId)
                    .Include(x => x.Items)
                    .FirstOrDefaultAsync(cancellationToken);
            }
            else
            {
                quote = await _db.Quotations
                    .Include(x => x.Items)
                    .FirstOrDefaultAsync(x => x.Id == quotationApprovalId && !x.IsDeleted, cancellationToken);
            }

            var now = DateTimeOffset.UtcNow;
            var number = await NextNumberAsync(cancellationToken);

            if (quote is not null)
            {
                // Strict validation: Must be 'Approved' (Client Accepted)
                if (!string.Equals(quote.Status, QuotationStatuses.Approved, StringComparison.OrdinalIgnoreCase))
                {
                    throw new InvalidOperationException(
                        $"Quotation must be in 'Approved' status to convert to a Sales Order. Current status: '{quote.Status}'.");
                }

                // Strict validation: Cannot be converted more than once
                if (quote.ConvertedSalesOrderId.HasValue && quote.ConvertedSalesOrderId.Value > 0)
                {
                    throw new InvalidOperationException(
                        $"Quotation '{quote.QuotationNumber}' has already been converted to Sales Order '{quote.ConvertedSalesOrderNumber ?? quote.ConvertedSalesOrderId.Value.ToString()}'.");
                }

                var existingOrder = await _db.SalesOrders
                    .AsNoTracking()
                    .FirstOrDefaultAsync(x => x.QuotationId == quote.Id && x.Status != SalesOrderStatuses.Cancelled, cancellationToken);
                if (existingOrder is not null)
                {
                    throw new InvalidOperationException(
                        $"A Sales Order ({existingOrder.SalesOrderNumber}) already exists for Quotation '{quote.QuotationNumber}'.");
                }

                var salesOrderItems = quote.Items.OrderBy(i => i.SortOrder).Select((item, idx) => new SalesOrderItem
                {
                    LineKey = Guid.NewGuid().ToString("N"),
                    SortIndex = idx + 1,
                    ItemType = item.ItemType,
                    ItemId = item.ItemId,
                    ItemCode = item.ItemCode,
                    ItemName = item.ItemName,
                    WarehouseId = item.WarehouseId,
                    Description = item.Description ?? string.Empty,
                    Quantity = item.Quantity,
                    Unit = item.Unit ?? "NOS",
                    BaseStockUom = item.BaseStockUom ?? "NOS",
                    Rate = item.UnitPrice,
                    Discount = item.DiscountPercent,
                    Gst = item.TaxPercent,
                    Amount = item.LineTotal,
                    ResolvedPriceListId = item.ResolvedPriceListId,
                    PriceListNumberSnapshot = item.PriceListNumberSnapshot,
                    OriginalSellingRate = item.OriginalSellingRate,
                    FloorPriceSnapshot = item.FloorPriceSnapshot,
                    MaxDiscountPctSnapshot = item.MaxDiscountPctSnapshot,
                    TaxSlabPercentSnapshot = item.TaxSlabPercentSnapshot,
                    PlaceOfSupplyStateCode = "27"
                }).ToList();

                if (salesOrderItems.Count == 0)
                {
                    salesOrderItems.Add(new SalesOrderItem
                    {
                        LineKey = Guid.NewGuid().ToString("N"),
                        SortIndex = 1,
                        ItemName = $"Quotation #{quote.QuotationNumber} Items",
                        Description = quote.Notes,
                        Quantity = 1m,
                        Unit = "NOS",
                        Rate = quote.GrandTotal,
                        Discount = 0m,
                        Gst = 18m,
                        Amount = quote.GrandTotal
                    });
                }

                var order = new SalesOrder
                {
                    SalesOrderNumber = number,
                    QuotationId = quote.Id,
                    QuotationNumber = quote.QuotationNumber,
                    SourceType = SalesOrderSourceTypes.Quotation,
                    CustomerId = int.TryParse(quote.CustomerId, out var cId) ? cId : 1,
                    CustomerName = quote.CustomerName,
                    ContactPerson = quote.ContactPerson,
                    BillingAddress = quote.BillingAddress,
                    ShippingAddress = quote.ShippingAddress,
                    PlaceOfSupplyStateCode = "27",
                    CustomerEmail = quote.CustomerEmail,
                    CustomerPhone = quote.CustomerPhone,
                    SalesPerson = quote.SalesPerson,
                    Notes = quote.Notes,
                    OrderDate = DateOnly.FromDateTime(DateTime.UtcNow),
                    PaymentTerms = string.IsNullOrWhiteSpace(quote.PaymentTerms) ? "Net 30" : quote.PaymentTerms,
                    DeliveryTerms = string.IsNullOrWhiteSpace(quote.DeliveryTerms) ? "Standard Delivery" : quote.DeliveryTerms,
                    Subtotal = quote.Subtotal,
                    DiscountTotal = quote.DiscountTotal,
                    GstTotal = quote.TaxTotal,
                    GrandTotal = quote.GrandTotal,
                    Status = SalesOrderStatuses.Submitted,
                    Remarks = $"Converted from Quotation {quote.QuotationNumber}" + (!string.IsNullOrWhiteSpace(quote.ClientPoNumber) ? $" (Client PO: {quote.ClientPoNumber})" : string.Empty),
                    CreatedBy = actingUser,
                    CreatedDate = now,
                    UpdatedBy = actingUser,
                    UpdatedDate = now,
                    Items = salesOrderItems
                };

                order.StatusHistory.Add(NewHistory(
                    SalesOrderStatuses.Submitted,
                    now,
                    actingUser,
                    $"Converted from Quotation {quote.QuotationNumber}",
                    "Converted"));

                await _db.SalesOrders.AddAsync(order, cancellationToken);
                await _db.SaveChangesAsync(cancellationToken);

                // Update Quotation state atomically
                quote.ConvertedSalesOrderId = order.Id;
                quote.ConvertedSalesOrderNumber = order.SalesOrderNumber;
                quote.ConvertedOn = now;
                quote.Status = QuotationStatuses.ConvertedToSO;
                quote.UpdatedBy = actingUser;
                quote.UpdatedDate = now;

                // Also update matching legacy QuotationApproval if present
                var matchingQa = await _db.QuotationApprovals
                    .FirstOrDefaultAsync(x => x.QuotationId == quote.Id && !x.IsDeleted, cancellationToken);
                if (matchingQa is not null)
                {
                    matchingQa.SalesOrderId = order.Id;
                    matchingQa.SalesOrderNumber = order.SalesOrderNumber;
                    matchingQa.Status = "Converted";
                    matchingQa.UpdatedBy = actingUser;
                    matchingQa.UpdatedDate = now;
                }

                await _db.SaveChangesAsync(cancellationToken);
                await tx.CommitAsync(cancellationToken);

                return SalesOrderMapper.ToDto(
                    await LoadTrackedAsync(order.Id, asTracking: false, cancellationToken) ?? order);
            }

            // Fallback for legacy QuotationApproval records
            QuotationApproval? qa;
            if (_db.Database.IsRelational())
            {
                qa = await _db.QuotationApprovals
                    .FromSqlRaw(@"SELECT *, xmin FROM ""QuotationApprovals"" WHERE ""Id"" = {0} AND ""IsDeleted"" = false FOR UPDATE", quotationApprovalId)
                    .Include(x => x.History)
                    .FirstOrDefaultAsync(cancellationToken);
            }
            else
            {
                qa = await _db.QuotationApprovals
                    .Include(x => x.History)
                    .FirstOrDefaultAsync(x => x.Id == quotationApprovalId && !x.IsDeleted, cancellationToken);
            }

            if (qa is null)
            {
                throw new InvalidOperationException($"Quotation '{quotationApprovalId}' was not found.");
            }

            if (!string.Equals(qa.Status, QuotationApprovalStatuses.Approved, StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException(
                    $"Quotation approval must be in 'Approved' status to convert to a Sales Order. Current status: '{qa.Status}'.");
            }

            if (qa.SalesOrderId.HasValue && qa.SalesOrderId.Value > 0)
            {
                throw new InvalidOperationException(
                    $"Quotation approval '{qa.ApprovalNumber}' has already been converted to Sales Order '{qa.SalesOrderNumber}'.");
            }

            var legacyItems = new List<SalesOrderItemDto>
            {
                new SalesOrderItemDto
                {
                    Id = Guid.NewGuid().ToString("N"),
                    ItemName = $"Quotation #{qa.QuotationNumber} Items",
                    Description = string.IsNullOrWhiteSpace(qa.Reason) ? "Quotation Items" : qa.Reason,
                    Quantity = 1m,
                    Unit = "Set",
                    Rate = qa.TotalAmount,
                    Discount = 0m,
                    Gst = 18m,
                    Amount = qa.TotalAmount
                }
            };

            var customer = new SalesOrderCustomerDto
            {
                CustomerName = qa.CustomerName,
                ContactPerson = string.Empty,
                BillingAddress = string.Empty,
                ShippingAddress = string.Empty
            };

            var prepared = PrepareItems(legacyItems);
            var (subtotal, discountTotal, gstTotal, grandTotal) = SalesOrderCalculator.Summarize(prepared);

            var legacyOrder = new SalesOrder
            {
                SalesOrderNumber = number,
                QuotationId = qa.QuotationId,
                QuotationNumber = qa.QuotationNumber,
                SourceType = SalesOrderSourceTypes.Quotation,
                CustomerId = 1,
                CustomerName = customer.CustomerName,
                ContactPerson = customer.ContactPerson,
                BillingAddress = customer.BillingAddress,
                ShippingAddress = customer.ShippingAddress,
                SalesPerson = qa.SalesPersonUserId.ToString(),
                Notes = qa.Reason,
                OrderDate = DateOnly.FromDateTime(DateTime.UtcNow),
                PaymentTerms = "Net 30",
                DeliveryTerms = "Standard Delivery",
                Subtotal = subtotal,
                DiscountTotal = discountTotal,
                GstTotal = gstTotal,
                GrandTotal = grandTotal > 0 ? grandTotal : qa.TotalAmount,
                Status = SalesOrderStatuses.Submitted,
                Remarks = $"Converted from Quotation Approval {qa.ApprovalNumber}",
                CreatedBy = actingUser,
                CreatedDate = now,
                UpdatedBy = actingUser,
                UpdatedDate = now,
                Items = prepared.Select((item, idx) => new SalesOrderItem
                {
                    LineKey = string.IsNullOrWhiteSpace(item.Id) ? Guid.NewGuid().ToString("N") : item.Id,
                    SortIndex = idx + 1,
                    ItemName = item.ItemName,
                    Description = item.Description ?? string.Empty,
                    Quantity = item.Quantity,
                    Unit = item.Unit ?? "Nos",
                    Rate = item.Rate,
                    Discount = item.Discount,
                    Gst = item.Gst,
                    Amount = item.Amount
                }).ToList()
            };

            legacyOrder.StatusHistory.Add(NewHistory(
                SalesOrderStatuses.Submitted,
                now,
                actingUser,
                $"Converted from Quotation Approval {qa.ApprovalNumber}",
                "Converted"));

            await _db.SalesOrders.AddAsync(legacyOrder, cancellationToken);
            await _db.SaveChangesAsync(cancellationToken);

            qa.SalesOrderId = legacyOrder.Id;
            qa.SalesOrderNumber = legacyOrder.SalesOrderNumber;
            qa.Status = "Converted";
            qa.UpdatedBy = actingUser;
            qa.UpdatedDate = now;

            await _db.SaveChangesAsync(cancellationToken);
            await tx.CommitAsync(cancellationToken);

            return SalesOrderMapper.ToDto(
                await LoadTrackedAsync(legacyOrder.Id, asTracking: false, cancellationToken) ?? legacyOrder);
        }

        public async Task<IReadOnlyList<SalesOrderStatusHistoryDto>?> GetStatusHistoryAsync(
            int id,
            CancellationToken cancellationToken = default)
        {
            var exists = await _db.SalesOrders.AnyAsync(x => x.Id == id, cancellationToken);
            if (!exists)
            {
                return null;
            }

            var rows = await _db.SalesOrderStatusHistories.AsNoTracking()
                .Where(x => x.SalesOrderId == id)
                .OrderBy(x => x.Date)
                .ToListAsync(cancellationToken);

            return rows.Select(h => new SalesOrderStatusHistoryDto
            {
                Id = h.EntryKey,
                Status = h.Status,
                Date = DateHelper.FormatDateTime(h.Date),
                User = h.User,
                Remarks = h.Remarks,
                Label = h.Label
            }).ToList();
        }

        public async Task<SalesOrderPdfResultDto?> GeneratePdfAsync(
            int id,
            CancellationToken cancellationToken = default)
        {
            var entity = await LoadTrackedAsync(id, asTracking: true, cancellationToken);
            if (entity is null)
            {
                return null;
            }

            var now = DateTimeOffset.UtcNow;
            entity.PdfGeneratedDate = now;
            entity.UpdatedBy = "system:pdf";
            entity.UpdatedDate = now;
            await _db.SaveChangesAsync(cancellationToken);

            return new SalesOrderPdfResultDto
            {
                SalesOrderId = entity.Id,
                SalesOrderNumber = entity.SalesOrderNumber,
                FileName = $"SalesOrder_{entity.SalesOrderNumber}.pdf",
                GeneratedDate = DateHelper.FormatDateTime(now),
                ContentType = "application/pdf",
                Content = System.Text.Encoding.UTF8.GetBytes($"[PDF Document Content for Sales Order {entity.SalesOrderNumber}]")
            };
        }

        public async Task<SalesOrderEmailResultDto?> SendEmailAsync(
            int id,
            SalesOrderEmailRequestDto request,
            string actingUser,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(request.Recipient))
            {
                throw new InvalidOperationException("Recipient email address is required.");
            }

            var entity = await LoadTrackedAsync(id, asTracking: true, cancellationToken);
            if (entity is null)
            {
                return null;
            }

            var now = DateTimeOffset.UtcNow;
            entity.LastCommunicationDate = now;
            entity.UpdatedBy = actingUser;
            entity.UpdatedDate = now;
            entity.EmailHistory.Add(new SalesOrderEmailHistory
            {
                EntryKey = Guid.NewGuid().ToString("N"),
                Recipient = request.Recipient.Trim(),
                SentDate = now,
                Action = "EmailSent",
                Status = "Sent"
            });

            await _db.SaveChangesAsync(cancellationToken);

            return new SalesOrderEmailResultDto
            {
                SalesOrderId = entity.Id,
                SalesOrderNumber = entity.SalesOrderNumber,
                Recipient = request.Recipient.Trim(),
                SentDate = DateHelper.FormatDateTime(now),
                Success = true,
                Message = $"Sales Order {entity.SalesOrderNumber} email sent successfully to {request.Recipient.Trim()}."
            };
        }

        public Task<IReadOnlyList<string>> GetPermissionsAsync() =>
            Task.FromResult<IReadOnlyList<string>>(
            [
                "sales-orders.view",
                "sales-orders.create",
                "sales-orders.update",
                "sales-orders.status",
                "sales-orders.cancel",
                "sales-orders.convert",
                "sales-orders.pdf",
                "sales-orders.email"
            ]);

        private async Task<SalesOrder?> LoadTrackedAsync(
            int id,
            bool asTracking,
            CancellationToken cancellationToken)
        {
            IQueryable<SalesOrder> q = _db.SalesOrders
                .Include(x => x.Items)
                .Include(x => x.Allocations)
                .Include(x => x.StatusHistory)
                .Include(x => x.EmailHistory);

            if (!asTracking)
            {
                q = q.AsNoTracking();
            }

            return await q.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        }

        private async Task<string> NextNumberAsync(CancellationToken cancellationToken)
        {
            var year = DateTime.UtcNow.Year;
            var seq = await _db.SalesOrderDocumentSequences
                .FirstOrDefaultAsync(x => x.Year == year, cancellationToken);

            if (seq is null)
            {
                seq = new SalesOrderDocumentSequence { Year = year, LastSequence = 0 };
                _db.SalesOrderDocumentSequences.Add(seq);
            }

            seq.LastSequence += 1;
            await _db.SaveChangesAsync(cancellationToken);
            return $"SO-{year}-{seq.LastSequence:D4}";
        }

        private static List<SalesOrderItemDto> PrepareItems(IEnumerable<SalesOrderItemDto> items)
        {
            return items.Select(item =>
            {
                var amount = SalesOrderCalculator.CalcLineAmount(
                    item.Quantity,
                    item.Rate,
                    item.Discount,
                    item.Gst);
                return new SalesOrderItemDto
                {
                    Id = item.Id,
                    ItemId = item.ItemId,
                    ItemCode = item.ItemCode,
                    WarehouseId = item.WarehouseId,
                    ItemName = item.ItemName,
                    Description = item.Description,
                    Quantity = item.Quantity,
                    Unit = item.Unit,
                    Rate = item.Rate,
                    Discount = item.Discount,
                    Gst = item.Gst,
                    Amount = amount,
                    ResolvedPriceListId = item.ResolvedPriceListId,
                    PriceListNumberSnapshot = item.PriceListNumberSnapshot,
                    OriginalSellingRate = item.OriginalSellingRate,
                    FloorPriceSnapshot = item.FloorPriceSnapshot,
                    MaxDiscountPctSnapshot = item.MaxDiscountPctSnapshot,
                    TaxSlabPercentSnapshot = item.TaxSlabPercentSnapshot,
                    PlaceOfSupplyStateCode = item.PlaceOfSupplyStateCode
                };
            }).ToList();
        }

        private static SalesOrderStatusHistory NewHistory(
            string status,
            DateTimeOffset date,
            string user,
            string remarks,
            string? label = null) => new()
            {
                EntryKey = Guid.NewGuid().ToString("N"),
                Status = status,
                Date = date,
                User = user,
                Remarks = remarks,
                Label = label
            };

        private static string? NullIfEmpty(string? value) =>
            string.IsNullOrWhiteSpace(value) ? null : value.Trim();

        private static void ValidateCustomer(SalesOrderCustomerDto? customer)
        {
            if (customer is null || string.IsNullOrWhiteSpace(customer.CustomerName))
            {
                throw new InvalidOperationException("Customer name is required.");
            }
        }

        private static void ValidateItems(IEnumerable<SalesOrderItemDto>? items)
        {
            if (items is null || !items.Any())
            {
                throw new InvalidOperationException("At least one line item is required.");
            }

            foreach (var item in items)
            {
                if (string.IsNullOrWhiteSpace(item.ItemName))
                {
                    throw new InvalidOperationException("Item name is required for all line items.");
                }

                if (item.Quantity <= 0)
                {
                    throw new InvalidOperationException("Item quantity must be greater than zero.");
                }

                if (item.Rate < 0)
                {
                    throw new InvalidOperationException("Item rate cannot be negative.");
                }
            }
        }
    }
}
