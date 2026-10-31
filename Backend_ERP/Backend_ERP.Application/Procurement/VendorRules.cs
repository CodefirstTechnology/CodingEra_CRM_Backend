using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using ERP.Domain.Procurement;

namespace ERP.Application.Procurement
{
    public static class VendorRules
    {
        private const string GstAlphabet = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ";

        private static readonly Regex EmailRegex = new(
            @"^[^@\s]+@[^@\s]+\.[^@\s]+$",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);

        private static readonly Regex GstinRegex = new(
            @"^[0-9]{2}[A-Z]{5}[0-9]{4}[A-Z]{1}[1-9A-Z]{1}Z[0-9A-Z]{1}$",
            RegexOptions.Compiled);

        private static readonly Regex PanRegex = new(
            @"^[A-Z]{5}[0-9]{4}[A-Z]{1}$",
            RegexOptions.Compiled);

        private static readonly Regex UdyamRegex = new(
            @"^UDYAM-[A-Z]{2}-[0-9]{2}-[0-9]{7}$",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);

        private static readonly Dictionary<string, string> IndianStateCodes = new(StringComparer.OrdinalIgnoreCase)
        {
            { "01", "Jammu and Kashmir" },
            { "02", "Himachal Pradesh" },
            { "03", "Punjab" },
            { "04", "Chandigarh" },
            { "05", "Uttarakhand" },
            { "06", "Haryana" },
            { "07", "Delhi" },
            { "08", "Rajasthan" },
            { "09", "Uttar Pradesh" },
            { "10", "Bihar" },
            { "11", "Sikkim" },
            { "12", "Arunachal Pradesh" },
            { "13", "Nagaland" },
            { "14", "Manipur" },
            { "15", "Mizoram" },
            { "16", "Tripura" },
            { "17", "Meghalaya" },
            { "18", "Assam" },
            { "19", "West Bengal" },
            { "20", "Jharkhand" },
            { "21", "Odisha" },
            { "22", "Chhattisgarh" },
            { "23", "Madhya Pradesh" },
            { "24", "Gujarat" },
            { "25", "Daman and Diu" },
            { "26", "Dadra and Nagar Haveli" },
            { "27", "Maharashtra" },
            { "28", "Andhra Pradesh (Old)" },
            { "29", "Karnataka" },
            { "30", "Goa" },
            { "31", "Lakshadweep" },
            { "32", "Kerala" },
            { "33", "Tamil Nadu" },
            { "34", "Puducherry" },
            { "35", "Andaman and Nicobar Islands" },
            { "36", "Telangana" },
            { "37", "Andhra Pradesh" },
            { "38", "Ladakh" },
            { "97", "Other Territory" },
            { "99", "Centre Jurisdiction" }
        };

        public static bool IsValidEmail(string? email)
        {
            if (string.IsNullOrWhiteSpace(email)) return false;
            return EmailRegex.IsMatch(email.Trim());
        }

        public static bool IsValidGstinFormat(string? gstin)
        {
            if (string.IsNullOrWhiteSpace(gstin)) return false;
            return GstinRegex.IsMatch(gstin.Trim().ToUpperInvariant());
        }

        public static bool IsValidGstin(string? gstin)
        {
            if (string.IsNullOrWhiteSpace(gstin)) return false;
            var clean = gstin.Trim().ToUpperInvariant();
            if (!GstinRegex.IsMatch(clean)) return false;

            return ValidateGstinMod36Checksum(clean);
        }

        public static bool ValidateGstinMod36Checksum(string cleanGstin)
        {
            if (cleanGstin.Length != 15) return false;

            int sum = 0;
            for (int i = 0; i < 14; i++)
            {
                int c = GstAlphabet.IndexOf(cleanGstin[i]);
                if (c < 0) return false;

                int factor = (i % 2 == 0) ? 1 : 2;
                int p = c * factor;
                int q = (p / 36) + (p % 36);
                sum += q;
            }

            int remainder = sum % 36;
            int checkDigitIndex = (36 - remainder) % 36;
            char expectedChar = GstAlphabet[checkDigitIndex];

            return cleanGstin[14] == expectedChar;
        }

        public static string CalculateGstinCheckDigit(string first14Chars)
        {
            if (string.IsNullOrWhiteSpace(first14Chars) || first14Chars.Length != 14)
                throw new ArgumentException("Requires exactly 14 characters", nameof(first14Chars));

            var clean = first14Chars.Trim().ToUpperInvariant();
            int sum = 0;
            for (int i = 0; i < 14; i++)
            {
                int c = GstAlphabet.IndexOf(clean[i]);
                if (c < 0) throw new ArgumentException($"Invalid character {clean[i]} in GSTIN");

                int factor = (i % 2 == 0) ? 1 : 2;
                int p = c * factor;
                int q = (p / 36) + (p % 36);
                sum += q;
            }

            int remainder = sum % 36;
            int checkDigitIndex = (36 - remainder) % 36;
            return GstAlphabet[checkDigitIndex].ToString();
        }

        public static bool IsValidStateCode(string stateCode)
        {
            if (string.IsNullOrWhiteSpace(stateCode)) return false;
            return IndianStateCodes.ContainsKey(stateCode.Trim());
        }

