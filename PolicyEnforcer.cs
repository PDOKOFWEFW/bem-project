using EndpointAgent.Models;
using Microsoft.Win32;

namespace EndpointAgent.Services
{
    /// <summary>
    /// ExtensionInstallBlocklist / ExtensionInstallForcelist registry policy yazımı.
    /// Not: HKLM yazımı genellikle admin yetkisi gerektirir.
    /// </summary>
    public class PolicyEnforcer : IPolicyEnforcer
    {
        private const string ChromePolicyPath = @"SOFTWARE\Policies\Google\Chrome";
        private const string EdgePolicyPath = @"SOFTWARE\Policies\Microsoft\Edge";

        private readonly ILogger<PolicyEnforcer> _logger;

        /// <summary>
        /// En son ApplyPolicies/SyncRegistryKey denemesinde oluşan hata mesajı.
        /// Worker tarafı bu bilgiyi payload içine taşır.
        /// </summary>
        public string? LastErrorMessage { get; private set; }

        public PolicyEnforcer(ILogger<PolicyEnforcer> logger)
        {
            _logger = logger;
        }

        /// <inheritdoc />
        public bool ApplyBrowserSettings(BrowserSettings? settings)
        {
            try
            {
                LastErrorMessage = null;
                settings ??= new BrowserSettings();

                var chromeOk = ApplyChromeBrowserPolicies(settings);
                var edgeOk = ApplyEdgeBrowserPolicies(settings);
                return chromeOk && edgeOk;
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogWarning(ex, "ApplyBrowserSettings için yetki yok (Run as Admin gerekebilir).");
                LastErrorMessage = ex.Message;
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ApplyBrowserSettings sırasında hata oluştu.");
                LastErrorMessage = ex.Message;
                return false;
            }
        }

        private bool ApplyChromeBrowserPolicies(BrowserSettings settings)
        {
            using var baseKey = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64);
            using var key = baseKey.CreateSubKey(ChromePolicyPath, writable: true);
            if (key == null)
            {
                LastErrorMessage = "Chrome policy anahtarı açılamadı.";
                return false;
            }

            if (settings.ChromeIncognitoModeAvailability.HasValue)
            {
                key.SetValue(
                    "IncognitoModeAvailability",
                    settings.ChromeIncognitoModeAvailability.Value,
                    RegistryValueKind.DWord);
                _logger.LogInformation(
                    "Chrome IncognitoModeAvailability={Value}",
                    settings.ChromeIncognitoModeAvailability.Value);
            }

            if (settings.ChromeDeveloperToolsAvailability.HasValue)
            {
                key.SetValue(
                    "DeveloperToolsAvailability",
                    settings.ChromeDeveloperToolsAvailability.Value,
                    RegistryValueKind.DWord);
                _logger.LogInformation(
                    "Chrome DeveloperToolsAvailability={Value}",
                    settings.ChromeDeveloperToolsAvailability.Value);
            }

            if (settings.ChromeHomePage != null)
            {
                if (string.IsNullOrWhiteSpace(settings.ChromeHomePage))
                    key.DeleteValue("HomepageLocation", throwOnMissingValue: false);
                else
                    key.SetValue("HomepageLocation", settings.ChromeHomePage.Trim(), RegistryValueKind.String);
            }

            return true;
        }

        private bool ApplyEdgeBrowserPolicies(BrowserSettings settings)
        {
            using var baseKey = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64);
            using var key = baseKey.CreateSubKey(EdgePolicyPath, writable: true);
            if (key == null)
            {
                LastErrorMessage = "Edge policy anahtarı açılamadı.";
                return false;
            }

            if (settings.EdgeInPrivateModeAvailability.HasValue)
            {
                key.SetValue(
                    "InPrivateModeAvailability",
                    settings.EdgeInPrivateModeAvailability.Value,
                    RegistryValueKind.DWord);
                _logger.LogInformation(
                    "Edge InPrivateModeAvailability={Value}",
                    settings.EdgeInPrivateModeAvailability.Value);
            }

            if (settings.EdgeDeveloperToolsAvailability.HasValue)
            {
                key.SetValue(
                    "DeveloperToolsAvailability",
                    settings.EdgeDeveloperToolsAvailability.Value,
                    RegistryValueKind.DWord);
                _logger.LogInformation(
                    "Edge DeveloperToolsAvailability={Value}",
                    settings.EdgeDeveloperToolsAvailability.Value);
            }

            if (settings.EdgeHomePage != null)
            {
                if (string.IsNullOrWhiteSpace(settings.EdgeHomePage))
                    key.DeleteValue("HomepageLocation", throwOnMissingValue: false);
                else
                    key.SetValue("HomepageLocation", settings.EdgeHomePage.Trim(), RegistryValueKind.String);
            }

