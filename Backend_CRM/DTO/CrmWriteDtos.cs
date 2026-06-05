using System.Text.Json.Serialization;
using CRM.models;

namespace CRM.DTO
{
    /// <summary>POST/PUT body for master-data endpoints (salutations, industries, roles, etc.). Server sets id and lastModified.</summary>
    public class MasterDataUpsertDto
    {
        /// <summary>Optional on POST; must match route id on PUT when sent.</summary>
        public int Id { get; set; }

        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public bool IsActive { get; set; } = true;

        /// <summary>Deal pipeline order (<c>deal-statuses</c> only).</summary>
        public int? SortOrder { get; set; }

        /// <summary>Terminal won flag (<c>deal-statuses</c> only).</summary>
        public bool? IsWon { get; set; }

        /// <summary>Terminal lost flag (<c>deal-statuses</c> only).</summary>
        public bool? IsLost { get; set; }
    }

    /// <summary>PATCH body for toggling <c>is_active</c> on master-data rows.</summary>
    public class MasterDataActivePatchDto
    {
        public bool IsActive { get; set; }
    }

    /// <summary>Response row for <c>GET /api/master-data/{entity}</c>.</summary>
    public class MasterDataRowDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        public DateTime? CreatedAt { get; set; }
        public int? SortOrder { get; set; }
        public bool? IsWon { get; set; }
        public bool? IsLost { get; set; }
    }

    public class DealStatusReorderItemDto
    {
        public int Id { get; set; }
        public int SortOrder { get; set; }
    }

    public class DealStatusReorderDto
    {
        public List<DealStatusReorderItemDto> Items { get; set; } = new();
    }

    /// <summary>POST/PUT <c>/api/contacts</c>. Server sets id and lastModified.</summary>
    public class ContactUpsertDto
    {
        public int Id { get; set; }
        public string Salutation { get; set; } = string.Empty;
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public string Gender { get; set; } = string.Empty;
        public int? OrganizationId { get; set; }
        public string Designation { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
    }

    /// <summary>POST/PUT <c>/api/deals</c>. Server sets id and lastModified.</summary>
    public class DealUpsertDto
    {
        public int Id { get; set; }
        public int? OrganizationId { get; set; }
        public int? ContactId { get; set; }
        public string OrganizationName { get; set; } = string.Empty;
        public string Salutation { get; set; } = string.Empty;
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Mobile { get; set; } = string.Empty;
        public string Gender { get; set; } = string.Empty;
        public decimal? AnnualRevenue { get; set; }
        public string Employees { get; set; } = string.Empty;
        public string Website { get; set; } = string.Empty;
        public string Gst { get; set; } = string.Empty;
        public string Territory { get; set; } = string.Empty;
        public string Industry { get; set; } = string.Empty;

        /// <summary>FK to <see cref="CRM.models.DealStatus"/>.</summary>
        public int? DealStatusId { get; set; }

        /// <summary>Resolved to <see cref="DealStatusId"/> by master name when id is not set. Omit on PUT to leave status unchanged.</summary>
        public string Status { get; set; } = "Follow-Up Ongoing";
        public int? DealOwnerId { get; set; }
        public int? AssignedToUserId { get; set; }
        public string AssignedInitials { get; set; } = string.Empty;
        public int? RelatedContactId { get; set; }
        public int? RelatedOrganizationId { get; set; }
        public int? ProbabilityPercent { get; set; }
        public string NextStep { get; set; } = string.Empty;
        public DateTime? NextFollowUpDate { get; set; }

        /// <summary>Set when closing a deal as lost.</summary>
        public string? LostReason { get; set; }
    }

    /// <summary>POST <c>AddNote</c> / PUT <c>UpdateNote</c>. Server sets id, createdAt, updatedAt.</summary>
    public class NoteUpsertDto
    {
        public int Id { get; set; }
        public int RecordId { get; set; }
        public int? AuthorId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;

        [JsonPropertyName("body")]
        public string NoteText { get; set; } = string.Empty;

        public string RelatedType { get; set; } = "lead";
        public int? RelatedEntityId { get; set; }
        public string RelatedName { get; set; } = string.Empty;
        public string Visibility { get; set; } = "team";
        public int? RelatedLeadId { get; set; }
        public int? RelatedDealId { get; set; }
        public int? RelatedContactId { get; set; }
        public int? RelatedOrganizationId { get; set; }
        public string Status { get; set; } = "active";
        public string Priority { get; set; } = "medium";
        public string Tags { get; set; } = string.Empty;
        public string Attachments { get; set; } = string.Empty;
    }

    /// <summary>POST/PUT <c>/api/tasks</c>. Server sets taskId and lastModified.</summary>
    public class TaskUpsertDto
    {
        public int TaskId { get; set; }
        public string TaskTitle { get; set; } = string.Empty;
        public string TaskDescription { get; set; } = string.Empty;
        public string TaskStatus { get; set; } = string.Empty;
        public string TaskAssignee { get; set; } = string.Empty;
        public DateTime TaskDueDate { get; set; } = DateTime.UtcNow;
        public string TaskPriority { get; set; } = string.Empty;
        public int? AssigneeUserId { get; set; }
        public int? RelatedLeadId { get; set; }
        public int? RelatedDealId { get; set; }
    }

    /// <summary>POST <c>AddCall</c> / PUT <c>UpdateCall</c>. Server sets callId and lastModified.</summary>
    public class CallLogUpsertDto
    {
        public int CallId { get; set; }
        public string Direction { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public string ContactCompany { get; set; } = string.Empty;
        public string ContactName { get; set; } = string.Empty;
        public DateTime CallStarted { get; set; }
        public int DurationMinutes { get; set; }
        public int DurationSeconds { get; set; }
        public string Outcome { get; set; } = string.Empty;

        [JsonPropertyName("summary")]
        public string? CallSummary { get; set; }

        public int? ContactId { get; set; }
        public int? RelatedLeadId { get; set; }
        public int? RelatedDealId { get; set; }
    }

    public static class CrmWriteMappings
    {
        private static int? Fk(int? v) => v is > 0 ? v : null;

        public static Contact ToContact(ContactUpsertDto d, int id = 0) => new()
        {
            Id = id,
            Salutation = d.Salutation ?? string.Empty,
            FirstName = d.FirstName ?? string.Empty,
            LastName = d.LastName ?? string.Empty,
            Email = d.Email ?? string.Empty,
            Phone = d.Phone ?? string.Empty,
            Gender = d.Gender ?? string.Empty,
            OrganizationId = Fk(d.OrganizationId),
            Designation = d.Designation ?? string.Empty,
            Address = d.Address ?? string.Empty,
        };

        public static void Apply(Contact e, ContactUpsertDto d)
        {
            e.Salutation = d.Salutation ?? string.Empty;
            e.FirstName = d.FirstName ?? string.Empty;
            e.LastName = d.LastName ?? string.Empty;
            e.Email = d.Email ?? string.Empty;
            e.Phone = d.Phone ?? string.Empty;
            e.Gender = d.Gender ?? string.Empty;
            e.OrganizationId = Fk(d.OrganizationId);
            e.Designation = d.Designation ?? string.Empty;
            e.Address = d.Address ?? string.Empty;
        }

        public static Deal ToDeal(DealUpsertDto d, int id = 0) => new()
        {
            Id = id,
            OrganizationId = Fk(d.OrganizationId),
            ContactId = Fk(d.ContactId),
            OrganizationName = d.OrganizationName ?? string.Empty,
            Salutation = d.Salutation ?? string.Empty,
            FirstName = d.FirstName ?? string.Empty,
            LastName = d.LastName ?? string.Empty,
            Email = d.Email ?? string.Empty,
            Mobile = d.Mobile ?? string.Empty,
            Gender = d.Gender ?? string.Empty,
            AnnualRevenue = d.AnnualRevenue,
            Employees = d.Employees ?? string.Empty,
            Website = d.Website ?? string.Empty,
            Gst = d.Gst ?? string.Empty,
            Territory = d.Territory ?? string.Empty,
            Industry = d.Industry ?? string.Empty,
            DealOwnerId = Fk(d.DealOwnerId),
            AssignedToUserId = Fk(d.AssignedToUserId),
            AssignedInitials = d.AssignedInitials ?? string.Empty,
            RelatedContactId = Fk(d.RelatedContactId),
            RelatedOrganizationId = Fk(d.RelatedOrganizationId),
            ProbabilityPercent = d.ProbabilityPercent,
            NextStep = d.NextStep ?? string.Empty,
            NextFollowUpDate = d.NextFollowUpDate,
            LostReason = d.LostReason ?? string.Empty,
        };

        public static void Apply(Deal e, DealUpsertDto d)
        {
            e.OrganizationId = Fk(d.OrganizationId);
            e.ContactId = Fk(d.ContactId);
            e.OrganizationName = d.OrganizationName ?? string.Empty;
            e.Salutation = d.Salutation ?? string.Empty;
            e.FirstName = d.FirstName ?? string.Empty;
            e.LastName = d.LastName ?? string.Empty;
            e.Email = d.Email ?? string.Empty;
            e.Mobile = d.Mobile ?? string.Empty;
            e.Gender = d.Gender ?? string.Empty;
            e.AnnualRevenue = d.AnnualRevenue;
            e.Employees = d.Employees ?? string.Empty;
            e.Website = d.Website ?? string.Empty;
            e.Gst = d.Gst ?? string.Empty;
            e.Territory = d.Territory ?? string.Empty;
            e.Industry = d.Industry ?? string.Empty;
            e.DealOwnerId = Fk(d.DealOwnerId);
            e.AssignedToUserId = Fk(d.AssignedToUserId);
            e.AssignedInitials = d.AssignedInitials ?? string.Empty;
            e.RelatedContactId = Fk(d.RelatedContactId);
            e.RelatedOrganizationId = Fk(d.RelatedOrganizationId);
            e.ProbabilityPercent = d.ProbabilityPercent;
            e.NextStep = d.NextStep ?? string.Empty;
            e.NextFollowUpDate = d.NextFollowUpDate;
            if (d.LostReason != null)
            {
                e.LostReason = d.LostReason;
            }
        }

        public static Note ToNote(NoteUpsertDto d, int id = 0) => new()
        {
            Id = id,
            RecordId = d.RecordId,
            AuthorId = Fk(d.AuthorId),
            Name = d.Name ?? string.Empty,
            Title = d.Title ?? string.Empty,
            NoteText = d.NoteText ?? string.Empty,
            RelatedType = d.RelatedType ?? "lead",
            RelatedEntityId = Fk(d.RelatedEntityId),
            RelatedName = d.RelatedName ?? string.Empty,
            Visibility = d.Visibility ?? "team",
            RelatedLeadId = Fk(d.RelatedLeadId),
            RelatedDealId = Fk(d.RelatedDealId),
            RelatedContactId = Fk(d.RelatedContactId),
            RelatedOrganizationId = Fk(d.RelatedOrganizationId),
            Status = d.Status ?? "active",
            Priority = d.Priority ?? "medium",
            Tags = d.Tags ?? string.Empty,
            Attachments = d.Attachments ?? string.Empty,
        };

        public static void Apply(Note e, NoteUpsertDto d)
        {
            e.AuthorId = Fk(d.AuthorId);
            e.Name = d.Name ?? string.Empty;
            e.Title = d.Title ?? string.Empty;
            e.NoteText = d.NoteText ?? string.Empty;
            e.RelatedType = d.RelatedType ?? "lead";
            e.RelatedEntityId = Fk(d.RelatedEntityId);
            e.RelatedName = d.RelatedName ?? string.Empty;
            e.Visibility = d.Visibility ?? "team";
            e.RelatedLeadId = Fk(d.RelatedLeadId);
            e.RelatedDealId = Fk(d.RelatedDealId);
            e.RelatedContactId = Fk(d.RelatedContactId);
            e.RelatedOrganizationId = Fk(d.RelatedOrganizationId);
            e.RecordId = d.RecordId;
            e.Status = d.Status ?? "active";
            e.Priority = d.Priority ?? "medium";
            e.Tags = d.Tags ?? string.Empty;
            e.Attachments = d.Attachments ?? string.Empty;
        }

        public static TaskTable ToTask(TaskUpsertDto d, int taskId = 0) => new()
        {
            TaskId = taskId,
            TaskTitle = d.TaskTitle ?? string.Empty,
            TaskDescription = d.TaskDescription ?? string.Empty,
            TaskStatus = d.TaskStatus ?? string.Empty,
            TaskAssignee = d.TaskAssignee ?? string.Empty,
            TaskDueDate = d.TaskDueDate,
            TaskPriority = d.TaskPriority ?? string.Empty,
            AssigneeUserId = Fk(d.AssigneeUserId),
            RelatedLeadId = Fk(d.RelatedLeadId),
            RelatedDealId = Fk(d.RelatedDealId),
        };

        public static void Apply(TaskTable e, TaskUpsertDto d)
        {
            e.TaskTitle = d.TaskTitle ?? string.Empty;
            e.TaskDescription = d.TaskDescription ?? string.Empty;
            e.TaskStatus = d.TaskStatus ?? string.Empty;
            e.TaskAssignee = d.TaskAssignee ?? string.Empty;
            e.TaskDueDate = d.TaskDueDate;
            e.TaskPriority = d.TaskPriority ?? string.Empty;
            e.AssigneeUserId = Fk(d.AssigneeUserId);
            e.RelatedLeadId = Fk(d.RelatedLeadId);
            e.RelatedDealId = Fk(d.RelatedDealId);
        }

        public static CallLog ToCallLog(CallLogUpsertDto d, int callId = 0) => new()
        {
            CallId = callId,
            Direction = d.Direction ?? string.Empty,
            PhoneNumber = d.PhoneNumber ?? string.Empty,
            ContactCompany = d.ContactCompany ?? string.Empty,
            ContactName = d.ContactName ?? string.Empty,
            CallStarted = d.CallStarted,
            DurationMinutes = d.DurationMinutes,
            DurationSeconds = d.DurationSeconds,
            Outcome = d.Outcome ?? string.Empty,
            CallSummary = d.CallSummary,
            ContactId = Fk(d.ContactId),
            RelatedLeadId = Fk(d.RelatedLeadId),
            RelatedDealId = Fk(d.RelatedDealId),
        };

        public static void Apply(CallLog e, CallLogUpsertDto d)
        {
            e.ContactName = d.ContactName ?? string.Empty;
            e.Direction = d.Direction ?? string.Empty;
            e.PhoneNumber = d.PhoneNumber ?? string.Empty;
            e.ContactCompany = d.ContactCompany ?? string.Empty;
            e.CallStarted = d.CallStarted;
            e.DurationMinutes = d.DurationMinutes;
            e.DurationSeconds = d.DurationSeconds;
            e.Outcome = d.Outcome ?? string.Empty;
            e.CallSummary = d.CallSummary;
            e.ContactId = Fk(d.ContactId);
            e.RelatedLeadId = Fk(d.RelatedLeadId);
            e.RelatedDealId = Fk(d.RelatedDealId);
        }
    }
}
