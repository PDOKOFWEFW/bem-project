using EndpointApi.Models.Dashboard;

namespace EndpointApi.Services;

public interface IDashboardQueryService
{
    Task<DashboardBundleDto> GetBundleAsync(CancellationToken cancellationToken = default);
}
