namespace EndpointAgent
{
    /// <summary>
    /// Worker döngü süresi konfigurasyonu (dakika cinsinden).
    /// appsettings.json -> WorkerSettings:IntervalMinutes
    /// </summary>
    public class WorkerSettings
    {
        public int IntervalMinutes { get; set; } = 60;
    }
}

