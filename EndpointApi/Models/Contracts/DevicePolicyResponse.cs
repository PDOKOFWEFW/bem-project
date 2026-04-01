namespace EndpointApi.Models.Contracts;

/// <summary>
/// Ajanın uygulayacağı politika listeleri (EndpointAgent.DevicePolicyResponse ile uyumlu).
/// </summary>
public class DevicePolicyResponse
{
    public List<string> ForceInstallChrome { get; set; } = new();
    public List<string> BlockChrome { get; set; } = new();
    public List<string> ForceInstallEdge { get; set; } = new();
    public List<string> BlockEdge { get; set; } = new();
    public List<string> AllowChrome { get; set; } = new();
    public List<string> AllowEdge { get; set; } = new();
}
