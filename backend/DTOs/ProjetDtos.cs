using System.ComponentModel.DataAnnotations;
using IncidentsDsi.Api.Models;

namespace IncidentsDsi.Api.DTOs;

public sealed class ProjetWriteDto : IValidatableObject
{
    [Range(1, int.MaxValue)]
    public int DomaineProjetId { get; init; }

    [Required, StringLength(300, MinimumLength = 2)]
    public string Nom { get; init; } = string.Empty;

    [StringLength(10000)]
    public string? Description { get; init; }

    [Range(typeof(decimal), "0", "1")]
    public decimal TauxAvancement { get; init; }

    public DateTime? DateDebut { get; init; }
    public DateTime? DateFin { get; init; }
    public DateTime? DateEcheance { get; init; }

    [Range(1, int.MaxValue)]
    public int StatutProjetId { get; init; }

    [StringLength(5000)]
    public string? Support { get; init; }

    [StringLength(5000)]
    public string? ActeursMetiers { get; init; }

    [StringLength(10000)]
    public string? Contraintes { get; init; }

    [StringLength(10000)]
    public string? Commentaires { get; init; }

    public IReadOnlyList<int> ResponsableIds { get; init; } = [];
    public IReadOnlyList<EtapeProjetWriteDto> Etapes { get; init; } = [];

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (DateDebut.HasValue && DateFin.HasValue && DateFin.Value < DateDebut.Value)
        {
            yield return new ValidationResult(
                "La date de fin doit être postérieure ou égale à la date de début.",
                [nameof(DateFin)]);
        }

        if (DateEcheance.HasValue && DateDebut.HasValue && DateEcheance.Value < DateDebut.Value)
        {
            yield return new ValidationResult(
                "La date d'échéance doit être postérieure ou égale à la date de début.",
                [nameof(DateEcheance)]);
        }

        if (ResponsableIds.Any(id => id < 1))
        {
            yield return new ValidationResult("La liste des responsables contient une valeur invalide.", [nameof(ResponsableIds)]);
        }

        if (Etapes.Any(step => string.IsNullOrWhiteSpace(step.Nom)))
        {
            yield return new ValidationResult("Chaque étape doit avoir un nom.", [nameof(Etapes)]);
        }
    }
}

public sealed class EtapeProjetWriteDto : IValidatableObject
{
    public int? Id { get; init; }

    [Required, StringLength(10000, MinimumLength = 1)]
    public string Nom { get; init; } = string.Empty;

    [Range(typeof(decimal), "0", "1")]
    public decimal TauxAvancement { get; init; }

    [Range(1, int.MaxValue)]
    public int StatutEtapeId { get; init; }

    public DateTime? DateDebut { get; init; }
    public DateTime? DateFin { get; init; }

    [StringLength(5000)]
    public string? Support { get; init; }

    [StringLength(10000)]
    public string? Contraintes { get; init; }

    [StringLength(10000)]
    public string? Commentaires { get; init; }

    public int Ordre { get; init; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (DateDebut.HasValue && DateFin.HasValue && DateFin.Value < DateDebut.Value)
        {
            yield return new ValidationResult(
                "La date de fin de l'étape doit être postérieure ou égale à sa date de début.",
                [nameof(DateFin)]);
        }
    }
}

public sealed class ProjetDto
{
    public int Id { get; init; }
    public string Numero { get; init; } = string.Empty;
    public int DomaineProjetId { get; init; }
    public ReferentielDto DomaineProjet { get; init; } = null!;
    public string Nom { get; init; } = string.Empty;
    public string? Description { get; init; }
    public decimal TauxAvancement { get; init; }
    public DateTime? DateDebut { get; init; }
    public DateTime? DateFin { get; init; }
    public DateTime? DateEcheance { get; init; }
    public int StatutProjetId { get; init; }
    public ReferentielDto StatutProjet { get; init; } = null!;
    public string? Support { get; init; }
    public string? ActeursMetiers { get; init; }
    public string? Contraintes { get; init; }
    public string? Commentaires { get; init; }
    public IReadOnlyList<ReferentielDto> Responsables { get; init; } = [];
    public IReadOnlyList<EtapeProjetDto> Etapes { get; init; } = [];
    public DateTime CreatedAt { get; init; }
    public DateTime UpdatedAt { get; init; }
}

public sealed class EtapeProjetDto
{
    public int Id { get; init; }
    public string Nom { get; init; } = string.Empty;
    public decimal TauxAvancement { get; init; }
    public int StatutEtapeId { get; init; }
    public ReferentielDto StatutEtape { get; init; } = null!;
    public DateTime? DateDebut { get; init; }
    public DateTime? DateFin { get; init; }
    public string? Support { get; init; }
    public string? Contraintes { get; init; }
    public string? Commentaires { get; init; }
    public int Ordre { get; init; }
}

public sealed class ProjetQueryParameters
{
    public string? Search { get; set; }
    public int? DomaineProjetId { get; set; }
    public int? StatutProjetId { get; set; }
    public int? ResponsableId { get; set; }
    public DateTime? DateEcheanceAvant { get; set; }
    public DateTime? DateEcheanceApres { get; set; }
    public decimal? TauxMin { get; set; }
    public decimal? TauxMax { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
    public string SortBy { get; set; } = "dateEcheance";
    public string SortDirection { get; set; } = "asc";

    public void Normalize()
    {
        Page = Math.Max(1, Page);
        PageSize = Math.Clamp(PageSize, 1, 100);
        TauxMin = TauxMin.HasValue ? Math.Clamp(TauxMin.Value, 0, 1) : null;
        TauxMax = TauxMax.HasValue ? Math.Clamp(TauxMax.Value, 0, 1) : null;
        SortBy = string.IsNullOrWhiteSpace(SortBy) ? "dateEcheance" : SortBy.Trim();
        SortDirection = SortDirection.Equals("desc", StringComparison.OrdinalIgnoreCase) ? "desc" : "asc";
    }

    public bool HasInvalidDateRange => DateEcheanceApres.HasValue
        && DateEcheanceAvant.HasValue
        && DateEcheanceApres.Value.Date > DateEcheanceAvant.Value.Date;
}

public sealed class ProjectDashboardStatsDto
{
    public int TotalProjets { get; init; }
    public int ProjetsEnCours { get; init; }
    public int ProjetsPlanifies { get; init; }
    public int ProjetsClotures { get; init; }
    public decimal TauxMoyen { get; init; }
    public IReadOnlyList<DashboardCountDto> ProjetsParDomaine { get; init; } = [];
    public IReadOnlyList<DashboardCountDto> ProjetsParStatut { get; init; } = [];
}

