using EndpointAgent.Models;
using EndpointAgent.Services;
using Microsoft.Extensions.Options;

namespace EndpointAgent
{
    /// <summary>
    /// Periyodik keşif + raporlama döngüsünü çalıştıran ana Worker.
    /// Döngü süresi appsettings.json -> WorkerSettings:IntervalMinutes üzerinden okunur.
    /// </summary>
    public class Worker : BackgroundService
    {
        private bool _lastStatus = true;
        private string? _lastError = null;

        /// <summary>Son başarılı API raporundan sonra envanter hash'i (delta rapor için).</summary>
        private string? _lastSuccessfulExtensionsHash;

        private readonly ILogger<Worker> _logger;
        private readonly IDiscoveryService _discoveryService;
        private readonly IApiReporter _apiReporter;
        private readonly IPolicyEnforcer _policyEnforcer;
        private readonly TimeSpan _interval;

        public Worker(
            ILogger<Worker> logger,
            IDiscoveryService discoveryService,
            IApiReporter apiReporter,
            IPolicyEnforcer policyEnforcer,
            IOptions<WorkerSettings> workerSettingsOptions)
        {
            _logger = logger;
            _discoveryService = discoveryService;
            _apiReporter = apiReporter;
            _policyEnforcer = policyEnforcer;

            var minutes = workerSettingsOptions.Value.IntervalMinutes;
            if (minutes <= 0)
            {
                minutes = 1;
            }

            _interval = TimeSpan.FromMinutes(minutes);
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("EndpointAgent Worker başlatıldı. Interval={Interval} dakika.", _interval.TotalMinutes);

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    _logger.LogInformation("Keşif + Raporlama döngüsü başladı.");

                    var extensions = _discoveryService.DiscoverInstalledExtensions();
                    var hash = _discoveryService.GetExtensionsHash(extensions);
                    var useDelta = _lastSuccessfulExtensionsHash != null &&
                        string.Equals(hash, _lastSuccessfulExtensionsHash, StringComparison.Ordinal);

                    if (useDelta)
                    {
                        _logger.LogInformation(
                            "Eklenti envanteri önceki başarılı rapor ile aynı hash; delta (HasChanged=false) gönderiliyor.");
                    }

                    var policy = await _apiReporter.SendReportAsync(
                        extensions,
                        useDelta,
                        _lastStatus,
                        _lastError,
                        stoppingToken);

                    if (policy != null)
                    {
                        _lastSuccessfulExtensionsHash = hash;

                        _logger.LogInformation(
                            "Policy yanıtı alındı. ForceInstallChrome={ForceC}, BlockChrome={BlockC}, AllowChrome={AllowC}, ForceInstallEdge={ForceE}, BlockEdge={BlockE}, AllowEdge={AllowE}, BrowserSettings={HasBs}",
                            policy.ForceInstallChrome?.Count ?? 0,
                            policy.BlockChrome?.Count ?? 0,
                            policy.AllowChrome?.Count ?? 0,
                            policy.ForceInstallEdge?.Count ?? 0,
                            policy.BlockEdge?.Count ?? 0,
                            policy.AllowEdge?.Count ?? 0,
                            policy.Settings != null);

                        if (policy.Settings != null)
                        {
                            var bsOk = _policyEnforcer.ApplyBrowserSettings(policy.Settings);
                            if (!bsOk)
                                _logger.LogWarning("ApplyBrowserSettings tamamlanamadı: {Err}", _policyEnforcer.LastErrorMessage);
                        }

                        var chromeOk = _policyEnforcer.ApplyPolicies(
                            "Chrome",
                            policy.ForceInstallChrome ?? new List<string>(),
                            policy.BlockChrome ?? new List<string>(),
                            policy.AllowChrome ?? new List<string>());
                        var chromeErr = _policyEnforcer.LastErrorMessage;

                        var edgeOk = _policyEnforcer.ApplyPolicies(
                            "Edge",
                            policy.ForceInstallEdge ?? new List<string>(),
                            policy.BlockEdge ?? new List<string>(),
                            policy.AllowEdge ?? new List<string>());
                        var edgeErr = _policyEnforcer.LastErrorMessage;

                        _lastStatus = chromeOk && edgeOk;
                        _lastError = _lastStatus
                            ? null
                            : (!chromeOk ? chromeErr : edgeErr);
                    }
                    else
                    {
                        _logger.LogDebug("Bu döngüde uygulanacak bir politika alınmadı.");
                        if (useDelta)
                        {
                            _lastSuccessfulExtensionsHash = null;
                            _logger.LogInformation("Delta raporu başarısız; sonraki döngüde tam envanter gönderilecek.");
                        }
                    }

                    _logger.LogInformation(
                        "Döngü tamamlandı. Eklenti sayısı: {Count}. Sonraki çalışma: {NextRun}",
                        extensions.Count,
                        DateTimeOffset.Now.Add(_interval));
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Keşif/raporlama döngüsü sırasında hata oluştu.");
                }

                try
                {
                    await Task.Delay(_interval, stoppingToken);
                }
                catch (TaskCanceledException)
                {
                }
            }

            _logger.LogInformation("EndpointAgent Worker durduruluyor.");
        }
    }
}
