using System.Text.Json.Serialization;

namespace EndpointAgent.Models
{
    /// <summary>
    /// Backend API'ye gönderilecek rapor payload modeli.
    /// </summary>
    public class DeviceReportPayload
    {
        public string DeviceId { get; set; } = string.Empty;
        public string EnrollmentToken { get; set; } = string.Empty;
        public string MachineName { get; set; } = string.Empty;
        public string? OsVersion { get; set; }

        /// <summary>Oturum açmış kullanıcı (ör. DOMAIN\user veya user).</summary>
        public string? LoggedOnUser { get; set; }

        /// <summary>Algılanan yerel IPv4 adresi (veya boş).</summary>
        public string? IpAddress { get; set; }

        public bool LastPolicyAppliedStatus { get; set; } = true;
        public string? LastErrorMessage { get; set; }

        /// <summary>
        /// false ise envanter değişmemiş kabul edilir; Extensions boş gönderilebilir.
        /// </summary>
        public bool HasChanged { get; set; } = true;

        [JsonPropertyName("Extensions")]
        public List<ExtensionInfo> Extensions { get; set; } = new();
    }
}
