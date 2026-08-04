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

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.ApplyConfigurationsFromAssembly(typeof(ERPDbContext).Assembly);
        }
    }
}
