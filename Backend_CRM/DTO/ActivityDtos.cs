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
