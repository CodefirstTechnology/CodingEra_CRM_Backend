using System.Text;
using HRMS.Authorization;
using HRMS.Data;
using HRMS.Interfaces;
using HRMS.Repositories;
using HRMS.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Npgsql;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<HRMSDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddScoped<IEmployeeRepository, EmployeeRepository>();
builder.Services.AddScoped<IEmployeeService, EmployeeService>();
builder.Services.AddScoped<IEmployeeAccountService, EmployeeAccountService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IJwtTokenService, JwtTokenService>();
builder.Services.AddScoped<ILeaveService, LeaveService>();
builder.Services.AddScoped<IAttendanceService, AttendanceService>();
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUserAccessor, CurrentUserAccessor>();

var jwtSection = builder.Configuration.GetSection("Jwt");
var jwtKey = jwtSection["Key"] ?? throw new InvalidOperationException("Jwt:Key is required.");

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtSection["Issuer"],
            ValidAudience = jwtSection["Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
            RoleClaimType = System.Security.Claims.ClaimTypes.Role
        };
    });

builder.Services.AddAuthorization(options =>
{
    options.FallbackPolicy = new AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .Build();

    foreach (var permission in typeof(HrmsPermissions).GetFields(
                 System.Reflection.BindingFlags.Public |
                 System.Reflection.BindingFlags.Static |
                 System.Reflection.BindingFlags.FlattenHierarchy))
    {
        if (permission.GetValue(null) is string permissionName)
        {
            options.AddPolicy($"Permission:{permissionName}",
                policy => policy.Requirements.Add(new PermissionRequirement(permissionName)));
        }
    }
});

builder.Services.AddSingleton<IAuthorizationHandler, PermissionAuthorizationHandler>();

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.AddServer(new OpenApiServer
    {
        Url = "/",
        Description = "Current host"
    });
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme.",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT"
    });
    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAngular", policy =>
    {
        policy.WithOrigins("http://localhost:4200")
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("Startup");
    var db = scope.ServiceProvider.GetRequiredService<HRMSDbContext>();
    var employeeAccounts = scope.ServiceProvider.GetRequiredService<IEmployeeAccountService>();

    try
    {
        logger.LogInformation("Applying database migrations...");
        db.Database.Migrate();
        await LeaveWorkflowSchemaBootstrap.EnsureAsync(db);
        await AttendanceSchemaBootstrap.EnsureAsync(db);
        await AuthSeed.SeedAsync(db, employeeAccounts);
        logger.LogInformation("Database startup completed successfully.");
    }
    catch (PostgresException ex)
    {
        logger.LogCritical(ex,
            "PostgreSQL error during startup ({SqlState}). " +
            "Verify PostgreSQL is running and appsettings.json DefaultConnection is correct.",
            ex.SqlState);
        throw;
    }
    catch (NpgsqlException ex)
    {
        logger.LogCritical(ex,
            "Could not connect to PostgreSQL. " +
            "Ensure the PostgreSQL service is running on localhost:5432 and the connection string is valid.");
        throw;
    }
    catch (Exception ex)
    {
        logger.LogCritical(ex, "HRMS startup failed while initializing the database.");
        throw;
    }
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "HRMS v1");
        options.RoutePrefix = "swagger";
    });
}
else
{
    app.UseHttpsRedirection();
}

app.UseCors("AllowAngular");
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();
