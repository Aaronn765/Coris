using IncidentsDsi.Api.Data;
using IncidentsDsi.Api.DTOs;
using Microsoft.EntityFrameworkCore;

namespace IncidentsDsi.Api.Services;

public sealed class DashboardService(AppDbContext db) : IDashboardService
{
    public async Task<DashboardStatsDto> GetAsync(
        DateTime? dateDebut,
        DateTime? dateFin,
        CancellationToken cancellationToken)
    {
        var query = db.Incidents.AsNoTracking();

        if (dateDebut.HasValue)
            query = query.Where(x => x.DateDeclaration >= dateDebut.Value.Date);
        if (dateFin.HasValue)
            query = query.Where(x => x.DateDeclaration < dateFin.Value.Date.AddDays(1));

        var total = await query.CountAsync(cancellationToken);
        var statusCounts = await query
            .GroupBy(x => x.Statut.Nom)
            .Select(x => new { Nom = x.Key, Count = x.Count() })
            .ToListAsync(cancellationToken);

        var closed = statusCounts
            .Where(x => IsClosed(x.Nom))
            .Sum(x => x.Count);

        var byCriticite = await query
            .GroupBy(x => new { x.CriticiteId, x.Criticite.Nom })
            .Select(x => new DashboardCountDto { Id = x.Key.CriticiteId, Nom = x.Key.Nom, Count = x.Count() })
            .OrderByDescending(x => x.Count)
            .ThenBy(x => x.Nom)
            .ToListAsync(cancellationToken);

        var byApplication = await query
            .GroupBy(x => new { x.ApplicationId, x.Application.Nom })
            .Select(x => new DashboardCountDto { Id = x.Key.ApplicationId, Nom = x.Key.Nom, Count = x.Count() })
            .OrderByDescending(x => x.Count)
            .ThenBy(x => x.Nom)
            .ToListAsync(cancellationToken);

        var byType = await query
            .GroupBy(x => new { x.TypeIncidentId, x.TypeIncident.Nom })
            .Select(x => new DashboardCountDto { Id = x.Key.TypeIncidentId, Nom = x.Key.Nom, Count = x.Count() })
            .OrderByDescending(x => x.Count)
            .ThenBy(x => x.Nom)
            .ToListAsync(cancellationToken);

        var evolution = await query
            .GroupBy(x => new { Annee = x.DateDeclaration.Year, Mois = x.DateDeclaration.Month })
            .Select(x => new DashboardEvolutionDto { Annee = x.Key.Annee, Mois = x.Key.Mois, Count = x.Count() })
            .OrderBy(x => x.Annee)
            .ThenBy(x => x.Mois)
            .ToListAsync(cancellationToken);

        return new DashboardStatsDto
        {
            TotalIncidents = total,
            IncidentsEnCours = Math.Max(0, total - closed),
            IncidentsClotures = closed,
            IncidentsParCriticite = byCriticite,
            IncidentsParApplication = byApplication,
            IncidentsParType = byType,
            Evolution = evolution
        };
    }

    private static bool IsClosed(string name) =>
        name.Equals("résolu", StringComparison.OrdinalIgnoreCase) ||
        name.Equals("resolu", StringComparison.OrdinalIgnoreCase) ||
        name.Equals("clôturé", StringComparison.OrdinalIgnoreCase) ||
        name.Equals("cloturé", StringComparison.OrdinalIgnoreCase) ||
        name.Equals("cloture", StringComparison.OrdinalIgnoreCase) ||
        name.Equals("ferme", StringComparison.OrdinalIgnoreCase) ||
        name.Equals("FermÃ©", StringComparison.OrdinalIgnoreCase) ||
        name.Equals("Fermé", StringComparison.OrdinalIgnoreCase);
}
