namespace EndpointApi.Models.Contracts;

/// <summary>
/// Ajanın raporladığı eklenti satırı (EndpointAgent.ExtensionInfo ile uyumlu).
/// </summary>
public class ExtensionInfo
{
    public string ExtensionId { get; set; } = string.Empty;
    public string ExtensionName { get; set; } = string.Empty;
    public string ExtensionVersion { get; set; } = string.Empty;
    public string BrowserType { get; set; } = string.Empty;
}
