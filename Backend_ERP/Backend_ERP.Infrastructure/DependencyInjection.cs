using ERP.Application.Sales;
using ERP.Infrastructure.Data;
using ERP.Infrastructure.Sales;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace ERP.Infrastructure
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddInfrastructure(
            this IServiceCollection services,
            IConfiguration configuration)
        {
            services.AddDbContext<ERPDbContext>((sp, options) =>
            {
                options.UseNpgsql(configuration.GetConnectionString("DefaultConnection"));
                var interceptor = sp.GetService<ERP.Infrastructure.Data.Interceptors.PricingTransactionCommitInterceptor>();
                if (interceptor != null)
                {
                    options.AddInterceptors(interceptor);
                }
            });

            services.AddHttpContextAccessor();
            services.AddScoped<ERP.Application.Common.Security.ICurrentUser, ERP.Infrastructure.Security.CurrentUser>();
            services.AddScoped<ERP.Application.Common.Security.IErpAuthorizationService, ERP.Infrastructure.Security.ErpAuthorizationService>();
            services.AddScoped<ERP.Application.Common.Security.IErpWorkflowAuthorizationService, ERP.Infrastructure.Security.ErpWorkflowAuthorizationService>();
            services.AddScoped<ERP.Application.Dashboard.IDashboardService, ERP.Infrastructure.Dashboard.DashboardService>();

            services.AddScoped<ISalesOrderAllocationService, SalesOrderAllocationService>();
            services.AddScoped<ICustomerCreditAssessmentService, CustomerCreditAssessmentService>();
            services.AddScoped<ISalesOrderAdvanceGateService, SalesOrderAdvanceGateService>();
            services.AddScoped<IMtoProductionTriggerService, MtoProductionTriggerService>();
            services.AddScoped<ISalesOrderService, SalesOrderService>();
            services.AddScoped<IProformaInvoiceRepository, ProformaInvoiceRepository>();
            services.AddScoped<IProformaInvoiceService, ProformaInvoiceService>();
            services.AddScoped<IProformaApprovalService, ProformaApprovalService>();
            services.AddScoped<IProformaSettlementService, ProformaSettlementService>();
            services.AddScoped<IProformaDashboardService, ProformaDashboardService>();
            services.AddScoped<IProformaReportService, ProformaReportService>();
            services.AddScoped<IAdvancePaymentNumberingService, AdvancePaymentNumberingService>();
            services.AddScoped<IAdvancePaymentRepository, AdvancePaymentRepository>();
            services.AddScoped<IAdvancePaymentService, AdvancePaymentService>();
            services.AddScoped<IAdvancePaymentSettlementService, AdvancePaymentSettlementService>();
            services.AddScoped<IAdvancePaymentDashboardService, AdvancePaymentDashboardService>();
            services.AddScoped<IAdvancePaymentReportService, AdvancePaymentReportService>();
            services.AddScoped<ISalesTargetNumberingService, SalesTargetNumberingService>();
            services.AddScoped<ISalesTargetRepository, SalesTargetRepository>();
            services.AddScoped<ISalesTargetService, SalesTargetService>();
            services.AddScoped<ERP.Infrastructure.Sales.Caching.ITargetRedisCacheManager, ERP.Infrastructure.Sales.Caching.TargetRedisCacheManager>();
            services.AddScoped<ISalesTargetRealizationEngine, SalesTargetRealizationEngine>();
            services.AddScoped<ISalesTargetDashboardService, SalesTargetDashboardService>();
            services.AddScoped<ISalesTargetReportService, SalesTargetReportService>();
            services.AddScoped<ERP.Application.Common.Events.INotificationHandler<ERP.Domain.Sales.Events.AdvancePaymentClearedDomainEvent>, ERP.Infrastructure.Sales.Events.AdvancePaymentTargetEventHandler>();
            services.AddScoped<ERP.Application.Common.Events.INotificationHandler<ERP.Domain.Sales.Events.AdvancePaymentRefundedDomainEvent>, ERP.Infrastructure.Sales.Events.AdvancePaymentTargetEventHandler>();
            services.AddScoped<ERP.Application.Common.Events.INotificationHandler<ERP.Domain.Sales.Events.SalesInvoiceDispatchedDomainEvent>, ERP.Infrastructure.Sales.Events.InvoiceDispatchTargetEventHandler>();
            services.AddScoped<ERP.Application.Common.Events.INotificationHandler<ERP.Domain.Sales.Events.SalesOrderConfirmedDomainEvent>, ERP.Infrastructure.Sales.Events.SalesOrderConfirmedEventHandler>();
            services.AddScoped<ERP.Application.Common.Events.INotificationHandler<ERP.Domain.Sales.Events.SalesOrderCancelledDomainEvent>, ERP.Infrastructure.Sales.Events.SalesOrderCancelledEventHandler>();
            services.AddScoped<ERP.Infrastructure.Sales.Events.SalesOrderConfirmedEventHandler>();
            services.AddScoped<ERP.Infrastructure.Sales.Events.SalesOrderCancelledEventHandler>();
            services.AddScoped<IPerformanceRepository, PerformanceRepository>();
            services.AddScoped<IPerformanceService, PerformanceService>();
            services.AddScoped<IPerformanceRecalculationService, PerformanceRecalculationService>();
            services.AddScoped<ERP.Application.Sales.IPerformanceScoringEngine, ERP.Infrastructure.Sales.PerformanceScoringEngine>();
            services.AddScoped<ERP.Application.Sales.IPerformanceRedisCacheManager, ERP.Infrastructure.Sales.Caching.PerformanceRedisCacheManager>();
            services.AddScoped<ERP.Application.Sales.ISalesIncentiveEngine, ERP.Infrastructure.Sales.SalesIncentiveEngine>();
            services.AddScoped<ERP.Application.Sales.IPerformanceHubService, ERP.Infrastructure.Sales.PerformanceHubService>();
            services.AddScoped<ERP.Application.Sales.IPerformanceReportService, ERP.Infrastructure.Sales.PerformanceReportService>();
            services.AddMemoryCache();
            var redisConn = configuration.GetConnectionString("Redis");
            if (!string.IsNullOrWhiteSpace(redisConn))
            {
                try
                {
                    services.AddSingleton<StackExchange.Redis.IConnectionMultiplexer>(
                        StackExchange.Redis.ConnectionMultiplexer.Connect(redisConn));
                }
                catch
                {
                    // Fall back to L1 MemoryCache gracefully
                }
            }

            services.AddScoped<ERP.Application.Common.Interfaces.ITenantSettingsService, ERP.Infrastructure.Services.TenantSettingsService>();
            services.AddScoped<ERP.Application.Sales.Pricing.IHybridPricingCache, ERP.Infrastructure.Sales.Caching.HybridPricingCacheService>();
            services.AddScoped<ERP.Application.Sales.Pricing.IPriceResolutionEngine, ERP.Infrastructure.Sales.Pricing.PriceResolutionEngine>();
            services.AddScoped<ERP.Application.Sales.Pricing.IBomMarginAuditService, ERP.Infrastructure.Sales.Pricing.BomMarginAuditService>();
            services.AddScoped<ERP.Application.Sales.Quotations.IQuotationLineSnapshotService, ERP.Application.Sales.Quotations.QuotationLineSnapshotService>();
            services.AddScoped<ERP.Application.Sales.Approvals.ICommercialComplianceService, ERP.Infrastructure.Sales.CommercialComplianceService>();
            services.AddScoped<ERP.Infrastructure.Data.Interceptors.PricingTransactionCommitInterceptor>();

            services.AddScoped<IPriceListNumberingService, PriceListNumberingService>();
            services.AddScoped<IPriceListRepository, PriceListRepository>();
            services.AddScoped<IPriceListService, PriceListService>();
            services.AddScoped<IDiscountApprovalNumberingService, DiscountApprovalNumberingService>();
            services.AddScoped<IDiscountApprovalRepository, DiscountApprovalRepository>();
            services.AddScoped<IDiscountApprovalService, DiscountApprovalService>();
            services.AddScoped<IQuotationApprovalNumberingService, QuotationApprovalNumberingService>();
            services.AddScoped<IQuotationApprovalRepository, QuotationApprovalRepository>();
            services.AddScoped<IQuotationApprovalService, QuotationApprovalService>();
            services.AddScoped<IQuotationService, QuotationService>();
            services.AddScoped<ERP.Application.Sales.Quotations.IQuotationRevisionService, ERP.Infrastructure.Sales.QuotationRevisionService>();
            services.AddScoped<ERP.Application.Sales.Quotations.IInventoryAllocationService, ERP.Infrastructure.Sales.InventoryAllocationService>();
            services.AddScoped<ERP.Application.Sales.Quotations.IStockDecrementService, ERP.Infrastructure.Sales.StockDecrementService>();
            services.AddScoped<ERP.Application.Sales.Quotations.IQuotationConversionService, ERP.Infrastructure.Sales.QuotationConversionService>();

            // Procurement Phase 1 Vendor Master Services
            services.AddScoped<ERP.Infrastructure.Procurement.VendorNumberingService>();
            services.AddScoped<ERP.Application.Procurement.IVendorService, ERP.Infrastructure.Procurement.VendorService>();

            // Procurement Phase 2 PR & RFQ Services
            services.AddScoped<ERP.Infrastructure.Procurement.PurchaseRequisitionNumberingService>();
            services.AddScoped<ERP.Application.Procurement.IPurchaseRequisitionService, ERP.Infrastructure.Procurement.PurchaseRequisitionService>();

            services.AddScoped<ERP.Infrastructure.Procurement.RFQNumberingService>();
            services.AddScoped<ERP.Application.Procurement.IRFQService, ERP.Infrastructure.Procurement.RFQService>();

            // Procurement Phase 3 Vendor Quotations & Vendor Comparison Services
            services.AddScoped<ERP.Infrastructure.Procurement.VendorQuotationNumberingService>();
            services.AddScoped<ERP.Application.Procurement.IVendorQuotationService, ERP.Infrastructure.Procurement.VendorQuotationService>();

            services.AddScoped<ERP.Infrastructure.Procurement.VendorComparisonNumberingService>();
            services.AddScoped<ERP.Application.Procurement.IVendorComparisonService, ERP.Infrastructure.Procurement.VendorComparisonService>();

            // Procurement Phase 4 Purchase Order Services
            services.AddScoped<ERP.Infrastructure.Procurement.PurchaseOrderNumberingService>();
            services.AddScoped<ERP.Application.Procurement.IPurchaseOrderService, ERP.Infrastructure.Procurement.PurchaseOrderService>();

            // Procurement Phase 5 Goods Receipt (GRN) Services
            services.AddScoped<ERP.Infrastructure.Procurement.GoodsReceiptNumberingService>();
            services.AddScoped<ERP.Application.Procurement.IGoodsReceiptService, ERP.Infrastructure.Procurement.GoodsReceiptService>();

            // Procurement Phase 6 Quality Control / Incoming Inspection Services
            services.AddScoped<ERP.Infrastructure.Procurement.QualityControlNumberingService>();
            services.AddScoped<ERP.Application.Procurement.IQualityControlService, ERP.Infrastructure.Procurement.QualityControlService>();

            // Procurement Phase 7 Store Inventory & Stock Transactions Services
            services.AddScoped<ERP.Infrastructure.Procurement.StoreInventoryNumberingService>();
            services.AddScoped<ERP.Application.Procurement.IStoreInventoryService, ERP.Infrastructure.Procurement.StoreInventoryService>();

            // Procurement Phase 8 Purchase Bills & 3-Way Matching Services
            services.AddScoped<ERP.Infrastructure.Procurement.PurchaseBillNumberingService>();
            services.AddScoped<ERP.Application.Procurement.IPurchaseBillService, ERP.Infrastructure.Procurement.PurchaseBillService>();
            services.AddScoped<ERP.Application.Procurement.IVendorLedgerService, ERP.Infrastructure.Procurement.VendorLedgerService>();
            services.AddScoped<ERP.Application.Procurement.IVendorPaymentService, ERP.Infrastructure.Procurement.VendorPaymentService>();

            // Procurement Reports & Audit Trail Services
            services.AddScoped<ERP.Application.Procurement.IProcurementAuditTrailService, ERP.Infrastructure.Procurement.ProcurementAuditTrailService>();
            services.AddScoped<ERP.Application.Procurement.IProcurementReportsService, ERP.Infrastructure.Procurement.ProcurementReportsService>();

            // Production Services
            services.AddScoped<ERP.Application.Production.IProductionService, ERP.Infrastructure.Production.ProductionService>();
            services.AddScoped<Backend_ERP.Infrastructure.Production.IAtomicProductionEntryHandler, Backend_ERP.Infrastructure.Production.AtomicProductionEntryHandler>();
            services.AddScoped<Backend_ERP.Infrastructure.Production.IDailyProductionReportService, Backend_ERP.Infrastructure.Production.DailyProductionReportService>();

            // Dispatch & Logistics - Dispatch Planning Services
            services.AddScoped<ERP.Infrastructure.Sales.DispatchPlanningNumberingService>();
            services.AddScoped<ERP.Application.Sales.IDispatchPlanningService, ERP.Infrastructure.Sales.DispatchPlanningService>();

            // Dispatch & Logistics - Vehicle Assignment Services
            services.AddScoped<ERP.Infrastructure.Sales.VehicleAssignmentNumberingService>();
            services.AddScoped<ERP.Infrastructure.Sales.TransportNumberingService>();
            services.AddScoped<ERP.Infrastructure.Sales.LrNumberingService>();
            services.AddScoped<ERP.Infrastructure.Sales.EwayNumberingService>();
            services.AddScoped<ERP.Infrastructure.Sales.PodNumberingService>();
            services.AddScoped<ERP.Application.Sales.IVehicleAssignmentService, ERP.Infrastructure.Sales.VehicleAssignmentService>();
            services.AddScoped<ERP.Application.Sales.ITransportDetailsService, ERP.Infrastructure.Sales.TransportDetailsService>();
            services.AddScoped<ERP.Application.Sales.ILrManagementService, ERP.Infrastructure.Sales.LrManagementService>();
            services.AddScoped<ERP.Application.Sales.IEwayBillService, ERP.Infrastructure.Sales.EwayBillService>();
            services.AddScoped<ERP.Application.Sales.IDispatchStatusService, ERP.Infrastructure.Sales.DispatchStatusService>();
            services.AddScoped<ERP.Application.Sales.IPodService, ERP.Infrastructure.Sales.PodService>();

            // Accounting Services
            services.AddScoped<ERP.Infrastructure.Accounting.CustomerLedgerNumberingService>();
            services.AddScoped<ERP.Infrastructure.Accounting.GstNumberingService>();
            services.AddScoped<ERP.Infrastructure.Accounting.PaymentNumberingService>();
            services.AddScoped<ERP.Infrastructure.Accounting.ReceiptNumberingService>();
            services.AddScoped<ERP.Infrastructure.Accounting.BankReconNumberingService>();
            services.AddScoped<ERP.Application.Accounting.IAccountingFoundationService, ERP.Infrastructure.Accounting.AccountingFoundationService>();
            services.AddScoped<ERP.Application.Accounting.ICustomerLedgerService, ERP.Infrastructure.Accounting.CustomerLedgerService>();
            services.AddScoped<ERP.Application.Accounting.IOutstandingService, ERP.Infrastructure.Accounting.OutstandingService>();
            services.AddScoped<ERP.Application.Accounting.IPaymentService, ERP.Infrastructure.Accounting.PaymentService>();
            services.AddScoped<ERP.Application.Accounting.IReceiptService, ERP.Infrastructure.Accounting.ReceiptService>();
            services.AddScoped<ERP.Application.Accounting.IGstService, ERP.Infrastructure.Accounting.GstService>();
            services.AddScoped<ERP.Application.Accounting.IBankReconciliationService, ERP.Infrastructure.Accounting.BankReconciliationService>();
            services.AddScoped<ERP.Application.Accounting.IFinancialReportService, ERP.Infrastructure.Accounting.FinancialReportService>();
            // Group 1, 2, 3 Inventory Hardening & Control Services
            services.AddScoped<Backend_ERP.Infrastructure.Services.IHardenedStockTransactionService, Backend_ERP.Infrastructure.Services.HardenedStockTransactionService>();
            services.AddScoped<Backend_ERP.Infrastructure.Services.IBatchAllocationService, Backend_ERP.Infrastructure.Services.BatchAllocationService>();
            services.AddScoped<Backend_ERP.Infrastructure.Services.IStockTransferService, Backend_ERP.Infrastructure.Services.StockTransferService>();
            services.AddScoped<Backend_ERP.Infrastructure.Services.IStockValuationService, Backend_ERP.Infrastructure.Services.StockValuationService>();
            services.AddScoped<Backend_ERP.Infrastructure.Services.IPhysicalStockVerificationService, Backend_ERP.Infrastructure.Services.PhysicalStockVerificationService>();

            // Background Workers
            services.AddHostedService<Backend_ERP.Infrastructure.BackgroundServices.OutboxProcessorBackgroundService>();
            services.AddHostedService<Backend_ERP.Infrastructure.BackgroundServices.StockAlertMonitoringWorker>();

            return services;
        }
    }
}
