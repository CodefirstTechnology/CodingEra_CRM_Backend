using CRM.models;

namespace CRM.DTO
{
    public class LeadSyncIntervalOptionDto
    {
        public int Id { get; set; }
        public int Hours { get; set; }
        public string Label { get; set; } = string.Empty;
        public int SortOrder { get; set; }
    }

    public class LeadSyncEligibleUserDto
    {
        public int Id { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string RoleName { get; set; } = string.Empty;
    }

    public class LeadSyncAssignmentDto
    {
        public int UserId { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public int SortOrder { get; set; }
    }

    public class LeadSyncSourceDto
    {
        public int Id { get; set; }
        public string Code { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public string MarkerName { get; set; } = string.Empty;
        public bool ApiIntegrationReady { get; set; }
        public bool AutoSyncEnabled { get; set; }
        public int? IntervalOptionId { get; set; }
        public int? IntervalHours { get; set; }
        public string? IntervalLabel { get; set; }
        public DateTime? LastSyncAt { get; set; }
        public DateTime? NextSyncAt { get; set; }
        public IReadOnlyList<LeadSyncAssignmentDto> Assignments { get; set; } = Array.Empty<LeadSyncAssignmentDto>();
    }

    public class LeadSyncMyAccessDto
    {
        public int SourceId { get; set; }
        public string Code { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public string SyncButtonLabel { get; set; } = string.Empty;
        public bool ApiIntegrationReady { get; set; }
        public bool AutoSyncEnabled { get; set; }
        public DateTime? LastSyncAt { get; set; }
        public DateTime? NextSyncAt { get; set; }
    }

    public class LeadSyncUpdateAssignmentsDto
    {
        public IReadOnlyList<int> UserIds { get; set; } = Array.Empty<int>();
    }

    public class LeadSyncUpdateAutoSyncDto
    {
        public bool AutoSyncEnabled { get; set; }
        public int? IntervalOptionId { get; set; }
    }

    public class LeadSyncManualLogDto
    {
        public int SourceId { get; set; }
        public DateTime StartedAt { get; set; }
        public DateTime EndedAt { get; set; }
        public int TotalReceived { get; set; }
        public int TotalCreated { get; set; }
        public int FailedCount { get; set; }
        public string? ErrorMessage { get; set; }
    }

    public class LeadSyncLogDto
    {
        public int Id { get; set; }
        public int SourceId { get; set; }
        public string SourceName { get; set; } = string.Empty;
        public string SyncType { get; set; } = string.Empty;
        public DateTime StartedAt { get; set; }
        public DateTime? EndedAt { get; set; }
        public int TotalReceived { get; set; }
        public int TotalCreated { get; set; }
        public int FailedCount { get; set; }
        public string? TriggeredByName { get; set; }
        public string Status { get; set; } = string.Empty;
        public string? ErrorMessage { get; set; }
    }

    public class LeadSyncLogQueryDto
    {
        public int? SourceId { get; set; }
        public int Limit { get; set; } = 50;
    }
}
