namespace CRM.Helpers
{
    public static class QuotationNumberHelper
    {
        /// <summary>Indian fiscal year label e.g. 2025-26 (April–March).</summary>
        public static string FiscalYearLabelFor(DateTime date)
        {
            var utc = date.Kind == DateTimeKind.Unspecified
                ? DateTime.SpecifyKind(date, DateTimeKind.Utc)
                : date.ToUniversalTime();
            var local = utc;
            int startYear = local.Month >= 4 ? local.Year : local.Year - 1;
            int endShort = (startYear + 1) % 100;
            return $"{startYear}-{endShort:D2}";
        }

        public static string FormatNumber(string companyCode, string docType, string fiscalYear, int sequence)
        {
            var cc = (companyCode ?? string.Empty).Trim();
            var dt = (docType ?? "QTN").Trim();
            var fy = (fiscalYear ?? string.Empty).Trim();
            return $"{cc}/{dt}/{fy}/{sequence:D3}";
        }

        public static bool TryParseSequenceFromNumber(string quotationNumber, out int sequence)
        {
            sequence = 0;
            if (string.IsNullOrWhiteSpace(quotationNumber))
            {
                return false;
            }

            var parts = quotationNumber.Trim().Split('/');
            if (parts.Length < 1)
            {
                return false;
            }

            var last = parts[^1];
            return int.TryParse(last, out sequence) && sequence > 0;
        }
    }
}
