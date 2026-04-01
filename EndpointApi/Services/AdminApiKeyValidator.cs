using EndpointApi.Configuration;
using Microsoft.Extensions.Options;

namespace EndpointApi.Services;

public class AdminApiKeyValidator : IAdminApiKeyValidator
{
    private readonly SecuritySettings _security;

    public AdminApiKeyValidator(IOptions<SecuritySettings> security)
    {
        _security = security.Value;
    }

    public bool IsValid(string? headerValue)
    {
        var expected = _security.AdminApiKey ?? string.Empty;
        if (string.IsNullOrWhiteSpace(expected))
            return true;

        return string.Equals(headerValue, expected, StringComparison.Ordinal);
    }
}
