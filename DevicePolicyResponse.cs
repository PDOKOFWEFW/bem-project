namespace EndpointAgent.Models
{
    /// <summary>
    /// Backend API'den dönen, cihaza uygulanacak eklenti politikaları.
    /// </summary>
    public class DevicePolicyResponse
    {
        /// <summary>
        /// Chrome için zorunlu yüklenecek eklenti ID listesi.
        /// </summary>
        public List<string> ForceInstallChrome { get; set; } = new();

        /// <summary>
        /// Chrome için engellenecek (blocklist) eklenti ID listesi.
        /// </summary>
        public List<string> BlockChrome { get; set; } = new();

        /// <summary>
        /// Edge için zorunlu yüklenecek eklenti ID listesi.
        /// </summary>
        public List<string> ForceInstallEdge { get; set; } = new();

        /// <summary>
        /// Edge için engellenecek (blocklist) eklenti ID listesi.
        /// </summary>
        public List<string> BlockEdge { get; set; } = new();

        /// <summary>
        /// Chrome için izin verilecek (allowlist) eklenti ID listesi.
        /// </summary>
        public List<string> AllowChrome { get; set; } = new();

        /// <summary>
        /// Edge için izin verilecek (allowlist) eklenti ID listesi.
        /// </summary>
        public List<string> AllowEdge { get; set; } = new();

        /// <summary>
        /// İsteğe bağlı genel tarayıcı (GPO) ayarları.
        /// </summary>
        public BrowserSettings? Settings { get; set; }
    }
}

