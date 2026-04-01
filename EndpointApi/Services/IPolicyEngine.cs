using EndpointApi.Models.Contracts;

namespace EndpointApi.Services;

/// <summary>
/// OU'ya göre politika kurallarını birleştirir ve ajan yanıt modeline dönüştürür.
/// </summary>
public interface IPolicyEngine
{
    DevicePolicyResponse BuildResponseForOrganizationUnit(int organizationUnitId);
}
