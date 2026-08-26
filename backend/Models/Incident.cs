using System.ComponentModel.DataAnnotations.Schema;

namespace IncidentsDsi.Api.Models;

public sealed class Incident
{
    public int Id { get; set; }
    public string Numero { get; set; } = string.Empty;
    public DateTime DateDeclaration { get; set; }
    public DateTime? DateFin { get; set; }

    public int TypeIncidentId { get; set; }
    public TypeIncident TypeIncident { get; set; } = null!;

    public int ApplicationId { get; set; }
    public Application Application { get; set; } = null!;

    public string Intitule { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;

    public int EntiteId { get; set; }
    public Entite Entite { get; set; } = null!;

    public int CriticiteId { get; set; }
    public Criticite Criticite { get; set; } = null!;

    public string? Impact { get; set; }
    public string? Cause { get; set; }

    public int RisqueId { get; set; }
    public Risque Risque { get; set; } = null!;

    public string? ActionsMenees { get; set; }
    public string? Solution { get; set; }
    public string? ActionsEnCours { get; set; }

    public int ResponsableN1Id { get; set; }
    public Responsable ResponsableN1 { get; set; } = null!;

    public int? ResponsableN2Id { get; set; }
    public Responsable? ResponsableN2 { get; set; }

    public string? MesuresPreventives { get; set; }

    public int StatutId { get; set; }
    public Statut Statut { get; set; } = null!;

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    [NotMapped]
    public int? DureeMinutes => DateFin.HasValue
        ? Math.Max(0, (int)Math.Floor((DateFin.Value - DateDeclaration).TotalMinutes))
        : null;

    [NotMapped]
    public int? DureeJours => DateFin.HasValue
        ? Math.Max(0, (int)Math.Ceiling((DateFin.Value - DateDeclaration).TotalDays))
        : null;
}
