using EndpointApi.Data.Enums;

namespace EndpointApi.Data.Entities;

/// <summary>
/// OU'ya bağlı eklenti politikası (Chrome/Edge ve Force/Block/Allow).
/// </summary>
public class PolicyRule
{
    public int Id { get; set; }

    public int OrganizationUnitId { get; set; }
    public OrganizationalUnit OrganizationUnit { get; set; } = null!;

    /// <summary>Chrome veya Edge (ajan ExtensionInfo ile uyumlu).</summary>
    public string BrowserType { get; set; } = string.Empty;

    public PolicyRuleAction Action { get; set; }

    /// <summary>Chrome Web Store / Edge Add-ons eklenti kimliği.</summary>
    public string ExtensionId { get; set; } = string.Empty;
}
