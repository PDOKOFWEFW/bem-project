using EndpointApi.Configuration;
using Microsoft.Extensions.Options;

namespace EndpointApi.Services;

public class EnrollmentTokenValidator : IEnrollmentTokenValidator
{
    private readonly SecuritySettings _security;

    public EnrollmentTokenValidator(IOptions<SecuritySettings> security)
    {
        _security = security.Value;
    }

    public bool IsValid(string? headerToken, string? bodyToken)
    {
        var expected = _security.EnrollmentToken ?? string.Empty;
        if (string.IsNullOrWhiteSpace(expected))
            return true;

        var provided = !string.IsNullOrWhiteSpace(headerToken) ? headerToken : bodyToken;
        return string.Equals(provided, expected, StringComparison.Ordinal);
    }
}
