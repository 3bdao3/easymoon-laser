using ErpClink.Api.Middleware;
using ErpClink.Modules.Administration.Infrastructure;
using ErpClink.Modules.Administration.Infrastructure.Persistence;
using ErpClink.Modules.LaserClinic.Infrastructure;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddHealthChecks();

var corsOrigins = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
{
    "http://localhost:4200",
    "https://localhost:4200"
};

var frontendUrl = builder.Configuration["FRONTEND_URL"];
if (!string.IsNullOrWhiteSpace(frontendUrl))
{
    foreach (var origin in frontendUrl.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
    {
        corsOrigins.Add(origin.TrimEnd('/'));
    }
}

builder.Services.AddCors(options =>
{
    options.AddPolicy("AppCors", policy =>
        policy.WithOrigins(corsOrigins.ToArray())
            .AllowAnyHeader()
            .AllowAnyMethod());
});

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "ErpClink Laser Clinic API",
        Version = "v1",
        Description = "Laser Clinic Management System API"
    });

    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Enter JWT access token"
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

builder.Services.AddAdministrationModule(builder.Configuration);
builder.Services.AddLaserClinicModule(builder.Configuration);

var app = builder.Build();

if (!app.Environment.IsDevelopment()
    && string.IsNullOrWhiteSpace(builder.Configuration["FRONTEND_URL"]))
{
    app.Logger.LogWarning(
        "FRONTEND_URL is not configured. Cross-origin requests from the deployed Angular site will be blocked by CORS.");
}

app.UseMiddleware<ExceptionHandlingMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// CORS must run in all environments (JWT uses Authorization header; origin is restricted via FRONTEND_URL).
app.UseCors("AppCors");

// Render terminates TLS at the edge and forwards HTTP to the container — skip HTTPS redirection in Production.
if (app.Environment.IsDevelopment())
{
    // no-op: local HTTPS handled by launchSettings if used
}

app.UseAuthentication();
app.UseAuthorization();

app.MapHealthChecks("/health", new HealthCheckOptions
{
    ResponseWriter = async (context, _) =>
    {
        context.Response.ContentType = "application/json";
        await context.Response.WriteAsync("""{"status":"Healthy"}""");
    }
});

app.MapControllers();

await AdministrationDbSeeder.SeedAsync(app.Services);
await app.Services.MigrateLaserClinicModuleAsync();

app.Run();

public partial class Program;
