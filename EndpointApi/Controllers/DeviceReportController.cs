using EndpointApi.Data;
using EndpointApi.Data.Entities;
using EndpointApi.Models.Contracts;
using EndpointApi.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EndpointApi.Controllers;

[ApiController]
[Route("api/device")]
public class DeviceReportController : ControllerBase
{
    private const string EnrollmentHeaderName = "X-Enrollment-Token";
    private const string AdminApiKeyHeaderName = "X-Admin-Api-Key";
    private const string UnassignedOuName = "Unassigned";

    private readonly AppDbContext _db;
    private readonly IPolicyEngine _policyEngine;
    private readonly IRiskScoringService _riskScoring;
    private readonly IEnrollmentTokenValidator _tokenValidator;
    private readonly IAdminApiKeyValidator _adminApiKeyValidator;
    private readonly ILogger<DeviceReportController> _logger;

    public DeviceReportController(
        AppDbContext db,
        IPolicyEngine policyEngine,
        IRiskScoringService riskScoring,
        IEnrollmentTokenValidator tokenValidator,
        IAdminApiKeyValidator adminApiKeyValidator,
        ILogger<DeviceReportController> logger)
    {
        _db = db;
        _policyEngine = policyEngine;
        _riskScoring = riskScoring;
        _tokenValidator = tokenValidator;
        _adminApiKeyValidator = adminApiKeyValidator;
        _logger = logger;
    }

