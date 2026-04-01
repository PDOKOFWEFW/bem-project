using EndpointAgent.Models;

namespace EndpointAgent.Services
{
    public interface IDiscoveryService
    {
        List<ExtensionInfo> DiscoverInstalledExtensions();

        /// <summary>
        /// Eklenti listesi için deterministik hash (delta raporlama).
        /// </summary>
        string GetExtensionsHash(List<ExtensionInfo> extensions);
    }
}
