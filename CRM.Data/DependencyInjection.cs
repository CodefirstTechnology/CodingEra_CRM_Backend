using CRM.DATA;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace CRM.Data;

public static class DependencyInjection
{
    public static IServiceCollection AddCrmData(this IServiceCollection services, string connectionString)
    {
        services.AddDbContext<TaskDbcontext>(options =>
            options.UseNpgsql(connectionString));

        return services;
    }
}
