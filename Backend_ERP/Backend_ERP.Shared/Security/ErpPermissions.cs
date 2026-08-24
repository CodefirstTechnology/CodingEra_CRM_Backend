using System.Collections.Generic;

namespace ERP.Shared.Security
{
    /// <summary>
    /// Centralized catalog of all ERP Sales & cross-module permission codes.
    /// </summary>
    public static class ErpPermissions
    {
        public static class PriceLists
        {
            public const string View = "price-lists.view";
            public const string Create = "price-lists.create";
            public const string Edit = "price-lists.edit";
            public const string Delete = "price-lists.delete";
            public const string Activate = "price-lists.activate";
            public const string Export = "price-lists.export";
            public const string Compare = "price-lists.compare";
        }

        public static class Quotations
        {
            public const string View = "quotations.view";
            public const string Create = "quotations.create";
            public const string Edit = "quotations.edit";
            public const string Delete = "quotations.delete";
            public const string Duplicate = "quotations.duplicate";
            public const string Submit = "quotations.submit";
            public const string Approve = "quotations.approve";
            public const string Reject = "quotations.reject";
            public const string Return = "quotations.return";
            public const string Reopen = "quotations.reopen";
            public const string Convert = "quotations.convert";
            public const string GeneratePdf = "quotations.generate-pdf";
            public const string SendEmail = "quotations.send-email";
        }

        public static class DiscountApprovals
        {
            public const string View = "discount-approvals.view";
            public const string Create = "discount-approvals.create";
            public const string Approve = "discount-approvals.approve";
            public const string Reject = "discount-approvals.reject";
            public const string Return = "discount-approvals.return";
            public const string Cancel = "discount-approvals.cancel";
            public const string Resubmit = "discount-approvals.resubmit";
        }

        public static class SalesOrders
        {
            public const string View = "sales-orders.view";
            public const string Create = "sales-orders.create";
            public const string Edit = "sales-orders.edit";
            public const string Submit = "sales-orders.submit";
            public const string Confirm = "sales-orders.confirm";
            public const string Process = "sales-orders.process";
            public const string Complete = "sales-orders.complete";
            public const string Cancel = "sales-orders.cancel";
            public const string GeneratePdf = "sales-orders.generate-pdf";
            public const string SendEmail = "sales-orders.send-email";
            public const string AuditView = "sales-orders.audit.view";
        }

        public static class ProformaInvoices
        {
            public const string View = "proforma-invoices.view";
            public const string Create = "proforma-invoices.create";
            public const string Edit = "proforma-invoices.edit";
            public const string Submit = "proforma-invoices.submit";
            public const string Approve = "proforma-invoices.approve";
            public const string Reject = "proforma-invoices.reject";
            public const string Return = "proforma-invoices.return";
            public const string Convert = "proforma-invoices.convert";
            public const string Email = "proforma-invoices.email";
            public const string Whatsapp = "proforma-invoices.whatsapp";
            public const string GeneratePdf = "proforma-invoices.generate-pdf";
        }

        public static class AdvancePayments
        {
            public const string View = "advance-payments.view";
            public const string Create = "advance-payments.create";
            public const string Edit = "advance-payments.edit";
            public const string Verify = "advance-payments.verify";
            public const string Receive = "advance-payments.receive";
            public const string Apply = "advance-payments.apply";
        }

        public static class SalesTargets
        {
            public const string View = "sales-targets.view";
            public const string Create = "sales-targets.create";
            public const string Edit = "sales-targets.edit";
            public const string Delete = "sales-targets.delete";
            public const string Duplicate = "sales-targets.duplicate";
            public const string DashboardView = "sales-targets.dashboard.view";
        }

        public static class Performance
        {
            public const string View = "performance.view";
            public const string LeaderboardView = "performance.leaderboard.view";
            public const string Export = "performance.export";
        }

        public static class DispatchLogistics
        {
            public const string View = "dispatch-logistics.view";
            public const string Assign = "dispatch-logistics.assign";
            public const string Lr = "dispatch-logistics.lr";
            public const string Eway = "dispatch-logistics.eway";
            public const string Pod = "dispatch-logistics.pod";
        }

        public static class Accounting
        {
            public const string View = "accounting.view";
            public const string Outstanding = "accounting.outstanding";
        }

