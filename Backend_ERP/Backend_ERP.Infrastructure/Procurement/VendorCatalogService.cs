using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ERP.Application.Procurement;
using ERP.Application.Procurement.Dtos;
using ERP.Domain.Procurement;
using ERP.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace ERP.Infrastructure.Procurement
{
    public class VendorCatalogService : IVendorCatalogService
    {
        private readonly ERPDbContext _db;

        public VendorCatalogService(ERPDbContext db)
        {
            _db = db;
        }

        public async Task<IReadOnlyList<VendorItemCatalogDto>> GetVendorCatalogsAsync(
            int vendorId,
            CancellationToken cancellationToken = default)
        {
            var catalogs = await _db.VendorItemCatalogs
                .AsNoTracking()
                .Where(c => c.VendorId == vendorId)
                .OrderBy(c => c.ItemCode)
                .ToListAsync(cancellationToken);

            return catalogs.Select(VendorMapper.ToCatalogDto).ToList();
        }

        public async Task<VendorItemCatalogDto> AddCatalogItemAsync(
            int vendorId,
            VendorItemCatalogCreateDto dto,
            string actingUser,
            CancellationToken cancellationToken = default)
        {
            var vendor = await _db.Vendors
                .FirstOrDefaultAsync(v => v.Id == vendorId && !v.IsDeleted, cancellationToken);

            if (vendor is null)
            {
                throw new KeyNotFoundException($"Vendor with ID {vendorId} not found.");
            }

            if (dto.MinimumOrderQuantity <= 0)
            {
                throw new ArgumentException("Minimum Order Quantity (MOQ) must be greater than zero.", nameof(dto.MinimumOrderQuantity));
            }

            if (dto.OrderMultiple <= 0)
            {
                throw new ArgumentException("Order Multiple must be greater than zero.", nameof(dto.OrderMultiple));
            }

            if (dto.BaseUomConversionMultiplier <= 0)
            {
                throw new ArgumentException("Base UOM Conversion Multiplier must be greater than zero.", nameof(dto.BaseUomConversionMultiplier));
            }

            var catalog = new VendorItemCatalog
            {
                TenantId = vendor.TenantId,
                VendorId = vendorId,
                ItemType = string.IsNullOrWhiteSpace(dto.ItemType) ? "RawMaterial" : dto.ItemType.Trim(),
                ItemId = dto.ItemId,
                ItemCode = dto.ItemCode.Trim().ToUpperInvariant(),
                SupplierPartNumber = dto.SupplierPartNumber?.Trim(),
                StandardLeadTimeDays = dto.StandardLeadTimeDays <= 0 ? 14 : dto.StandardLeadTimeDays,
                MinimumOrderQuantity = dto.MinimumOrderQuantity,
                OrderMultiple = dto.OrderMultiple,
                PackagingUnit = string.IsNullOrWhiteSpace(dto.PackagingUnit) ? "Units" : dto.PackagingUnit.Trim(),
                BaseUomConversionMultiplier = dto.BaseUomConversionMultiplier,
                UnitPriceSnapshot = dto.UnitPriceSnapshot,
                Currency = string.IsNullOrWhiteSpace(dto.Currency) ? vendor.Currency : dto.Currency.Trim().ToUpperInvariant(),
                EffectiveFrom = dto.EffectiveFrom,
                EffectiveTo = dto.EffectiveTo,
                IsApproved = dto.IsApproved,
                TechnicalRemarks = dto.TechnicalRemarks?.Trim()
            };

            _db.VendorItemCatalogs.Add(catalog);
            await _db.SaveChangesAsync(cancellationToken);

            return VendorMapper.ToCatalogDto(catalog);
        }

        public async Task<VendorItemCatalogDto?> UpdateCatalogItemAsync(
            int vendorId,
            int catalogId,
            VendorItemCatalogUpdateDto dto,
            string actingUser,
            CancellationToken cancellationToken = default)
        {
            var catalog = await _db.VendorItemCatalogs
                .FirstOrDefaultAsync(c => c.Id == catalogId && c.VendorId == vendorId, cancellationToken);

            if (catalog is null) return null;

            if (dto.MinimumOrderQuantity <= 0)
            {
                throw new ArgumentException("Minimum Order Quantity (MOQ) must be greater than zero.", nameof(dto.MinimumOrderQuantity));
            }

            if (dto.OrderMultiple <= 0)
            {
                throw new ArgumentException("Order Multiple must be greater than zero.", nameof(dto.OrderMultiple));
            }

            if (dto.BaseUomConversionMultiplier <= 0)
            {
                throw new ArgumentException("Base UOM Conversion Multiplier must be greater than zero.", nameof(dto.BaseUomConversionMultiplier));
            }

            catalog.ItemType = string.IsNullOrWhiteSpace(dto.ItemType) ? catalog.ItemType : dto.ItemType.Trim();
            catalog.ItemId = dto.ItemId;
            catalog.ItemCode = dto.ItemCode.Trim().ToUpperInvariant();
            catalog.SupplierPartNumber = dto.SupplierPartNumber?.Trim();
            catalog.StandardLeadTimeDays = dto.StandardLeadTimeDays <= 0 ? 14 : dto.StandardLeadTimeDays;
            catalog.MinimumOrderQuantity = dto.MinimumOrderQuantity;
            catalog.OrderMultiple = dto.OrderMultiple;
            catalog.PackagingUnit = string.IsNullOrWhiteSpace(dto.PackagingUnit) ? catalog.PackagingUnit : dto.PackagingUnit.Trim();
            catalog.BaseUomConversionMultiplier = dto.BaseUomConversionMultiplier;
            catalog.UnitPriceSnapshot = dto.UnitPriceSnapshot;
            catalog.Currency = string.IsNullOrWhiteSpace(dto.Currency) ? catalog.Currency : dto.Currency.Trim().ToUpperInvariant();
            catalog.EffectiveFrom = dto.EffectiveFrom;
            catalog.EffectiveTo = dto.EffectiveTo;
            catalog.IsApproved = dto.IsApproved;
            catalog.TechnicalRemarks = dto.TechnicalRemarks?.Trim();

            await _db.SaveChangesAsync(cancellationToken);

            return VendorMapper.ToCatalogDto(catalog);
        }

        public async Task<bool> DeleteCatalogItemAsync(
            int vendorId,
            int catalogId,
            string actingUser,
            CancellationToken cancellationToken = default)
        {
            var catalog = await _db.VendorItemCatalogs
                .FirstOrDefaultAsync(c => c.Id == catalogId && c.VendorId == vendorId, cancellationToken);

            if (catalog is null) return false;

            _db.VendorItemCatalogs.Remove(catalog);
            await _db.SaveChangesAsync(cancellationToken);
            return true;
        }

        public async Task<IReadOnlyList<VendorItemCatalogDto>> GetApprovedVendorsForItemAsync(
            string itemCode,
            CancellationToken cancellationToken = default)
        {
            var today = DateOnly.FromDateTime(DateTime.UtcNow);
            var normalizedCode = itemCode.Trim().ToUpperInvariant();

            var catalogs = await _db.VendorItemCatalogs
                .Include(c => c.Vendor)
                .AsNoTracking()
                .Where(c => c.ItemCode == normalizedCode &&
                            c.IsApproved &&
                            c.EffectiveFrom <= today &&
                            (c.EffectiveTo == null || c.EffectiveTo >= today) &&
                            c.Vendor != null &&
                            !c.Vendor.IsDeleted &&
                            c.Vendor.Status == VendorStatus.Active &&
                            c.Vendor.IsKycVerified)
                .OrderBy(c => c.UnitPriceSnapshot)
                .ToListAsync(cancellationToken);

            return catalogs.Select(VendorMapper.ToCatalogDto).ToList();
        }

        public async Task<SourcingGateValidationResultDto> ValidateItemSourcingGateAsync(
            string tenantId,
            int vendorId,
            string itemCode,
            decimal orderQuantity,
            CancellationToken cancellationToken = default)
        {
            var vendor = await _db.Vendors
                .AsNoTracking()
                .FirstOrDefaultAsync(v => v.Id == vendorId && !v.IsDeleted, cancellationToken);

            if (vendor is null)
            {
                return new SourcingGateValidationResultDto
                {
                    IsAllowed = false,
                    Reason = $"Vendor with ID {vendorId} does not exist or has been deleted."
                };
            }

            if (vendor.Status != VendorStatus.Active)
            {
                return new SourcingGateValidationResultDto
                {
                    IsAllowed = false,
                    Reason = $"Vendor '{vendor.Name}' ({vendor.VendorCode}) is in '{vendor.Status}' status. POs can only be placed with 'Active' vendors."
                };
            }

            if (!vendor.IsKycVerified)
            {
                return new SourcingGateValidationResultDto
                {
                    IsAllowed = false,
                    Reason = $"Vendor '{vendor.Name}' ({vendor.VendorCode}) is not KYC verified. Purchase Orders cannot be released."
                };
            }

            var today = DateOnly.FromDateTime(DateTime.UtcNow);
            var normalizedCode = itemCode.Trim().ToUpperInvariant();

            var catalog = await _db.VendorItemCatalogs
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.VendorId == vendorId &&
                                          c.ItemCode == normalizedCode &&
                                          c.IsApproved &&
                                          c.EffectiveFrom <= today &&
                                          (c.EffectiveTo == null || c.EffectiveTo >= today),
                                     cancellationToken);

            if (catalog is null)
            {
                return new SourcingGateValidationResultDto
                {
                    IsAllowed = false,
                    Reason = $"Item '{itemCode}' has restricted sourcing and is NOT in the Approved Vendor List (AVL) for vendor '{vendor.Name}'."
                };
            }

            if (orderQuantity < catalog.MinimumOrderQuantity)
            {
                return new SourcingGateValidationResultDto
                {
                    IsAllowed = false,
                    Reason = $"Requested quantity {orderQuantity:N2} is below the Approved Vendor MOQ of {catalog.MinimumOrderQuantity:N2} {catalog.PackagingUnit}."
                };
            }

            if (catalog.OrderMultiple > 0)
            {
                var remainder = orderQuantity % catalog.OrderMultiple;
                if (Math.Abs((double)remainder) > 0.0001 && Math.Abs((double)(catalog.OrderMultiple - remainder)) > 0.0001)
                {
                    return new SourcingGateValidationResultDto
                    {
                        IsAllowed = false,
                        Reason = $"Requested quantity {orderQuantity:N2} must be in exact packaging multiples of {catalog.OrderMultiple:N2} {catalog.PackagingUnit}."
                    };
                }
            }

            return new SourcingGateValidationResultDto
            {
                IsAllowed = true,
                Reason = "Item AVL sourcing gate passed successfully.",
                CatalogItem = VendorMapper.ToCatalogDto(catalog)
            };
        }
    }
}

