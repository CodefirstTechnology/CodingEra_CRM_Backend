using System;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using CRM.Configuration;
using CRM.DATA;
using CRM.DTO;
using CRM.Hubs;
using CRM.models;
using CRM.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

var builder = WebApplication.CreateBuilder(args);

// Options Configuration
builder.Services.Configure<JustdialWebhookOptions>(
    builder.Configuration.GetSection(JustdialWebhookOptions.SectionName));
builder.Services.Configure<IndiaMartWebhookOptions>(
    builder.Configuration.GetSection(IndiaMartWebhookOptions.SectionName));

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddSignalR();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Configure EF Core DbContext with PostgreSQL
builder.Services.AddDbContext<TaskDbcontext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// CORS Configuration
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

// Dependency Injection Registrations
builder.Services.AddDataProtection();
builder.Services.AddHttpClient();
builder.Services.AddScoped<ILeadSyncCredentialService, LeadSyncCredentialService>();
builder.Services.AddScoped<ILeadSyncManagementService, LeadSyncManagementService>();
builder.Services.AddScoped<ILeadSyncRoundRobinService, LeadSyncRoundRobinService>();
builder.Services.AddScoped<IMarketplaceLeadPersistenceService, MarketplaceLeadPersistenceService>();
builder.Services.AddScoped<ILeadSyncExecutionService, LeadSyncExecutionService>();
builder.Services.AddScoped<ILeadSyncProvider, LeadSyncIndiaMartProvider>();
builder.Services.AddScoped<ILeadSyncProvider, LeadSyncTradeIndiaProvider>();
builder.Services.AddScoped<ILeadSyncProvider, LeadSyncJustdialProvider>();
builder.Services.AddHostedService<LeadSyncAutoSyncHostedService>();
builder.Services.AddHostedService<FollowUpSlaWorker>();
builder.Services.AddScoped<IEmailService, SmtpEmailService>();
builder.Services.AddScoped<IMasterDataAdminService, MasterDataAdminService>();
builder.Services.AddScoped<IQuotationService, QuotationService>();
builder.Services.AddScoped<ICompanyProfileService, CompanyProfileService>();
builder.Services.AddScoped<IItemMasterService, ItemMasterService>();
builder.Services.AddScoped<ILeadImportService, LeadImportService>();
builder.Services.AddScoped<ILeadImportFileParser, LeadImportFileParser>();
builder.Services.AddScoped<IContactImportService, ContactImportService>();
builder.Services.AddScoped<IContactImportFileParser, ContactImportFileParser>();
builder.Services.AddScoped<ILeadExportService, LeadExportService>();
builder.Services.AddScoped<IDealExportService, DealExportService>();
builder.Services.AddSingleton<IIndiaMartWebhookMetrics, IndiaMartWebhookMetrics>();
builder.Services.AddScoped<IIndiaMartWebhookSecurityService, IndiaMartWebhookSecurityService>();
builder.Services.AddScoped<IIndiaMartWebhookService, IndiaMartWebhookService>();
builder.Services.AddSingleton<IJustdialWebhookMetrics, JustdialWebhookMetrics>();
builder.Services.AddScoped<IJustdialWebhookSecurityService, JustdialWebhookSecurityService>();
builder.Services.AddScoped<IJustdialWebhookService, JustdialWebhookService>();
builder.Services.AddScoped<IRbacService, RbacService>();
builder.Services.AddScoped<IUserTargetService, UserTargetService>();
builder.Services.AddScoped<ILeadTrackerService, LeadTrackerService>();
builder.Services.AddScoped<IDailySalesRecognitionService, DailySalesRecognitionService>();

var app = builder.Build();

// Safe, idempotent, and fault-tolerant startup database initialization
await app.InitializeDatabaseAsync();

var forwardedHeadersOptions = new ForwardedHeadersOptions
{
    ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
};
forwardedHeadersOptions.KnownNetworks.Clear();
forwardedHeadersOptions.KnownProxies.Clear();
app.UseForwardedHeaders(forwardedHeadersOptions);

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors("AllowAll");
app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();
app.MapHub<UserStatusHub>("/hubs/user-status");
app.MapHub<UserStatusHub>("/api/hubs/user-status");

app.Run();
