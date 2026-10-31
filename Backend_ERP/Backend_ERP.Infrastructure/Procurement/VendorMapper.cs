using System;
using System.Linq;
using ERP.Application.Procurement.Dtos;
using ERP.Domain.Procurement;

namespace ERP.Infrastructure.Procurement
{
    public static class VendorMapper
    {
        public static VendorListItemDto ToListItemDto(Vendor vendor)
        {
            return new VendorListItemDto
            {
                Id = vendor.Id,
                TenantId = vendor.TenantId,
                Code = vendor.VendorCode,
                Name = vendor.Name,
                LegalName = vendor.LegalName,
                TradeName = vendor.TradeName,
                DisplayName = vendor.DisplayName,
                VendorType = vendor.VendorType.ToString(),
                GSTIN = vendor.GSTIN,
                PAN = vendor.PAN,
                IsMsmeRegistered = vendor.IsMsmeRegistered,
                MsmeRegistrationNumber = vendor.MsmeRegistrationNumber,
                MsmeCategory = vendor.MsmeCategory.ToString(),
                Email = vendor.Email,
                Phone = vendor.Phone,
                Status = vendor.Status,
                PaymentTermName = vendor.PaymentTerm?.Name ?? string.Empty,
                PaymentTermsDays = vendor.PaymentTermsDays,
                Currency = vendor.Currency,
                CreditLimit = vendor.CreditLimit,
                TotalOpenCommitment = vendor.TotalOpenCommitment,
                RatingScore = vendor.RatingScore,
                IsKycVerified = vendor.IsKycVerified,
                ApprovalToken = vendor.ApprovalToken,
                CreatedAt = vendor.CreatedAt,
                CreatedBy = vendor.CreatedBy,
                ConcurrencyVersion = vendor.ConcurrencyVersion
            };
        }

        public static VendorDto ToDto(Vendor vendor)
        {
            return new VendorDto
            {
                Id = vendor.Id,
                TenantId = vendor.TenantId,
                Code = vendor.VendorCode,
                Name = vendor.Name,
                LegalName = vendor.LegalName,
                TradeName = vendor.TradeName,
                DisplayName = vendor.DisplayName,
                VendorType = vendor.VendorType.ToString(),
                GSTIN = vendor.GSTIN,
                PAN = vendor.PAN,
                Tan = vendor.Tan,
                Cin = vendor.Cin,
                TaxIdentificationNumber = vendor.TaxIdentificationNumber,
                IsMsmeRegistered = vendor.IsMsmeRegistered,
                MsmeRegistrationNumber = vendor.MsmeRegistrationNumber,
                MsmeCategory = vendor.MsmeCategory.ToString(),
                Email = vendor.Email,
                Phone = vendor.Phone,
                AlternatePhone = vendor.AlternatePhone,
                Website = vendor.Website,
                PaymentTermId = vendor.PaymentTermId,
                PaymentTerm = vendor.PaymentTerm is null ? null : ToPaymentTermDto(vendor.PaymentTerm),
                PaymentTermsDays = vendor.PaymentTermsDays,
                Currency = vendor.Currency,
                CreditLimit = vendor.CreditLimit,
                TotalOpenCommitment = vendor.TotalOpenCommitment,
                Incoterms = vendor.Incoterms,
                RatingScore = vendor.RatingScore,
                Status = vendor.Status,
                IsKycVerified = vendor.IsKycVerified,
                ApprovalToken = vendor.ApprovalToken,
                Notes = vendor.Notes,
                CreatedAt = vendor.CreatedAt,
                CreatedBy = vendor.CreatedBy,
                UpdatedAt = vendor.UpdatedAt,
                UpdatedBy = vendor.UpdatedBy,
                ConcurrencyVersion = vendor.ConcurrencyVersion,
                Contacts = vendor.Contacts?.Select(ToContactDto).ToList() ?? new(),
                Addresses = vendor.Addresses?.Select(ToAddressDto).ToList() ?? new(),
                BankAccounts = vendor.BankAccounts?.Select(ToBankAccountDto).ToList() ?? new(),
                ItemCatalogs = vendor.ItemCatalogs?.Select(ToCatalogDto).ToList() ?? new(),
                PerformanceScores = vendor.PerformanceScores?.Select(ToPerformanceScoreDto).ToList() ?? new(),
                ApprovalHistories = vendor.ApprovalHistories?.Select(ToApprovalHistoryDto).ToList() ?? new(),
                Compliance = vendor.Compliance is null ? null : ToComplianceDto(vendor.Compliance),
                StatusHistory = vendor.StatusHistory?.Select(ToStatusHistoryDto).ToList() ?? new()
            };
        }

        public static VendorContactDto ToContactDto(VendorContact contact)
        {
            return new VendorContactDto
            {
                Id = contact.Id,
                VendorId = contact.VendorId,
                FirstName = contact.FirstName,
                LastName = contact.LastName,
                Department = contact.Department,
                Designation = contact.Designation,
                Email = contact.Email,
                Phone = contact.Phone,
                AlternatePhone = contact.AlternatePhone,
                Mobile = contact.Mobile,
                IsPrimary = contact.IsPrimary,
                IsActive = contact.IsActive
            };
        }

