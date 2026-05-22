namespace CRM.Configuration
{
    public class DatabaseOptions
    {
        public const string SectionName = "Database";

        /// <summary>Apply pending EF migrations when the API starts.</summary>
        public bool AutoMigrateOnStartup { get; set; } = true;

        /// <summary>After Debug build: add a migration if the model changed, then update the database.</summary>
        public bool AutoMigrateOnBuild { get; set; } = true;
    }
}
