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
            services.AddDbContext<ERPDbContext>(options =>
                options.UseNpgsql(
                    configuration.GetConnectionString("DefaultConnection")));

            services.AddScoped<ISalesOrderService, SalesOrderService>();
            services.AddScoped<IProformaInvoiceRepository, ProformaInvoiceRepository>();
            services.AddScoped<IProformaInvoiceService, ProformaInvoiceService>();
            services.AddScoped<IAdvancePaymentNumberingService, AdvancePaymentNumberingService>();
            services.AddScoped<IAdvancePaymentRepository, AdvancePaymentRepository>();
            services.AddScoped<IAdvancePaymentService, AdvancePaymentService>();
            services.AddScoped<ISalesTargetNumberingService, SalesTargetNumberingService>();
            services.AddScoped<ISalesTargetRepository, SalesTargetRepository>();
            services.AddScoped<ISalesTargetService, SalesTargetService>();
            services.AddScoped<IPerformanceRepository, PerformanceRepository>();
            services.AddScoped<IPerformanceService, PerformanceService>();
            services.AddScoped<IPriceListNumberingService, PriceListNumberingService>();
            services.AddScoped<IPriceListRepository, PriceListRepository>();
            services.AddScoped<IPriceListService, PriceListService>();
            services.AddScoped<IDiscountApprovalNumberingService, DiscountApprovalNumberingService>();
            services.AddScoped<IDiscountApprovalRepository, DiscountApprovalRepository>();
            services.AddScoped<IDiscountApprovalService, DiscountApprovalService>();
            services.AddScoped<IQuotationApprovalNumberingService, QuotationApprovalNumberingService>();
            services.AddScoped<IQuotationApprovalRepository, QuotationApprovalRepository>();
            services.AddScoped<IQuotationApprovalService, QuotationApprovalService>();

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

            return services;
        }
    }
}
