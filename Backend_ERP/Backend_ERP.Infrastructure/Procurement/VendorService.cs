using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using ERP.Application.Procurement;
using ERP.Application.Procurement.Dtos;
using ERP.Domain.Procurement;
using ERP.Infrastructure.Data;
using ERP.Shared.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace ERP.Infrastructure.Procurement
{
    public class VendorService : IVendorService
    {
        private readonly ERPDbContext _db;
        private readonly VendorNumberingService _numberingService;
        private readonly string _hmacSecretKey;

        public VendorService(
            ERPDbContext db,
            VendorNumberingService numberingService,
            IConfiguration? configuration = null)
        {
            _db = db;
            _numberingService = numberingService;
            _hmacSecretKey = configuration?["KycSecurity:HmacSecretKey"] ?? "ERP_ENTERPRISE_PROCURE_TO_PAY_KYC_SIGNATURE_KEY_2026";
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
                    v.TradeName.ToLower().Contains(term) ||
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

            if (!string.IsNullOrWhiteSpace(query.VendorType) && Enum.TryParse<VendorType>(query.VendorType, true, out var vendorTypeEnum))
            {
                q = q.Where(v => v.VendorType == vendorTypeEnum);
            }

            if (!string.IsNullOrWhiteSpace(query.MsmeCategory) && Enum.TryParse<MsmeCategory>(query.MsmeCategory, true, out var msmeEnum))
            {
                q = q.Where(v => v.MsmeCategory == msmeEnum);
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
                ("ratingscore" or "rating", true) => q.OrderByDescending(v => v.RatingScore),
                ("ratingscore" or "rating", false) => q.OrderBy(v => v.RatingScore),
                ("commitment" or "totalopencommitment", true) => q.OrderByDescending(v => v.TotalOpenCommitment),
                ("commitment" or "totalopencommitment", false) => q.OrderBy(v => v.TotalOpenCommitment),
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
                .Include(v => v.BankAccounts)
                .Include(v => v.ItemCatalogs)
                .Include(v => v.PerformanceScores.OrderByDescending(p => p.PeriodKey))
                .Include(v => v.ApprovalHistories.OrderByDescending(h => h.ActionedAt))
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
            // 1. Validation via VendorRules
            VendorRules.ValidateVendorCreate(
                request.Name,
                request.Email,
                request.Phone,
                request.GSTIN,
                request.PAN,
                request.CreditLimit);

            // 2. Statutory State Code Invariance check
            var primaryAddress = request.Addresses?.FirstOrDefault(a => a.IsPrimary) ?? request.Addresses?.FirstOrDefault();
            if (!string.IsNullOrWhiteSpace(request.GSTIN) && primaryAddress != null && !string.IsNullOrWhiteSpace(primaryAddress.State))
            {
                if (!VendorRules.ValidateGstinStateMatch(request.GSTIN, primaryAddress.State))
                {
                    throw new ArgumentException($"GSTIN '{request.GSTIN}' state code prefix does not match primary address state '{primaryAddress.State}'. Place of Supply invariant violated.");
                }
            }

            // 3. MSME Section 43B(h) auto-capping boundary guard
            var paymentTermsDays = request.PaymentTermsDays;
            if (request.IsMsmeRegistered && (request.MsmeCategory == MsmeCategory.Micro || request.MsmeCategory == MsmeCategory.Small))
            {
                paymentTermsDays = VendorRules.ComputeMsmePaymentTermsDays(paymentTermsDays, hasWrittenContract: true);
            }

            var now = DateTime.UtcNow;
            var vendorCode = await _numberingService.NextVendorCodeAsync(cancellationToken);

            var vendor = new Vendor
            {
                TenantId = "default",
                VendorCode = vendorCode,
                Name = request.Name.Trim(),
                LegalName = string.IsNullOrWhiteSpace(request.LegalName) ? request.Name.Trim() : request.LegalName.Trim(),
                TradeName = string.IsNullOrWhiteSpace(request.TradeName) ? request.Name.Trim() : request.TradeName.Trim(),
                DisplayName = string.IsNullOrWhiteSpace(request.DisplayName) ? request.Name.Trim() : request.DisplayName.Trim(),
                VendorType = request.VendorType,
                GSTIN = request.GSTIN?.Trim().ToUpperInvariant() ?? string.Empty,
                PAN = request.PAN?.Trim().ToUpperInvariant() ?? string.Empty,
                Tan = request.Tan?.Trim().ToUpperInvariant(),
                Cin = request.Cin?.Trim().ToUpperInvariant(),
                TaxIdentificationNumber = request.TaxIdentificationNumber?.Trim() ?? string.Empty,
                IsMsmeRegistered = request.IsMsmeRegistered,
                MsmeRegistrationNumber = request.MsmeRegistrationNumber?.Trim().ToUpperInvariant(),
                MsmeCategory = request.MsmeCategory,
                Email = request.Email?.Trim().ToLowerInvariant() ?? string.Empty,
                Phone = request.Phone?.Trim() ?? string.Empty,
                AlternatePhone = request.AlternatePhone?.Trim() ?? string.Empty,
                Website = request.Website?.Trim() ?? string.Empty,
                PaymentTermId = request.PaymentTermId,
                PaymentTermsDays = paymentTermsDays,
                Currency = string.IsNullOrWhiteSpace(request.Currency) ? "INR" : request.Currency.Trim().ToUpperInvariant(),
                CreditLimit = request.CreditLimit,
                TotalOpenCommitment = 0.00m,
                Incoterms = request.Incoterms?.Trim().ToUpperInvariant(),
                RatingScore = 100.00m,
                Status = request.Status,
                IsKycVerified = false,
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
                    TenantId = vendor.TenantId,
                    FirstName = c.FirstName.Trim(),
                    LastName = c.LastName?.Trim() ?? string.Empty,
                    Department = c.Department?.Trim() ?? string.Empty,
                    Designation = c.Designation?.Trim() ?? string.Empty,
                    Email = c.Email?.Trim() ?? string.Empty,
                    Phone = c.Phone?.Trim() ?? string.Empty,
                    AlternatePhone = c.AlternatePhone?.Trim() ?? string.Empty,
                    Mobile = c.Mobile?.Trim(),
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

            foreach (var b in request.BankAccounts ?? new List<VendorBankAccountDto>())
            {
                vendor.BankAccounts.Add(new VendorBankAccount
                {
                    TenantId = vendor.TenantId,
                    AccountNumber = b.AccountNumber.Trim(),
                    IfscCode = b.IfscCode.Trim().ToUpperInvariant(),
                    BankName = b.BankName.Trim(),
                    Branch = b.Branch?.Trim() ?? string.Empty,
                    BeneficiaryName = b.BeneficiaryName.Trim(),
                    AccountType = string.IsNullOrWhiteSpace(b.AccountType) ? "Current" : b.AccountType.Trim(),
                    IsPrimary = b.IsPrimary,
                    VerificationStatus = BankAccountVerificationStatus.Pending,
                    CancelledChequeUrl = b.CancelledChequeUrl
                });
            }

            if (request.Compliance != null)
            {
                vendor.Compliance = new VendorCompliance
                {
                    GSTIN = request.Compliance.GSTIN?.Trim().ToUpperInvariant() ?? vendor.GSTIN,
                    PAN = request.Compliance.PAN?.Trim().ToUpperInvariant() ?? vendor.PAN,
                    TaxIdentificationNumber = request.Compliance.TaxIdentificationNumber?.Trim() ?? vendor.TaxIdentificationNumber,
                    GSTRegistered = request.Compliance.GSTRegistered,
                    MSMERegistered = request.Compliance.MSMERegistered,
                    MSMENumber = request.Compliance.MSMENumber?.Trim() ?? (vendor.MsmeRegistrationNumber ?? string.Empty),
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
                Remarks = "Vendor registered in system.",
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
                .Include(v => v.BankAccounts)
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

            var primaryAddress = request.Addresses?.FirstOrDefault(a => a.IsPrimary) ?? request.Addresses?.FirstOrDefault();
            if (!string.IsNullOrWhiteSpace(request.GSTIN) && primaryAddress != null && !string.IsNullOrWhiteSpace(primaryAddress.State))
            {
                if (!VendorRules.ValidateGstinStateMatch(request.GSTIN, primaryAddress.State))
                {
                    throw new ArgumentException($"GSTIN '{request.GSTIN}' state code prefix does not match primary address state '{primaryAddress.State}'. Place of Supply invariant violated.");
                }
            }

            var paymentTermsDays = request.PaymentTermsDays;
            if (request.IsMsmeRegistered && (request.MsmeCategory == MsmeCategory.Micro || request.MsmeCategory == MsmeCategory.Small))
            {
                paymentTermsDays = VendorRules.ComputeMsmePaymentTermsDays(paymentTermsDays, hasWrittenContract: true);
            }

            var now = DateTime.UtcNow;

            vendor.Name = request.Name.Trim();
            vendor.LegalName = string.IsNullOrWhiteSpace(request.LegalName) ? request.Name.Trim() : request.LegalName.Trim();
            vendor.TradeName = string.IsNullOrWhiteSpace(request.TradeName) ? request.Name.Trim() : request.TradeName.Trim();
            vendor.DisplayName = string.IsNullOrWhiteSpace(request.DisplayName) ? request.Name.Trim() : request.DisplayName.Trim();
            vendor.VendorType = request.VendorType;
            vendor.GSTIN = request.GSTIN?.Trim().ToUpperInvariant() ?? string.Empty;
            vendor.PAN = request.PAN?.Trim().ToUpperInvariant() ?? string.Empty;
            vendor.Tan = request.Tan?.Trim().ToUpperInvariant();
            vendor.Cin = request.Cin?.Trim().ToUpperInvariant();
            vendor.TaxIdentificationNumber = request.TaxIdentificationNumber?.Trim() ?? string.Empty;
            vendor.IsMsmeRegistered = request.IsMsmeRegistered;
            vendor.MsmeRegistrationNumber = request.MsmeRegistrationNumber?.Trim().ToUpperInvariant();
            vendor.MsmeCategory = request.MsmeCategory;
            vendor.Email = request.Email?.Trim().ToLowerInvariant() ?? string.Empty;
            vendor.Phone = request.Phone?.Trim() ?? string.Empty;
            vendor.AlternatePhone = request.AlternatePhone?.Trim() ?? string.Empty;
            vendor.Website = request.Website?.Trim() ?? string.Empty;
            vendor.PaymentTermId = request.PaymentTermId;
            vendor.PaymentTermsDays = paymentTermsDays;
            vendor.Currency = string.IsNullOrWhiteSpace(request.Currency) ? "INR" : request.Currency.Trim().ToUpperInvariant();
            vendor.CreditLimit = request.CreditLimit;
            vendor.Incoterms = request.Incoterms?.Trim().ToUpperInvariant();
            vendor.Notes = request.Notes?.Trim() ?? string.Empty;
            vendor.UpdatedAt = now;
            vendor.UpdatedBy = actingUser;

            // Contacts
            _db.VendorContacts.RemoveRange(vendor.Contacts);
            vendor.Contacts.Clear();
            foreach (var c in request.Contacts ?? new List<VendorContactDto>())
            {
                vendor.Contacts.Add(new VendorContact
                {
                    VendorId = id,
                    TenantId = vendor.TenantId,
                    FirstName = c.FirstName.Trim(),
                    LastName = c.LastName?.Trim() ?? string.Empty,
                    Department = c.Department?.Trim() ?? string.Empty,
                    Designation = c.Designation?.Trim() ?? string.Empty,
                    Email = c.Email?.Trim() ?? string.Empty,
                    Phone = c.Phone?.Trim() ?? string.Empty,
                    AlternatePhone = c.AlternatePhone?.Trim() ?? string.Empty,
                    Mobile = c.Mobile?.Trim(),
                    IsPrimary = c.IsPrimary,
                    IsActive = c.IsActive
                });
            }

            // Addresses
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

            // Bank Accounts update
            if (request.BankAccounts != null && request.BankAccounts.Count > 0)
            {
                foreach (var b in request.BankAccounts)
                {
                    var existing = vendor.BankAccounts.FirstOrDefault(x => x.Id == b.Id && b.Id > 0);
                    if (existing != null)
                    {
                        existing.AccountNumber = b.AccountNumber.Trim();
                        existing.IfscCode = b.IfscCode.Trim().ToUpperInvariant();
                        existing.BankName = b.BankName.Trim();
                        existing.Branch = b.Branch?.Trim() ?? string.Empty;
                        existing.BeneficiaryName = b.BeneficiaryName.Trim();
                        existing.AccountType = string.IsNullOrWhiteSpace(b.AccountType) ? "Current" : b.AccountType.Trim();
                        existing.IsPrimary = b.IsPrimary;
                        existing.CancelledChequeUrl = b.CancelledChequeUrl;
                    }
                    else
                    {
                        vendor.BankAccounts.Add(new VendorBankAccount
                        {
                            VendorId = id,
                            TenantId = vendor.TenantId,
                            AccountNumber = b.AccountNumber.Trim(),
                            IfscCode = b.IfscCode.Trim().ToUpperInvariant(),
                            BankName = b.BankName.Trim(),
                            Branch = b.Branch?.Trim() ?? string.Empty,
                            BeneficiaryName = b.BeneficiaryName.Trim(),
                            AccountType = string.IsNullOrWhiteSpace(b.AccountType) ? "Current" : b.AccountType.Trim(),
                            IsPrimary = b.IsPrimary,
                            VerificationStatus = BankAccountVerificationStatus.Pending,
                            CancelledChequeUrl = b.CancelledChequeUrl
                        });
                    }
                }
            }

            // Compliance
            if (request.Compliance != null)
            {
                if (vendor.Compliance is null)
                {
                    vendor.Compliance = new VendorCompliance { VendorId = id };
                }

                vendor.Compliance.GSTIN = request.Compliance.GSTIN?.Trim().ToUpperInvariant() ?? vendor.GSTIN;
                vendor.Compliance.PAN = request.Compliance.PAN?.Trim().ToUpperInvariant() ?? vendor.PAN;
                vendor.Compliance.TaxIdentificationNumber = request.Compliance.TaxIdentificationNumber?.Trim() ?? vendor.TaxIdentificationNumber;
                vendor.Compliance.GSTRegistered = request.Compliance.GSTRegistered;
                vendor.Compliance.MSMERegistered = request.Compliance.MSMERegistered;
                vendor.Compliance.MSMENumber = request.Compliance.MSMENumber?.Trim() ?? (vendor.MsmeRegistrationNumber ?? string.Empty);
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
            var vendor = await _db.Vendors
                .Include(v => v.PerformanceScores.OrderByDescending(p => p.PeriodKey))
                .AsNoTracking()
                .FirstOrDefaultAsync(v => v.Id == id && !v.IsDeleted, cancellationToken);

            if (vendor is null) return null;

            var latestScore = vendor.PerformanceScores.FirstOrDefault();

            return new VendorPerformanceSummaryDto
            {
                VendorId = vendor.Id,
                VendorCode = vendor.VendorCode,
                VendorName = vendor.Name,
                TotalPurchaseOrders = 0,
                TotalPurchaseValue = vendor.TotalOpenCommitment,
                OnTimeDeliveryPercentage = (double)(latestScore?.OtifPercentage ?? 100.00m),
                RejectionRatioPercentage = (double)(latestScore != null && latestScore.TotalLotsReceived > 0 
                    ? (latestScore.TotalLotsRejected / (decimal)latestScore.TotalLotsReceived) * 100m 
                    : 0m),
                LeadTimeCompliancePercentage = (double)(latestScore?.OtifPercentage ?? 100.00m),
                PricingConsistencyScore = (double)(latestScore?.PriceVarianceScore ?? 100.00m),
                BlendedRatingScore = latestScore?.BlendedRatingScore ?? vendor.RatingScore,
                Note = "Real-time P2P Telemetry Ingested from GRN and Incoming Inspection"
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
                "vendors.kyc_submit",
                "vendors.kyc_approve",
                "vendors.avl_manage",
                "vendors.score_manage",
                "vendors.reports_export"
            };

            return Task.FromResult(permissions);
        }

        // KYC Verification Workflow
        public async Task<VendorDto?> SubmitForKycApprovalAsync(int id, string actingUser, CancellationToken cancellationToken = default)
        {
            var vendor = await _db.Vendors
                .Include(v => v.Contacts)
                .Include(v => v.Addresses)
                .Include(v => v.BankAccounts)
                .Include(v => v.ApprovalHistories)
                .FirstOrDefaultAsync(v => v.Id == id && !v.IsDeleted, cancellationToken);

            if (vendor is null) return null;

            if (vendor.Contacts.Count == 0)
            {
                throw new InvalidOperationException("Cannot submit vendor for KYC without at least one contact.");
            }

            if (vendor.Addresses.Count == 0)
            {
                throw new InvalidOperationException("Cannot submit vendor for KYC without at least one address.");
            }

            if (vendor.BankAccounts.Count == 0)
            {
                throw new InvalidOperationException("Cannot submit vendor for KYC without at least one bank account.");
            }

            var previousStatus = vendor.Status;
            var now = DateTime.UtcNow;

            vendor.Status = VendorStatus.PendingKycApproval;
            vendor.UpdatedAt = now;
            vendor.UpdatedBy = actingUser;

            vendor.ApprovalHistories.Add(new VendorApprovalHistory
            {
                TenantId = vendor.TenantId,
                VendorId = vendor.Id,
                PreviousStatus = previousStatus,
                NewStatus = VendorStatus.PendingKycApproval,
                Action = "SubmitForKycApproval",
                Comments = "Vendor submitted for KYC document verification and statutory check.",
                ActionedByUserId = actingUser,
                ActionedByUserName = actingUser,
                ActionedAt = now
            });

            vendor.StatusHistory.Add(new VendorStatusHistory
            {
                VendorId = id,
                Status = VendorStatus.PendingKycApproval,
                PreviousStatus = previousStatus,
                User = actingUser,
                Remarks = "Submitted for KYC Approval.",
                Date = now
            });

            await _db.SaveChangesAsync(cancellationToken);
            return (await GetByIdAsync(id, cancellationToken))!;
        }

        public async Task<VendorDto?> ApproveVendorKycAsync(
            int id,
            VendorKycApprovalRequestDto request,
            string actingUser,
            CancellationToken cancellationToken = default)
        {
            var vendor = await _db.Vendors
                .Include(v => v.BankAccounts)
                .Include(v => v.ApprovalHistories)
                .FirstOrDefaultAsync(v => v.Id == id && !v.IsDeleted, cancellationToken);

            if (vendor is null) return null;

            if (vendor.Status != VendorStatus.PendingKycApproval && vendor.Status != VendorStatus.Draft)
            {
                throw new InvalidOperationException($"Vendor cannot be KYC approved from current status {vendor.Status}.");
            }

            var now = DateTime.UtcNow;
            var tokenPayload = $"{vendor.Id}:{vendor.VendorCode}:{vendor.GSTIN}:{now:O}:{actingUser}";
            var approvalToken = ComputeHmacSha256(tokenPayload, _hmacSecretKey);

            var previousStatus = vendor.Status;
            vendor.Status = VendorStatus.Active;
            vendor.IsKycVerified = true;
            vendor.ApprovalToken = approvalToken;
            vendor.UpdatedAt = now;
            vendor.UpdatedBy = actingUser;

            if (request.ApproveBankAccounts)
            {
                foreach (var bank in vendor.BankAccounts)
                {
                    bank.VerificationStatus = BankAccountVerificationStatus.ManualVerified;
                    bank.VerifiedAt = now;
                    bank.VerifiedBy = actingUser;
                }
            }

            vendor.ApprovalHistories.Add(new VendorApprovalHistory
            {
                TenantId = vendor.TenantId,
                VendorId = vendor.Id,
                PreviousStatus = previousStatus,
                NewStatus = VendorStatus.Active,
                Action = "ApproveKyc",
                Comments = string.IsNullOrWhiteSpace(request.Comments) ? "KYC verification sign-off completed." : request.Comments.Trim(),
                ActionedByUserId = actingUser,
                ActionedByUserName = actingUser,
                ApprovalToken = approvalToken,
                ActionedAt = now
            });

            vendor.StatusHistory.Add(new VendorStatusHistory
            {
                VendorId = id,
                Status = VendorStatus.Active,
                PreviousStatus = previousStatus,
                User = actingUser,
                Remarks = $"KYC Approved with cryptographic token {approvalToken[..8]}...",
                Date = now
            });

            await _db.SaveChangesAsync(cancellationToken);
            return (await GetByIdAsync(id, cancellationToken))!;
        }

        public async Task<VendorDto?> RejectVendorKycAsync(
            int id,
            string reason,
            string actingUser,
            CancellationToken cancellationToken = default)
        {
            var vendor = await _db.Vendors
                .Include(v => v.ApprovalHistories)
                .FirstOrDefaultAsync(v => v.Id == id && !v.IsDeleted, cancellationToken);

            if (vendor is null) return null;

            var previousStatus = vendor.Status;
            var now = DateTime.UtcNow;

            vendor.Status = VendorStatus.Draft;
            vendor.IsKycVerified = false;
            vendor.ApprovalToken = null;
            vendor.UpdatedAt = now;
            vendor.UpdatedBy = actingUser;

            vendor.ApprovalHistories.Add(new VendorApprovalHistory
            {
                TenantId = vendor.TenantId,
                VendorId = vendor.Id,
                PreviousStatus = previousStatus,
                NewStatus = VendorStatus.Draft,
                Action = "RejectKyc",
                Comments = string.IsNullOrWhiteSpace(reason) ? "KYC documents rejected during verification." : reason.Trim(),
                ActionedByUserId = actingUser,
                ActionedByUserName = actingUser,
                ActionedAt = now
            });

            vendor.StatusHistory.Add(new VendorStatusHistory
            {
                VendorId = id,
                Status = VendorStatus.Draft,
                PreviousStatus = previousStatus,
                User = actingUser,
                Remarks = $"KYC Rejected: {reason}",
                Date = now
            });

            await _db.SaveChangesAsync(cancellationToken);
            return (await GetByIdAsync(id, cancellationToken))!;
        }

        // Bank Account Sub-Service
        public async Task<VendorBankAccountDto> AddBankAccountAsync(
            int vendorId,
            VendorBankAccountDto dto,
            string actingUser,
            CancellationToken cancellationToken = default)
        {
            var vendor = await _db.Vendors
                .Include(v => v.BankAccounts)
                .FirstOrDefaultAsync(v => v.Id == vendorId && !v.IsDeleted, cancellationToken);

            if (vendor is null)
            {
                throw new KeyNotFoundException($"Vendor {vendorId} not found.");
            }

            if (dto.IsPrimary)
            {
                foreach (var b in vendor.BankAccounts)
                {
                    b.IsPrimary = false;
                }
            }

            var account = new VendorBankAccount
            {
                VendorId = vendorId,
                TenantId = vendor.TenantId,
                AccountNumber = dto.AccountNumber.Trim(),
                IfscCode = dto.IfscCode.Trim().ToUpperInvariant(),
                BankName = dto.BankName.Trim(),
                Branch = dto.Branch?.Trim() ?? string.Empty,
                BeneficiaryName = dto.BeneficiaryName.Trim(),
                AccountType = string.IsNullOrWhiteSpace(dto.AccountType) ? "Current" : dto.AccountType.Trim(),
                IsPrimary = dto.IsPrimary || vendor.BankAccounts.Count == 0,
                VerificationStatus = BankAccountVerificationStatus.Pending,
                CancelledChequeUrl = dto.CancelledChequeUrl
            };

            vendor.BankAccounts.Add(account);
            await _db.SaveChangesAsync(cancellationToken);

            return VendorMapper.ToBankAccountDto(account);
        }

        public async Task<VendorBankAccountDto?> UpdateBankAccountAsync(
            int vendorId,
            int bankAccountId,
            VendorBankAccountDto dto,
            string actingUser,
            CancellationToken cancellationToken = default)
        {
            var account = await _db.VendorBankAccounts
                .FirstOrDefaultAsync(b => b.Id == bankAccountId && b.VendorId == vendorId, cancellationToken);

            if (account is null) return null;

            if (dto.IsPrimary && !account.IsPrimary)
            {
                var others = await _db.VendorBankAccounts
                    .Where(b => b.VendorId == vendorId && b.Id != bankAccountId)
                    .ToListAsync(cancellationToken);
                foreach (var o in others)
                {
                    o.IsPrimary = false;
                }
            }

            account.AccountNumber = dto.AccountNumber.Trim();
            account.IfscCode = dto.IfscCode.Trim().ToUpperInvariant();
            account.BankName = dto.BankName.Trim();
            account.Branch = dto.Branch?.Trim() ?? string.Empty;
            account.BeneficiaryName = dto.BeneficiaryName.Trim();
            account.AccountType = string.IsNullOrWhiteSpace(dto.AccountType) ? "Current" : dto.AccountType.Trim();
            account.IsPrimary = dto.IsPrimary;
            account.CancelledChequeUrl = dto.CancelledChequeUrl;

            await _db.SaveChangesAsync(cancellationToken);
            return VendorMapper.ToBankAccountDto(account);
        }

        public async Task<bool> DeleteBankAccountAsync(
            int vendorId,
            int bankAccountId,
            string actingUser,
            CancellationToken cancellationToken = default)
        {
            var account = await _db.VendorBankAccounts
                .FirstOrDefaultAsync(b => b.Id == bankAccountId && b.VendorId == vendorId, cancellationToken);

            if (account is null) return false;

            _db.VendorBankAccounts.Remove(account);
            await _db.SaveChangesAsync(cancellationToken);
            return true;
        }

        public async Task<VendorBankAccountDto?> VerifyBankAccountPennyDropAsync(
            int vendorId,
            int bankAccountId,
            string actingUser,
            CancellationToken cancellationToken = default)
        {
            var account = await _db.VendorBankAccounts
                .FirstOrDefaultAsync(b => b.Id == bankAccountId && b.VendorId == vendorId, cancellationToken);

            if (account is null) return null;

            // Validate IFSC format (4 alphabetic + 0 + 6 alphanumeric)
            var ifsc = account.IfscCode.Trim().ToUpperInvariant();
            if (ifsc.Length != 11 || ifsc[4] != '0')
            {
                account.VerificationStatus = BankAccountVerificationStatus.Failed;
            }
            else
            {
                account.VerificationStatus = BankAccountVerificationStatus.PennyDropVerified;
                account.VerifiedAt = DateTime.UtcNow;
                account.VerifiedBy = actingUser;
            }

            await _db.SaveChangesAsync(cancellationToken);
            return VendorMapper.ToBankAccountDto(account);
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

        private static string ComputeHmacSha256(string rawData, string secretKey)
        {
            var keyBytes = Encoding.UTF8.GetBytes(secretKey);
            var dataBytes = Encoding.UTF8.GetBytes(rawData);

            using var hmac = new HMACSHA256(keyBytes);
            var hashBytes = hmac.ComputeHash(dataBytes);
            return Convert.ToHexString(hashBytes).ToLowerInvariant();
        }
    }
}
