using System;
using System.Text.RegularExpressions;
using ERP.Domain.Procurement;

namespace ERP.Application.Procurement
{
    public static class VendorRules
    {
        private static readonly Regex EmailRegex = new(
            @"^[^@\s]+@[^@\s]+\.[^@\s]+$",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);

        private static readonly Regex GstinRegex = new(
            @"^[0-9]{2}[A-Z]{5}[0-9]{4}[A-Z]{1}[1-9A-Z]{1}Z[0-9A-Z]{1}$",
            RegexOptions.Compiled);

        private static readonly Regex PanRegex = new(
            @"^[A-Z]{5}[0-9]{4}[A-Z]{1}$",
            RegexOptions.Compiled);

        public static bool IsValidEmail(string? email)
        {
            if (string.IsNullOrWhiteSpace(email)) return false;
            return EmailRegex.IsMatch(email.Trim());
        }

        public static bool IsValidGstin(string? gstin)
        {
            if (string.IsNullOrWhiteSpace(gstin)) return false;
            return GstinRegex.IsMatch(gstin.Trim().ToUpperInvariant());
        }

        public static bool IsValidPan(string? pan)
        {
            if (string.IsNullOrWhiteSpace(pan)) return false;
            return PanRegex.IsMatch(pan.Trim().ToUpperInvariant());
        }

        public static bool CanTransition(VendorStatus current, VendorStatus target)
        {
            if (current == target) return true;

            return current switch
            {
                VendorStatus.Draft => target is VendorStatus.PendingApproval or VendorStatus.Active or VendorStatus.Inactive,
                VendorStatus.PendingApproval => target is VendorStatus.Approved or VendorStatus.Rejected or VendorStatus.Active or VendorStatus.Blocked,
                VendorStatus.Approved => target is VendorStatus.Active or VendorStatus.Inactive or VendorStatus.Blocked,
                VendorStatus.Rejected => target is VendorStatus.Draft or VendorStatus.PendingApproval,
                VendorStatus.Active => target is VendorStatus.Inactive or VendorStatus.Blocked,
                VendorStatus.Inactive => target is VendorStatus.Active or VendorStatus.Blocked,
                VendorStatus.Blocked => target is VendorStatus.Active or VendorStatus.Inactive,
                _ => false
            };
        }

        public static void ValidateVendorCreate(
            string name,
            string email,
            string phone,
            string? gstin,
            string? pan,
            decimal creditLimit)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                throw new InvalidOperationException("Vendor name is required.");
            }

            if (creditLimit < 0)
            {
                throw new InvalidOperationException("Credit limit cannot be negative.");
            }

            if (!string.IsNullOrWhiteSpace(email) && !IsValidEmail(email))
            {
                throw new InvalidOperationException($"Invalid vendor email address '{email}'.");
            }

            if (!string.IsNullOrWhiteSpace(gstin) && !IsValidGstin(gstin))
            {
                throw new InvalidOperationException($"Invalid GSTIN format '{gstin}'. Expected format: e.g. 27AAAAA0000A1Z5.");
            }

            if (!string.IsNullOrWhiteSpace(pan) && !IsValidPan(pan))
            {
                throw new InvalidOperationException($"Invalid PAN format '{pan}'. Expected format: e.g. ABCDE1234F.");
            }
        }
    }
}
