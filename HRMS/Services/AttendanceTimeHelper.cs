using System.Globalization;

namespace HRMS.Services;

internal static class AttendanceTimeHelper
{
    private static readonly TimeZoneInfo AppTimeZone = ResolveTimeZone();

    public static DateTime UtcToLocal(DateTime utc) =>
        TimeZoneInfo.ConvertTimeFromUtc(DateTime.SpecifyKind(utc, DateTimeKind.Utc), AppTimeZone);

    public static DateTime ResolveClockInstant(DateTime? deviceTimeUtc, DateTime utcFallback) =>
        deviceTimeUtc.HasValue
            ? UtcToLocal(DateTime.SpecifyKind(deviceTimeUtc.Value, DateTimeKind.Utc))
            : UtcToLocal(utcFallback);

    public static DateOnly TodayLocal() => DateOnly.FromDateTime(UtcToLocal(DateTime.UtcNow));

    public static string? FormatDisplay(DateTime? serverUtc, TimeOnly? fallbackTime)
    {
        if (serverUtc.HasValue)
        {
            return UtcToLocal(serverUtc.Value).ToString("h:mm tt", CultureInfo.InvariantCulture);
        }

        return fallbackTime?.ToString("h:mm tt", CultureInfo.InvariantCulture);
    }

    private static TimeZoneInfo ResolveTimeZone()
    {
        foreach (var id in new[] { "India Standard Time", "Asia/Kolkata" })
        {
            try
            {
                return TimeZoneInfo.FindSystemTimeZoneById(id);
            }
            catch (TimeZoneNotFoundException)
            {
                // try next
            }
        }

        return TimeZoneInfo.Local;
    }
}
