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

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.ApplyConfigurationsFromAssembly(typeof(ERPDbContext).Assembly);
        }
    }
}
