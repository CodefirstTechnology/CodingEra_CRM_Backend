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
                Code = vendor.VendorCode,
                Name = vendor.Name,
                LegalName = vendor.LegalName,
                DisplayName = vendor.DisplayName,
                GSTIN = vendor.GSTIN,
                PAN = vendor.PAN,
                Email = vendor.Email,
                Phone = vendor.Phone,
                Status = vendor.Status,
                PaymentTermName = vendor.PaymentTerm?.Name ?? string.Empty,
                CreditLimit = vendor.CreditLimit,
                CreatedAt = vendor.CreatedAt,
                CreatedBy = vendor.CreatedBy
            };
        }

        public static VendorDto ToDto(Vendor vendor)
        {
            return new VendorDto
            {
                Id = vendor.Id,
                Code = vendor.VendorCode,
                Name = vendor.Name,
                LegalName = vendor.LegalName,
                DisplayName = vendor.DisplayName,
                GSTIN = vendor.GSTIN,
                PAN = vendor.PAN,
                TaxIdentificationNumber = vendor.TaxIdentificationNumber,
                Email = vendor.Email,
                Phone = vendor.Phone,
                AlternatePhone = vendor.AlternatePhone,
                Website = vendor.Website,
                PaymentTermId = vendor.PaymentTermId,
                PaymentTerm = vendor.PaymentTerm is null ? null : ToPaymentTermDto(vendor.PaymentTerm),
                CreditLimit = vendor.CreditLimit,
                Status = vendor.Status,
                Notes = vendor.Notes,
                CreatedAt = vendor.CreatedAt,
                CreatedBy = vendor.CreatedBy,
                UpdatedAt = vendor.UpdatedAt,
                UpdatedBy = vendor.UpdatedBy,
                Contacts = vendor.Contacts?.Select(ToContactDto).ToList() ?? new(),
                Addresses = vendor.Addresses?.Select(ToAddressDto).ToList() ?? new(),
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
                Designation = contact.Designation,
                Email = contact.Email,
                Phone = contact.Phone,
                AlternatePhone = contact.AlternatePhone,
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
