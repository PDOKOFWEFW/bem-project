namespace EndpointApi.Data.Entities;

/// <summary>
/// Organizasyon birimi (OU). Cihaz ve politika kuralları OU üzerinden ilişkilendirilir.
/// </summary>
public class OrganizationalUnit
{
    public int Id { get; set; }

    /// <summary>Örn: Pazarlama, IT, Default</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>İsteğe bağlı hiyerarşi yolu (örn. /Root/Pazarlama).</summary>
    public string? DistinguishedPath { get; set; }

    public ICollection<Device> Devices { get; set; } = new List<Device>();
    public ICollection<PolicyRule> PolicyRules { get; set; } = new List<PolicyRule>();
}
