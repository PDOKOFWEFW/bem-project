using EndpointAgent;
using EndpointAgent.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

// Klasik HostBuilder ile Windows Service entegrasyonu ve appsettings desteği.
var host = Host.CreateDefaultBuilder(args)
    .UseWindowsService() // Microsoft.Extensions.Hosting.WindowsServices paketi ile gelir.
    .ConfigureServices((context, services) =>
    {
        var configuration = context.Configuration;

        // Worker interval ayarını configuration üzerinden okuyabilmek için IOptions desteği.
        services.Configure<WorkerSettings>(configuration.GetSection("WorkerSettings"));

        // BackgroundService
        services.AddHostedService<Worker>();

        // API raporlama için HttpClient - BaseUrl appsettings.json'dan okunur.
        services.AddHttpClient<IApiReporter, ApiReporter>((sp, client) =>
        {
            var baseUrl = configuration.GetSection("ApiSettings")["BaseUrl"] ?? "http://localhost:5000/";
            client.BaseAddress = new Uri(baseUrl);
            client.Timeout = TimeSpan.FromSeconds(30);
        });

        // Servis bağımlılıkları
        services.AddSingleton<IDiscoveryService, DiscoveryService>();
        services.AddSingleton<IDeviceIdentityService, DeviceIdentityService>();
        services.AddSingleton<IPolicyEnforcer, PolicyEnforcer>();
    })
    .Build();

await host.RunAsync();
