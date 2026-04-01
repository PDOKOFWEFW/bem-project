namespace EndpointApi.Models.Contracts;

/// <summary>
/// API yanıtı ve appsettings ile uyumlu tarayıcı politikası (ajan BrowserSettings ile aynı alan adları).
/// </summary>
public class BrowserSettings
{
    public int? ChromeIncognitoModeAvailability { get; set; }
    public int? ChromeDeveloperToolsAvailability { get; set; }
    public string? ChromeHomePage { get; set; }
    public int? EdgeInPrivateModeAvailability { get; set; }
    public int? EdgeDeveloperToolsAvailability { get; set; }
    public string? EdgeHomePage { get; set; }
}
