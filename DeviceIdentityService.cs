using Microsoft.Win32;

namespace EndpointAgent.Services
{
    /// <summary>
    /// Cihaza ait kalıcı DeviceId bilgisini Registry üzerinde yönetir.
    /// Önce HKLM denenir; yetki yoksa HKCU fallback olarak kullanılır.
    /// </summary>
    public class DeviceIdentityService : IDeviceIdentityService
    {
        private readonly ILogger<DeviceIdentityService> _logger;
        private const string AgentSubKeyPath = @"SOFTWARE\EndpointAgent";
        private const string DeviceIdValueName = "DeviceId";

        public DeviceIdentityService(ILogger<DeviceIdentityService> logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// Registry'den mevcut DeviceId'yi okur; yoksa yeni GUID üretip kaydeder.
        /// </summary>
        public string GetOrGenerateDeviceId()
        {
            // 1) HKLM'den okumayı dene.
            var hklmId = TryGetDeviceId(RegistryHive.LocalMachine);
            if (!string.IsNullOrWhiteSpace(hklmId))
            {
                return hklmId;
            }

            // 2) HKCU'dan okumayı dene.
            var hkcuId = TryGetDeviceId(RegistryHive.CurrentUser);
            if (!string.IsNullOrWhiteSpace(hkcuId))
            {
                return hkcuId;
            }

            // 3) Yoksa yeni ID üret.
            var newId = Guid.NewGuid().ToString();

            // Önce HKLM'e yazmayı dene, başarısız olursa HKCU'ya yaz.
            if (TrySetDeviceId(RegistryHive.LocalMachine, newId))
            {
                return newId;
            }

            if (TrySetDeviceId(RegistryHive.CurrentUser, newId))
            {
                return newId;
            }

            // Nadir durum: registry yazılamazsa dahi agent çalışmaya devam etsin.
            _logger.LogWarning("DeviceId registry'ye yazılamadı. Geçici DeviceId döndürülüyor.");
            return newId;
        }

        private string? TryGetDeviceId(RegistryHive hive)
        {
            try
            {
                using var baseKey = RegistryKey.OpenBaseKey(hive, RegistryView.Registry64);
                using var subKey = baseKey.OpenSubKey(AgentSubKeyPath, writable: false);

                if (subKey == null)
                {
                    return null;
                }

                var deviceId = subKey.GetValue(DeviceIdValueName)?.ToString();
                return string.IsNullOrWhiteSpace(deviceId) ? null : deviceId.Trim();
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "DeviceId okuma başarısız. Hive={Hive}", hive);
                return null;
            }
        }

        private bool TrySetDeviceId(RegistryHive hive, string deviceId)
        {
            try
            {
                using var baseKey = RegistryKey.OpenBaseKey(hive, RegistryView.Registry64);
                using var subKey = baseKey.CreateSubKey(AgentSubKeyPath, writable: true);

                if (subKey == null)
                {
                    return false;
                }

                subKey.SetValue(DeviceIdValueName, deviceId, RegistryValueKind.String);
                _logger.LogInformation("DeviceId registry'ye kaydedildi. Hive={Hive}", hive);
                return true;
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogDebug(ex, "DeviceId yazımı için yetki yok. Hive={Hive}", hive);
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "DeviceId yazma sırasında hata oluştu. Hive={Hive}", hive);
                return false;
            }
        }
    }
}

