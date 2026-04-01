using EndpointApi.Data;
using EndpointApi.Data.Enums;
using EndpointApi.Models.Contracts;
using Microsoft.EntityFrameworkCore;

namespace EndpointApi.Services;

public class PolicyEngine : IPolicyEngine
{
    private readonly AppDbContext _db;

    public PolicyEngine(AppDbContext db)
    {
        _db = db;
    }

    public DevicePolicyResponse BuildResponseForOrganizationUnit(int organizationUnitId)
    {
        var rules = _db.PolicyRules.AsNoTracking()
            .Where(r => r.OrganizationUnitId == organizationUnitId)
            .ToList();

        var response = new DevicePolicyResponse();

        foreach (var rule in rules)
        {
            if (string.IsNullOrWhiteSpace(rule.ExtensionId))
                continue;

            var browser = NormalizeBrowser(rule.BrowserType);
            if (browser is not ("Chrome" or "Edge"))
                continue;

            switch (rule.Action, browser)
            {
                case (PolicyRuleAction.ForceInstall, "Chrome"):
                    response.ForceInstallChrome.Add(rule.ExtensionId);
                    break;
                case (PolicyRuleAction.Block, "Chrome"):
                    response.BlockChrome.Add(rule.ExtensionId);
                    break;
                case (PolicyRuleAction.Allow, "Chrome"):
                    response.AllowChrome.Add(rule.ExtensionId);
                    break;
                case (PolicyRuleAction.ForceInstall, "Edge"):
                    response.ForceInstallEdge.Add(rule.ExtensionId);
                    break;
                case (PolicyRuleAction.Block, "Edge"):
                    response.BlockEdge.Add(rule.ExtensionId);
                    break;
                case (PolicyRuleAction.Allow, "Edge"):
                    response.AllowEdge.Add(rule.ExtensionId);
                    break;
            }
        }

        Deduplicate(response);
        return response;
    }

    private static void Deduplicate(DevicePolicyResponse r)
    {
        r.ForceInstallChrome = r.ForceInstallChrome.Distinct(StringComparer.Ordinal).ToList();
        r.BlockChrome = r.BlockChrome.Distinct(StringComparer.Ordinal).ToList();
        r.AllowChrome = r.AllowChrome.Distinct(StringComparer.Ordinal).ToList();
        r.ForceInstallEdge = r.ForceInstallEdge.Distinct(StringComparer.Ordinal).ToList();
        r.BlockEdge = r.BlockEdge.Distinct(StringComparer.Ordinal).ToList();
        r.AllowEdge = r.AllowEdge.Distinct(StringComparer.Ordinal).ToList();
    }

    private static string NormalizeBrowser(string browserType)
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
