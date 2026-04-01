using System.Net;
using System.Net.Http.Json;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using EndpointAgent.Models;
using Microsoft.Extensions.Configuration;

namespace EndpointAgent.Services
{
    /// <summary>
    /// Keşfedilen verileri backend API'ye raporlar ve dönen politikaları okur.
    /// </summary>
    public class ApiReporter : IApiReporter
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<ApiReporter> _logger;
        private readonly IDeviceIdentityService _identityService;
        private readonly string _enrollmentToken;

        public ApiReporter(
            HttpClient httpClient,
            ILogger<ApiReporter> logger,
            IConfiguration configuration,
            IDeviceIdentityService identityService)
        {
            _httpClient = httpClient;
            _logger = logger;
            _identityService = identityService;
            _enrollmentToken = configuration["SecuritySettings:EnrollmentToken"] ?? string.Empty;
        }

        /// <inheritdoc />
        public async Task<DevicePolicyResponse?> SendReportAsync(
            List<ExtensionInfo> extensions,
            bool extensionsUnchangedSinceLastSuccessfulReport,
            bool lastPolicyAppliedStatus,
            string? lastErrorMessage,
            CancellationToken cancellationToken = default)
        {
            var delta = extensionsUnchangedSinceLastSuccessfulReport;
            var payload = new DeviceReportPayload
            {
                DeviceId = _identityService.GetOrGenerateDeviceId(),
                EnrollmentToken = _enrollmentToken,
                MachineName = Environment.MachineName,
                OsVersion = RuntimeInformation.OSDescription,
                LoggedOnUser = GetLoggedOnUserSummary(),
                IpAddress = GetPrimaryLocalIPv4(),
                LastPolicyAppliedStatus = lastPolicyAppliedStatus,
                LastErrorMessage = lastErrorMessage,
                HasChanged = !delta,
                Extensions = delta ? new List<ExtensionInfo>() : extensions
            };

            const int maxAttempts = 3;
            for (var attempt = 1; attempt <= maxAttempts; attempt++)
            {
                try
                {
                    _httpClient.DefaultRequestHeaders.Remove("X-Enrollment-Token");
                    if (!string.IsNullOrWhiteSpace(_enrollmentToken))
                    {
                        _httpClient.DefaultRequestHeaders.Add("X-Enrollment-Token", _enrollmentToken);
                    }

                    using var response = await _httpClient.PostAsJsonAsync(
                        "api/device/report",
                        payload,
                        cancellationToken);

                    if (!response.IsSuccessStatusCode)
                    {
                        var body = await response.Content.ReadAsStringAsync(cancellationToken);

                        var transient = IsTransientStatusCode(response.StatusCode);
                        _logger.LogWarning(
                            "Rapor gönderimi başarısız. Attempt={Attempt}/{MaxAttempts}, StatusCode={StatusCode}, Body={Body}",
                            attempt, maxAttempts, (int)response.StatusCode, body);

                        if (transient && attempt < maxAttempts)
                        {
                            await Task.Delay(TimeSpan.FromSeconds(attempt * 2), cancellationToken);
                            continue;
                        }

                        return null;
                    }

                    _logger.LogInformation(
                        "Rapor gönderildi. Delta={Delta}, StatusCode={StatusCode}, ExtensionCount={Count}, Attempt={Attempt}",
                        delta,
                        (int)response.StatusCode,
                        payload.Extensions.Count,
                        attempt);

                    var policy = await response.Content.ReadFromJsonAsync<DevicePolicyResponse>(cancellationToken: cancellationToken);

                    if (policy == null)
                    {
                        _logger.LogInformation("API yanıtında herhangi bir politika dönmedi veya parse edilemedi.");
                    }

                    return policy;
                }
                catch (Exception ex) when (attempt < maxAttempts)
                {
                    _logger.LogWarning(
                        ex,
                        "API raporlama denemesi başarısız. Attempt={Attempt}/{MaxAttempts}. Yeniden denenecek.",
                        attempt,
                        maxAttempts);
                    await Task.Delay(TimeSpan.FromSeconds(attempt * 2), cancellationToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "API raporlama sırasında hata oluştu.");
                    return null;
                }
            }

            return null;
        }

        private static bool IsTransientStatusCode(HttpStatusCode statusCode)
        {
            return statusCode == HttpStatusCode.RequestTimeout ||
                   statusCode == HttpStatusCode.TooManyRequests ||
                   (int)statusCode >= 500;
        }

        private static string? GetLoggedOnUserSummary()
        {
            try
            {
                var user = Environment.UserName;
                var domain = Environment.UserDomainName;
                if (!string.IsNullOrWhiteSpace(domain) &&
                    !string.Equals(domain, Environment.MachineName, StringComparison.OrdinalIgnoreCase))
                {
                    return domain + "\\" + user;
                }

                return user;
            }
            catch
            {
                return null;
            }
        }

        private static string? GetPrimaryLocalIPv4()
        {
            try
            {
                foreach (var ni in NetworkInterface.GetAllNetworkInterfaces())
                {
                    if (ni.OperationalStatus != OperationalStatus.Up)
                        continue;
                    if (ni.NetworkInterfaceType == NetworkInterfaceType.Loopback)
                        continue;

                    foreach (var ua in ni.GetIPProperties().UnicastAddresses)
                    {
                        if (ua.Address.AddressFamily != AddressFamily.InterNetwork)
                            continue;
                        if (IPAddress.IsLoopback(ua.Address))
                            continue;
                        return ua.Address.ToString();
                    }
                }
            }
            catch
            {
            }

            return null;
        }
    }
}
