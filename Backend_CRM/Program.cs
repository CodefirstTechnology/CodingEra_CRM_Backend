using CRM.Configuration;
using CRM.DATA;
using CRM.Services;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();


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

builder.Services.AddDbContext<TaskDbcontext>(options =>
    options.UseNpgsql(
        builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.Configure<SmtpOptions>(builder.Configuration.GetSection(SmtpOptions.SectionName));
builder.Services.AddScoped<IEmailService, SmtpEmailService>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    using var scope = app.Services.CreateScope();
    await using var db = scope.ServiceProvider.GetRequiredService<TaskDbcontext>();
    await db.Database.MigrateAsync();
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseCors("AllowAngular");

app.UseAuthorization();

app.MapControllers();

app.Run();
