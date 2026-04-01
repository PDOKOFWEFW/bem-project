using EndpointApi.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace EndpointApi.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }

    public DbSet<OrganizationalUnit> OrganizationalUnits => Set<OrganizationalUnit>();
    public DbSet<Device> Devices => Set<Device>();
    public DbSet<Extension> Extensions => Set<Extension>();
    public DbSet<PolicyRule> PolicyRules => Set<PolicyRule>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Device>(e =>
        {
            e.HasIndex(d => d.DeviceId).IsUnique();
            e.HasOne(d => d.OrganizationUnit)
                .WithMany(ou => ou.Devices)
                .HasForeignKey(d => d.OrganizationUnitId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Extension>(e =>
        {
            e.ToTable("Extensions");
            e.HasOne(x => x.Device)
                .WithMany(d => d.Extensions)
                .HasForeignKey(x => x.DeviceId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<PolicyRule>(e =>
        {
            e.HasOne(p => p.OrganizationUnit)
                .WithMany(ou => ou.PolicyRules)
                .HasForeignKey(p => p.OrganizationUnitId)
                .OnDelete(DeleteBehavior.Cascade);

            e.HasIndex(p => new { p.OrganizationUnitId, p.BrowserType, p.Action, p.ExtensionId });
        });

        modelBuilder.Entity<AuditLog>(e =>
        {
            e.HasOne(a => a.Device)
                .WithMany()
                .HasForeignKey(a => a.DeviceId)
                .OnDelete(DeleteBehavior.SetNull);
        });
    }
}
