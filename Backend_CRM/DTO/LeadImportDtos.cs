namespace CRM.DTO
{
    /// <summary>One row from a lead import spreadsheet (matches CRM import template columns).</summary>
    public class LeadImportRowDto
    {
        /// <summary>1-based spreadsheet row number for error reporting (optional).</summary>
        public int RowNumber { get; set; }

        public string? Salutation { get; set; }
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public string? Mobile { get; set; }
        public string? Email { get; set; }
        public string? Organization { get; set; }
        public string? Industry { get; set; }
        public string? NoOfEmployees { get; set; }
        public string? AnnualRevenue { get; set; }
        public string? Website { get; set; }
        public string? Territory { get; set; }
        public string? Status { get; set; }
        public string? LeadOwner { get; set; }
        public string? RequestType { get; set; }
        public string? Requirement { get; set; }
        public string? AdditionalDetails { get; set; }
    }

    public class LeadImportRequestDto
    {
        public List<LeadImportRowDto> Rows { get; set; } = new();
    }

    public class LeadImportRowErrorDto
    {
        public int RowNumber { get; set; }
        public bool IsDuplicate { get; set; }
        public List<string> Errors { get; set; } = new();
    }

    public class LeadImportResultDto
    {
        public int ValidRows { get; set; }
        public int InvalidRows { get; set; }
        public int DuplicateRows { get; set; }
        public List<LeadImportRowErrorDto> ValidationErrors { get; set; } = new();
    }
}
