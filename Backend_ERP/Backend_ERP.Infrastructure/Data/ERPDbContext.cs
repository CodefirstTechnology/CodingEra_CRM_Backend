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

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.ApplyConfigurationsFromAssembly(typeof(ERPDbContext).Assembly);
        }
    }
}
