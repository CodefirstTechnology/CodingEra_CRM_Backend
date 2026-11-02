using System;
using System.Collections.Generic;
using ERP.Domain.Procurement;

namespace ERP.Application.Procurement.Dtos
{
    public class MultiGrnBillBuildRequestDto
    {
        public int VendorId { get; set; }
        public List<int> GrnIds { get; set; } = new();
    }

    public class VarianceOverrideRequestDto
    {
        public int BillId { get; set; }
        public string OverrideReason { get; set; } = string.Empty;
        public string? Notes { get; set; }
    }

    public class MsmeComplianceSummaryDto
    {
        public int TotalMsmeBillsCount { get; set; }
        public decimal TotalMsmePayableAmount { get; set; }
        public int NormalCount { get; set; }
        public decimal NormalAmount { get; set; }
        public int DueWithin5DaysCount { get; set; }
        public decimal DueWithin5DaysAmount { get; set; }
        public int OverdueDisallowanceCount { get; set; }
        public decimal OverdueDisallowanceAmount { get; set; }
        public List<MsmeOverdueBillItemDto> OverdueBills { get; set; } = new();
    }

    public class MsmeOverdueBillItemDto
    {
        public int BillId { get; set; }
        public string BillNumber { get; set; } = string.Empty;
        public string InvoiceNumber { get; set; } = string.Empty;
        public int VendorId { get; set; }
        public string VendorName { get; set; } = string.Empty;
        public string MsmeCategory { get; set; } = string.Empty;
        public DateTime InvoiceDate { get; set; }
        public DateTime MsmeDueDate { get; set; }
        public int OverdueDays { get; set; }
        public decimal OutstandingAmount { get; set; }
        public string DisallowanceWarning { get; set; } = string.Empty;
    }

    public class Gstr2BReconcileDto
    {
        public int BillId { get; set; }
        public string Gstr2BStatus { get; set; } = "Matched"; // Matched, Mismatched, Pending
        public string? Gstr2BRef { get; set; }
        public string? Notes { get; set; }
    }
}