            return true;
        }

        /// <summary>
        /// Tarayıcı tipine göre force/block/allow policy listelerini toplu senkronize eder.
        /// Registry state'i istenen liste ile birebir eşitler (add + remove).
        /// </summary>
        public bool ApplyPolicies(string browserType, List<string> forceInstallIds, List<string> blockIds, List<string> allowIds)
        {
            try
            {
                LastErrorMessage = null;

                var browser = (browserType ?? string.Empty).Trim().ToLowerInvariant();
                string forcePath;
                string blockPath;
                string allowPath;
                string updateUrl;

                if (browser == "chrome")
                {
                    forcePath = @"SOFTWARE\Policies\Google\Chrome\ExtensionInstallForcelist";
                    blockPath = @"SOFTWARE\Policies\Google\Chrome\ExtensionInstallBlocklist";
                    allowPath = @"SOFTWARE\Policies\Google\Chrome\ExtensionInstallAllowlist";
                    updateUrl = "https://clients2.google.com/service/update2/crx";
                }
                else if (browser == "edge")
                {
                    forcePath = @"SOFTWARE\Policies\Microsoft\Edge\ExtensionInstallForcelist";
                    blockPath = @"SOFTWARE\Policies\Microsoft\Edge\ExtensionInstallBlocklist";
                    allowPath = @"SOFTWARE\Policies\Microsoft\Edge\ExtensionInstallAllowlist";
                    updateUrl = "https://edge.microsoft.com/extensionwebstorebase/v1/crx";
                }
                else
                {
                    _logger.LogDebug("Desteklenmeyen browserType için ApplyPolicies atlandı: {BrowserType}", browserType);
                    LastErrorMessage = $"Desteklenmeyen browserType: {browserType}";
                    return false;
                }

                var normalizedForceIds = (forceInstallIds ?? new List<string>())
                    .Where(x => !string.IsNullOrWhiteSpace(x))
                    .Select(x => x.Trim())
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToList();

                var normalizedBlockIds = (blockIds ?? new List<string>())
                    .Where(x => !string.IsNullOrWhiteSpace(x))
                    .Select(x => x.Trim())
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToList();

                var normalizedAllowIds = (allowIds ?? new List<string>())
                    .Where(x => !string.IsNullOrWhiteSpace(x))
                    .Select(x => x.Trim())
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToList();

                // Force values: "{id};{updateUrl}" formatında yazılır.
                var desiredForceValues = normalizedForceIds
                    .Select(id => $"{id};{updateUrl}")
                    .ToList();

                // Block values: sadece extension ID.
                var desiredBlockValues = normalizedBlockIds;

                var forceOk = SyncRegistryKey(RegistryHive.LocalMachine, forcePath, desiredForceValues);
                var blockOk = SyncRegistryKey(RegistryHive.LocalMachine, blockPath, desiredBlockValues);
                var allowOk = SyncRegistryKey(RegistryHive.LocalMachine, allowPath, normalizedAllowIds);

                // Herhangi bir adım başarısız ise false dön.
                return forceOk && blockOk && allowOk;
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogWarning(
                    ex,
                    "ApplyPolicies için yetki yok (Run as Admin gerekli olabilir). BrowserType={BrowserType}",
                    browserType);

                LastErrorMessage = ex.Message;
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "ApplyPolicies sırasında hata oluştu. BrowserType={BrowserType}",
                    browserType);

                LastErrorMessage = ex.Message;
                return false;
            }
        }

        /// <summary>
        /// Registry key state'ini desiredValues listesi ile senkronize eder.
        /// - Registry'de olup desired'da olmayanları siler
        /// - Desired'da olup registry'de olmayanları yeni boş index'e ekler
        /// </summary>
        private bool SyncRegistryKey(RegistryHive hive, string subKeyPath, List<string> desiredValues)
        {
            try
            {
                var desired = (desiredValues ?? new List<string>())
                    .Where(v => !string.IsNullOrWhiteSpace(v))
                    .Select(v => v.Trim())
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToList();

                using var baseKey = RegistryKey.OpenBaseKey(hive, RegistryView.Registry64);
                using var subKey = baseKey.CreateSubKey(subKeyPath, writable: true);

                if (subKey == null)
                {
                    _logger.LogWarning("Policy anahtarı oluşturulamadı: {Hive}\\{Path}", hive, subKeyPath);
                    LastErrorMessage ??= "Policy registry anahtarı oluşturulamadı.";
                    return false;
                }

                // b) Mevcut tüm değerleri oku.
                var existingMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                foreach (var name in subKey.GetValueNames())
                {
                    var existingValue = subKey.GetValue(name)?.ToString();
                    if (!string.IsNullOrWhiteSpace(existingValue))
                    {
                        existingMap[name] = existingValue.Trim();
                    }
                }

                // c) Registry'de olup desired'da olmayanları sil.
                foreach (var kv in existingMap)
                {
                    if (!desired.Contains(kv.Value, StringComparer.OrdinalIgnoreCase))
                    {
                        subKey.DeleteValue(kv.Key, throwOnMissingValue: false);
                        _logger.LogInformation(
                            "Registry value silindi. Hive={Hive}, Path={Path}, Name={Name}, Value={Value}",
                            hive, subKeyPath, kv.Key, kv.Value);
                    }
                }

                // d) Desired'da olup registry'de olmayanları ekle.
                var currentValues = subKey.GetValueNames()
                    .Select(name => subKey.GetValue(name)?.ToString())
                    .Where(v => !string.IsNullOrWhiteSpace(v))
                    .Select(v => v!.Trim())
                    .ToHashSet(StringComparer.OrdinalIgnoreCase);

                foreach (var value in desired)
                {
                    if (currentValues.Contains(value))
                        continue;

                    var nextIndex = 1;
                    while (subKey.GetValue(nextIndex.ToString()) != null)
                    {
                        nextIndex++;
                    }

                    subKey.SetValue(nextIndex.ToString(), value, RegistryValueKind.String);
                    currentValues.Add(value);

                    _logger.LogInformation(
                        "Registry value eklendi. Hive={Hive}, Path={Path}, Name={Name}, Value={Value}",
                        hive, subKeyPath, nextIndex, value);
                }

                return true;
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogWarning(
                    ex,
                    "SyncRegistryKey için yetki yok (Run as Admin gerekli olabilir). Hive={Hive}, Path={Path}",
                    hive,
                    subKeyPath);

                LastErrorMessage = ex.Message;
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "SyncRegistryKey sırasında hata oluştu. Hive={Hive}, Path={Path}",
                    hive,
                    subKeyPath);

                LastErrorMessage = ex.Message;
                return false;
            }
        }
    }
}
