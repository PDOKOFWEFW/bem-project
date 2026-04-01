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
        // Bir önceki döngüde policy enforceni sonucu (payload ile geri raporlanır).
        private bool _lastStatus = true;
        private string? _lastError = null;

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
                minutes = 1; // Koruyucu: yanlış konfigurasyonda bile en az 1 dakika.
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

                    // Raporu gönder ve backend'den cihaz politikalarını al.
                    DevicePolicyResponse? policy = await _apiReporter.SendReportAsync(
                        extensions,
                        _lastStatus,
                        _lastError,
                        stoppingToken);

                    // Gelen politika varsa, cihaza uygula.
                    if (policy != null)
                    {
                        _logger.LogInformation(
                            "Policy yanıtı alındı. ForceInstallChrome={ForceC}, BlockChrome={BlockC}, AllowChrome={AllowC}, ForceInstallEdge={ForceE}, BlockEdge={BlockE}, AllowEdge={AllowE}",
                            policy.ForceInstallChrome?.Count ?? 0,
                            policy.BlockChrome?.Count ?? 0,
                            policy.AllowChrome?.Count ?? 0,
                            policy.ForceInstallEdge?.Count ?? 0,
                            policy.BlockEdge?.Count ?? 0,
                            policy.AllowEdge?.Count ?? 0);

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
                    }

                    _logger.LogInformation(
                        "Döngü tamamlandı. Eklenti sayısı: {Count}. Sonraki çalışma: {NextRun}",
                        extensions.Count,
                        DateTimeOffset.Now.Add(_interval));
                }
                catch (Exception ex)
                {
                    // Worker'ın crash olmasını engellememek için tüm hataları loglarız.
                    _logger.LogError(ex, "Keşif/raporlama döngüsü sırasında hata oluştu.");
                }

                try
                {
                    await Task.Delay(_interval, stoppingToken);
                }
                catch (TaskCanceledException)
                {
                    // Servis durdurulurken beklenen davranış.
                }
            }

            _logger.LogInformation("EndpointAgent Worker durduruluyor.");
        }
    }
}
