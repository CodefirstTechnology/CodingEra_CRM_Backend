using System.Collections.Generic;

namespace ERP.Application.Accounting
{
    public static class AccountingPermissions
    {
        public const string View = "accounting.view";
        public const string Create = "accounting.create";
        public const string Edit = "accounting.edit";
        public const string Delete = "accounting.delete";
        public const string Approve = "accounting.approve";
        public const string Post = "accounting.post";
        public const string CustomerLedger = "accounting.customer-ledger";
        public const string Gst = "accounting.gst";
        public const string Payment = "accounting.payment";
        public const string Receipt = "accounting.receipt";
        public const string Outstanding = "accounting.outstanding";
        public const string BankRecon = "accounting.bank-recon";
        public const string FinancialReports = "accounting.financial-reports";
        public const string Dashboard = "accounting.dashboard.view";
        public const string Export = "accounting.export";
        public const string Print = "accounting.print";
        public const string Audit = "accounting.audit.view";

        public static readonly IReadOnlyList<string> All = new List<string>
        {
            View,
            Create,
            Edit,
            Delete,
            Approve,
            Post,
            CustomerLedger,
            Gst,
            Payment,
            Receipt,
            Outstanding,
            BankRecon,
            FinancialReports,
            Dashboard,
            Export,
            Print,
            Audit
        };
    }
}
