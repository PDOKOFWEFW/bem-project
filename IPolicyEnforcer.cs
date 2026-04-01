using EndpointAgent.Models;

namespace EndpointAgent.Services
{
    public interface IPolicyEnforcer
    {
        string? LastErrorMessage { get; }

        bool ApplyPolicies(string browserType, List<string> forceInstallIds, List<string> blockIds, List<string> allowIds);

        /// <summary>
        /// Genel tarayıcı ayarlarını (GPO benzeri) HKLM politikalarına yazar.
        /// </summary>
        bool ApplyBrowserSettings(BrowserSettings? settings);
    }
}