    /// <summary>
    /// Ajanın periyodik cihaz/eklenti raporunu alır ve OU politikalarını döner.
    /// </summary>
    [HttpPost("report")]
    [ProducesResponseType(typeof(DevicePolicyResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<DevicePolicyResponse>> ReportAsync(
        [FromBody] DeviceReportPayload payload,
        CancellationToken cancellationToken)
    {
        var headerToken = Request.Headers[EnrollmentHeaderName].FirstOrDefault();
        if (!_tokenValidator.IsValid(headerToken, payload.EnrollmentToken))
        {
            _logger.LogWarning("Geçersiz veya eksik enrollment token.");
            return Unauthorized();
        }

        if (string.IsNullOrWhiteSpace(payload.DeviceId))
            return BadRequest("DeviceId gerekli.");

        var unassignedOuId = await _db.OrganizationalUnits
            .AsNoTracking()
            .Where(o => o.Name == UnassignedOuName)
            .Select(o => o.Id)
            .FirstOrDefaultAsync(cancellationToken);

        if (unassignedOuId == 0)
        {
            _logger.LogError("{Ou} OU bulunamadı. DbInitializer / Ensure çalıştırıldığından emin olun.", UnassignedOuName);
            return Problem($"{UnassignedOuName} OU eksik. Veritabanı başlatma adımını çalıştırın.");
        }

        var now = DateTimeOffset.UtcNow;

        await using var tx = await _db.Database.BeginTransactionAsync(cancellationToken);

        var device = await _db.Devices
            .FirstOrDefaultAsync(d => d.DeviceId == payload.DeviceId, cancellationToken);

        if (device == null)
        {
            device = new Device
            {
                DeviceId = payload.DeviceId.Trim(),
                OrganizationUnitId = unassignedOuId,
                CreatedAtUtc = now,
                MachineName = payload.MachineName,
                OsVersion = payload.OsVersion,
                AssignedUserDisplayName = payload.LoggedOnUser,
                IpAddress = payload.IpAddress,
                LastSeenUtc = now,
                LastPolicyAppliedStatus = payload.LastPolicyAppliedStatus,
                LastErrorMessage = payload.LastErrorMessage
            };
            _db.Devices.Add(device);
            await _db.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("Yeni cihaz {DeviceId} {Ou} OU altında oluşturuldu.", device.DeviceId, UnassignedOuName);
        }
        else
        {
            device.MachineName = payload.MachineName;
            device.OsVersion = payload.OsVersion;
            if (!string.IsNullOrWhiteSpace(payload.LoggedOnUser))
                device.AssignedUserDisplayName = payload.LoggedOnUser;
            if (!string.IsNullOrWhiteSpace(payload.IpAddress))
                device.IpAddress = payload.IpAddress;
            device.LastSeenUtc = now;
            device.LastPolicyAppliedStatus = payload.LastPolicyAppliedStatus;
            device.LastErrorMessage = payload.LastErrorMessage;
        }

        List<Extension> newRows;
        var skipInventory = !payload.HasChanged && device.Id != 0;

        if (skipInventory && await _db.Extensions.AnyAsync(e => e.DeviceId == device.Id, cancellationToken))
        {
            newRows = await _db.Extensions
                .Where(e => e.DeviceId == device.Id)
                .ToListAsync(cancellationToken);
            _logger.LogInformation("Delta rapor: cihaz {DeviceId} için envanter DB'den korunuyor.", device.DeviceId);
        }
        else
        {
            var existingExtensions = await _db.Extensions
                .Where(e => e.DeviceId == device.Id)
                .ToListAsync(cancellationToken);
            _db.Extensions.RemoveRange(existingExtensions);

            newRows = new List<Extension>();
            foreach (var item in payload.Extensions)
            {
                newRows.Add(new Extension
                {
                    DeviceId = device.Id,
                    ExtensionId = item.ExtensionId ?? string.Empty,
                    ExtensionName = item.ExtensionName ?? string.Empty,
                    ExtensionVersion = item.ExtensionVersion ?? string.Empty,
                    BrowserType = item.BrowserType ?? string.Empty,
                    LastReportedUtc = now
                });
            }

            if (newRows.Count > 0)
                await _db.Extensions.AddRangeAsync(newRows, cancellationToken);
        }

        await _db.SaveChangesAsync(cancellationToken);

        device.RiskScore = _riskScoring.ComputeRiskScore(device, newRows);

        _db.AuditLogs.Add(new AuditLog
        {
            OccurredUtc = now,
            Action = skipInventory ? "DeviceReport (delta)" : "DeviceReport",
            Actor = "EndpointAgent",
            DeviceId = device.Id,
            Details = $"Eklenti: {newRows.Count}, OU: {device.OrganizationUnitId}, HasChanged: {payload.HasChanged}"
        });

        await _db.SaveChangesAsync(cancellationToken);
        await tx.CommitAsync(cancellationToken);

        var policy = _policyEngine.BuildResponseForOrganizationUnit(device.OrganizationUnitId);
        return Ok(policy);
    }

    /// <summary>
    /// Cihazı hedef OU'ya taşır (dashboard).
    /// </summary>
    [HttpPost("move-ou")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> MoveOrganizationUnitAsync(
        [FromBody] MoveDeviceOuRequest body,
        CancellationToken cancellationToken)
    {
        var adminKey = Request.Headers[AdminApiKeyHeaderName].FirstOrDefault();
        if (!_adminApiKeyValidator.IsValid(adminKey))
        {
            _logger.LogWarning("move-ou: geçersiz veya eksik Admin API anahtarı.");
            return Unauthorized();
        }

        if (string.IsNullOrWhiteSpace(body.DeviceId))
            return BadRequest("deviceId gerekli.");

        OrganizationalUnit? targetOu = null;
        if (body.TargetOrganizationUnitId is > 0)
        {
            targetOu = await _db.OrganizationalUnits
                .FirstOrDefaultAsync(o => o.Id == body.TargetOrganizationUnitId.Value, cancellationToken);
        }
        else if (!string.IsNullOrWhiteSpace(body.TargetOrganizationUnitName))
        {
            var name = body.TargetOrganizationUnitName.Trim();
            targetOu = await _db.OrganizationalUnits
                .FirstOrDefaultAsync(o => o.Name == name, cancellationToken);
        }

        if (targetOu == null)
            return NotFound("Hedef OU bulunamadı (ad veya Id kontrol edin).");

        var device = await _db.Devices.FirstOrDefaultAsync(d => d.DeviceId == body.DeviceId.Trim(), cancellationToken);
        if (device == null)
            return NotFound("Cihaz bulunamadı.");

        var oldOuId = device.OrganizationUnitId;
        device.OrganizationUnitId = targetOu.Id;
        await _db.SaveChangesAsync(cancellationToken);

        _db.AuditLogs.Add(new AuditLog
        {
            OccurredUtc = DateTimeOffset.UtcNow,
            Action = "DeviceMoveOu",
            Actor = "Dashboard",
            DeviceId = device.Id,
            Details = $"OU {oldOuId} -> {targetOu.Id} ({targetOu.Name})"
        });
        await _db.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Cihaz {DeviceId} OU taşındı: {OldOu} -> {NewOu}",
            device.DeviceId,
            oldOuId,
            targetOu.Name);

        return NoContent();
    }
}
