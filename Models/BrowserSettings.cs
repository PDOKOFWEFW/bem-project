namespace EndpointAgent.Models
{
    /// <summary>
    /// Chrome / Edge için GPO benzeri genel tarayıcı politikaları (registry hedefli).
    /// </summary>
    public class BrowserSettings
    {
        /// <summary>
        /// Chrome: IncognitoModeAvailability (DWORD). 0 = kullanılabilir, 1 = devre dışı, 2 = zorunlu mod davranışı.
        /// </summary>
        public int? ChromeIncognitoModeAvailability { get; set; }

        /// <summary>
        /// Chrome: DeveloperToolsAvailability. 0 = varsayılan, 1 = devre dışı, 2 = izinli.
        /// </summary>
        public int? ChromeDeveloperToolsAvailability { get; set; }

        /// <summary>
        /// Chrome: Home / başlangıç URL (HomepageLocation).
        /// </summary>
        public string? ChromeHomePage { get; set; }

        /// <summary>
        /// Edge: InPrivateModeAvailability (DWORD). Chrome ile benzer değerler.
        /// </summary>
        public int? EdgeInPrivateModeAvailability { get; set; }

        /// <summary>
        /// Edge: DeveloperToolsAvailability.
        /// </summary>
        public int? EdgeDeveloperToolsAvailability { get; set; }

        /// <summary>
        /// Edge: HomepageLocation.
        /// </summary>
        public string? EdgeHomePage { get; set; }
    }
}
