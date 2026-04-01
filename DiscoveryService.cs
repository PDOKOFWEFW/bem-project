using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using EndpointAgent.Models;
using Microsoft.Win32;

namespace EndpointAgent.Services
{
    /// <summary>
    /// Registry üzerinden Chrome ve Edge eklenti ID keşfi yapar.
    /// MVP: Sadece extension ID okumak yeterli.
    /// </summary>
    public class DiscoveryService : IDiscoveryService
    {
        private readonly ILogger<DiscoveryService> _logger;

        public DiscoveryService(ILogger<DiscoveryService> logger)
        {
            _logger = logger;
        }

        public List<ExtensionInfo> DiscoverInstalledExtensions()
        {
            var results = new List<ExtensionInfo>();

            try
            {
                // Chrome
                DiscoverFromPath(
                    RegistryHive.CurrentUser,
                    @"Software\Google\Chrome\PreferenceMACs\Default\extensions",
                    "Chrome",
                    results);

                DiscoverFromPath(
                    RegistryHive.LocalMachine,
                    @"Software\Google\Chrome\PreferenceMACs\Default\extensions",
                    "Chrome",
                    results);

                // Edge
                DiscoverFromPath(
                    RegistryHive.CurrentUser,
                    @"Software\Microsoft\Edge\PreferenceMACs\Default\extensions",
                    "Edge",
                    results);

                DiscoverFromPath(
                    RegistryHive.LocalMachine,
                    @"Software\Microsoft\Edge\PreferenceMACs\Default\extensions",
                    "Edge",
                    results);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Eklenti keşfi sırasında beklenmeyen hata oluştu.");
            }

            // Aynı BrowserType + ExtensionId kombinasyonlarını tekilleştir.
            return results
                .GroupBy(x => new { x.ExtensionId, x.BrowserType })
                .Select(g => g.First())
                .ToList();
        }

        /// <inheritdoc />
        public string GetExtensionsHash(List<ExtensionInfo> extensions)
        {
            var items = (extensions ?? new List<ExtensionInfo>())
                .OrderBy(x => x.BrowserType ?? "", StringComparer.OrdinalIgnoreCase)
                .ThenBy(x => x.ExtensionId ?? "", StringComparer.OrdinalIgnoreCase)
                .Select(x => $"{x.BrowserType}|{x.ExtensionId}|{x.ExtensionVersion}|{x.ExtensionName}");

            var canonical = string.Join('\n', items);
            var bytes = Encoding.UTF8.GetBytes(canonical);
            var hash = SHA256.HashData(bytes);
            return Convert.ToHexString(hash);
        }

        private void DiscoverFromPath(
            RegistryHive hive,
            string subKeyPath,
            string browserType,
            List<ExtensionInfo> target)
        {
            try
            {
                using var baseKey = RegistryKey.OpenBaseKey(hive, RegistryView.Registry64);
                using var key = baseKey.OpenSubKey(subKeyPath);

                if (key == null)
                {
                    _logger.LogDebug("Registry yolu bulunamadı: {Hive}\\{Path}", hive, subKeyPath);
                    return;
                }

                foreach (var extensionId in key.GetSubKeyNames())
                {
                    if (string.IsNullOrWhiteSpace(extensionId))
                        continue;

                    using var extKey = key.OpenSubKey(extensionId);
                    var extensionName = extKey?.GetValue("name")?.ToString() ?? string.Empty;
                    var extensionVersion = extKey?.GetValue("version")?.ToString() ?? string.Empty;

                    // Registry'de yoksa kullanıcı profilindeki extension klasöründen manifest okuma denemesi.
                    if (string.IsNullOrWhiteSpace(extensionName) || string.IsNullOrWhiteSpace(extensionVersion))
                    {
                        TryReadManifestMetadata(browserType, extensionId, ref extensionName, ref extensionVersion);
                    }

                    target.Add(new ExtensionInfo
                    {
                        ExtensionId = extensionId,
                        ExtensionName = extensionName,
                        ExtensionVersion = extensionVersion,
                        BrowserType = browserType
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(
                    ex,
                    "Registry okuma hatası. Hive={Hive}, Path={Path}",
                    hive, subKeyPath);
            }
        }

        private void TryReadManifestMetadata(
            string browserType,
            string extensionId,
            ref string extensionName,
            ref string extensionVersion)
        {
            try
            {
                var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
                if (string.IsNullOrWhiteSpace(localAppData))
                    return;

                var browserFolder = browserType.Equals("Edge", StringComparison.OrdinalIgnoreCase)
                    ? "Microsoft\\Edge\\User Data\\Default\\Extensions"
                    : "Google\\Chrome\\User Data\\Default\\Extensions";

                var extensionBasePath = Path.Combine(localAppData, browserFolder, extensionId);
                if (!Directory.Exists(extensionBasePath))
                    return;

                // Genellikle version klasörleri bulunur, en güncelini al.
                var versionDirs = Directory.GetDirectories(extensionBasePath);
                if (versionDirs.Length == 0)
                    return;

                var latestVersionDir = versionDirs
                    .OrderByDescending(x => x, StringComparer.OrdinalIgnoreCase)
                    .First();

                var manifestPath = Path.Combine(latestVersionDir, "manifest.json");
                if (!File.Exists(manifestPath))
                    return;

                using var stream = File.OpenRead(manifestPath);
                using var doc = JsonDocument.Parse(stream);
                var root = doc.RootElement;

                if (string.IsNullOrWhiteSpace(extensionName) &&
                    root.TryGetProperty("name", out var nameProp))
                {
                    extensionName = nameProp.GetString() ?? string.Empty;
                }

                if (string.IsNullOrWhiteSpace(extensionVersion) &&
                    root.TryGetProperty("version", out var versionProp))
                {
                    extensionVersion = versionProp.GetString() ?? string.Empty;
                }
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "Manifest metadata okunamadı. Browser={Browser}, ExtensionId={ExtensionId}", browserType, extensionId);
            }
        }
    }
}
