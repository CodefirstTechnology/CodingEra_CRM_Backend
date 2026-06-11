namespace CRM.Configuration
{
    public class LeadSyncIndiaMartOptions
    {
        public const string SectionName = "LeadSync:IndiaMart";

        public string PullApiUrl { get; set; } = string.Empty;

        public string ApiKey { get; set; } = string.Empty;
    }
}
