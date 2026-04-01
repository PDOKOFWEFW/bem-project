using EndpointApi.Data.Entities;
using EndpointApi.Data.Enums;
using Microsoft.EntityFrameworkCore;

namespace EndpointApi.Data;

public static class DbInitializer
{
    /// <summary>
    /// İlk kurulumda OU + örnek politikalar; mevcut veritabanında eksikse sadece Unassigned eklenir.
    /// </summary>
    public static async Task SeedAsync(AppDbContext db)
    {
        if (!await db.OrganizationalUnits.AnyAsync())
        {
            await SeedFreshDatabaseAsync(db);
            return;
        }

        await EnsureUnassignedOuAsync(db);
    }

    private static async Task EnsureUnassignedOuAsync(AppDbContext db)
    {
        if (await db.OrganizationalUnits.AnyAsync(o => o.Name == "Unassigned"))
            return;

        db.OrganizationalUnits.Add(new OrganizationalUnit
        {
            Name = "Unassigned",
            DistinguishedPath = "/OU=Unassigned"
        });
        await db.SaveChangesAsync();
    }

    private static async Task SeedFreshDatabaseAsync(AppDbContext db)
    {
        var unassigned = new OrganizationalUnit { Name = "Unassigned", DistinguishedPath = "/OU=Unassigned" };
        var defaultOu = new OrganizationalUnit { Name = "Default", DistinguishedPath = "/OU=Default" };
        var pazarlama = new OrganizationalUnit { Name = "Pazarlama", DistinguishedPath = "/OU=Pazarlama" };

        db.OrganizationalUnits.AddRange(unassigned, defaultOu, pazarlama);
        await db.SaveChangesAsync();

        var sampleRules = new List<PolicyRule>
        {
            new()
            {
                OrganizationUnitId = pazarlama.Id,
                BrowserType = "Chrome",
                Action = PolicyRuleAction.ForceInstall,
                ExtensionId = "cjpalhdlnbpafiamejdnhcphjbkeiagm"
            },
            new()
            {
                OrganizationUnitId = pazarlama.Id,
                BrowserType = "Chrome",
                Action = PolicyRuleAction.Block,
                ExtensionId = "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa"
            },
            new()
            {
                OrganizationUnitId = pazarlama.Id,
                BrowserType = "Edge",
                Action = PolicyRuleAction.ForceInstall,
                ExtensionId = "odfafepnkmbhccpbejgmiehpchacaeak"
            },
            new()
            {
                OrganizationUnitId = pazarlama.Id,
                BrowserType = "Edge",
                Action = PolicyRuleAction.Block,
                ExtensionId = "bbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbb"
            },
            new()
            {
                OrganizationUnitId = pazarlama.Id,
                BrowserType = "Chrome",
                Action = PolicyRuleAction.Allow,
                ExtensionId = "ghbmnnjooekpmoecnnnilnnbdlolhkhi"
            },
            new()
            {
                OrganizationUnitId = pazarlama.Id,
                BrowserType = "Edge",
                Action = PolicyRuleAction.Allow,
                ExtensionId = "microsoft_edge_example_extension_id"
            }
        };

        db.PolicyRules.AddRange(sampleRules);

        db.AuditLogs.Add(new AuditLog
        {
            OccurredUtc = DateTimeOffset.UtcNow,
            Action = "Sistem başlatıldı",
            Actor = "EndpointApi",
            Details = "Unassigned, Default ve Pazarlama OU'ları ile örnek politikalar yüklendi."
        });

        await db.SaveChangesAsync();
    }
}
