namespace EndpointApi.Data.Entities;

/// <summary>
/// Denetim kaydı (cihaz raporu, politika değişikliği vb.).
/// </summary>
public class AuditLog
{
    public long Id { get; set; }

    public DateTimeOffset OccurredUtc { get; set; }

    /// <summary>Örn: DeviceReport, PolicySync</summary>
    public string Action { get; set; } = string.Empty;

    public string? Actor { get; set; }

    public int? DeviceId { get; set; }
    public Device? Device { get; set; }

    public string? Details { get; set; }
}
