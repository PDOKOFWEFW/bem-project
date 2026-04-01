using EndpointApi.Models.Dashboard;
using EndpointApi.Services;
using Microsoft.AspNetCore.Mvc;

namespace EndpointApi.Controllers;

/// <summary>
/// Statik dashboard (index.html, devices.html) için özet veri API'si taslağı.
/// </summary>
[ApiController]
[Route("api/dashboard")]
public class DashboardController : ControllerBase
{
    private readonly IDashboardQueryService _dashboard;

    public DashboardController(IDashboardQueryService dashboard)
    {
        _dashboard = dashboard;
    }

    /// <summary>
    /// KPI, grafik serileri, son olaylar ve cihaz tablosu için birleşik yanıt.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(DashboardBundleDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<DashboardBundleDto>> GetAsync(CancellationToken cancellationToken)
    {
        var bundle = await _dashboard.GetBundleAsync(cancellationToken);
        return Ok(bundle);
    }
}
