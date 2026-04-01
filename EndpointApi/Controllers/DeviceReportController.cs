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
    private const string UnassignedOuName = "Unassigned";

    private readonly AppDbContext _db;
    private readonly IPolicyEngine _policyEngine;
    private readonly IRiskScoringService _riskScoring;
    private readonly IEnrollmentTokenValidator _tokenValidator;
    private readonly ILogger<DeviceReportController> _logger;

    public DeviceReportController(
        AppDbContext db,
        IPolicyEngine policyEngine,
        IRiskScoringService riskScoring,
        IEnrollmentTokenValidator tokenValidator,
        ILogger<DeviceReportController> logger)
    {
        _db = db;
        _policyEngine = policyEngine;
        _riskScoring = riskScoring;
        _tokenValidator = tokenValidator;
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

        var existingExtensions = await _db.Extensions
            .Where(e => e.DeviceId == device.Id)
            .ToListAsync(cancellationToken);
        _db.Extensions.RemoveRange(existingExtensions);

        var newRows = new List<Extension>();
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

        await _db.SaveChangesAsync(cancellationToken);

        device.RiskScore = _riskScoring.ComputeRiskScore(device, newRows);

        _db.AuditLogs.Add(new AuditLog
        {
            OccurredUtc = now,
            Action = "DeviceReport",
            Actor = "EndpointAgent",
            DeviceId = device.Id,
            Details = $"Eklenti: {newRows.Count}, OU: {device.OrganizationUnitId}, User: {payload.LoggedOnUser}, IP: {payload.IpAddress}"
        });

        await _db.SaveChangesAsync(cancellationToken);
        await tx.CommitAsync(cancellationToken);

        var policy = _policyEngine.BuildResponseForOrganizationUnit(device.OrganizationUnitId);
        return Ok(policy);
    }
}
