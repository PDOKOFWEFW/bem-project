namespace EndpointApi.Configuration;

/// <summary>
/// Genel API yapılandırması (temel URL, CORS vb.).
/// </summary>
public class ApiSettings
{
    public const string SectionName = "ApiSettings";

    /// <summary>
    /// Dışarıdan referans verilen taban adres (ör. ajan appsettings BaseUrl ile uyumlu).
    /// </summary>
    public string BaseUrl { get; set; } = "http://localhost:5000/";

    /// <summary>
    /// Dashboard veya statik istemciler için izin verilen CORS kökenleri.
    /// </summary>
    public string[] CorsOrigins { get; set; } = Array.Empty<string>();
}
