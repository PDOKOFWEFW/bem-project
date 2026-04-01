using System.Text;
using EndpointApi.Data;
using EndpointApi.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EndpointApi.Controllers;

[ApiController]
[Route("api/export")]
public class ExportController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly IAdminApiKeyValidator _adminApiKeyValidator;
    private readonly ILogger<ExportController> _logger;

    // Logger eklendi ki içeride ne patlarsa terminalde net görelim
    public ExportController(AppDbContext db, IAdminApiKeyValidator adminApiKeyValidator, ILogger<ExportController> logger)
    {
        _db = db;
        _adminApiKeyValidator = adminApiKeyValidator;
        _logger = logger;
    }

    [HttpGet("devices/csv")]
    public async Task<IActionResult> ExportDevicesCsv(CancellationToken cancellationToken)
    {
        try
        {
            // Güvenlik: X-Admin-Api-Key Header kontrolü
            var adminKey = Request.Headers["X-Admin-Api-Key"].FirstOrDefault();
            if (!_adminApiKeyValidator.IsValid(adminKey))
            {
                return Unauthorized("Geçersiz Admin yetkisi.");
            }

            var devices = await _db.Devices
                .Include(d => d.OrganizationUnit)
                .AsNoTracking()
                .ToListAsync(cancellationToken);

            var sb = new StringBuilder();
            // CSV Başlıkları
            sb.AppendLine("Cihaz ID,Makine Adi,Kullanici,Isletim Sistemi,IP Adresi,OU,Risk Skoru,Son Gorulme");

            foreach (var d in devices)
            {
                var lastSeen = d.LastSeenUtc.ToString("yyyy-MM-dd HH:mm:ss");
                
                // BOŞ VERİ (NULL) KORUMASI: Eğer veri yoksa hata vermek yerine "Atanmamis" veya "Bilinmiyor" yazacak
                var ouName = d.OrganizationUnit?.Name ?? "Atanmamis";
                
                // Virgül koruması: İsimlerin içinde virgül varsa CSV formatını bozar, onları boşlukla değiştiriyoruz.
                var machine = d.MachineName?.Replace(",", " ") ?? "Bilinmiyor";
                var user = d.AssignedUserDisplayName?.Replace(",", " ") ?? "Bilinmiyor";
                var os = d.OsVersion?.Replace(",", " ") ?? "Bilinmiyor";
                var ip = d.IpAddress?.Replace(",", " ") ?? "Bilinmiyor";

                sb.AppendLine($"{d.DeviceId},{machine},{user},{os},{ip},{ouName},{d.RiskScore},{lastSeen}");
            }

            var fileBytes = Encoding.UTF8.GetBytes(sb.ToString());
            
            // Türkçe karakter bozulmaması için UTF-8 BOM ekliyoruz
            var fileBytesWithBom = new byte[] { 0xEF, 0xBB, 0xBF }.Concat(fileBytes).ToArray();

            return File(fileBytesWithBom, "text/csv", $"Cihaz_Envanteri_{DateTime.Now:yyyyMMdd}.csv");
        }
        catch (Exception ex)
        {
            // Eğer yine bir hata olursa, terminale kırmızı renkli olarak hatanın TAM nedenini yazdıracak
            _logger.LogError(ex, "CSV disari aktarilirken sunucu tarafinda bir hata olustu!");
            return StatusCode(500, $"Sunucu Hatası: {ex.Message}");
        }
    }
}