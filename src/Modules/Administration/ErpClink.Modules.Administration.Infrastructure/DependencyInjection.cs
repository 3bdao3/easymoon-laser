using System.Text;
using ErpClink.BuildingBlocks.Application.Abstractions;
using ErpClink.BuildingBlocks.AspNetCore;
using ErpClink.BuildingBlocks.Infrastructure.Time;
using ErpClink.Modules.Administration.Application.Auth;
using ErpClink.Modules.Administration.Application.Options;
using ErpClink.Modules.Administration.Application.Users;
using ErpClink.Modules.Administration.Infrastructure.Auth;
using ErpClink.Modules.Administration.Infrastructure.Identity;
using ErpClink.Modules.Administration.Infrastructure.Persistence;
using ErpClink.Modules.Administration.Infrastructure.Users;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;

namespace ErpClink.Modules.Administration.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddAdministrationModule(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddBuildingBlocksAspNetCore();
        services.AddSingleton<IDateTimeProvider, SystemDateTimeProvider>();
        services.AddSingleton<IBusinessClock, UtcBusinessClock>();

        services.Configure<JwtOptions>(configuration.GetSection(JwtOptions.SectionName));
        services.Configure<InitialAdminOptions>(configuration.GetSection(InitialAdminOptions.SectionName));
        services.Configure<InitialReceptionistOptions>(configuration.GetSection(InitialReceptionistOptions.SectionName));

        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Connection string 'DefaultConnection' is not configured.");

        services.AddDbContext<AdministrationDbContext>(options =>
            options.UseSqlServer(connectionString));

        services
            .AddIdentityCore<ApplicationUser>(options =>
            {
                options.User.RequireUniqueEmail = true;
                options.Password.RequiredLength = 8;
                options.Password.RequireDigit = true;
                options.Password.RequireLowercase = true;
                options.Password.RequireUppercase = true;
                options.Password.RequireNonAlphanumeric = true;
                options.Lockout.AllowedForNewUsers = true;
                options.Lockout.MaxFailedAccessAttempts = 5;
                options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
            })
            .AddRoles<ApplicationRole>()
            .AddEntityFrameworkStores<AdministrationDbContext>()
            .AddDefaultTokenProviders();

        var jwt = configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>()
            ?? throw new InvalidOperationException("Authentication:Jwt configuration is missing.");

        if (string.IsNullOrWhiteSpace(jwt.SigningKey) || jwt.SigningKey.Length < 32)
        {
            throw new InvalidOperationException("Authentication:Jwt:SigningKey must be configured and at least 32 characters.");
        }

        services
            .AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer();

        services.AddOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme)
            .Configure<Microsoft.Extensions.Options.IOptions<JwtOptions>>((bearerOptions, jwtOptionsAccessor) =>
            {
                var jwtOptions = jwtOptionsAccessor.Value;
                bearerOptions.RequireHttpsMetadata = false;
                bearerOptions.SaveToken = true;
                bearerOptions.MapInboundClaims = false;
                bearerOptions.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateIssuerSigningKey = true,
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.FromMinutes(1),
                    ValidIssuer = jwtOptions.Issuer,
                    ValidAudience = jwtOptions.Audience,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOptions.SigningKey)),
                    RoleClaimType = System.Security.Claims.ClaimTypes.Role,
                    NameClaimType = System.Security.Claims.ClaimTypes.Name
                };
            });

        services.AddAuthorization();

        services.AddScoped<IJwtTokenService, JwtTokenService>();
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IUserManagementService, UserManagementService>();

        return services;
    }
}
