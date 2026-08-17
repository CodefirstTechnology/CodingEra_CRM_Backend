using ERP.Domain.Sales;
using Microsoft.EntityFrameworkCore;

namespace ERP.Infrastructure.Data
{
    public class ERPDbContext : DbContext
    {
        public ERPDbContext(DbContextOptions<ERPDbContext> options) : base(options)
        {
        }

        public DbSet<SalesOrder> SalesOrders => Set<SalesOrder>();

        public DbSet<SalesOrderItem> SalesOrderItems => Set<SalesOrderItem>();

        public DbSet<SalesOrderStatusHistory> SalesOrderStatusHistories => Set<SalesOrderStatusHistory>();

        public DbSet<SalesOrderEmailHistory> SalesOrderEmailHistories => Set<SalesOrderEmailHistory>();

        public DbSet<SalesOrderDocumentSequence> SalesOrderDocumentSequences => Set<SalesOrderDocumentSequence>();

        public DbSet<ProformaInvoice> ProformaInvoices => Set<ProformaInvoice>();

        public DbSet<ProformaInvoiceItem> ProformaInvoiceItems => Set<ProformaInvoiceItem>();

        public DbSet<ProformaInvoiceStatusHistory> ProformaInvoiceStatusHistories => Set<ProformaInvoiceStatusHistory>();

        public DbSet<ProformaInvoiceApprovalHistory> ProformaInvoiceApprovalHistories => Set<ProformaInvoiceApprovalHistory>();

        public DbSet<ProformaInvoiceDocumentSequence> ProformaInvoiceDocumentSequences => Set<ProformaInvoiceDocumentSequence>();

        public DbSet<AdvancePayment> AdvancePayments => Set<AdvancePayment>();

        public DbSet<AdvancePaymentApplication> AdvancePaymentApplications => Set<AdvancePaymentApplication>();

        public DbSet<AdvancePaymentTimeline> AdvancePaymentTimelines => Set<AdvancePaymentTimeline>();

        public DbSet<AdvancePaymentDocumentSequence> AdvancePaymentDocumentSequences => Set<AdvancePaymentDocumentSequence>();

        public DbSet<SalesTarget> SalesTargets => Set<SalesTarget>();

        public DbSet<SalesTargetAssignment> SalesTargetAssignments => Set<SalesTargetAssignment>();

        public DbSet<SalesTargetProgressHistory> SalesTargetProgressHistories => Set<SalesTargetProgressHistory>();

        public DbSet<SalesTargetStatusHistory> SalesTargetStatusHistories => Set<SalesTargetStatusHistory>();

        public DbSet<SalesTargetDocumentSequence> SalesTargetDocumentSequences => Set<SalesTargetDocumentSequence>();

        public DbSet<PerformanceSnapshot> PerformanceSnapshots => Set<PerformanceSnapshot>();

        public DbSet<PerformanceHistory> PerformanceHistory => Set<PerformanceHistory>();

        public DbSet<PerformanceExportHistory> PerformanceExportHistory => Set<PerformanceExportHistory>();

        public DbSet<PriceList> PriceLists => Set<PriceList>();

        public DbSet<PriceListItem> PriceListItems => Set<PriceListItem>();

        public DbSet<PriceListHistory> PriceListHistories => Set<PriceListHistory>();

        public DbSet<PriceListDocumentSequence> PriceListDocumentSequences => Set<PriceListDocumentSequence>();

        public DbSet<DiscountApproval> DiscountApprovals => Set<DiscountApproval>();

        public DbSet<DiscountApprovalHistory> DiscountApprovalHistories => Set<DiscountApprovalHistory>();

        public DbSet<DiscountApprovalComment> DiscountApprovalComments => Set<DiscountApprovalComment>();

        public DbSet<DiscountApprovalDocumentSequence> DiscountApprovalDocumentSequences =>
            Set<DiscountApprovalDocumentSequence>();

        public DbSet<QuotationApproval> QuotationApprovals => Set<QuotationApproval>();

        public DbSet<QuotationApprovalHistory> QuotationApprovalHistories => Set<QuotationApprovalHistory>();

        public DbSet<QuotationApprovalComment> QuotationApprovalComments => Set<QuotationApprovalComment>();

        public DbSet<QuotationApprovalDocumentSequence> QuotationApprovalDocumentSequences =>
            Set<QuotationApprovalDocumentSequence>();

        // Procurement Phase 1 Vendor Master DbSets
        public DbSet<ERP.Domain.Procurement.Vendor> Vendors => Set<ERP.Domain.Procurement.Vendor>();

        public DbSet<ERP.Domain.Procurement.VendorContact> VendorContacts => Set<ERP.Domain.Procurement.VendorContact>();

        public DbSet<ERP.Domain.Procurement.VendorAddress> VendorAddresses => Set<ERP.Domain.Procurement.VendorAddress>();

        public DbSet<ERP.Domain.Procurement.VendorCompliance> VendorCompliances => Set<ERP.Domain.Procurement.VendorCompliance>();

        public DbSet<ERP.Domain.Procurement.VendorPaymentTerm> VendorPaymentTerms => Set<ERP.Domain.Procurement.VendorPaymentTerm>();

        public DbSet<ERP.Domain.Procurement.VendorStatusHistory> VendorStatusHistories => Set<ERP.Domain.Procurement.VendorStatusHistory>();

        public DbSet<ERP.Domain.Procurement.VendorDocumentSequence> VendorDocumentSequences => Set<ERP.Domain.Procurement.VendorDocumentSequence>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.ApplyConfigurationsFromAssembly(typeof(ERPDbContext).Assembly);
        }
    }
}
