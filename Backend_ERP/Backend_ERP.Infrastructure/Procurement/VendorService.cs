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
    public class VendorService : IVendorService
    {
        private readonly ERPDbContext _db;
        private readonly VendorNumberingService _numberingService;

        public VendorService(ERPDbContext db, VendorNumberingService numberingService)
        {
            _db = db;
            _numberingService = numberingService;
        }

        public async Task<PagedResult<VendorListItemDto>> GetAllAsync(
            VendorListQueryDto query,
            CancellationToken cancellationToken = default)
        {
            var q = _db.Vendors
                .Include(v => v.PaymentTerm)
                .AsNoTracking()
                .Where(v => !v.IsDeleted);

            if (!string.IsNullOrWhiteSpace(query.Search))
            {
                var term = query.Search.Trim().ToLower();
                q = q.Where(v =>
                    v.VendorCode.ToLower().Contains(term) ||
                    v.Name.ToLower().Contains(term) ||
                    v.LegalName.ToLower().Contains(term) ||
                    v.GSTIN.ToLower().Contains(term) ||
                    v.PAN.ToLower().Contains(term) ||
                    v.Email.ToLower().Contains(term) ||
                    v.Phone.ToLower().Contains(term));
            }

            if (!string.IsNullOrWhiteSpace(query.Code))
            {
                q = q.Where(v => v.VendorCode.ToLower() == query.Code.Trim().ToLower());
            }

            if (!string.IsNullOrWhiteSpace(query.GSTIN))
            {
                q = q.Where(v => v.GSTIN.ToLower() == query.GSTIN.Trim().ToLower());
            }

            if (!string.IsNullOrWhiteSpace(query.PAN))
            {
                q = q.Where(v => v.PAN.ToLower() == query.PAN.Trim().ToLower());
            }

            if (!string.IsNullOrWhiteSpace(query.Email))
            {
                q = q.Where(v => v.Email.ToLower().Contains(query.Email.Trim().ToLower()));
            }

            if (!string.IsNullOrWhiteSpace(query.Phone))
            {
                q = q.Where(v => v.Phone.ToLower().Contains(query.Phone.Trim().ToLower()));
            }

            if (!string.IsNullOrWhiteSpace(query.Status) && Enum.TryParse<VendorStatus>(query.Status, true, out var statusEnum))
            {
                q = q.Where(v => v.Status == statusEnum);
            }

            // Sorting
            q = (query.SortBy?.ToLowerInvariant(), query.SortDescending) switch
            {
                ("code" or "vendorcode", true) => q.OrderByDescending(v => v.VendorCode),
                ("code" or "vendorcode", false) => q.OrderBy(v => v.VendorCode),
                ("name" or "vendorname", true) => q.OrderByDescending(v => v.Name),
                ("name" or "vendorname", false) => q.OrderBy(v => v.Name),
                ("gstin" or "gstnumber", true) => q.OrderByDescending(v => v.GSTIN),
                ("gstin" or "gstnumber", false) => q.OrderBy(v => v.GSTIN),
                ("status", true) => q.OrderByDescending(v => v.Status),
                ("status", false) => q.OrderBy(v => v.Status),
                _ => query.SortDescending ? q.OrderByDescending(v => v.CreatedAt) : q.OrderBy(v => v.CreatedAt)
            };

            var totalCount = await q.CountAsync(cancellationToken);
            var page = Math.Max(1, query.Page);
            var pageSize = Math.Clamp(query.PageSize, 1, 1000);

            var items = await q
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(cancellationToken);

            var dtos = items.Select(VendorMapper.ToListItemDto).ToList();
            return PagedResult<VendorListItemDto>.Create(dtos, totalCount, page, pageSize);
        }

        public async Task<VendorDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
        {
            var vendor = await _db.Vendors
                .Include(v => v.PaymentTerm)
                .Include(v => v.Contacts)
                .Include(v => v.Addresses)
                .Include(v => v.Compliance)
                .Include(v => v.StatusHistory.OrderByDescending(h => h.Date))
                .AsNoTracking()
                .FirstOrDefaultAsync(v => v.Id == id && !v.IsDeleted, cancellationToken);

            return vendor is null ? null : VendorMapper.ToDto(vendor);
        }

        public async Task<VendorDto> CreateAsync(
            VendorCreateRequestDto request,
            string actingUser,
            CancellationToken cancellationToken = default)
        {
            VendorRules.ValidateVendorCreate(
                request.Name,
                request.Email,
                request.Phone,
                request.GSTIN,
                request.PAN,
                request.CreditLimit);

            var now = DateTime.UtcNow;
            var vendorCode = await _numberingService.NextVendorCodeAsync(cancellationToken);

            var vendor = new Vendor
            {
                VendorCode = vendorCode,
                Name = request.Name.Trim(),
                LegalName = request.LegalName?.Trim() ?? string.Empty,
                DisplayName = request.DisplayName?.Trim() ?? request.Name.Trim(),
                GSTIN = request.GSTIN?.Trim().ToUpperInvariant() ?? string.Empty,
                PAN = request.PAN?.Trim().ToUpperInvariant() ?? string.Empty,
                TaxIdentificationNumber = request.TaxIdentificationNumber?.Trim() ?? string.Empty,
                Email = request.Email?.Trim().ToLowerInvariant() ?? string.Empty,
                Phone = request.Phone?.Trim() ?? string.Empty,
                AlternatePhone = request.AlternatePhone?.Trim() ?? string.Empty,
                Website = request.Website?.Trim() ?? string.Empty,
                PaymentTermId = request.PaymentTermId,
                CreditLimit = request.CreditLimit,
                Status = request.Status,
                Notes = request.Notes?.Trim() ?? string.Empty,
                CreatedAt = now,
                CreatedBy = actingUser,
                UpdatedAt = now,
                UpdatedBy = actingUser
            };

            foreach (var c in request.Contacts ?? new List<VendorContactDto>())
            {
                vendor.Contacts.Add(new VendorContact
                {
                    FirstName = c.FirstName.Trim(),
                    LastName = c.LastName?.Trim() ?? string.Empty,
                    Designation = c.Designation?.Trim() ?? string.Empty,
                    Email = c.Email?.Trim() ?? string.Empty,
                    Phone = c.Phone?.Trim() ?? string.Empty,
                    AlternatePhone = c.AlternatePhone?.Trim() ?? string.Empty,
                    IsPrimary = c.IsPrimary,
                    IsActive = c.IsActive
                });
            }

            foreach (var a in request.Addresses ?? new List<VendorAddressDto>())
            {
                vendor.Addresses.Add(new VendorAddress
                {
                    AddressType = a.AddressType,
                    AddressLine1 = a.AddressLine1.Trim(),
                    AddressLine2 = a.AddressLine2?.Trim() ?? string.Empty,
                    City = a.City?.Trim() ?? string.Empty,
                    State = a.State?.Trim() ?? string.Empty,
                    PostalCode = a.PostalCode?.Trim() ?? string.Empty,
                    Country = string.IsNullOrWhiteSpace(a.Country) ? "India" : a.Country.Trim(),
                    IsPrimary = a.IsPrimary,
                    IsActive = a.IsActive
                });
            }

            if (request.Compliance != null)
            {
                vendor.Compliance = new VendorCompliance
                {
                    GSTIN = request.Compliance.GSTIN?.Trim().ToUpperInvariant() ?? string.Empty,
                    PAN = request.Compliance.PAN?.Trim().ToUpperInvariant() ?? string.Empty,
                    TaxIdentificationNumber = request.Compliance.TaxIdentificationNumber?.Trim() ?? string.Empty,
                    GSTRegistered = request.Compliance.GSTRegistered,
                    MSMERegistered = request.Compliance.MSMERegistered,
                    MSMENumber = request.Compliance.MSMENumber?.Trim() ?? string.Empty,
                    CertificateNumber = request.Compliance.CertificateNumber?.Trim() ?? string.Empty,
                    CertificateExpiryDate = request.Compliance.CertificateExpiryDate,
                    ComplianceStatus = request.Compliance.ComplianceStatus,
                    Remarks = request.Compliance.Remarks?.Trim() ?? string.Empty
                };
            }

            vendor.StatusHistory.Add(new VendorStatusHistory
            {
                Status = request.Status,
                PreviousStatus = null,
                User = actingUser,
                Remarks = "Vendor created.",
                Date = now
            });

            _db.Vendors.Add(vendor);
            await _db.SaveChangesAsync(cancellationToken);

            return (await GetByIdAsync(vendor.Id, cancellationToken))!;
        }

        public async Task<VendorDto?> UpdateAsync(
            int id,
            VendorUpdateRequestDto request,
            string actingUser,
            CancellationToken cancellationToken = default)
        {
            var vendor = await _db.Vendors
                .Include(v => v.Contacts)
                .Include(v => v.Addresses)
                .Include(v => v.Compliance)
                .FirstOrDefaultAsync(v => v.Id == id && !v.IsDeleted, cancellationToken);

            if (vendor is null) return null;

            VendorRules.ValidateVendorCreate(
                request.Name,
                request.Email,
                request.Phone,
                request.GSTIN,
                request.PAN,
                request.CreditLimit);

            var now = DateTime.UtcNow;

            vendor.Name = request.Name.Trim();
            vendor.LegalName = request.LegalName?.Trim() ?? string.Empty;
            vendor.DisplayName = request.DisplayName?.Trim() ?? request.Name.Trim();
            vendor.GSTIN = request.GSTIN?.Trim().ToUpperInvariant() ?? string.Empty;
            vendor.PAN = request.PAN?.Trim().ToUpperInvariant() ?? string.Empty;
            vendor.TaxIdentificationNumber = request.TaxIdentificationNumber?.Trim() ?? string.Empty;
            vendor.Email = request.Email?.Trim().ToLowerInvariant() ?? string.Empty;
            vendor.Phone = request.Phone?.Trim() ?? string.Empty;
            vendor.AlternatePhone = request.AlternatePhone?.Trim() ?? string.Empty;
            vendor.Website = request.Website?.Trim() ?? string.Empty;
            vendor.PaymentTermId = request.PaymentTermId;
            vendor.CreditLimit = request.CreditLimit;
            vendor.Notes = request.Notes?.Trim() ?? string.Empty;
            vendor.UpdatedAt = now;
            vendor.UpdatedBy = actingUser;

            _db.VendorContacts.RemoveRange(vendor.Contacts);
            vendor.Contacts.Clear();
            foreach (var c in request.Contacts ?? new List<VendorContactDto>())
            {
                vendor.Contacts.Add(new VendorContact
                {
                    VendorId = id,
                    FirstName = c.FirstName.Trim(),
                    LastName = c.LastName?.Trim() ?? string.Empty,
                    Designation = c.Designation?.Trim() ?? string.Empty,
                    Email = c.Email?.Trim() ?? string.Empty,
                    Phone = c.Phone?.Trim() ?? string.Empty,
                    AlternatePhone = c.AlternatePhone?.Trim() ?? string.Empty,
                    IsPrimary = c.IsPrimary,
                    IsActive = c.IsActive
                });
            }

            _db.VendorAddresses.RemoveRange(vendor.Addresses);
            vendor.Addresses.Clear();
            foreach (var a in request.Addresses ?? new List<VendorAddressDto>())
            {
                vendor.Addresses.Add(new VendorAddress
                {
                    VendorId = id,
                    AddressType = a.AddressType,
                    AddressLine1 = a.AddressLine1.Trim(),
                    AddressLine2 = a.AddressLine2?.Trim() ?? string.Empty,
                    City = a.City?.Trim() ?? string.Empty,
                    State = a.State?.Trim() ?? string.Empty,
                    PostalCode = a.PostalCode?.Trim() ?? string.Empty,
                    Country = string.IsNullOrWhiteSpace(a.Country) ? "India" : a.Country.Trim(),
                    IsPrimary = a.IsPrimary,
                    IsActive = a.IsActive
                });
            }

            if (request.Compliance != null)
            {
                if (vendor.Compliance is null)
                {
                    vendor.Compliance = new VendorCompliance { VendorId = id };
                }

                vendor.Compliance.GSTIN = request.Compliance.GSTIN?.Trim().ToUpperInvariant() ?? string.Empty;
                vendor.Compliance.PAN = request.Compliance.PAN?.Trim().ToUpperInvariant() ?? string.Empty;
                vendor.Compliance.TaxIdentificationNumber = request.Compliance.TaxIdentificationNumber?.Trim() ?? string.Empty;
                vendor.Compliance.GSTRegistered = request.Compliance.GSTRegistered;
                vendor.Compliance.MSMERegistered = request.Compliance.MSMERegistered;
                vendor.Compliance.MSMENumber = request.Compliance.MSMENumber?.Trim() ?? string.Empty;
                vendor.Compliance.CertificateNumber = request.Compliance.CertificateNumber?.Trim() ?? string.Empty;
                vendor.Compliance.CertificateExpiryDate = request.Compliance.CertificateExpiryDate;
                vendor.Compliance.ComplianceStatus = request.Compliance.ComplianceStatus;
                vendor.Compliance.Remarks = request.Compliance.Remarks?.Trim() ?? string.Empty;
            }

            await _db.SaveChangesAsync(cancellationToken);
            return (await GetByIdAsync(id, cancellationToken))!;
        }

        public async Task<bool> DeleteAsync(int id, string actingUser, CancellationToken cancellationToken = default)
        {
            var vendor = await _db.Vendors.FirstOrDefaultAsync(v => v.Id == id && !v.IsDeleted, cancellationToken);
            if (vendor is null) return false;

            vendor.IsDeleted = true;
            vendor.UpdatedAt = DateTime.UtcNow;
            vendor.UpdatedBy = actingUser;

            await _db.SaveChangesAsync(cancellationToken);
            return true;
        }

        public async Task<VendorDto?> ActivateAsync(int id, string remarks, string actingUser, CancellationToken cancellationToken = default)
        {
            return await ChangeStatusAsync(id, VendorStatus.Active, remarks, actingUser, cancellationToken);
        }

        public async Task<VendorDto?> DeactivateAsync(int id, string remarks, string actingUser, CancellationToken cancellationToken = default)
        {
            return await ChangeStatusAsync(id, VendorStatus.Inactive, remarks, actingUser, cancellationToken);
        }

        public async Task<VendorDto?> UpdateStatusAsync(int id, VendorStatusUpdateRequestDto request, string actingUser, CancellationToken cancellationToken = default)
        {
            return await ChangeStatusAsync(id, request.TargetStatus, request.Remarks, actingUser, cancellationToken);
        }

        public async Task<string> GetNextVendorCodeAsync(CancellationToken cancellationToken = default)
        {
            return await _numberingService.NextVendorCodeAsync(cancellationToken);
        }

        public async Task<VendorPerformanceSummaryDto?> GetPerformanceSummaryAsync(int id, CancellationToken cancellationToken = default)
        {
            var vendor = await _db.Vendors.AsNoTracking().FirstOrDefaultAsync(v => v.Id == id && !v.IsDeleted, cancellationToken);
            if (vendor is null) return null;

            return new VendorPerformanceSummaryDto
            {
                VendorId = vendor.Id,
                VendorCode = vendor.VendorCode,
                VendorName = vendor.Name
            };
        }

        public Task<IReadOnlyList<string>> GetPermissionsAsync()
        {
            IReadOnlyList<string> permissions = new List<string>
            {
                "vendors.view",
                "vendors.create",
                "vendors.update",
                "vendors.delete",
                "vendors.workflow",
                "vendors.export"
            };

            return Task.FromResult(permissions);
        }

        private async Task<VendorDto?> ChangeStatusAsync(
            int id,
            VendorStatus targetStatus,
            string remarks,
            string actingUser,
            CancellationToken cancellationToken = default)
        {
            var vendor = await _db.Vendors
                .Include(v => v.StatusHistory)
                .FirstOrDefaultAsync(v => v.Id == id && !v.IsDeleted, cancellationToken);

            if (vendor is null) return null;

            if (!VendorRules.CanTransition(vendor.Status, targetStatus))
            {
                throw new InvalidOperationException($"Cannot transition vendor status from {vendor.Status} to {targetStatus}.");
            }

            var previousStatus = vendor.Status;
            var now = DateTime.UtcNow;

            vendor.Status = targetStatus;
            vendor.UpdatedAt = now;
            vendor.UpdatedBy = actingUser;

            vendor.StatusHistory.Add(new VendorStatusHistory
            {
                VendorId = id,
                Status = targetStatus,
                PreviousStatus = previousStatus,
                User = actingUser,
                Remarks = string.IsNullOrWhiteSpace(remarks) ? $"Status changed to {targetStatus}." : remarks.Trim(),
                Date = now
            });

            await _db.SaveChangesAsync(cancellationToken);
            return (await GetByIdAsync(id, cancellationToken))!;
        }
    }
}
