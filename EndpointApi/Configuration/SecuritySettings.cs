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

    /// <summary>
    /// Dashboard yönetim uçları için X-Admin-Api-Key başlığı (boşsa doğrulama yapılmaz — yalnızca geliştirme).
    /// </summary>
    public string AdminApiKey { get; set; } = string.Empty;
}
