using IncidentsDsi.Api.DTOs;

namespace IncidentsDsi.Api.Services;

public interface IDashboardService
{
    Task<DashboardStatsDto> GetAsync(DateTime? dateDebut, DateTime? dateFin, CancellationToken cancellationToken);
}
