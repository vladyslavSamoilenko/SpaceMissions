using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.IdentityModel.Tokens;
using System.Text;

namespace SpaceMissions.WebAP.Configuration;

public static class Configure
{
    public static void ConfigureCors(this IServiceCollection services)
    {
        services.AddCors(options =>
        {
            options.AddPolicy("AllowAll", policy =>
            {
                policy.AllowAnyOrigin()
                      .AllowAnyHeader()
                      .AllowAnyMethod();
            });
        });
    }

    public static void ConfigureJWT(this IServiceCollection services, JwtSettings jwtSettings, IWebHostEnvironment env)
    {
        var key = jwtSettings.Key ?? throw new InvalidOperationException("JWT Key is not set in configuration");

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.RequireHttpsMetadata = !env.IsDevelopment();
                options.SaveToken = true;

                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key)),

                    ValidateIssuer = !string.IsNullOrEmpty(jwtSettings.Issuer),
                    ValidIssuer = jwtSettings.Issuer,

                    ValidateAudience = !string.IsNullOrEmpty(jwtSettings.Audience),
                    ValidAudience = jwtSettings.Audience,

                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.FromMinutes(1)
                };
            });

        services.AddAuthorization(opt =>
        {
            opt.AddPolicy("Bearer", new AuthorizationPolicyBuilder()
                .AddAuthenticationSchemes(JwtBearerDefaults.AuthenticationScheme)
                .RequireAuthenticatedUser()
                .Build());
        });
    }
}