        public static string? GetStateNameByCode(string stateCode)
        {
            if (string.IsNullOrWhiteSpace(stateCode)) return null;
            return IndianStateCodes.TryGetValue(stateCode.Trim(), out var name) ? name : null;
        }

        public static bool ValidateGstinStateMatch(string? gstin, string? stateNameOrCode)
        {
            if (string.IsNullOrWhiteSpace(gstin) || string.IsNullOrWhiteSpace(stateNameOrCode)) return true;
            var cleanGstin = gstin.Trim().ToUpperInvariant();
            if (cleanGstin.Length < 2) return true;
            var gstinStateCode = cleanGstin.Substring(0, 2);

            var cleanState = stateNameOrCode.Trim();

            if (string.Equals(gstinStateCode, cleanState, StringComparison.OrdinalIgnoreCase))
                return true;

            if (IndianStateCodes.TryGetValue(gstinStateCode, out var registeredStateName))
            {
                if (registeredStateName.Contains(cleanState, StringComparison.OrdinalIgnoreCase) ||
                    cleanState.Contains(registeredStateName, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }

        public static int ComputeMsmePaymentTermsDays(int requestedDays, bool hasWrittenContract = true)
        {
            int maxDays = hasWrittenContract ? 45 : 15;
            if (requestedDays <= 0) return maxDays;
            return Math.Min(requestedDays, maxDays);
        }

        public static bool IsValidPan(string? pan)
        {
            if (string.IsNullOrWhiteSpace(pan)) return false;
            return PanRegex.IsMatch(pan.Trim().ToUpperInvariant());
        }

        public static bool IsValidUdyamNumber(string? udyam)
        {
            if (string.IsNullOrWhiteSpace(udyam)) return false;
            return UdyamRegex.IsMatch(udyam.Trim().ToUpperInvariant());
        }

        public static void ValidateMsmePaymentTerms(MsmeCategory category, int paymentTermsDays)
        {
            if (category is MsmeCategory.Micro or MsmeCategory.Small)
            {
                if (paymentTermsDays > 45)
                {
                    throw new InvalidOperationException(
                        $"MSME Section 43B(h) Violation: Payment terms for {category} enterprise cannot exceed 45 days. Provided: {paymentTermsDays} days.");
                }
            }
        }

        public static DateOnly CalculateMsmeStatutoryDueDate(DateOnly billDate, int paymentTermsDays, bool hasWrittenAgreement = true)
        {
            int maxAllowedDays = hasWrittenAgreement ? 45 : 15;
            int effectiveDays = Math.Min(Math.Max(paymentTermsDays, 1), maxAllowedDays);
            return billDate.AddDays(effectiveDays);
        }

        public static bool CanTransition(VendorStatus current, VendorStatus target)
        {
            if (current == target) return true;

            return (current, target) switch
            {
                (VendorStatus.Draft, VendorStatus.PendingKycApproval) => true,
                (VendorStatus.Draft, VendorStatus.Active) => true,
                (VendorStatus.Draft, VendorStatus.Inactive) => true,

                (VendorStatus.PendingKycApproval, VendorStatus.Active) => true,
                (VendorStatus.PendingKycApproval, VendorStatus.Draft) => true,
                (VendorStatus.PendingKycApproval, VendorStatus.Disqualified) => true,
                (VendorStatus.PendingKycApproval, VendorStatus.OnHold) => true,

                (VendorStatus.Active, VendorStatus.OnHold) => true,
                (VendorStatus.Active, VendorStatus.Inactive) => true,
                (VendorStatus.Active, VendorStatus.Disqualified) => true,

                (VendorStatus.OnHold, VendorStatus.Active) => true,
                (VendorStatus.OnHold, VendorStatus.Disqualified) => true,
                (VendorStatus.OnHold, VendorStatus.Inactive) => true,

                (VendorStatus.Disqualified, VendorStatus.OnHold) => true, // QA Audit Reinstatement
                (VendorStatus.Disqualified, VendorStatus.Draft) => true,

                (VendorStatus.Inactive, VendorStatus.PendingKycApproval) => true,
                (VendorStatus.Inactive, VendorStatus.Active) => true,

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

            if (!string.IsNullOrWhiteSpace(gstin))
            {
                var cleanGstin = gstin.Trim().ToUpperInvariant();
                if (!IsValidGstinFormat(cleanGstin))
                {
                    throw new InvalidOperationException($"Invalid GSTIN format '{gstin}'. Expected 15-char standard format (e.g., 27AAAAA0000A1Z5).");
                }

                if (!ValidateGstinMod36Checksum(cleanGstin))
                {
                    throw new InvalidOperationException($"GSTIN '{gstin}' failed statutory ISO/IEC 7064 Mod 36, 36 checksum verification.");
                }

                var stateCode = cleanGstin.Substring(0, 2);
                if (!IsValidStateCode(stateCode))
                {
                    throw new InvalidOperationException($"GSTIN '{gstin}' contains invalid Indian State/UT code '{stateCode}'.");
                }
            }

            if (!string.IsNullOrWhiteSpace(pan) && !IsValidPan(pan))
            {
                throw new InvalidOperationException($"Invalid PAN format '{pan}'. Expected standard 10-char format (e.g., ABCDE1234F).");
            }
        }
    }
}
