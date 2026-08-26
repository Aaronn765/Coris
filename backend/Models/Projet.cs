namespace IncidentsDsi.Api.Models;

public sealed class Projet
{
    public int Id { get; set; }
    public string Numero { get; set; } = string.Empty;

    public int DomaineProjetId { get; set; }
    public DomaineProjet DomaineProjet { get; set; } = null!;

    public string Nom { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal TauxAvancement { get; set; }

    public DateTime? DateDebut { get; set; }
    public DateTime? DateFin { get; set; }
    public DateTime? DateEcheance { get; set; }

    public int StatutProjetId { get; set; }
    public StatutProjet StatutProjet { get; set; } = null!;

    public string? Support { get; set; }
    public string? ActeursMetiers { get; set; }
    public string? Contraintes { get; set; }
    public string? Commentaires { get; set; }

    public ICollection<ProjetResponsable> ProjetResponsables { get; set; } = [];
    public ICollection<EtapeProjet> Etapes { get; set; } = [];

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public sealed class ProjetNumberSequence
{
    public int Year { get; set; }
    public int LastValue { get; set; }
}

public sealed class DomaineProjet : ReferentielBase;
public sealed class StatutProjet : ReferentielBase;
public sealed class StatutEtape : ReferentielBase;

public sealed class ProjetResponsable
{
    public int ProjetId { get; set; }
    public Projet Projet { get; set; } = null!;

    public int ResponsableId { get; set; }
    public Responsable Responsable { get; set; } = null!;
}

public sealed class EtapeProjet
{
    public int Id { get; set; }

    public int ProjetId { get; set; }
    public Projet Projet { get; set; } = null!;

    public string Nom { get; set; } = string.Empty;
    public decimal TauxAvancement { get; set; }

    public int StatutEtapeId { get; set; }
    public StatutEtape StatutEtape { get; set; } = null!;

    public DateTime? DateDebut { get; set; }
    public DateTime? DateFin { get; set; }
    public string? Support { get; set; }
    public string? Contraintes { get; set; }
    public string? Commentaires { get; set; }
    public int Ordre { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
