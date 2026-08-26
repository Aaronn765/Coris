using System.ComponentModel.DataAnnotations;

namespace IncidentsDsi.Api.DTOs;

public sealed class ReferentielDto
{
    public int Id { get; init; }
    public string Nom { get; init; } = string.Empty;
    public bool Actif { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime UpdatedAt { get; init; }
}

public sealed class ReferentielWriteDto
{
    [Required, StringLength(150, MinimumLength = 1)]
    public string Nom { get; init; } = string.Empty;
    public bool Actif { get; init; } = true;
}

public sealed class ReferentielSetDto
{
    public IReadOnlyList<ReferentielDto> Statuts { get; init; } = [];
    public IReadOnlyList<ReferentielDto> Criticites { get; init; } = [];
    public IReadOnlyList<ReferentielDto> Applications { get; init; } = [];
    public IReadOnlyList<ReferentielDto> TypesIncident { get; init; } = [];
    public IReadOnlyList<ReferentielDto> Entites { get; init; } = [];
    public IReadOnlyList<ReferentielDto> Risques { get; init; } = [];
    public IReadOnlyList<ReferentielDto> Responsables { get; init; } = [];
    public IReadOnlyList<ReferentielDto> DomainesProjet { get; init; } = [];
    public IReadOnlyList<ReferentielDto> StatutsProjet { get; init; } = [];
    public IReadOnlyList<ReferentielDto> StatutsEtape { get; init; } = [];
}
