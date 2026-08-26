namespace IncidentsDsi.Api.Models;

public abstract class ReferentielBase
{
    public int Id { get; set; }
    public string Nom { get; set; } = string.Empty;
    public bool Actif { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

public sealed class Responsable : ReferentielBase;
public sealed class Application : ReferentielBase;
public sealed class TypeIncident : ReferentielBase;
public sealed class Entite : ReferentielBase;
public sealed class Criticite : ReferentielBase;
public sealed class Risque : ReferentielBase;
public sealed class Statut : ReferentielBase;
