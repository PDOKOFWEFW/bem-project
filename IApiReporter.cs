using EndpointAgent.Models;

namespace EndpointAgent.Services
{
    public interface IApiReporter
    {
        /// <param name="extensions">Keşfedilen tam liste (delta modunda bile hash tutarlılığı için tutulur).</param>
        /// <param name="extensionsUnchangedSinceLastSuccessfulReport">true ise API'ye boş Extensions ve HasChanged=false gönderilir.</param>
        Task<DevicePolicyResponse?> SendReportAsync(
            List<ExtensionInfo> extensions,
            bool extensionsUnchangedSinceLastSuccessfulReport,
            bool lastPolicyAppliedStatus,
            string? lastErrorMessage,
            CancellationToken cancellationToken = default);
    }
}
