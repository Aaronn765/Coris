namespace IncidentsDsi.Api.DTOs;

public sealed class DashboardStatsDto
{
    public int TotalIncidents { get; init; }
    public int IncidentsEnCours { get; init; }
    public int IncidentsClotures { get; init; }
    public IReadOnlyList<DashboardCountDto> IncidentsParCriticite { get; init; } = [];
    public IReadOnlyList<DashboardCountDto> IncidentsParApplication { get; init; } = [];
    public IReadOnlyList<DashboardCountDto> IncidentsParType { get; init; } = [];
    public IReadOnlyList<DashboardEvolutionDto> Evolution { get; init; } = [];
}

public sealed class DashboardCountDto
{
    public int Id { get; init; }
    public string Nom { get; init; } = string.Empty;
    public int Count { get; init; }
}

public sealed class DashboardEvolutionDto
{
    public int Annee { get; init; }
    public int Mois { get; init; }
    public int Count { get; init; }
}
