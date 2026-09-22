using ErpClink.Modules.LaserClinic.Domain.Services;

namespace ErpClink.Modules.LaserClinic.Application.Scheduling;

/// <summary>Owns recommended session duration calculation (backend source of truth).</summary>
public static class DurationCalculator
{
    public static int RecommendedMinutes(LaserService service) =>
        service.GetRecommendedDurationMinutes();

    public static int RecommendedMinutes(int minDurationMinutes, int maxDurationMinutes)
    {
        if (minDurationMinutes <= 0)
            throw new ArgumentOutOfRangeException(nameof(minDurationMinutes));
        if (maxDurationMinutes < minDurationMinutes)
            throw new ArgumentOutOfRangeException(nameof(maxDurationMinutes));

        return minDurationMinutes == maxDurationMinutes
            ? minDurationMinutes
            : maxDurationMinutes;
    }

    public static int SumRecommended(IEnumerable<LaserService> services) =>
        services.Sum(RecommendedMinutes);

    public static bool RequiresManualDuration(string serviceName) =>
        !string.IsNullOrWhiteSpace(serviceName)
        && serviceName.Contains("نبضة", StringComparison.Ordinal);

    public static bool RequiresManualDuration(LaserService service) =>
        RequiresManualDuration(service.Name);

    /// <summary>Extracts 1000 / 5000 package size from service name, or null.</summary>
    public static int? ParsePulsePackageSize(string serviceName)
    {
        if (!RequiresManualDuration(serviceName))
            return null;
        if (serviceName.Contains("5000", StringComparison.Ordinal))
            return 5000;
        if (serviceName.Contains("1000", StringComparison.Ordinal))
            return 1000;
        return null;
    }

    public static int EffectiveBlockedMinutes(int clinicalDurationMinutes, int bufferMinutes)
    {
        if (clinicalDurationMinutes <= 0)
            throw new ArgumentOutOfRangeException(nameof(clinicalDurationMinutes));
        if (bufferMinutes < 0)
            throw new ArgumentOutOfRangeException(nameof(bufferMinutes));
        return clinicalDurationMinutes + bufferMinutes;
    }
}
