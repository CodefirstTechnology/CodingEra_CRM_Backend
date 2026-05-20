namespace CRM.DTO
{
    public class SendEmailDto
    {
        /// <summary>lead | deal | contact</summary>
        public string EntityType { get; set; } = "lead";

        public int EntityId { get; set; }

        public string ToEmail { get; set; } = string.Empty;

        public string Subject { get; set; } = string.Empty;

        public string Body { get; set; } = string.Empty;

        public bool IsHtml { get; set; } = true;

        public int? SentBy { get; set; }
    }

    public class EmailResponseDto
    {
        public int Id { get; set; }

        public string EntityType { get; set; } = string.Empty;

        public int EntityId { get; set; }

        public string ToEmail { get; set; } = string.Empty;

        public string Subject { get; set; } = string.Empty;

        public string Body { get; set; } = string.Empty;

        public string Status { get; set; } = string.Empty;

        public string? FailureMessage { get; set; }

        public int? SentBy { get; set; }

        public DateTime CreatedAt { get; set; }

        public DateTime UpdatedAt { get; set; }
    }
}
