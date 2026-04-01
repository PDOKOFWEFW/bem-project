namespace EndpointApi.Models.Dashboard;

/// <summary>
/// index.html ve devices.html için birleşik dashboard veri taslağı.
/// </summary>
public class DashboardBundleDto
{
    public DashboardStatsDto Stats { get; set; } = new();
    public ActivityTrendDto ActivityTrend { get; set; } = new();
    public StatusDistributionDto StatusDistribution { get; set; } = new();
    public IReadOnlyList<RecentEventDto> RecentEvents { get; set; } = Array.Empty<RecentEventDto>();
    public IReadOnlyList<DashboardDeviceRowDto> Devices { get; set; } = Array.Empty<DashboardDeviceRowDto>();
}

public class DashboardStatsDto
{
    public int TotalDevices { get; set; }
    public int RiskyDevices { get; set; }
    public int CompliantDevices { get; set; }
    public int OpenSecuritySignals { get; set; }
    public double ComplianceScorePercent { get; set; }
    public DateTimeOffset? LastSyncUtc { get; set; }
}

public class ActivityTrendDto
{
    public IReadOnlyList<string> Labels { get; set; } = Array.Empty<string>();
    public IReadOnlyList<int> RegisteredDevicesSeries { get; set; } = Array.Empty<int>();
    public IReadOnlyList<int> ActiveDevicesSeries { get; set; } = Array.Empty<int>();
}

public class StatusDistributionDto
{
    public IReadOnlyList<string> Labels { get; set; } = Array.Empty<string>();
    public IReadOnlyList<int> Values { get; set; } = Array.Empty<int>();
}

public class RecentEventDto
{
    public string Title { get; set; } = string.Empty;
    public string Source { get; set; } = string.Empty;
    public DateTimeOffset OccurredUtc { get; set; }
    public string Severity { get; set; } = string.Empty;
}

public class DashboardDeviceRowDto
{
    public string DeviceId { get; set; } = string.Empty;
    public string MachineName { get; set; } = string.Empty;
    public string? UserDisplayName { get; set; }
    public string? UserEmail { get; set; }
    public string? OsVersion { get; set; }
    public string OrganizationUnitName { get; set; } = string.Empty;
    public int RiskScore { get; set; }
    public string RiskLabel { get; set; } = string.Empty;
    public DateTimeOffset LastSeenUtc { get; set; }

    public string? IpAddress { get; set; }
}
