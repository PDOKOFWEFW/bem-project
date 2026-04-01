namespace EndpointApi.Services;

public interface IAdminApiKeyValidator
{
    bool IsValid(string? headerValue);
}
