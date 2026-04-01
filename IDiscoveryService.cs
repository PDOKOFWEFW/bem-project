using EndpointAgent.Models;

namespace EndpointAgent.Services
{
    public interface IDiscoveryService
    {
        List<ExtensionInfo> DiscoverInstalledExtensions();
    }
}

