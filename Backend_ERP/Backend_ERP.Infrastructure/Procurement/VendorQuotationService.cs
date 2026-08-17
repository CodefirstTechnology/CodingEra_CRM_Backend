using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ERP.Application.Procurement;
using ERP.Application.Procurement.Dtos;
using ERP.Domain.Procurement;
using ERP.Infrastructure.Data;
using ERP.Shared.Models;
using Microsoft.EntityFrameworkCore;

namespace ERP.Infrastructure.Procurement
{
    public class VendorQuotationService : IVendorQuotationService
    {
        private readonly ERPDbContext _db;
        private readonly VendorQuotationNumberingService _numberingService;

        public VendorQuotationService(ERPDbContext db, VendorQuotationNumberingService numberingService)
        {
            _db = db;
            _numberingService = numberingService;
        }

        public async Task<PagedResult<VendorQuotationDto>> GetAllAsync(
            int? rfqId,
            int? vendorId,
            int page = 1,
            int pageSize = 20,
            CancellationToken cancellationToken = default)
        {
            var q = _db.VendorQuotations
                .Include(q => q.Lines)
                .AsNoTracking()
                .Where(q => !q.IsDeleted);

            if (rfqId.HasValue && rfqId.Value > 0)
            {
                q = q.Where(x => x.RequestForQuotationId == rfqId.Value);
            }

            if (vendorId.HasValue && vendorId.Value > 0)
            {
                q = q.Where(x => x.VendorId == vendorId.Value);
            }

            var totalCount = await q.CountAsync(cancellationToken);
            var p = Math.Max(1, page);
            var ps = Math.Clamp(pageSize, 1, 1000);

            var items = await q
                .OrderByDescending(x => x.CreatedAt)
                .Skip((p - 1) * ps)
                .Take(ps)
                .ToListAsync(cancellationToken);

            var dtos = items.Select(MapToDto).ToList();
            return PagedResult<VendorQuotationDto>.Create(dtos, totalCount, p, ps);
        }

        public async Task<VendorQuotationDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
        {
            var vq = await _db.VendorQuotations
                .Include(x => x.Lines)
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted, cancellationToken);

            return vq is null ? null : MapToDto(vq);
        }

        public async Task<VendorQuotationDto> CreateAsync(
            VendorQuotationCreateRequestDto request,
            string actingUser,
            CancellationToken cancellationToken = default)
        {
            var vendor = await _db.Vendors.FirstOrDefaultAsync(v => v.Id == request.VendorId && !v.IsDeleted, cancellationToken);
            if (vendor is null)
            {
                throw new InvalidOperationException($"Vendor with ID {request.VendorId} not found.");
            }

            var now = DateTime.UtcNow;
            var quotationNumber = await _numberingService.NextQuotationNumberAsync(cancellationToken);

            var vq = new VendorQuotation
            {
                QuotationNumber = quotationNumber,
                RequestForQuotationId = request.RequestForQuotationId,
                VendorId = request.VendorId,
                VendorName = vendor.Name,
                QuotationRef = string.IsNullOrWhiteSpace(request.QuotationRef) ? quotationNumber : request.QuotationRef.Trim(),
                QuotationDate = request.QuotationDate.ToUniversalTime(),
                ValidityDate = request.ValidityDate?.ToUniversalTime(),
                DeliveryTimeDays = request.DeliveryTimeDays,
                LeadTime = request.LeadTime?.Trim() ?? string.Empty,
                WarrantyPeriod = request.WarrantyPeriod?.Trim() ?? string.Empty,
                PaymentTerms = request.PaymentTerms?.Trim() ?? string.Empty,
                DiscountPercent = request.DiscountPercent,
                TaxPercent = request.TaxPercent,
                Remarks = request.Remarks?.Trim() ?? string.Empty,
                CreatedAt = now,
                CreatedBy = actingUser,
                UpdatedAt = now,
                UpdatedBy = actingUser
            };

            decimal subTotal = 0m;
            foreach (var l in request.Lines ?? new List<VendorQuotationLineDto>())
            {
                var lineAmount = Math.Round(l.Quantity * l.UnitPrice, 4);
                if (l.DiscountPercent > 0)
                {
                    lineAmount -= Math.Round(lineAmount * (l.DiscountPercent / 100m), 4);
                }
                if (l.TaxPercent > 0)
                {
                    lineAmount += Math.Round(lineAmount * (l.TaxPercent / 100m), 4);
                }

                subTotal += Math.Round(l.Quantity * l.UnitPrice, 4);

                vq.Lines.Add(new VendorQuotationLine
                {
                    ItemName = l.ItemName.Trim(),
                    Quantity = l.Quantity,
                    Uom = string.IsNullOrWhiteSpace(l.Uom) ? "PCS" : l.Uom.Trim(),
                    UnitPrice = l.UnitPrice,
                    TaxPercent = l.TaxPercent,
                    DiscountPercent = l.DiscountPercent,
                    Amount = lineAmount
                });
            }

            vq.SubTotal = Math.Round(subTotal, 4);

            var discountAmount = request.DiscountPercent > 0 ? Math.Round(subTotal * (request.DiscountPercent / 100m), 4) : 0m;
            var subAfterDiscount = subTotal - discountAmount;
            var taxAmount = request.TaxPercent > 0 ? Math.Round(subAfterDiscount * (request.TaxPercent / 100m), 4) : 0m;

            vq.DiscountAmount = discountAmount;
            vq.TaxAmount = taxAmount;
            vq.TotalCost = Math.Round(subAfterDiscount + taxAmount, 4);

            _db.VendorQuotations.Add(vq);
            await _db.SaveChangesAsync(cancellationToken);

            return (await GetByIdAsync(vq.Id, cancellationToken))!;
        }

