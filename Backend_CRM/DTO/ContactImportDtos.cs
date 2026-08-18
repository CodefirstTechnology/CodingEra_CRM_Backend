using System.Collections.Generic;

namespace CRM.DTO
{
    /// <summary>One row from a contact import spreadsheet (matches CRM import template columns).</summary>
    public class ContactImportRowDto
    {
        /// <summary>1-based spreadsheet row number for error reporting (optional).</summary>
        public int RowNumber { get; set; }

        public string? Salutation { get; set; }
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public string? Mobile { get; set; }
        public string? Email { get; set; }
        public string? Gender { get; set; }
        public string? Organization { get; set; } // Company Name
        public string? Designation { get; set; }
        public string? Address { get; set; }
    }

    public class ContactImportRequestDto
    {
        public List<ContactImportRowDto> Rows { get; set; } = new();
    }

    public class ContactImportRowErrorDto
    {
        public int RowNumber { get; set; }
        public bool IsDuplicate { get; set; }
        public List<string> Errors { get; set; } = new();
    }

    public class ContactImportResultDto
    {
        public int ValidRows { get; set; }
        public int InvalidRows { get; set; }
        public int DuplicateRows { get; set; }
        public List<ContactImportRowErrorDto> ValidationErrors { get; set; } = new();
    }

    /// <summary>Result of persisting validated import rows into CRM.</summary>
    public class ContactImportCommitResultDto
    {
        public int ImportedCount { get; set; }
        public int DuplicateCount { get; set; }
        public int InvalidCount { get; set; }
        public List<ContactImportRowErrorDto> ValidationErrors { get; set; } = new();
    }
}
