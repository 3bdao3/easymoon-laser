using ErpClink.Modules.Reports.Application;
using Microsoft.Extensions.DependencyInjection;

namespace ErpClink.Modules.Reports.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddReportsModule(this IServiceCollection services)
    {
        services.AddScoped<IReportingQueryService, ReportingQueryService>();
        return services;
    }
}
