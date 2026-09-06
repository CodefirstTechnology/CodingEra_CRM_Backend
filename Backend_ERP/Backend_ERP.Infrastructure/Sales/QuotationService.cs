using ERP.Shared.Models;
using ERP.Application.Sales;
using ERP.Application.Sales.Dtos;
using ERP.Domain.Sales;
using ERP.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace ERP.Infrastructure.Sales
{
    public class QuotationService : IQuotationService
    {
        private readonly ERPDbContext _db;

        public QuotationService(ERPDbContext db)
        {
            _db = db;
        }

        public async Task<PagedResult<QuotationDto>> GetPagedAsync(
            QuotationListQueryDto query,
            CancellationToken cancellationToken = default)
        {
            var q = _db.Quotations
                .AsNoTracking()
                .Include(x => x.Items)
                .Where(x => !x.IsDeleted);

            if (query.CurrentOnly)
            {
                q = q.Where(x => x.IsCurrentRevision);
            }

            if (!string.IsNullOrWhiteSpace(query.Status))
            {
                q = q.Where(x => x.Status == query.Status);
            }

            if (!string.IsNullOrWhiteSpace(query.Customer))
            {
                q = q.Where(x => EF.Functions.ILike(x.CustomerName, $"%{query.Customer}%"));
            }

            if (!string.IsNullOrWhiteSpace(query.Search))
            {
                var s = $"%{query.Search}%";
                q = q.Where(x =>
                    EF.Functions.ILike(x.QuotationNumber, s) ||
                    EF.Functions.ILike(x.CustomerName, s) ||
                    (x.ClientPoNumber != null && EF.Functions.ILike(x.ClientPoNumber, s)));
            }

            if (query.DateFrom.HasValue)
            {
                q = q.Where(x => x.QuotationDate >= query.DateFrom.Value);
            }

            if (query.DateTo.HasValue)
            {
                q = q.Where(x => x.QuotationDate <= query.DateTo.Value);
            }

            var total = await q.CountAsync(cancellationToken);
            var page = Math.Max(1, query.Page);
            var pageSize = Math.Clamp(query.PageSize, 1, 200);

            var items = await q
                .OrderByDescending(x => x.Id)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(cancellationToken);

            return PagedResult<QuotationDto>.Create(
                items.Select(MapToDto).ToList(),
                total,
                page,
                pageSize);
        }

        public async Task<QuotationDto?> GetByIdAsync(
            int id,
            CancellationToken cancellationToken = default)
        {
            var entity = await _db.Quotations
                .AsNoTracking()
                .Include(x => x.Items)
                .FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted, cancellationToken);

            return entity is null ? null : MapToDto(entity);
        }

        public async Task<QuotationDto> CreateAsync(
            QuotationCreateRequestDto request,
            string actingUser,
            CancellationToken cancellationToken = default)
        {
            var now = DateTimeOffset.UtcNow;
            var number = await GetNextNumberAsync(cancellationToken);

            var entity = new Quotation
            {
                QuotationNumber = number,
                RevisionNumber = 1,
                IsCurrentRevision = true,
                CustomerId = request.CustomerId ?? string.Empty,
                CustomerName = request.CustomerName,
                ContactPerson = request.ContactPerson ?? string.Empty,
                CustomerEmail = request.CustomerEmail,
                CustomerPhone = request.CustomerPhone,
                BillingAddress = request.BillingAddress ?? string.Empty,
                ShippingAddress = request.ShippingAddress ?? string.Empty,
                SalesPerson = request.SalesPerson ?? actingUser,
                Status = QuotationStatuses.Draft,
                QuotationDate = request.QuotationDate ?? DateOnly.FromDateTime(DateTime.UtcNow),
                ValidUntil = request.ValidUntil ?? DateOnly.FromDateTime(DateTime.UtcNow.AddDays(14)),
                Currency = request.Currency ?? "INR",
                ExchangeRate = request.ExchangeRate ?? 1.0m,
                FreightAmount = request.FreightAmount ?? 0m,
                PackagingAmount = request.PackagingAmount ?? 0m,
                RoundOff = request.RoundOff ?? 0m,
                PaymentTerms = request.PaymentTerms ?? "Net 30",
                DeliveryTerms = request.DeliveryTerms ?? "Ex-Works",
                Notes = request.Notes ?? string.Empty,
                CreatedBy = actingUser,
                CreatedDate = now,
                UpdatedBy = actingUser,
                UpdatedDate = now,
                IsDeleted = false
            };

            HydrateItems(entity, request.Items);
            CalculateTotals(entity);

            _db.Quotations.Add(entity);
            await _db.SaveChangesAsync(cancellationToken);

            return MapToDto(entity);
        }

        public async Task<QuotationDto?> UpdateAsync(
            int id,
            QuotationUpdateRequestDto request,
            string actingUser,
            CancellationToken cancellationToken = default)
        {
            var entity = await _db.Quotations
                .Include(x => x.Items)
                .FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted, cancellationToken);

            if (entity is null) return null;

            if (entity.Status == QuotationStatuses.ConvertedToSO)
            {
                throw new InvalidOperationException("Converted quotation cannot be modified.");
            }

            var now = DateTimeOffset.UtcNow;
            entity.CustomerName = request.CustomerName;
            entity.ContactPerson = request.ContactPerson ?? string.Empty;
            entity.CustomerEmail = request.CustomerEmail;
            entity.CustomerPhone = request.CustomerPhone;
            entity.BillingAddress = request.BillingAddress ?? string.Empty;
            entity.ShippingAddress = request.ShippingAddress ?? string.Empty;
            entity.SalesPerson = request.SalesPerson ?? entity.SalesPerson;
            if (request.QuotationDate.HasValue) entity.QuotationDate = request.QuotationDate.Value;
            if (request.ValidUntil.HasValue) entity.ValidUntil = request.ValidUntil.Value;
            entity.Currency = request.Currency ?? entity.Currency;
            entity.ExchangeRate = request.ExchangeRate ?? entity.ExchangeRate;
            if (request.FreightAmount.HasValue) entity.FreightAmount = request.FreightAmount.Value;
            if (request.PackagingAmount.HasValue) entity.PackagingAmount = request.PackagingAmount.Value;
            if (request.RoundOff.HasValue) entity.RoundOff = request.RoundOff.Value;
            entity.PaymentTerms = request.PaymentTerms ?? entity.PaymentTerms;
            entity.DeliveryTerms = request.DeliveryTerms ?? entity.DeliveryTerms;
            entity.Notes = request.Notes ?? entity.Notes;
            entity.UpdatedBy = actingUser;
            entity.UpdatedDate = now;

            _db.QuotationItems.RemoveRange(entity.Items);
            entity.Items.Clear();

            HydrateItems(entity, request.Items);
            CalculateTotals(entity);

            await _db.SaveChangesAsync(cancellationToken);
            return MapToDto(entity);
        }

        public async Task<bool> DeleteAsync(
            int id,
            string actingUser,
            CancellationToken cancellationToken = default)
        {
            var entity = await _db.Quotations.FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted, cancellationToken);
            if (entity is null) return false;

            if (entity.Status == QuotationStatuses.ConvertedToSO)
            {
                throw new InvalidOperationException("Cannot delete a quotation that has been converted to a Sales Order.");
            }

            entity.IsDeleted = true;
            entity.UpdatedBy = actingUser;
            entity.UpdatedDate = DateTimeOffset.UtcNow;

            await _db.SaveChangesAsync(cancellationToken);
            return true;
        }

        public async Task<QuotationDto> MarkClientAcceptedAsync(
            int id,
            QuotationClientAcceptRequestDto request,
            string actingUser,
            CancellationToken cancellationToken = default)
        {
            var entity = await _db.Quotations
                .Include(x => x.Items)
                .FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted, cancellationToken);

            if (entity is null)
            {
                throw new KeyNotFoundException($"Quotation with ID {id} not found.");
            }

            if (entity.Status == QuotationStatuses.ConvertedToSO)
            {
                throw new InvalidOperationException("Quotation has already been converted to a Sales Order.");
            }

            var now = DateTimeOffset.UtcNow;
            entity.Status = QuotationStatuses.Approved; // Approved strictly means Client Accepted
            entity.ClientPoNumber = request.ClientPoNumber?.Trim();
            entity.ClientAcceptedAt = request.ClientAcceptedAt ?? now;
            entity.ClientPoAttachmentUrl = request.ClientPoAttachmentUrl?.Trim();
            entity.UpdatedBy = actingUser;
            entity.UpdatedDate = now;

            await _db.SaveChangesAsync(cancellationToken);
            return MapToDto(entity);
        }

        public async Task<QuotationDto> ReviseAsync(
            int id,
            QuotationReviseRequestDto request,
            string actingUser,
            CancellationToken cancellationToken = default)
        {
            var existing = await _db.Quotations
                .Include(x => x.Items)
                .FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted, cancellationToken);

            if (existing is null)
            {
                throw new KeyNotFoundException($"Quotation with ID {id} not found.");
            }

            if (existing.Status == QuotationStatuses.ConvertedToSO)
            {
                throw new InvalidOperationException("Cannot revise a quotation that has already been converted to a Sales Order.");
            }

            var now = DateTimeOffset.UtcNow;

            // Mark current revision as obsolete/revised
            existing.IsCurrentRevision = false;
            existing.Status = QuotationStatuses.Revised;
            existing.UpdatedBy = actingUser;
            existing.UpdatedDate = now;

            // Clone to new revision
            var clone = new Quotation
            {
                QuotationNumber = existing.QuotationNumber,
                RevisionNumber = existing.RevisionNumber + 1,
                IsCurrentRevision = true,
                CustomerId = existing.CustomerId,
                CustomerName = existing.CustomerName,
                ContactPerson = existing.ContactPerson,
                CustomerEmail = existing.CustomerEmail,
                CustomerPhone = existing.CustomerPhone,
                BillingAddress = existing.BillingAddress,
                ShippingAddress = existing.ShippingAddress,
                SalesPerson = existing.SalesPerson,
                Status = QuotationStatuses.Draft,
                QuotationDate = DateOnly.FromDateTime(DateTime.UtcNow),
                ValidUntil = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(14)),
                Currency = existing.Currency,
                ExchangeRate = existing.ExchangeRate,
                Subtotal = existing.Subtotal,
                DiscountTotal = existing.DiscountTotal,
                TaxTotal = existing.TaxTotal,
                FreightAmount = existing.FreightAmount,
                PackagingAmount = existing.PackagingAmount,
                RoundOff = existing.RoundOff,
                GrandTotal = existing.GrandTotal,
                PaymentTerms = existing.PaymentTerms,
                DeliveryTerms = existing.DeliveryTerms,
                Notes = string.IsNullOrWhiteSpace(request.Reason)
                    ? existing.Notes
                    : $"{existing.Notes}\n[Revision Note]: {request.Reason}".Trim(),
                CreatedBy = actingUser,
                CreatedDate = now,
                UpdatedBy = actingUser,
                UpdatedDate = now,
                IsDeleted = false
            };

            foreach (var item in existing.Items.OrderBy(i => i.SortOrder))
            {
                clone.Items.Add(new QuotationItem
                {
                    ProductId = item.ProductId,
                    LineNumber = item.LineNumber,
                    ItemCode = item.ItemCode,
                    ItemName = item.ItemName,
                    Description = item.Description,
                    Quantity = item.Quantity,
                    Unit = item.Unit,
                    UnitPrice = item.UnitPrice,
                    DiscountPercent = item.DiscountPercent,
                    DiscountAmount = item.DiscountAmount,
                    TaxPercent = item.TaxPercent,
                    TaxAmount = item.TaxAmount,
                    LineTotal = item.LineTotal,
                    SortOrder = item.SortOrder
                });
            }

            _db.Quotations.Add(clone);
            await _db.SaveChangesAsync(cancellationToken);

            return MapToDto(clone);
        }

        public async Task<QuotationDto> DuplicateAsync(
            int id,
            string actingUser,
            CancellationToken cancellationToken = default)
        {
            var existing = await _db.Quotations
                .Include(x => x.Items)
                .FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted, cancellationToken);

            if (existing is null)
            {
                throw new KeyNotFoundException($"Quotation with ID {id} not found.");
            }

            var now = DateTimeOffset.UtcNow;
            var number = await GetNextNumberAsync(cancellationToken);

            var clone = new Quotation
            {
                QuotationNumber = number,
                RevisionNumber = 1,
                IsCurrentRevision = true,
                CustomerId = existing.CustomerId,
                CustomerName = existing.CustomerName,
                ContactPerson = existing.ContactPerson,
                CustomerEmail = existing.CustomerEmail,
                CustomerPhone = existing.CustomerPhone,
                BillingAddress = existing.BillingAddress,
                ShippingAddress = existing.ShippingAddress,
                SalesPerson = actingUser,
                Status = QuotationStatuses.Draft,
                QuotationDate = DateOnly.FromDateTime(DateTime.UtcNow),
                ValidUntil = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(14)),
                Currency = existing.Currency,
                ExchangeRate = existing.ExchangeRate,
                Subtotal = existing.Subtotal,
                DiscountTotal = existing.DiscountTotal,
                TaxTotal = existing.TaxTotal,
                FreightAmount = existing.FreightAmount,
                PackagingAmount = existing.PackagingAmount,
                RoundOff = existing.RoundOff,
                GrandTotal = existing.GrandTotal,
                PaymentTerms = existing.PaymentTerms,
                DeliveryTerms = existing.DeliveryTerms,
                Notes = existing.Notes,
                CreatedBy = actingUser,
                CreatedDate = now,
                UpdatedBy = actingUser,
                UpdatedDate = now,
                IsDeleted = false
            };

            foreach (var item in existing.Items.OrderBy(i => i.SortOrder))
            {
                clone.Items.Add(new QuotationItem
                {
                    ProductId = item.ProductId,
                    LineNumber = item.LineNumber,
                    ItemCode = item.ItemCode,
                    ItemName = item.ItemName,
                    Description = item.Description,
                    Quantity = item.Quantity,
                    Unit = item.Unit,
                    UnitPrice = item.UnitPrice,
                    DiscountPercent = item.DiscountPercent,
                    DiscountAmount = item.DiscountAmount,
                    TaxPercent = item.TaxPercent,
                    TaxAmount = item.TaxAmount,
                    LineTotal = item.LineTotal,
                    SortOrder = item.SortOrder
                });
            }

            _db.Quotations.Add(clone);
            await _db.SaveChangesAsync(cancellationToken);

            return MapToDto(clone);
        }

        public async Task<string> GetNextNumberAsync(CancellationToken cancellationToken = default)
        {
            var yearMonth = DateTime.UtcNow.ToString("yyyyMM");
            var prefix = $"QT-{yearMonth}-";
            var count = await _db.Quotations
                .CountAsync(x => x.QuotationNumber.StartsWith(prefix), cancellationToken);

            return $"{prefix}{(count + 1):D4}";
        }

        private static void HydrateItems(Quotation quote, IEnumerable<QuotationItemUpsertDto> dtos)
        {
            var idx = 1;
            foreach (var dto in dtos)
            {
                var discountAmt = Math.Round(dto.Quantity * dto.UnitPrice * (dto.DiscountPercent / 100m), 2);
                var taxable = (dto.Quantity * dto.UnitPrice) - discountAmt;
                var taxAmt = Math.Round(taxable * (dto.TaxPercent / 100m), 2);
                var lineTotal = taxable + taxAmt;

                quote.Items.Add(new QuotationItem
                {
                    ProductId = dto.ProductId,
                    LineNumber = idx,
                    SortOrder = dto.SortOrder > 0 ? dto.SortOrder : idx,
                    ItemCode = dto.ItemCode ?? string.Empty,
                    ItemName = dto.ItemName,
                    Description = dto.Description ?? string.Empty,
                    Quantity = dto.Quantity,
                    Unit = dto.Unit ?? "NOS",
                    UnitPrice = dto.UnitPrice,
                    DiscountPercent = dto.DiscountPercent,
                    DiscountAmount = discountAmt,
                    TaxPercent = dto.TaxPercent,
                    TaxAmount = taxAmt,
                    LineTotal = lineTotal
                });
                idx++;
            }
        }

        private static void CalculateTotals(Quotation quote)
        {
            quote.CalculateTotals();
        }

        private static QuotationDto MapToDto(Quotation q)
        {
            return new QuotationDto
            {
                Id = q.Id,
                QuotationNumber = q.QuotationNumber,
                RevisionNumber = q.RevisionNumber,
                IsCurrentRevision = q.IsCurrentRevision,
                CustomerId = q.CustomerId,
                CustomerName = q.CustomerName,
                ContactPerson = q.ContactPerson,
                CustomerEmail = q.CustomerEmail,
                CustomerPhone = q.CustomerPhone,
                BillingAddress = q.BillingAddress,
                ShippingAddress = q.ShippingAddress,
                SalesPerson = q.SalesPerson,
                Status = q.Status,
                QuotationDate = q.QuotationDate.ToString("yyyy-MM-dd"),
                ValidUntil = q.ValidUntil.ToString("yyyy-MM-dd"),
                Currency = q.Currency,
                ExchangeRate = q.ExchangeRate,
                Subtotal = q.Subtotal,
                DiscountTotal = q.DiscountTotal,
                TaxTotal = q.TaxTotal,
                FreightAmount = q.FreightAmount,
                PackagingAmount = q.PackagingAmount,
                RoundOff = q.RoundOff,
                GrandTotal = q.GrandTotal,
                PaymentTerms = q.PaymentTerms,
                DeliveryTerms = q.DeliveryTerms,
                Notes = q.Notes,
                ClientPoNumber = q.ClientPoNumber,
                ClientAcceptedAt = q.ClientAcceptedAt?.ToString("o"),
                ClientPoAttachmentUrl = q.ClientPoAttachmentUrl,
                ConvertedSalesOrderId = q.ConvertedSalesOrderId,
                ConvertedSalesOrderNumber = q.ConvertedSalesOrderNumber,
                ConvertedOn = q.ConvertedOn?.ToString("o"),
                CreatedBy = q.CreatedBy,
                CreatedDate = q.CreatedDate,
                UpdatedBy = q.UpdatedBy,
                UpdatedDate = q.UpdatedDate,
                Items = q.Items.OrderBy(i => i.SortOrder).Select(i => new QuotationItemDto
                {
                    Id = i.Id,
                    ProductId = i.ProductId,
                    LineNumber = i.LineNumber,
                    ItemCode = i.ItemCode,
                    ItemName = i.ItemName,
                    Description = i.Description,
                    Quantity = i.Quantity,
                    Unit = i.Unit,
                    UnitPrice = i.UnitPrice,
                    DiscountPercent = i.DiscountPercent,
                    DiscountAmount = i.DiscountAmount,
                    TaxPercent = i.TaxPercent,
                    TaxAmount = i.TaxAmount,
                    LineTotal = i.LineTotal,
                    SortOrder = i.SortOrder
                }).ToList()
            };
        }
    }
}
