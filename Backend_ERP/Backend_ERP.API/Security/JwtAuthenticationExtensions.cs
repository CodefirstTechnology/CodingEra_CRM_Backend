using System;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;

namespace ERP.API.Security
{
    public static class JwtAuthenticationExtensions
    {
        public const string DefaultJwtKey = "CodingEra_CRM_ERP_Super_Secret_Shared_Key_2026_!@#$%^&*()_+";
        public const string DefaultIssuer = "CodingEra_CRM";
        public const string DefaultAudience = "CodingEra_ERP";

        public static IServiceCollection AddErpJwtAuthentication(
            this IServiceCollection services,
            IConfiguration configuration)
        {
            var jwtSection = configuration.GetSection("Jwt");
            var keyString = jwtSection["Key"] ?? DefaultJwtKey;
            var issuer = jwtSection["Issuer"] ?? DefaultIssuer;
            var audience = jwtSection["Audience"] ?? DefaultAudience;

            var keyBytes = Encoding.UTF8.GetBytes(keyString);
            if (keyBytes.Length < 32)
            {
                Array.Resize(ref keyBytes, 32);
            }

            services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(options =>
            {
                options.RequireHttpsMetadata = false;
                options.SaveToken = true;
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(keyBytes),
                    ValidateIssuer = false, // flexible for CRM-issued tokens across domains/ports
                    ValidateAudience = false, // flexible for CRM-issued tokens across domains/ports
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.FromMinutes(5)
                };

                // Add custom token fallback handler to support CRM session tokens or JWT payload decoding
                options.Events = new JwtBearerEvents
                {
                    OnMessageReceived = context =>
                    {
                        var authHeader = context.Request.Headers["Authorization"].ToString();
                        if (!string.IsNullOrWhiteSpace(authHeader) && authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
                        {
                            context.Token = authHeader["Bearer ".Length..].Trim();
                        }
                        return System.Threading.Tasks.Task.CompletedTask;
                    }
                };
            });

            return services;
        }
    }
}
