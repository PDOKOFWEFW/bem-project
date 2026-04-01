using EndpointApi.Data;
using EndpointApi.Data.Entities;
using EndpointApi.Data.Enums;
using Microsoft.EntityFrameworkCore;

namespace EndpointApi.Services;

public class RiskScoringService : IRiskScoringService
{
    private readonly AppDbContext _db;

    public RiskScoringService(AppDbContext db)
    {
        _db = db;
    }

    public int ComputeRiskScore(Device device, IReadOnlyCollection<Extension> extensions)
    {
        var blockRules = _db.PolicyRules.AsNoTracking()
            .Where(r => r.OrganizationUnitId == device.OrganizationUnitId && r.Action == PolicyRuleAction.Block)
            .Select(r => new { r.BrowserType, r.ExtensionId })
            .ToList();

        if (blockRules.Count == 0)
            return 0;

        var hits = 0;
        foreach (var ext in extensions)
        {
            var browser = Normalize(ext.BrowserType);
            foreach (var rule in blockRules)
            {
                if (!string.Equals(rule.ExtensionId, ext.ExtensionId, StringComparison.Ordinal))
                    continue;
                if (Normalize(rule.BrowserType) == browser)
                    hits++;
            }
        }

        var score = hits * 35;
        if (extensions.Count > 40)
            score += 10;
        return Math.Clamp(score, 0, 100);
    }

    private static string Normalize(string browserType)
    {
        if (string.IsNullOrWhiteSpace(browserType))
            return string.Empty;
        var b = browserType.Trim();
        if (b.Equals("Chrome", StringComparison.OrdinalIgnoreCase))
            return "Chrome";
        if (b.Equals("Edge", StringComparison.OrdinalIgnoreCase) || b.Equals("Microsoft Edge", StringComparison.OrdinalIgnoreCase))
            return "Edge";
        return b;
    }
}
