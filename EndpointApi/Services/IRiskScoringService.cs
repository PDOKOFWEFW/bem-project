using EndpointApi.Data.Entities;

namespace EndpointApi.Services;

public interface IRiskScoringService
{
    /// <summary>
    /// OU blok listesine göre yüklü eklentilerden risk skoru (0–100) üretir.
    /// </summary>
    int ComputeRiskScore(Device device, IReadOnlyCollection<Extension> extensions);
}
