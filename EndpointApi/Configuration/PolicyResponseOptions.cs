using EndpointApi.Models.Contracts;

namespace EndpointApi.Configuration;

/// <summary>
/// Politika yanıtına eklenecek varsayılan tarayıcı ayarları (appsettings).
/// </summary>
public class PolicyResponseOptions
{
    public const string SectionName = "PolicyResponse";

    public BrowserSettings? BrowserSettings { get; set; }
}
