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

            return services;
        }
    }
}
