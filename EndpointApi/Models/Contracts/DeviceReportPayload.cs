namespace EndpointApi.Models.Contracts;

/// <summary>
/// POST /api/device/report gövdesi (ajan DeviceReportPayload ile uyumlu).
/// </summary>
public class DeviceReportPayload
{
    public string DeviceId { get; set; } = string.Empty;
    public string EnrollmentToken { get; set; } = string.Empty;
    public string MachineName { get; set; } = string.Empty;
    public string? OsVersion { get; set; }
    public string? LoggedOnUser { get; set; }
    public string? IpAddress { get; set; }
    public bool LastPolicyAppliedStatus { get; set; } = true;
    public string? LastErrorMessage { get; set; }

    /// <summary>false = envanter aynı, Extensions boş olabilir.</summary>
    public bool HasChanged { get; set; } = true;

    public List<ExtensionInfo> Extensions { get; set; } = new();
}
