namespace CRM.DTO
{
    public class ActivityLogDto
    {
        public int Id { get; set; }

        public string EntityType { get; set; } = string.Empty;

        public int EntityId { get; set; }

        public string ActionType { get; set; } = string.Empty;

        public int? ActorUserId { get; set; }

        public string ActorName { get; set; } = string.Empty;

        public string Message { get; set; } = string.Empty;

        public string? FieldName { get; set; }

        public string? OldValue { get; set; }

        public string? NewValue { get; set; }

        public string? RelatedRecordType { get; set; }

        public int? RelatedRecordId { get; set; }

        public DateTime CreatedAt { get; set; }
    }

    public class CreateActivityDto
    {
        /// <summary>created | updated | status_changed | field_updated | note_added | comment_added | task_added | call_logged | email_sent</summary>
        public string ActionType { get; set; } = "updated";

        public string Message { get; set; } = string.Empty;

        public string? FieldName { get; set; }

        public string? OldValue { get; set; }

        public string? NewValue { get; set; }

        public string? RelatedRecordType { get; set; }

        public int? RelatedRecordId { get; set; }
    }

    public class DealStatusUpdateDto
    {
        public int? DealStatusId { get; set; }

        public string? Status { get; set; }
    }

    public class CommentUpsertDto
    {
        public int Id { get; set; }

        /// <summary>lead | deal | contact | organization</summary>
        public string EntityType { get; set; } = "lead";

        public int EntityId { get; set; }

        public int? AuthorId { get; set; }

        public string Body { get; set; } = string.Empty;
    }
}