        /// <summary>
        /// All permissions defined in the ERP Sales catalog.
        /// </summary>
        public static readonly IReadOnlyList<string> All = new[]
        {
            PriceLists.View,
            PriceLists.Create,
            PriceLists.Edit,
            PriceLists.Delete,
            PriceLists.Activate,
            PriceLists.Export,
            PriceLists.Compare,

            Quotations.View,
            Quotations.Create,
            Quotations.Edit,
            Quotations.Delete,
            Quotations.Duplicate,
            Quotations.Submit,
            Quotations.Approve,
            Quotations.Reject,
            Quotations.Return,
            Quotations.Reopen,
            Quotations.Convert,
            Quotations.GeneratePdf,
            Quotations.SendEmail,

            DiscountApprovals.View,
            DiscountApprovals.Create,
            DiscountApprovals.Approve,
            DiscountApprovals.Reject,
            DiscountApprovals.Return,
            DiscountApprovals.Cancel,
            DiscountApprovals.Resubmit,

            SalesOrders.View,
            SalesOrders.Create,
            SalesOrders.Edit,
            SalesOrders.Submit,
            SalesOrders.Confirm,
            SalesOrders.Process,
            SalesOrders.Complete,
            SalesOrders.Cancel,
            SalesOrders.GeneratePdf,
            SalesOrders.SendEmail,
            SalesOrders.AuditView,

            ProformaInvoices.View,
            ProformaInvoices.Create,
            ProformaInvoices.Edit,
            ProformaInvoices.Submit,
            ProformaInvoices.Approve,
            ProformaInvoices.Reject,
            ProformaInvoices.Return,
            ProformaInvoices.Convert,
            ProformaInvoices.Email,
            ProformaInvoices.Whatsapp,
            ProformaInvoices.GeneratePdf,

            AdvancePayments.View,
            AdvancePayments.Create,
            AdvancePayments.Edit,
            AdvancePayments.Verify,
            AdvancePayments.Receive,
            AdvancePayments.Apply,

            SalesTargets.View,
            SalesTargets.Create,
            SalesTargets.Edit,
            SalesTargets.Delete,
            SalesTargets.Duplicate,
            SalesTargets.DashboardView,

            Performance.View,
            Performance.LeaderboardView,
            Performance.Export,

            DispatchLogistics.View,
            DispatchLogistics.Assign,
            DispatchLogistics.Lr,
            DispatchLogistics.Eway,
            DispatchLogistics.Pod,

            Accounting.View,
            Accounting.Outstanding
        };

        /// <summary>
        /// Exact permissions allowed for the Sales Executive role in Phase 2.
        /// </summary>
        public static readonly IReadOnlyList<string> SalesExecutiveAllowed = new[]
        {
            // Price Lists
            PriceLists.View,
            PriceLists.Compare,

            // Quotations (Own scope)
            Quotations.View,
            Quotations.Create,
            Quotations.Edit,
            Quotations.Duplicate,
            Quotations.Submit,
            Quotations.GeneratePdf,
            Quotations.SendEmail,
            Quotations.Convert,

            // Discount Approvals (Own scope)
            DiscountApprovals.View,
            DiscountApprovals.Create,
            DiscountApprovals.Resubmit,

            // Sales Orders (Own scope)
            SalesOrders.View,
            SalesOrders.Create,
            SalesOrders.Edit,
            SalesOrders.Submit,
            SalesOrders.GeneratePdf,
            SalesOrders.SendEmail,
            SalesOrders.AuditView,

            // Proforma Invoices (Own scope)
            ProformaInvoices.View,
            ProformaInvoices.Create,
            ProformaInvoices.Edit,
            ProformaInvoices.Submit,
            ProformaInvoices.Email,
            ProformaInvoices.Whatsapp,
            ProformaInvoices.GeneratePdf,

            // Advance Payments (Own scope)
            AdvancePayments.View,
            AdvancePayments.Create,
            AdvancePayments.Apply,

            // Sales Targets (Own scope)
            SalesTargets.View,
            SalesTargets.DashboardView,

            // Performance (Own scope)
            Performance.View,

            // Dispatch Logistics (Read-only tracking)
            DispatchLogistics.View,
            DispatchLogistics.Pod,

            // Accounting (Read-only own scope)
            Accounting.View,
            Accounting.Outstanding
        };
    }
}
