using ERP.Domain.Sales;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ERP.Infrastructure.Data.Configurations
{
    public class ProformaInvoiceConfiguration : IEntityTypeConfiguration<ProformaInvoice>
    {
        public void Configure(EntityTypeBuilder<ProformaInvoice> builder)
        {
            builder.ToTable("proforma_invoices");
            builder.HasKey(x => x.Id);

            builder.Property(x => x.TenantId).HasMaxLength(64).IsRequired().HasDefaultValue("default");
            builder.Property(x => x.PiNumber).HasMaxLength(64).IsRequired();
            builder.HasIndex(x => x.PiNumber).IsUnique();

            builder.Property(x => x.CustomerId).HasMaxLength(64);
            builder.Property(x => x.CustomerName).HasMaxLength(256).IsRequired();
            builder.Property(x => x.CustomerGstin).HasMaxLength(15);
            builder.Property(x => x.CustomerCategory).HasMaxLength(64).HasDefaultValue("Standard");
            builder.Property(x => x.PlaceOfSupplyStateCode).HasMaxLength(8).IsRequired().HasDefaultValue("27");
            builder.Property(x => x.ContactPerson).HasMaxLength(256);
            builder.Property(x => x.BillingAddress).HasMaxLength(2000);
            builder.Property(x => x.ShippingAddress).HasMaxLength(2000);
            builder.Property(x => x.Incoterm).HasMaxLength(16).HasDefaultValue("EXW");
            builder.Property(x => x.SalesPerson).HasMaxLength(256);
            builder.Property(x => x.Currency).HasMaxLength(8).IsRequired();
            builder.Property(x => x.ExchangeRate).HasPrecision(18, 6);
            builder.Property(x => x.ExchangeRateDate);
            builder.Property(x => x.BaseCurrencyEquivalentAmount).HasPrecision(18, 2).HasDefaultValue(0m);
            builder.Property(x => x.QuotationNumber).HasMaxLength(64);
            builder.Property(x => x.SalesOrderNumber).HasMaxLength(64);
            builder.Property(x => x.PaymentTerms).HasMaxLength(128);
            builder.Property(x => x.DeliveryTerms).HasMaxLength(128);
            builder.Property(x => x.CustomerNotes).HasMaxLength(4000);
            builder.Property(x => x.InternalNotes).HasMaxLength(4000);
            builder.Property(x => x.Status).HasMaxLength(64).IsRequired();
            builder.Property(x => x.Remarks).HasMaxLength(2000);
            builder.Property(x => x.CreatedBy).HasMaxLength(64);
            builder.Property(x => x.UpdatedBy).HasMaxLength(64);
            builder.Property(x => x.ConvertedInvoiceNumber).HasMaxLength(64);

            builder.Property(x => x.Subtotal).HasPrecision(18, 2);
            builder.Property(x => x.DiscountTotal).HasPrecision(18, 2);
            builder.Property(x => x.TaxTotal).HasPrecision(18, 2);
            builder.Property(x => x.GrandTotal).HasPrecision(18, 2);
            builder.Property(x => x.AdvanceReceivedAmount).HasPrecision(18, 2).HasDefaultValue(0m);
            builder.Property(x => x.PaymentStatus).HasMaxLength(32).HasDefaultValue("Unpaid");
            builder.Property(x => x.BillingType).HasMaxLength(32).HasDefaultValue("Full");
            builder.Property(x => x.MilestonePercentage).HasPrecision(5, 2);
            builder.Property(x => x.RequiresFinanceCreditReview).HasDefaultValue(false);
            builder.Property(x => x.CreditReviewReason);

            builder.Property(x => x.ApprovalToken).HasMaxLength(256);
            builder.Property(x => x.ApprovedBy).HasMaxLength(256);
            builder.Property(x => x.ApprovedDate);

            builder.Property(x => x.IsProductionReleased).HasDefaultValue(false);
            builder.Property(x => x.ProductionReleasedOn);
            builder.Property(x => x.ProductionReleasedBy).HasMaxLength(256);

            builder.Property(x => x.IsDispatchReleased).HasDefaultValue(false);
            builder.Property(x => x.DispatchReleasedOn);
            builder.Property(x => x.DispatchReleasedBy).HasMaxLength(256);

            // Optimistic concurrency mapped to PostgreSQL xmin
            builder.Property(x => x.ConcurrencyVersion).IsRowVersion();

            builder.HasIndex(x => x.Status);
            builder.HasIndex(x => x.InvoiceDate);
            builder.HasIndex(x => x.CustomerName);
            builder.HasIndex(x => x.IsDeleted);
            builder.HasIndex(x => new { x.TenantId, x.PiNumber });
            builder.HasIndex(x => new { x.TenantId, x.Status, x.InvoiceDate });
            builder.HasIndex(x => new { x.TenantId, x.CustomerId, x.PaymentStatus });
            builder.HasIndex(x => new { x.Status, x.ValidUntil, x.AdvanceReceivedAmount, x.IsDeleted })
                .HasDatabaseName("IX_proforma_invoices_expiry_scan");
            builder.HasIndex(x => new { x.SalesOrderId, x.Status, x.IsDeleted })
                .HasDatabaseName("IX_proforma_invoices_so_lookup");

            builder.HasOne(x => x.SalesOrder)
                .WithMany()
                .HasForeignKey(x => x.SalesOrderId)
                .OnDelete(DeleteBehavior.SetNull);

            builder.HasMany(x => x.Items)
                .WithOne(x => x.ProformaInvoice)
                .HasForeignKey(x => x.ProformaInvoiceId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasMany(x => x.StatusHistory)
                .WithOne(x => x.ProformaInvoice)
                .HasForeignKey(x => x.ProformaInvoiceId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasMany(x => x.ApprovalHistory)
                .WithOne(x => x.ProformaInvoice)
                .HasForeignKey(x => x.ProformaInvoiceId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasMany(x => x.SettlementAllocations)
                .WithOne(x => x.ProformaInvoice)
                .HasForeignKey(x => x.ProformaInvoiceId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }

    public class ProformaInvoiceItemConfiguration : IEntityTypeConfiguration<ProformaInvoiceItem>
    {
        public void Configure(EntityTypeBuilder<ProformaInvoiceItem> builder)
        {
            builder.ToTable("proforma_invoice_items");
            builder.HasKey(x => x.Id);

            builder.Property(x => x.LineKey).HasMaxLength(64).IsRequired();
            builder.Property(x => x.ItemCode).HasMaxLength(64).HasDefaultValue(string.Empty);
            builder.Property(x => x.ItemName).HasMaxLength(512).IsRequired();
            builder.Property(x => x.Description).HasMaxLength(2000);
            builder.Property(x => x.HsnCode).HasMaxLength(16).HasDefaultValue(string.Empty);
            builder.Property(x => x.Unit).HasMaxLength(32);
            builder.Property(x => x.Quantity).HasPrecision(18, 4);
            builder.Property(x => x.Rate).HasPrecision(18, 4);
            builder.Property(x => x.Discount).HasPrecision(9, 4);
            builder.Property(x => x.DiscountAmount).HasPrecision(18, 2).HasDefaultValue(0m);
            builder.Property(x => x.Gst).HasPrecision(9, 4);
            builder.Property(x => x.TaxAmount).HasPrecision(18, 2);
            builder.Property(x => x.Amount).HasPrecision(18, 2);

            builder.Property(x => x.OriginalSellingRate).HasPrecision(18, 4).HasDefaultValue(0m);
            builder.Property(x => x.FloorPriceSnapshot).HasPrecision(18, 4).HasDefaultValue(0m);
            builder.Property(x => x.MaxDiscountPctSnapshot).HasPrecision(9, 4).HasDefaultValue(0m);
            builder.Property(x => x.TaxSlabPercentSnapshot).HasPrecision(9, 4).HasDefaultValue(0m);
            builder.Property(x => x.PlaceOfSupplyStateCode).HasMaxLength(8).HasDefaultValue("27");

            builder.Property(x => x.ConvertedQuantity).HasPrecision(18, 4).HasDefaultValue(0m);
            builder.HasIndex(x => new { x.ProformaInvoiceId, x.SortOrder });
            builder.HasIndex(x => x.SalesOrderItemId);
        }
    }

    public class ProformaInvoiceStatusHistoryConfiguration : IEntityTypeConfiguration<ProformaInvoiceStatusHistory>
    {
        public void Configure(EntityTypeBuilder<ProformaInvoiceStatusHistory> builder)
        {
            builder.ToTable("proforma_invoice_status_histories");
            builder.HasKey(x => x.Id);
            builder.Property(x => x.EntryKey).HasMaxLength(64).IsRequired();
            builder.Property(x => x.OldStatus).HasMaxLength(64);
            builder.Property(x => x.NewStatus).HasMaxLength(64).IsRequired();
            builder.Property(x => x.Remarks).HasMaxLength(2000);
            builder.Property(x => x.ChangedBy).HasMaxLength(64);
            builder.HasIndex(x => new { x.ProformaInvoiceId, x.ChangedOn });
        }
    }

    public class ProformaInvoiceApprovalHistoryConfiguration : IEntityTypeConfiguration<ProformaInvoiceApprovalHistory>
    {
        public void Configure(EntityTypeBuilder<ProformaInvoiceApprovalHistory> builder)
        {
            builder.ToTable("proforma_invoice_approval_histories");
            builder.HasKey(x => x.Id);
            builder.Property(x => x.EntryKey).HasMaxLength(64).IsRequired();
            builder.Property(x => x.ApprovalLevel).HasMaxLength(64);
            builder.Property(x => x.Decision).HasMaxLength(64);
            builder.Property(x => x.Remarks).HasMaxLength(2000);
            builder.Property(x => x.ApprovedBy).HasMaxLength(64);
            builder.Property(x => x.ApproverUserId).HasMaxLength(64);
            builder.Property(x => x.ApprovalToken).HasMaxLength(256);
            builder.Property(x => x.IpAddress).HasMaxLength(64);
            builder.HasIndex(x => new { x.ProformaInvoiceId, x.ApprovedOn });
        }
    }

    public class ProformaInvoiceDocumentSequenceConfiguration : IEntityTypeConfiguration<ProformaInvoiceDocumentSequence>
    {
        public void Configure(EntityTypeBuilder<ProformaInvoiceDocumentSequence> builder)
        {
            builder.ToTable("proforma_invoice_document_sequences");
            builder.HasKey(x => x.Id);
            builder.HasIndex(x => x.Year).IsUnique();
        }
    }

    public class ProformaInvoiceSettlementAllocationConfiguration : IEntityTypeConfiguration<ProformaInvoiceSettlementAllocation>
    {
        public void Configure(EntityTypeBuilder<ProformaInvoiceSettlementAllocation> builder)
        {
            builder.ToTable("proforma_invoice_settlement_allocations");
            builder.HasKey(x => x.Id);

            builder.Property(x => x.TenantId).HasMaxLength(64).IsRequired().HasDefaultValue("default");
            builder.Property(x => x.AllocatedAmount).HasPrecision(18, 2);
            builder.Property(x => x.ExchangeRateAtSettlement).HasPrecision(18, 4).HasDefaultValue(1.0000m);
            builder.Property(x => x.BaseCurrencyAllocatedAmount).HasPrecision(18, 2);
            builder.Property(x => x.ReceiptVoucherNumber).HasMaxLength(64);
            builder.Property(x => x.AllocatedBy).HasMaxLength(64);
            builder.Property(x => x.Remarks).HasMaxLength(500);

            builder.HasOne(x => x.ProformaInvoice)
                .WithMany(x => x.SettlementAllocations)
                .HasForeignKey(x => x.ProformaInvoiceId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(x => x.AdvancePayment)
                .WithMany()
                .HasForeignKey(x => x.AdvancePaymentId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasIndex(x => new { x.ProformaInvoiceId, x.AdvancePaymentId });
            builder.HasIndex(x => new { x.TenantId, x.AllocatedOn });
        }
    }
}
