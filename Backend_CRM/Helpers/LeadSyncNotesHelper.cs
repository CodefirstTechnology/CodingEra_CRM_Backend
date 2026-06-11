using System.Text.RegularExpressions;

namespace CRM.Helpers
{
    public static class LeadSyncNotesHelper
    {
        private static readonly Regex ExtMarkerRegex = new(
            @"\[crm-ext:([^:\]]+):([^\]]+)\]",
            RegexOptions.Compiled | RegexOptions.CultureInvariant);

        public static string? TryExtractMarkerName(string? notes)
        {
            if (string.IsNullOrWhiteSpace(notes))
            {
                return null;
            }

            var m = ExtMarkerRegex.Match(notes);
            return m.Success ? m.Groups[1].Value.Trim() : null;
        }
    }
}