        public async Task<bool> DeleteAsync(int id, string actingUser, CancellationToken cancellationToken = default)
        {
            var vq = await _db.VendorQuotations.FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted, cancellationToken);
            if (vq is null) return false;

            vq.IsDeleted = true;
            vq.UpdatedAt = DateTime.UtcNow;
            vq.UpdatedBy = actingUser;

            await _db.SaveChangesAsync(cancellationToken);
            return true;
        }

        public async Task<string> GetNextQuotationNumberAsync(CancellationToken cancellationToken = default)
        {
            return await _numberingService.NextQuotationNumberAsync(cancellationToken);
        }

        private static VendorQuotationDto MapToDto(VendorQuotation vq) => new()
        {
            Id = vq.Id,
            QuotationNumber = vq.QuotationNumber,
            RequestForQuotationId = vq.RequestForQuotationId,
            VendorId = vq.VendorId,
            VendorName = vq.VendorName,
            QuotationRef = vq.QuotationRef,
            QuotationDate = vq.QuotationDate,
            ValidityDate = vq.ValidityDate,
            DeliveryTimeDays = vq.DeliveryTimeDays,
            LeadTime = vq.LeadTime,
            WarrantyPeriod = vq.WarrantyPeriod,
            PaymentTerms = vq.PaymentTerms,
            SubTotal = vq.SubTotal,
            DiscountPercent = vq.DiscountPercent,
            DiscountAmount = vq.DiscountAmount,
            TaxPercent = vq.TaxPercent,
            TaxAmount = vq.TaxAmount,
            TotalCost = vq.TotalCost,
            Remarks = vq.Remarks,
            CreatedAt = vq.CreatedAt,
            CreatedBy = vq.CreatedBy,
            Lines = vq.Lines?.Select(l => new VendorQuotationLineDto
            {
                Id = l.Id,
                VendorQuotationId = l.VendorQuotationId,
                ItemName = l.ItemName,
                Quantity = l.Quantity,
                Uom = l.Uom,
                UnitPrice = l.UnitPrice,
                TaxPercent = l.TaxPercent,
                DiscountPercent = l.DiscountPercent,
                Amount = l.Amount
            }).ToList() ?? new()
        };
    }
}
