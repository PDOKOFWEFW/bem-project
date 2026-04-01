using EndpointAgent.Models;

namespace EndpointAgent.Services
{
    public interface IApiReporter
    {
        Task<DevicePolicyResponse?> SendReportAsync(
            List<ExtensionInfo> extensions,
            bool lastPolicyAppliedStatus,
            string? lastErrorMessage,
            CancellationToken cancellationToken = default);
    }
}

