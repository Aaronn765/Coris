using IncidentsDsi.Api.DTOs;
using IncidentsDsi.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace IncidentsDsi.Api.Controllers;

[ApiController]
[Route("api/dashboard/projets")]
public sealed class ProjetDashboardController(IProjetDashboardService dashboard) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<ProjectDashboardStatsDto>> Get(CancellationToken cancellationToken)
        => Ok(await dashboard.GetAsync(cancellationToken));
}