        public static VendorAddressDto ToAddressDto(VendorAddress address)
        {
            return new VendorAddressDto
            {
                Id = address.Id,
                VendorId = address.VendorId,
                AddressType = address.AddressType,
                AddressLine1 = address.AddressLine1,
                AddressLine2 = address.AddressLine2,
                City = address.City,
                State = address.State,
                PostalCode = address.PostalCode,
                Country = address.Country,
                IsPrimary = address.IsPrimary,
                IsActive = address.IsActive
            };
        }

        public static VendorBankAccountDto ToBankAccountDto(VendorBankAccount bank)
        {
            return new VendorBankAccountDto
            {
                Id = bank.Id,
                VendorId = bank.VendorId,
                AccountNumber = bank.AccountNumber,
                IfscCode = bank.IfscCode,
                BankName = bank.BankName,
                Branch = bank.Branch,
                BeneficiaryName = bank.BeneficiaryName,
                AccountType = bank.AccountType,
                IsPrimary = bank.IsPrimary,
                VerificationStatus = bank.VerificationStatus.ToString(),
                CancelledChequeUrl = bank.CancelledChequeUrl,
                VerifiedAt = bank.VerifiedAt,
                VerifiedBy = bank.VerifiedBy
            };
        }

        public static VendorItemCatalogDto ToCatalogDto(VendorItemCatalog catalog)
        {
            return new VendorItemCatalogDto
            {
                Id = catalog.Id,
                VendorId = catalog.VendorId,
                ItemType = catalog.ItemType,
                ItemId = catalog.ItemId,
                ItemCode = catalog.ItemCode,
                SupplierPartNumber = catalog.SupplierPartNumber,
                StandardLeadTimeDays = catalog.StandardLeadTimeDays,
                MinimumOrderQuantity = catalog.MinimumOrderQuantity,
                OrderMultiple = catalog.OrderMultiple,
                PackagingUnit = catalog.PackagingUnit,
                BaseUomConversionMultiplier = catalog.BaseUomConversionMultiplier,
                UnitPriceSnapshot = catalog.UnitPriceSnapshot,
                Currency = catalog.Currency,
                EffectiveFrom = catalog.EffectiveFrom,
                EffectiveTo = catalog.EffectiveTo,
                IsApproved = catalog.IsApproved,
                TechnicalRemarks = catalog.TechnicalRemarks
            };
        }

        public static VendorPerformanceScoreDto ToPerformanceScoreDto(VendorPerformanceScore score)
        {
            return new VendorPerformanceScoreDto
            {
                Id = score.Id,
                VendorId = score.VendorId,
                PeriodKey = score.PeriodKey,
                FiscalYear = score.FiscalYear,
                Month = score.Month,
                OtifPercentage = score.OtifPercentage,
                QualityPassRate = score.QualityPassRate,
                PriceVarianceScore = score.PriceVarianceScore,
                ComplianceScore = score.ComplianceScore,
                BlendedRatingScore = score.BlendedRatingScore,
                TotalLotsReceived = score.TotalLotsReceived,
                TotalLotsRejected = score.TotalLotsRejected,
                DefectPartsPerMillion = score.DefectPartsPerMillion,
                CalculatedAt = score.CalculatedAt
            };
        }

        public static VendorApprovalHistoryDto ToApprovalHistoryDto(VendorApprovalHistory history)
        {
            return new VendorApprovalHistoryDto
            {
                Id = history.Id,
                VendorId = history.VendorId,
                PreviousStatus = history.PreviousStatus.ToString(),
                NewStatus = history.NewStatus.ToString(),
                Action = history.Action,
                Comments = history.Comments,
                ActionedByUserId = history.ActionedByUserId,
                ActionedByUserName = history.ActionedByUserName,
                ApprovalToken = history.ApprovalToken,
                ActionedAt = history.ActionedAt
            };
        }

        public static VendorComplianceDto ToComplianceDto(VendorCompliance compliance)
        {
            return new VendorComplianceDto
            {
                Id = compliance.Id,
                VendorId = compliance.VendorId,
                GSTIN = compliance.GSTIN,
                PAN = compliance.PAN,
                TaxIdentificationNumber = compliance.TaxIdentificationNumber,
                GSTRegistered = compliance.GSTRegistered,
                MSMERegistered = compliance.MSMERegistered,
                MSMENumber = compliance.MSMENumber,
                CertificateNumber = compliance.CertificateNumber,
                CertificateExpiryDate = compliance.CertificateExpiryDate,
                ComplianceStatus = compliance.ComplianceStatus,
                Remarks = compliance.Remarks
            };
        }

        public static VendorPaymentTermDto ToPaymentTermDto(VendorPaymentTerm term)
        {
            return new VendorPaymentTermDto
            {
                Id = term.Id,
                Code = term.Code,
                Name = term.Name,
                Description = term.Description,
                Days = term.Days,
                AdvancePercentage = term.AdvancePercentage,
                IsActive = term.IsActive
            };
        }

        public static VendorStatusHistoryDto ToStatusHistoryDto(VendorStatusHistory history)
        {
            return new VendorStatusHistoryDto
            {
                Id = history.Id,
                VendorId = history.VendorId,
                Status = history.Status,
                PreviousStatus = history.PreviousStatus,
                User = history.User,
                Remarks = history.Remarks,
                Date = history.Date
            };
        }
    }
}
