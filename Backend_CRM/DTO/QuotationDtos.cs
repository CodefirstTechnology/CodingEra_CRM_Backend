namespace CRM.DTO
{
    public class QuotationSettingsDto
    {
        public string CompanyCode { get; set; } = string.Empty;
        public string DocumentTypeCode { get; set; } = "QTN";
    }

    public class QuotationNextNumberDto
    {
        public string CompanyCode { get; set; } = string.Empty;
        public string DocumentTypeCode { get; set; } = "QTN";
        public string FiscalYearLabel { get; set; } = string.Empty;
        public int SequenceNumber { get; set; }
        public string QuotationNumber { get; set; } = string.Empty;
        public DateTime QuotationDate { get; set; }
    }

    public class QuotationLineItemDto
    {
        public int Id { get; set; }
        public int LineIndex { get; set; }
        public string ItemCode { get; set; } = string.Empty;
        public string ItemName { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public decimal Quantity { get; set; } = 1;
        public string Uom { get; set; } = string.Empty;
        public decimal Weight { get; set; }
        public decimal UnitWeight { get; set; }
        public int? ItemId { get; set; }
        public decimal Rate { get; set; }
        public decimal SteelRate { get; set; }
        public string ItemSnapshotJson { get; set; } = string.Empty;
        public decimal DiscountPercent { get; set; }
        public decimal GstPercent { get; set; }
        public decimal Amount { get; set; }
        public decimal TaxAmount { get; set; }
        public decimal LineTotal { get; set; }
    }

    public class QuotationGridColumnDto
    {
        public string Key { get; set; } = string.Empty;
        public string Label { get; set; } = string.Empty;
        public bool Visible { get; set; } = true;
        public int Order { get; set; }
        public int Width { get; set; } = 100;
        public bool Editable { get; set; } = true;
    }

    public class QuotationGridColumnsDto
    {
        public List<QuotationGridColumnDto> Columns { get; set; } = new();
    }

    public class QuotationTotalsDto
    {
        public decimal Subtotal { get; set; }
        public decimal AdditionalChargesTotal { get; set; }
        public decimal TaxableAmount { get; set; }
        public decimal TaxTotal { get; set; }
        public decimal GrandTotal { get; set; }
        public decimal TotalQuantity { get; set; }
        public decimal TotalWeight { get; set; }
    }

    public class QuotationAdditionalChargeDto
    {
        public int Id { get; set; }
        public int SortIndex { get; set; }
        public string ChargeName { get; set; } = string.Empty;
        public decimal Amount { get; set; }
    }

    /// <summary>Template-specific fields for Technical Proposal quotations.</summary>
    public class TechnicalProposalPayloadDto
    {
        public string ProjectName { get; set; } = string.Empty;
        public string KindAttnDesignation { get; set; } = string.Empty;
        public string CommercialTerms { get; set; } = string.Empty;
        public string TaxLabel { get; set; } = string.Empty;
        public string PaymentTerms { get; set; } = string.Empty;
        public string HsnCode { get; set; } = string.Empty;
        public string Incoterms { get; set; } = string.Empty;
        public string DispatchLeadTime { get; set; } = string.Empty;
        /// <summary>ISO 4217 code, e.g. INR or USD. Defaults to INR.</summary>
        public string CurrencyCode { get; set; } = "INR";
        public string ProposalIntro { get; set; } = string.Empty;
        public List<CompanyProfileTermDto> TechnicalSections { get; set; } = new();
        public List<CompanyProfileTermDto> CommercialSections { get; set; } = new();
    }

    public class QuotationUpsertDto
    {
        public int Id { get; set; }
        public int? DealId { get; set; }
        /// <summary>True when the linked deal is closed (Won/Lost). Read-only on API responses.</summary>
        public bool DealClosed { get; set; }
        public string Salutation { get; set; } = string.Empty;
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string Gender { get; set; } = string.Empty;
        public string CustomerName { get; set; } = string.Empty;
        public string CompanyName { get; set; } = string.Empty;
        public string Employees { get; set; } = string.Empty;
        public decimal? AnnualRevenue { get; set; }
        public string Website { get; set; } = string.Empty;
        public string Gst { get; set; } = string.Empty;
        public string Territory { get; set; } = string.Empty;
        public string Industry { get; set; } = string.Empty;
        public string ContactPerson { get; set; } = string.Empty;
        public string MobileNumber { get; set; } = string.Empty;
        public string EmailAddress { get; set; } = string.Empty;
        public string OfficeAddress { get; set; } = string.Empty;
        public string SiteAddress { get; set; } = string.Empty;
        public string ReferenceNumber { get; set; } = string.Empty;
        public DateTime? ReferenceDate { get; set; }
        public string CompanyCode { get; set; } = string.Empty;
        public string DocumentTypeCode { get; set; } = "QTN";
        public string FiscalYearLabel { get; set; } = string.Empty;
        public int SequenceNumber { get; set; }
        public string QuotationNumber { get; set; } = string.Empty;
        public DateTime? QuotationDate { get; set; }
        public string Status { get; set; } = CRM.models.QuotationStatuses.Draft;
        public string Remarks { get; set; } = string.Empty;
        public decimal Subtotal { get; set; }
        public decimal TaxTotal { get; set; }
        public decimal GstPercent { get; set; }
        public decimal GrandTotal { get; set; }
        public decimal TotalQuantity { get; set; }
        public decimal TotalWeight { get; set; }
        public decimal TransportationCharges { get; set; }
        public decimal LoadingCharges { get; set; }
        public decimal ServiceCharges { get; set; }
        /// <summary>When true, quotation-specific terms content is used on PDF instead of company profile defaults.</summary>
        public bool CustomizeTerms { get; set; }
        public string IntroText { get; set; } = string.Empty;
        public string TransportationLabel { get; set; } = string.Empty;
        public string Jurisdiction { get; set; } = string.Empty;
        public List<CompanyProfileTermDto> Terms { get; set; } = new();
        public List<QuotationAdditionalChargeDto> CustomCharges { get; set; } = new();
        public List<QuotationLineItemDto> LineItems { get; set; } = new();
        public string QuotationTemplate { get; set; } = CRM.models.QuotationTemplateTypes.Standard;
        public TechnicalProposalPayloadDto? TechnicalProposal { get; set; }
    }

    public class QuotationStatusPatchDto
    {
        public string Status { get; set; } = string.Empty;
    }
}
