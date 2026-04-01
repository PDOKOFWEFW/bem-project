using EndpointApi.Data;
using EndpointApi.Models.Dashboard;
using Microsoft.EntityFrameworkCore;

namespace EndpointApi.Services;

public class DashboardQueryService : IDashboardQueryService
{
    private readonly AppDbContext _db;

    public DashboardQueryService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<DashboardBundleDto> GetBundleAsync(CancellationToken cancellationToken = default)
    {
        var devices = await _db.Devices.AsNoTracking()
            .Include(d => d.OrganizationUnit)
            .OrderByDescending(d => d.LastSeenUtc)
            .ToListAsync(cancellationToken);

        var lastSync = devices.Count == 0 ? (DateTimeOffset?)null : devices.Max(d => d.LastSeenUtc);

        var risky = devices.Count(d => d.RiskScore >= 60);
        var compliant = devices.Count(d => d.RiskScore < 30);
        var avgRisk = devices.Count == 0 ? 0 : devices.Average(d => d.RiskScore);
        var compliance = devices.Count == 0 ? 100.0 : Math.Round(100.0 - avgRisk, 1);

        var stats = new DashboardStatsDto
        {
            TotalDevices = devices.Count,
            RiskyDevices = risky,
            CompliantDevices = compliant,
            OpenSecuritySignals = risky,
            ComplianceScorePercent = compliance,
            LastSyncUtc = lastSync
        };

        var trend = BuildTrend(devices);
        var distribution = BuildDistribution(devices);

        var recentLogs = await _db.AuditLogs.AsNoTracking()
            .OrderByDescending(a => a.OccurredUtc)
            .Take(12)
            .ToListAsync(cancellationToken);

        var recent = recentLogs.Select(a => new RecentEventDto
        {
            Title = a.Action,
            Source = a.Actor ?? "EndpointApi",
            OccurredUtc = a.OccurredUtc,
            Severity = "Bilgi"
        }).ToList();

        var rows = devices.Select(d => new DashboardDeviceRowDto
        {
            DeviceId = d.DeviceId,
            MachineName = d.MachineName,
            UserDisplayName = d.AssignedUserDisplayName,
            UserEmail = d.AssignedUserEmail,
            OsVersion = d.OsVersion,
            OrganizationUnitName = d.OrganizationUnit?.Name ?? "",
            RiskScore = d.RiskScore,
            RiskLabel = RiskLabel(d.RiskScore),
            LastSeenUtc = d.LastSeenUtc,
            IpAddress = d.IpAddress
        }).ToList();

        return new DashboardBundleDto
        {
            Stats = stats,
            ActivityTrend = trend,
            StatusDistribution = distribution,
            RecentEvents = recent,
            Devices = rows
        };
    }

    private static ActivityTrendDto BuildTrend(IReadOnlyList<Data.Entities.Device> devices)
    {
        var end = DateTimeOffset.UtcNow;
        var anchor = new DateTimeOffset(end.Year, end.Month, 1, 0, 0, 0, TimeSpan.Zero).AddMonths(-11);
        var labels = new List<string>();
        var registered = new List<int>();
        var active = new List<int>();
        var tr = System.Globalization.CultureInfo.GetCultureInfo("tr-TR");

        for (var i = 0; i < 12; i++)
        {
            var monthStart = anchor.AddMonths(i);
            var monthEnd = monthStart.AddMonths(1);
            labels.Add(monthStart.ToString("MMM", tr));

            registered.Add(devices.Count(d => d.CreatedAtUtc >= monthStart && d.CreatedAtUtc < monthEnd));
            active.Add(devices.Count(d => d.LastSeenUtc >= monthStart && d.LastSeenUtc < monthEnd));
        }

        return new ActivityTrendDto
        {
            Labels = labels,
            RegisteredDevicesSeries = registered,
            ActiveDevicesSeries = active
        };
    }

    private static StatusDistributionDto BuildDistribution(IReadOnlyList<Data.Entities.Device> devices)
    {
        var low = devices.Count(d => d.RiskScore < 30);
        var mid = devices.Count(d => d.RiskScore >= 30 && d.RiskScore < 60);
        var high = devices.Count(d => d.RiskScore >= 60);

        return new StatusDistributionDto
        {
            Labels = new[] { "Düşük Risk", "Orta Risk", "Yüksek Risk" },
            Values = new[] { low, mid, high }
        };
    }

    private static string RiskLabel(int score)
    {
        if (score >= 60) return "Yüksek Risk";
        if (score >= 30) return "Orta Risk";
        return "Düşük Risk";
    }
}
