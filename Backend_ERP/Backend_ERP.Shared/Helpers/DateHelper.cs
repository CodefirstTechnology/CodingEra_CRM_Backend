namespace ERP.Shared.Helpers
{
    /// <summary>
    /// Shared date formatting and parsing helpers reused across all Sales module mappers.
    /// Eliminates the identical FormatDate / FormatDateTime / ParseDate implementations
    /// that were previously duplicated in every mapper class.
    /// </summary>
    public static class DateHelper
    {
        /// <summary>Returns "yyyy-MM-dd" string from a DateOnly value.</summary>
        public static string FormatDate(DateOnly date) => date.ToString("yyyy-MM-dd");

        /// <summary>Returns ISO-8601 UTC string from a DateTimeOffset value.</summary>
        public static string FormatDateTime(DateTimeOffset value) =>
            value.UtcDateTime.ToString("O");

        /// <summary>
        /// Parses a date string to DateOnly. Accepts "yyyy-MM-dd" and ISO-8601 formats.
        /// Returns <paramref name="fallback"/> when the value is null, empty, or unparseable.
        /// </summary>
        public static DateOnly ParseDate(string? value, DateOnly fallback)
        {
            if (string.IsNullOrWhiteSpace(value))
                return fallback;

            if (DateOnly.TryParse(value, out var date))
                return date;

            if (DateTimeOffset.TryParse(value, out var dto))
                return DateOnly.FromDateTime(dto.UtcDateTime);

            return fallback;
        }

        /// <summary>Returns today's date as DateOnly in UTC.</summary>
        public static DateOnly Today() => DateOnly.FromDateTime(DateTime.UtcNow);
    }
}
