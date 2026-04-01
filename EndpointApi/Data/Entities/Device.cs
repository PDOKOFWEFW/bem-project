namespace EndpointApi.Data.Entities;

/// <summary>
/// Kayıtlı uç nokta cihazı (ajan DeviceId ile eşleşir).
/// </summary>
public class Device
{
    public int Id { get; set; }

    /// <summary>Ajan tarafından üretilen kalıcı cihaz kimliği (benzersiz).</summary>
    public string DeviceId { get; set; } = string.Empty;

    public string MachineName { get; set; } = string.Empty;
    public string? OsVersion { get; set; }

    public int OrganizationUnitId { get; set; }
    public OrganizationalUnit OrganizationUnit { get; set; } = null!;

    public DateTimeOffset CreatedAtUtc { get; set; }
    public DateTimeOffset LastSeenUtc { get; set; }
    public bool LastPolicyAppliedStatus { get; set; } = true;
    public string? LastErrorMessage { get; set; }

    /// <summary>0–100 arası özet risk skoru (dashboard).</summary>
    public int RiskScore { get; set; }

    /// <summary>İsteğe bağlı görüntüleme alanları (dashboard cihaz tablosu).</summary>
    public string? AssignedUserDisplayName { get; set; }
    public string? AssignedUserEmail { get; set; }

    /// <summary>Son raporlanan yerel IP (ajan).</summary>
    public string? IpAddress { get; set; }

    public ICollection<Extension> Extensions { get; set; } = new List<Extension>();
}
