namespace EndpointApi.Configuration;

/// <summary>
/// Güvenlik ile ilgili ayarlar (kayıt token doğrulaması vb.).
/// </summary>
public class SecuritySettings
{
    public const string SectionName = "SecuritySettings";

    /// <summary>
    /// Cihaz raporlarında zorunlu tutulan kayıt (enrollment) anahtarı.
    /// </summary>
    public string EnrollmentToken { get; set; } = string.Empty;
}
