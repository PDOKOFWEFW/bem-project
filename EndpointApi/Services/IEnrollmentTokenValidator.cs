namespace EndpointApi.Services;

public interface IEnrollmentTokenValidator
{
    bool IsValid(string? headerToken, string? bodyToken);
}
