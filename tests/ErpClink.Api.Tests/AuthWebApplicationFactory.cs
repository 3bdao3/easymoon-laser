using ErpClink.Modules.Administration.Infrastructure.Persistence;
using ErpClink.Modules.LaserClinic.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using System.Text;

namespace ErpClink.Api.Tests;

public sealed class AuthWebApplicationFactory : WebApplicationFactory<Program>
{
    private readonly string _databaseName = $"ErpClink_AuthTests_{Guid.NewGuid():N}";
    public const string TestSigningKey = "TEST_ONLY_SIGNING_KEY_MUST_BE_AT_LEAST_32_CHARS_LONG";
    public const string TestIssuer = "ErpClink.Tests";
    public const string TestAudience = "ErpClink.Tests.Client";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.UseSetting("ConnectionStrings:DefaultConnection",
            $"Server=(localdb)\\mssqllocaldb;Database={_databaseName};Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True");
        builder.UseSetting("Authentication:Jwt:Issuer", TestIssuer);
        builder.UseSetting("Authentication:Jwt:Audience", TestAudience);
        builder.UseSetting("Authentication:Jwt:SigningKey", TestSigningKey);
        builder.UseSetting("Authentication:Jwt:AccessTokenMinutes", "15");
        builder.UseSetting("Authentication:Jwt:RefreshTokenDays", "7");
        builder.UseSetting("Authentication:InitialAdmin:Email", "admin@erpclink.local");
        builder.UseSetting("Authentication:InitialAdmin:Password", "ChangeMe!Admin123");
        builder.UseSetting("Authentication:InitialAdmin:FullName", "System Super Admin");
        builder.UseSetting("Authentication:InitialReceptionist:UserName", "socialmedia");
        builder.UseSetting("Authentication:InitialReceptionist:Email", "socialmedia@easymoon.local");
        builder.UseSetting("Authentication:InitialReceptionist:Password", "EasyMoon@2026");
        builder.UseSetting("Authentication:InitialReceptionist:FullName", "موظفة السوشيال ميديا");
        builder.UseSetting("Organization:DefaultOrganizationId", "11111111-1111-1111-1111-111111111111");
        builder.UseSetting("Organization:DefaultBranchId", "22222222-2222-2222-2222-222222222222");

        builder.ConfigureServices(services =>
        {
            services.PostConfigure<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme, options =>
            {
                options.RequireHttpsMetadata = false;
                options.MapInboundClaims = false;
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateIssuerSigningKey = true,
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.FromMinutes(1),
                    ValidIssuer = TestIssuer,
                    ValidAudience = TestAudience,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(TestSigningKey)),
                    RoleClaimType = System.Security.Claims.ClaimTypes.Role,
                    NameClaimType = System.Security.Claims.ClaimTypes.Name
                };
            });
        });
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            try
            {
                using var scope = Services.CreateScope();
                scope.ServiceProvider.GetRequiredService<AdministrationDbContext>().Database.EnsureDeleted();
                scope.ServiceProvider.GetRequiredService<LaserClinicDbContext>().Database.EnsureDeleted();
            }
            catch
            {
                // best-effort cleanup
            }
        }

        base.Dispose(disposing);
    }
}
