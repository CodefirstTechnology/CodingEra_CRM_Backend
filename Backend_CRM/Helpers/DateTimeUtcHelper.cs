namespace CRM.Helpers
{
    /// <summary>
    /// Npgsql timestamptz requires <see cref="DateTimeKind.Utc"/>.
    /// API/JSON dates are often <see cref="DateTimeKind.Unspecified"/>.
    /// </summary>
    public static class DateTimeUtcHelper
    {
        public static DateTime? ToUtcOrNull(DateTime? value) =>
            value.HasValue ? ToUtc(value.Value) : null;

        public static DateTime ToUtc(DateTime value) =>
            value.Kind switch
            {
                DateTimeKind.Utc => value,
                DateTimeKind.Local => value.ToUniversalTime(),
                _ => DateTime.SpecifyKind(value, DateTimeKind.Utc),
            };
    }
}
