using ERP.Application.Sales;
using ERP.Application.Sales.Dtos;
using ERP.Domain.Sales;
using ERP.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace ERP.Infrastructure.Sales
{
    public class SalesOrderService : ISalesOrderService
    {
        private readonly ERPDbContext _db;

        public SalesOrderService(ERPDbContext db)
        {
            _db = db;
        }

        public async Task<IReadOnlyList<SalesOrderListItemDto>> GetAllAsync(
            SalesOrderListQueryDto? query,
            CancellationToken cancellationToken = default)
        {
            var q = _db.SalesOrders.AsNoTracking();

            if (!string.IsNullOrWhiteSpace(query?.Status))
            {
                var status = SalesOrderStatusRules.Normalize(query.Status)
                    ?? query.Status.Trim();
                q = q.Where(x => x.Status == status);
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
                CustomerName = request.Customer.CustomerName.Trim(),
                ContactPerson = request.Customer.ContactPerson?.Trim() ?? string.Empty,
                BillingAddress = request.Customer.BillingAddress?.Trim() ?? string.Empty,
                ShippingAddress = request.Customer.ShippingAddress?.Trim() ?? string.Empty,
                CustomerEmail = NullIfEmpty(request.Customer.CustomerEmail),
                CustomerPhone = NullIfEmpty(request.Customer.CustomerPhone),
                SalesPerson = request.SalesPerson?.Trim() ?? string.Empty,
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
                entity.Items.Add(new SalesOrderItem
                {
                    LineKey = string.IsNullOrWhiteSpace(item.Id) ? Guid.NewGuid().ToString("N") : item.Id.Trim(),
                    SortIndex = i,
                    ItemName = item.ItemName.Trim(),
                    Description = item.Description?.Trim() ?? string.Empty,
                    Quantity = item.Quantity,
                    Unit = string.IsNullOrWhiteSpace(item.Unit) ? "Nos" : item.Unit.Trim(),
                    Rate = item.Rate,
                    Discount = item.Discount,
                    Gst = item.Gst,
                    Amount = item.Amount
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

            if (entity.Status is SalesOrderStatuses.Completed or SalesOrderStatuses.Cancelled)
            {
                throw new InvalidOperationException(
                    $"Cannot update a sales order in '{entity.Status}' status.");
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
            entity.CustomerName = request.Customer.CustomerName.Trim();
            entity.ContactPerson = request.Customer.ContactPerson?.Trim() ?? string.Empty;
            entity.BillingAddress = request.Customer.BillingAddress?.Trim() ?? string.Empty;
            entity.ShippingAddress = request.Customer.ShippingAddress?.Trim() ?? string.Empty;
            entity.CustomerEmail = NullIfEmpty(request.Customer.CustomerEmail);
            entity.CustomerPhone = NullIfEmpty(request.Customer.CustomerPhone);
            entity.SalesPerson = request.SalesPerson?.Trim() ?? string.Empty;
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
                    ItemName = item.ItemName.Trim(),
                    Description = item.Description?.Trim() ?? string.Empty,
                    Quantity = item.Quantity,
                    Unit = string.IsNullOrWhiteSpace(item.Unit) ? "Nos" : item.Unit.Trim(),
                    Rate = item.Rate,
                    Discount = item.Discount,
                    Gst = item.Gst,
                    Amount = item.Amount
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

        public async Task<IReadOnlyList<SalesOrderStatusHistoryDto>?> GetStatusHistoryAsync(
            int id,
            CancellationToken cancellationToken = default)
        {
            var exists = await _db.SalesOrders.AsNoTracking()
                .AnyAsync(x => x.Id == id, cancellationToken);
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
                Date = SalesOrderMapper.FormatDateTime(h.Date),
                User = h.User,
                Remarks = h.Remarks,
                Label = h.Label
            }).ToList();
        }

        private async Task<SalesOrder?> LoadTrackedAsync(
            int id,
            bool asTracking,
            CancellationToken cancellationToken)
        {
            IQueryable<SalesOrder> q = _db.SalesOrders
                .Include(x => x.Items)
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
                    ItemName = item.ItemName,
                    Description = item.Description,
                    Quantity = item.Quantity,
                    Unit = item.Unit,
                    Rate = item.Rate,
                    Discount = item.Discount,
                    Gst = item.Gst,
                    Amount = amount
                };
            }).ToList();
        }

        private static void ValidateCustomer(SalesOrderCustomerDto customer)
        {
            if (customer is null || string.IsNullOrWhiteSpace(customer.CustomerName))
            {
                throw new InvalidOperationException("Customer name is required.");
            }
        }

        private static void ValidateItems(IList<SalesOrderItemDto> items)
        {
            if (items is null || items.Count == 0)
            {
                throw new InvalidOperationException("At least one line item is required.");
            }

            foreach (var item in items)
            {
                if (string.IsNullOrWhiteSpace(item.ItemName))
                {
                    throw new InvalidOperationException("Each line item requires an item name.");
                }

                if (item.Quantity <= 0)
                {
                    throw new InvalidOperationException("Line item quantity must be greater than zero.");
                }
            }
        }

        private static SalesOrderStatusHistory NewHistory(
            string status,
            DateTimeOffset date,
            string user,
            string remarks,
            string? label) => new()
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
    }
}
