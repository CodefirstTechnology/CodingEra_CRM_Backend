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

        // Procurement Phase 2 PR & RFQ DbSets
        public DbSet<ERP.Domain.Procurement.PurchaseRequisition> PurchaseRequisitions => Set<ERP.Domain.Procurement.PurchaseRequisition>();

        public DbSet<ERP.Domain.Procurement.PurchaseRequisitionLine> PurchaseRequisitionLines => Set<ERP.Domain.Procurement.PurchaseRequisitionLine>();

        public DbSet<ERP.Domain.Procurement.PurchaseRequisitionStatusHistory> PurchaseRequisitionStatusHistories => Set<ERP.Domain.Procurement.PurchaseRequisitionStatusHistory>();

        public DbSet<ERP.Domain.Procurement.PurchaseRequisitionDocumentSequence> PurchaseRequisitionDocumentSequences => Set<ERP.Domain.Procurement.PurchaseRequisitionDocumentSequence>();

        public DbSet<ERP.Domain.Procurement.RequestForQuotation> RequestForQuotations => Set<ERP.Domain.Procurement.RequestForQuotation>();

        public DbSet<ERP.Domain.Procurement.RequestForQuotationLine> RequestForQuotationLines => Set<ERP.Domain.Procurement.RequestForQuotationLine>();

        public DbSet<ERP.Domain.Procurement.RequestForQuotationVendor> RequestForQuotationVendors => Set<ERP.Domain.Procurement.RequestForQuotationVendor>();

        public DbSet<ERP.Domain.Procurement.RequestForQuotationStatusHistory> RequestForQuotationStatusHistories => Set<ERP.Domain.Procurement.RequestForQuotationStatusHistory>();

        public DbSet<ERP.Domain.Procurement.RFQDocumentSequence> RFQDocumentSequences => Set<ERP.Domain.Procurement.RFQDocumentSequence>();

        // Procurement Phase 3 Vendor Quotations & Vendor Comparison DbSets
        public DbSet<ERP.Domain.Procurement.VendorQuotation> VendorQuotations => Set<ERP.Domain.Procurement.VendorQuotation>();

        public DbSet<ERP.Domain.Procurement.VendorQuotationLine> VendorQuotationLines => Set<ERP.Domain.Procurement.VendorQuotationLine>();

        public DbSet<ERP.Domain.Procurement.VendorQuotationDocumentSequence> VendorQuotationDocumentSequences => Set<ERP.Domain.Procurement.VendorQuotationDocumentSequence>();

        public DbSet<ERP.Domain.Procurement.VendorComparison> VendorComparisons => Set<ERP.Domain.Procurement.VendorComparison>();

        public DbSet<ERP.Domain.Procurement.VendorComparisonEntry> VendorComparisonEntries => Set<ERP.Domain.Procurement.VendorComparisonEntry>();

        public DbSet<ERP.Domain.Procurement.VendorComparisonStatusHistory> VendorComparisonStatusHistories => Set<ERP.Domain.Procurement.VendorComparisonStatusHistory>();

        public DbSet<ERP.Domain.Procurement.VendorComparisonDocumentSequence> VendorComparisonDocumentSequences => Set<ERP.Domain.Procurement.VendorComparisonDocumentSequence>();

        // Procurement Phase 4 Purchase Order DbSets
        public DbSet<ERP.Domain.Procurement.PurchaseOrder> PurchaseOrders => Set<ERP.Domain.Procurement.PurchaseOrder>();

        public DbSet<ERP.Domain.Procurement.PurchaseOrderLine> PurchaseOrderLines => Set<ERP.Domain.Procurement.PurchaseOrderLine>();

        public DbSet<ERP.Domain.Procurement.PurchaseOrderStatusHistory> PurchaseOrderStatusHistories => Set<ERP.Domain.Procurement.PurchaseOrderStatusHistory>();

        public DbSet<ERP.Domain.Procurement.PurchaseOrderApprovalHistory> PurchaseOrderApprovalHistories => Set<ERP.Domain.Procurement.PurchaseOrderApprovalHistory>();

        public DbSet<ERP.Domain.Procurement.PurchaseOrderDocumentSequence> PurchaseOrderDocumentSequences => Set<ERP.Domain.Procurement.PurchaseOrderDocumentSequence>();

        // Procurement Phase 5 Goods Receipt (GRN) DbSets
        public DbSet<ERP.Domain.Procurement.GoodsReceipt> GoodsReceipts => Set<ERP.Domain.Procurement.GoodsReceipt>();

        public DbSet<ERP.Domain.Procurement.GoodsReceiptItem> GoodsReceiptItems => Set<ERP.Domain.Procurement.GoodsReceiptItem>();

        public DbSet<ERP.Domain.Procurement.GoodsReceiptStatusHistory> GoodsReceiptStatusHistories => Set<ERP.Domain.Procurement.GoodsReceiptStatusHistory>();

        public DbSet<ERP.Domain.Procurement.GoodsReceiptDocumentSequence> GoodsReceiptDocumentSequences => Set<ERP.Domain.Procurement.GoodsReceiptDocumentSequence>();

        // Procurement Phase 6 Quality Control / Incoming Inspection DbSets
        public DbSet<ERP.Domain.Procurement.IncomingInspection> IncomingInspections => Set<ERP.Domain.Procurement.IncomingInspection>();

        public DbSet<ERP.Domain.Procurement.IncomingChecklistItem> IncomingChecklistItems => Set<ERP.Domain.Procurement.IncomingChecklistItem>();

        public DbSet<ERP.Domain.Procurement.InProcessCheck> InProcessChecks => Set<ERP.Domain.Procurement.InProcessCheck>();

        public DbSet<ERP.Domain.Procurement.FinalInspection> FinalInspections => Set<ERP.Domain.Procurement.FinalInspection>();

        public DbSet<ERP.Domain.Procurement.FinalInspectionParameter> FinalInspectionParameters => Set<ERP.Domain.Procurement.FinalInspectionParameter>();

        public DbSet<ERP.Domain.Procurement.LoadTestReport> LoadTestReports => Set<ERP.Domain.Procurement.LoadTestReport>();

        public DbSet<ERP.Domain.Procurement.TestCertificate> TestCertificates => Set<ERP.Domain.Procurement.TestCertificate>();

        public DbSet<ERP.Domain.Procurement.RejectionAnalysis> RejectionAnalyses => Set<ERP.Domain.Procurement.RejectionAnalysis>();

        public DbSet<ERP.Domain.Procurement.QualityControlDocumentSequence> QualityControlDocumentSequences => Set<ERP.Domain.Procurement.QualityControlDocumentSequence>();

        // Procurement Phase 7 Store Inventory & Stock Transactions DbSets
        public DbSet<ERP.Domain.Procurement.Warehouse> Warehouses => Set<ERP.Domain.Procurement.Warehouse>();

        public DbSet<ERP.Domain.Procurement.RawMaterial> RawMaterials => Set<ERP.Domain.Procurement.RawMaterial>();

        public DbSet<ERP.Domain.Procurement.FinishedGood> FinishedGoods => Set<ERP.Domain.Procurement.FinishedGood>();

        public DbSet<ERP.Domain.Procurement.StockTransaction> StockTransactions => Set<ERP.Domain.Procurement.StockTransaction>();

        public DbSet<ERP.Domain.Procurement.InventoryBatch> InventoryBatches => Set<ERP.Domain.Procurement.InventoryBatch>();

        public DbSet<ERP.Domain.Procurement.StockTransfer> StockTransfers => Set<ERP.Domain.Procurement.StockTransfer>();

        public DbSet<ERP.Domain.Procurement.StockTransferItem> StockTransferItems => Set<ERP.Domain.Procurement.StockTransferItem>();

        public DbSet<ERP.Domain.Procurement.StockAlert> StockAlerts => Set<ERP.Domain.Procurement.StockAlert>();

        public DbSet<ERP.Domain.Procurement.PhysicalVerification> PhysicalVerifications => Set<ERP.Domain.Procurement.PhysicalVerification>();

        public DbSet<ERP.Domain.Procurement.PhysicalVerificationLine> PhysicalVerificationLines => Set<ERP.Domain.Procurement.PhysicalVerificationLine>();

        public DbSet<ERP.Domain.Procurement.StoreInventoryDocumentSequence> StoreInventoryDocumentSequences => Set<ERP.Domain.Procurement.StoreInventoryDocumentSequence>();

        // Procurement Phase 8 Purchase Bills & 3-Way Matching DbSets
        public DbSet<ERP.Domain.Procurement.PurchaseBill> PurchaseBills => Set<ERP.Domain.Procurement.PurchaseBill>();

        public DbSet<ERP.Domain.Procurement.PurchaseBillLine> PurchaseBillLines => Set<ERP.Domain.Procurement.PurchaseBillLine>();

        public DbSet<ERP.Domain.Procurement.PurchaseBillHistory> PurchaseBillHistories => Set<ERP.Domain.Procurement.PurchaseBillHistory>();

        public DbSet<ERP.Domain.Procurement.PurchaseBillDocumentSequence> PurchaseBillDocumentSequences => Set<ERP.Domain.Procurement.PurchaseBillDocumentSequence>();

        // Procurement Reports & Audit Trail DbSets
        public DbSet<ERP.Domain.Procurement.ProcurementAuditTrailEntry> ProcurementAuditTrailEntries => Set<ERP.Domain.Procurement.ProcurementAuditTrailEntry>();

        // Production Module DbSets
        public DbSet<ERP.Domain.Production.BillOfMaterials> BillOfMaterials => Set<ERP.Domain.Production.BillOfMaterials>();
        public DbSet<ERP.Domain.Production.BomMaterialLine> BomMaterialLines => Set<ERP.Domain.Production.BomMaterialLine>();
        public DbSet<ERP.Domain.Production.ProductionPlan> ProductionPlans => Set<ERP.Domain.Production.ProductionPlan>();
        public DbSet<ERP.Domain.Production.WorkOrder> WorkOrders => Set<ERP.Domain.Production.WorkOrder>();
        public DbSet<ERP.Domain.Production.ProductionSchedule> ProductionSchedules => Set<ERP.Domain.Production.ProductionSchedule>();
        public DbSet<ERP.Domain.Production.Machine> Machines => Set<ERP.Domain.Production.Machine>();
        public DbSet<ERP.Domain.Production.ProductionEntry> ProductionEntries => Set<ERP.Domain.Production.ProductionEntry>();
        public DbSet<ERP.Domain.Production.MaterialConsumption> MaterialConsumptions => Set<ERP.Domain.Production.MaterialConsumption>();
        public DbSet<ERP.Domain.Production.RejectionRecord> RejectionRecords => Set<ERP.Domain.Production.RejectionRecord>();
        public DbSet<ERP.Domain.Production.ProductionDocumentSequence> ProductionDocumentSequences => Set<ERP.Domain.Production.ProductionDocumentSequence>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.ApplyConfigurationsFromAssembly(typeof(ERPDbContext).Assembly);
        }
    }
}
