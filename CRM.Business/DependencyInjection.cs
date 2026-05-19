using CRM.Business.Services;
using CRM.Business.Services.MasterData;
using CRM.DATA;
using CRM.models;
using Microsoft.Extensions.DependencyInjection;

namespace CRM.Business;

public static class DependencyInjection
{
    public static IServiceCollection AddCrmBusiness(this IServiceCollection services)
    {
        services.AddScoped<IAuditUserService, AuditUserService>();
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<ILeadService, LeadService>();
        services.AddScoped<IContactService, ContactService>();
        services.AddScoped<IDealService, DealService>();
        services.AddScoped<IOrganizationService, OrganizationService>();
        services.AddScoped<ITaskService, TaskService>();
        services.AddScoped<INoteService, NoteService>();
        services.AddScoped<ICallLogService, CallLogService>();

        RegisterMasterData<Territory>(services, c => c.Territories, "territory");
        RegisterMasterData<Industry>(services, c => c.Industries, "industry");
        RegisterMasterData<Role>(services, c => c.Roles, "role");
        RegisterMasterData<Salutation>(services, c => c.Salutations, "salutation");
        RegisterMasterData<EmployeeCount>(services, c => c.EmployeeCounts, "employee count");
        RegisterMasterData<LeadStatus>(services, c => c.LeadStatuses, "lead status");
        RegisterMasterData<RequestType>(services, c => c.RequestTypes, "request type");

        return services;
    }

    private static void RegisterMasterData<TEntity>(
        IServiceCollection services,
        Func<TaskDbcontext, Microsoft.EntityFrameworkCore.DbSet<TEntity>> dbSet,
        string entityLabel)
        where TEntity : class, INamedMasterEntity, new()
    {
        services.AddScoped<IMasterDataService<TEntity>>(sp =>
            new MasterDataService<TEntity>(
                sp.GetRequiredService<TaskDbcontext>(),
                sp.GetRequiredService<IAuditUserService>(),
                dbSet,
                entityLabel));
    }
}
