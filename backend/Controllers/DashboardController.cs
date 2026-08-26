using IncidentsDsi.Api.DTOs;
using IncidentsDsi.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace IncidentsDsi.Api.Controllers;

[ApiController]
[Route("api/dashboard")]
public sealed class DashboardController(IDashboardService dashboard) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<DashboardStatsDto>> Get(
        [FromQuery] DateTime? dateDebut,
        [FromQuery] DateTime? dateFin,
        CancellationToken cancellationToken)
        => Ok(await dashboard.GetAsync(dateDebut, dateFin, cancellationToken));
}
