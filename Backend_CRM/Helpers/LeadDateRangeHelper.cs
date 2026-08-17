namespace CRM.Helpers
{
    /// <summary>
    /// Resolves export/list date presets to an inclusive <see cref="CRM.models.Lead.LeadDate"/> range.
    /// Week starts Monday (aligned with admin dashboard period helpers).
    /// </summary>
    public static class LeadDateRangeHelper
    {
        public static bool TryResolve(
            string? datePreset,
            DateTime? fromDate,
            DateTime? toDate,
            DateTime referenceLocal,
            out DateOnly? from,
            out DateOnly? to,
            out string? error)
        {
            from = null;
            to = null;
            error = null;

            var preset = (datePreset ?? "all").Trim().ToLowerInvariant();
            if (string.IsNullOrEmpty(preset) || preset == "all" || preset == "all_time")
            {
                return true;
            }

            var today = DateOnly.FromDateTime(referenceLocal);

            switch (preset)
            {
                case "today":
                    from = today;
                    to = today;
                    return true;

                case "yesterday":
                    from = today.AddDays(-1);
                    to = today.AddDays(-1);
                    return true;

                case "this_week":
                {
                    var mondayOffset = today.DayOfWeek == DayOfWeek.Sunday ? -6 : DayOfWeek.Monday - today.DayOfWeek;
                    from = today.AddDays(mondayOffset);
                    to = from.Value.AddDays(6);
                    return true;
                }

                case "this_month":
                    from = new DateOnly(today.Year, today.Month, 1);
                    to = from.Value.AddMonths(1).AddDays(-1);
                    return true;

                case "last_month":
                {
                    var firstThisMonth = new DateOnly(today.Year, today.Month, 1);
                    to = firstThisMonth.AddDays(-1);
                    from = new DateOnly(to.Value.Year, to.Value.Month, 1);
                    return true;
                }

                case "custom":
                {
                    if (fromDate == null || toDate == null)
                    {
                        error = "Custom date range requires both fromDate and toDate.";
                        return false;
                    }

                    var start = DateOnly.FromDateTime(fromDate.Value);
                    var end = DateOnly.FromDateTime(toDate.Value);
                    if (start > end)
                    {
                        error = "fromDate must be on or before toDate.";
                        return false;
                    }

                    from = start;
                    to = end;
                    return true;
                }

                default:
                    error = $"Unsupported datePreset '{datePreset}'.";
                    return false;
            }
        }
    }
}
