using System.Text.Json.Serialization;
using ERP.API.Middleware;
using ERP.API.Security;
using ERP.Infrastructure;
using ERP.Shared.Configuration;
using ERP.Application.Procurement;

var builder = WebApplication.CreateBuilder(args);

builder.Configuration.AddJsonFile(
    $"appsettings.{builder.Environment.EnvironmentName}.local.json",
    optional: true,
    reloadOnChange: true);

// Add services to the container.

builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new VerificationStatusConverter());
        options.JsonSerializerOptions.Converters.Add(new StockTxnTypeConverter());
        options.JsonSerializerOptions.Converters.Add(new BatchStatusConverter());
        options.JsonSerializerOptions.Converters.Add(new FgDispatchStatusConverter());
        options.JsonSerializerOptions.Converters.Add(new StockAgeBandConverter());
        options.JsonSerializerOptions.Converters.Add(new ERP.Application.Production.WorkOrderStatusConverter());
        options.JsonSerializerOptions.Converters.Add(new ERP.Application.Sales.DispatchPlanStatusConverter());
        options.JsonSerializerOptions.Converters.Add(new ERP.Application.Sales.DispatchPriorityConverter());
        options.JsonSerializerOptions.Converters.Add(new ERP.Application.Sales.VehicleAssignmentStatusConverter());
        options.JsonSerializerOptions.Converters.Add(new ERP.Application.Sales.VehicleTypeConverter());
        options.JsonSerializerOptions.Converters.Add(new ERP.Application.Sales.TransportStatusConverter());
        options.JsonSerializerOptions.Converters.Add(new ERP.Application.Sales.TransportModeConverter());
        options.JsonSerializerOptions.Converters.Add(new ERP.Application.Sales.LrStatusConverter());
        options.JsonSerializerOptions.Converters.Add(new ERP.Application.Sales.FreightPaymentTypeConverter());
        options.JsonSerializerOptions.Converters.Add(new ERP.Application.Sales.EwayStatusConverter());
        options.JsonSerializerOptions.Converters.Add(new ERP.Application.Sales.DispatchTrackStatusConverter());
        options.JsonSerializerOptions.Converters.Add(new ERP.Application.Sales.TransportTrackStatusConverter());
        options.JsonSerializerOptions.Converters.Add(new ERP.Application.Sales.PodStatusConverter());
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    });

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.CustomSchemaIds(type => type.FullName?.Replace("+", ".") ?? type.Name);
    options.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.Http,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = Microsoft.OpenApi.Models.ParameterLocation.Header,
        Description = "Enter JWT Bearer token"
    });
    options.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
    {
        {
            new Microsoft.OpenApi.Models.OpenApiSecurityScheme
            {
                Reference = new Microsoft.OpenApi.Models.OpenApiReference
                {
                    Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAngular",
        policy =>
        {
            policy.WithOrigins("http://localhost:4200")
                .AllowAnyHeader()
                .AllowAnyMethod();
        });
});

builder.Services.Configure<DatabaseOptions>(builder.Configuration.GetSection(DatabaseOptions.SectionName));
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddErpJwtAuthentication(builder.Configuration);
builder.Services.AddAuthorization();
builder.Services.AddHostedService<ERP.Infrastructure.Sales.ProformaInvoiceExpiryWorker>();
builder.Services.AddHostedService<ERP.Infrastructure.Sales.Workers.SalesTargetEvaluationWorker>();

var app = builder.Build();

app.UseGlobalExceptionMiddleware();

await app.ApplyPendingMigrationsAsync();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseCors("AllowAngular");

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
