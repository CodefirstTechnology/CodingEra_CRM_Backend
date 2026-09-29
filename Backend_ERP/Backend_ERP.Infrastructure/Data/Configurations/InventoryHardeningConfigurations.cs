using Backend_ERP.Domain.Entities;
using ERP.Domain.Procurement;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Backend_ERP.Infrastructure.Data.Configurations
{
    public class RawMaterialHardeningConfiguration : IEntityTypeConfiguration<RawMaterial>
    {
        public void Configure(EntityTypeBuilder<RawMaterial> builder)
        {
            builder.Property(x => x.CatchWeightTolerancePercent)
                .HasColumnName("CatchWeightTolerancePercent")
                .HasColumnType("numeric(5,2)")
                .HasDefaultValue(3.00m);

            // PostgreSQL system xmin row versioning mapping
            builder.Property(x => x.RowVersion)
                .HasColumnName("xmin")
                .HasColumnType("uint")
                .ValueGeneratedOnAddOrUpdate()
                .IsRowVersion();
        }
    }

    public class StockTransactionHardeningConfiguration : IEntityTypeConfiguration<StockTransaction>
    {
        public void Configure(EntityTypeBuilder<StockTransaction> builder)
        {
            builder.Property(x => x.IdempotencyKey)
                .HasColumnName("IdempotencyKey")
                .IsRequired();

            builder.Property(x => x.SecondaryQty)
                .HasColumnName("SecondaryQty")
                .HasColumnType("numeric(18,4)");

            builder.Property(x => x.UnitCost)
                .HasColumnName("UnitCost")
                .HasColumnType("numeric(18,6)");
        }
    }

    public class OutboxMessageEntityTypeConfiguration : IEntityTypeConfiguration<OutboxMessageEntity>
    {
        public void Configure(EntityTypeBuilder<OutboxMessageEntity> builder)
        {
            builder.ToTable("outbox_messages");
            builder.HasKey(x => x.OutboxId);

            builder.Property(x => x.OutboxId).HasColumnName("outbox_id");
            builder.Property(x => x.EventType).HasColumnName("event_type").HasMaxLength(250).IsRequired();
            builder.Property(x => x.PayloadJson).HasColumnName("payload_json").IsRequired();
            builder.Property(x => x.CreatedAt).HasColumnName("created_at");
            builder.Property(x => x.ProcessedAt).HasColumnName("processed_at");
            builder.Property(x => x.ErrorLog).HasColumnName("error_log");
            builder.Property(x => x.RetryCount).HasColumnName("retry_count");
        }
    }
}
