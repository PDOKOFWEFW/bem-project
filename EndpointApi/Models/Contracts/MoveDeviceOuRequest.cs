namespace EndpointApi.Models.Contracts;

public class MoveDeviceOuRequest
{
    public string DeviceId { get; set; } = string.Empty;

    /// <summary>Hedef OU adı (örn. Pazarlama, Unassigned).</summary>
    public string? TargetOrganizationUnitName { get; set; }

    public int? TargetOrganizationUnitId { get; set; }
}
