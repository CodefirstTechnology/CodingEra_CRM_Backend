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

            return services;
        }
    }
}
