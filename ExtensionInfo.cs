namespace EndpointAgent.Models
{
    /// <summary>
    /// Cihazdan keşfedilen eklenti bilgisi.
    /// </summary>
    public class ExtensionInfo
    {
        public string ExtensionId { get; set; } = string.Empty;
        public string ExtensionName { get; set; } = string.Empty;
        public string ExtensionVersion { get; set; } = string.Empty;
        public string BrowserType { get; set; } = string.Empty;   // Chrome / Edge
    }
}
