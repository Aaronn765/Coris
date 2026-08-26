using IncidentsDsi.Api.DTOs;

namespace IncidentsDsi.Api.Services;

public interface IProjetDashboardService
{
    Task<ProjectDashboardStatsDto> GetAsync(CancellationToken cancellationToken);
}
