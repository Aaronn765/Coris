using System.ComponentModel.DataAnnotations;
using IncidentsDsi.Api.Models;

namespace IncidentsDsi.Api.DTOs;

public sealed class IncidentWriteDto : IValidatableObject
{
    [Required]
    public DateTime DateDeclaration { get; init; }

    public DateTime? DateFin { get; init; }

    [Range(1, int.MaxValue)]
    public int TypeIncidentId { get; init; }

    [Range(1, int.MaxValue)]
    public int ApplicationId { get; init; }

    [Required, StringLength(300, MinimumLength = 3)]
    public string Intitule { get; init; } = string.Empty;

    [Required, StringLength(10000, MinimumLength = 10)]
    public string Description { get; init; } = string.Empty;

    [Range(1, int.MaxValue)]
    public int EntiteId { get; init; }

    [Range(1, int.MaxValue)]
    public int CriticiteId { get; init; }

    [StringLength(10000)]
    public string? Impact { get; init; }

    [StringLength(10000)]
    public string? Cause { get; init; }

    [Range(1, int.MaxValue)]
    public int RisqueId { get; init; }

    [StringLength(10000)]
    public string? ActionsMenees { get; init; }

    [StringLength(10000)]
    public string? Solution { get; init; }

    [StringLength(10000)]
    public string? ActionsEnCours { get; init; }

    [Range(1, int.MaxValue)]
    public int ResponsableN1Id { get; init; }

    public int? ResponsableN2Id { get; init; }

    [StringLength(10000)]
    public string? MesuresPreventives { get; init; }

    [Range(1, int.MaxValue)]
    public int StatutId { get; init; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (DateFin.HasValue && DateFin.Value < DateDeclaration)
        {
            yield return new ValidationResult(
                "La date de fin doit être postérieure ou égale à la date de déclaration.",
                [nameof(DateFin)]);
        }
    }
}

public sealed class IncidentDto
{
    public int Id { get; init; }
    public string Numero { get; init; } = string.Empty;
    public DateTime DateDeclaration { get; init; }
    public DateTime? DateFin { get; init; }
    public int? DureeMinutes { get; init; }
    public int? DureeJours { get; init; }
    public int TypeIncidentId { get; init; }
    public ReferentielDto TypeIncident { get; init; } = null!;
    public int ApplicationId { get; init; }
    public ReferentielDto Application { get; init; } = null!;
    public string Intitule { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public int EntiteId { get; init; }
    public ReferentielDto Entite { get; init; } = null!;
    public int CriticiteId { get; init; }
    public ReferentielDto Criticite { get; init; } = null!;
    public string? Impact { get; init; }
    public string? Cause { get; init; }
    public int RisqueId { get; init; }
    public ReferentielDto Risque { get; init; } = null!;
    public string? ActionsMenees { get; init; }
    public string? Solution { get; init; }
    public string? ActionsEnCours { get; init; }
    public int ResponsableN1Id { get; init; }
    public ReferentielDto ResponsableN1 { get; init; } = null!;
    public int? ResponsableN2Id { get; init; }
    public ReferentielDto? ResponsableN2 { get; init; }
    public string? MesuresPreventives { get; init; }
    public int StatutId { get; init; }
    public ReferentielDto Statut { get; init; } = null!;
    public DateTime CreatedAt { get; init; }
    public DateTime UpdatedAt { get; init; }
}

public sealed class IncidentQueryParameters
{
    public string? Search { get; set; }
    public DateTime? DateDebut { get; set; }
    public DateTime? DateFin { get; set; }
    public int? StatutId { get; set; }
    public int? TypeIncidentId { get; set; }
    public int? ApplicationId { get; set; }
    public int? EntiteId { get; set; }
    public int? CriticiteId { get; set; }
    public int? RisqueId { get; set; }
    public int? ResponsableId { get; set; }
    public int? DureeMinMinutes { get; set; }
    public int? DureeMaxMinutes { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
    public string SortBy { get; set; } = "dateDeclaration";
    public string SortDirection { get; set; } = "desc";

    public void Normalize()
    {
        Page = Math.Max(1, Page);
        PageSize = Math.Clamp(PageSize, 1, 100);
        DureeMinMinutes = DureeMinMinutes.HasValue ? Math.Max(0, DureeMinMinutes.Value) : null;
        DureeMaxMinutes = DureeMaxMinutes.HasValue ? Math.Max(0, DureeMaxMinutes.Value) : null;
        SortBy = string.IsNullOrWhiteSpace(SortBy) ? "dateDeclaration" : SortBy.Trim();
        SortDirection = SortDirection.Equals("asc", StringComparison.OrdinalIgnoreCase) ? "asc" : "desc";
    }

    public bool HasInvalidDateRange => DateDebut.HasValue
        && DateFin.HasValue
        && DateDebut.Value.Date > DateFin.Value.Date;
}
