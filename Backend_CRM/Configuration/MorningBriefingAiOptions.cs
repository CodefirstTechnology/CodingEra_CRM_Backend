namespace CRM.Configuration
{
    public class MorningBriefingAiOptions
    {
        public const string SectionName = "MorningBriefingAi";

        /// <summary>Google Gemini generateContent endpoint (model included in path).</summary>
        public string ApiUrl { get; set; } =
            "https://generativelanguage.googleapis.com/v1beta/models/gemini-2.5-flash:generateContent";

        public string ApiKey { get; set; } = string.Empty;

        public string Model { get; set; } = "gemini-2.5-flash";

        public bool Enabled { get; set; }
    }
}
