using Microsoft.EntityFrameworkCore;

namespace EndpointApi.Data.Entities;

/// <summary>
/// Cihaz üzerinde raporlanan tarayıcı eklentisi envanteri.
/// </summary>
[Index(nameof(DeviceId), nameof(ExtensionId), nameof(BrowserType), IsUnique = true)]
public class Extension
{
    public int Id { get; set; }

    public int DeviceId { get; set; }
    public Device Device { get; set; } = null!;

    public string ExtensionId { get; set; } = string.Empty;
    public string ExtensionName { get; set; } = string.Empty;
    public string ExtensionVersion { get; set; } = string.Empty;
    public string BrowserType { get; set; } = string.Empty;

    public DateTimeOffset LastReportedUtc { get; set; }
}
