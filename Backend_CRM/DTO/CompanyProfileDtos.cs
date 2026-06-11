namespace CRM.DTO
{
    public class CompanyProfileTermDto
    {
        public string Title { get; set; } = string.Empty;
        public string Body { get; set; } = string.Empty;
    }

    public class CompanyProfileDto
    {
        public string BrandName { get; set; } = string.Empty;
        public string CompanyName { get; set; } = string.Empty;
        public string Tagline { get; set; } = string.Empty;
        public string BusinessLine { get; set; } = string.Empty;
        public string LogoContentType { get; set; } = string.Empty;
        /// <summary>Base64 payload without data-URL prefix; omit on GET when empty.</summary>
        public string? LogoBase64 { get; set; }
        public string Gstin { get; set; } = string.Empty;
        public string CinNumber { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public string ContactNumber { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Website { get; set; } = string.Empty;
        public string BankName { get; set; } = string.Empty;
        public string AccountNumber { get; set; } = string.Empty;
        public string IfscCode { get; set; } = string.Empty;
        public string BranchName { get; set; } = string.Empty;
        public string SignatoryName { get; set; } = string.Empty;
        public string SignatoryMobile { get; set; } = string.Empty;
        public List<CompanyProfileTermDto> Terms { get; set; } = new();
        public string IntroText { get; set; } = string.Empty;
        public string TransportationLabel { get; set; } = string.Empty;
        public string Jurisdiction { get; set; } = string.Empty;
        public decimal DefaultGstPercent { get; set; } = 18m;
        public DateTime? UpdatedAt { get; set; }
    }

    public class CompanyProfileUpsertDto
    {
        public string BrandName { get; set; } = string.Empty;
        public string CompanyName { get; set; } = string.Empty;
        public string Tagline { get; set; } = string.Empty;
        public string BusinessLine { get; set; } = string.Empty;
        public string LogoContentType { get; set; } = string.Empty;
        public string? LogoBase64 { get; set; }
        /// <summary>When true, clears stored logo.</summary>
        public bool RemoveLogo { get; set; }
        public string Gstin { get; set; } = string.Empty;
        public string CinNumber { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public string ContactNumber { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Website { get; set; } = string.Empty;
        public string BankName { get; set; } = string.Empty;
        public string AccountNumber { get; set; } = string.Empty;
        public string IfscCode { get; set; } = string.Empty;
        public string BranchName { get; set; } = string.Empty;
        public string SignatoryName { get; set; } = string.Empty;
        public string SignatoryMobile { get; set; } = string.Empty;
        public List<CompanyProfileTermDto> Terms { get; set; } = new();
        public string IntroText { get; set; } = string.Empty;
        public string TransportationLabel { get; set; } = string.Empty;
        public string Jurisdiction { get; set; } = string.Empty;
        public decimal DefaultGstPercent { get; set; } = 18m;
    }
}
