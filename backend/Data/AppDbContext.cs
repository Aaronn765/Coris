using IncidentsDsi.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace IncidentsDsi.Api.Data;

public sealed class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Incident> Incidents => Set<Incident>();
    public DbSet<IncidentNumberSequence> IncidentNumberSequences => Set<IncidentNumberSequence>();
    public DbSet<Responsable> Responsables => Set<Responsable>();
    public DbSet<Application> Applications => Set<Application>();
    public DbSet<TypeIncident> TypesIncident => Set<TypeIncident>();
    public DbSet<Entite> Entites => Set<Entite>();
    public DbSet<Criticite> Criticites => Set<Criticite>();
    public DbSet<Risque> Risques => Set<Risque>();
    public DbSet<Statut> Statuts => Set<Statut>();
    public DbSet<Projet> Projets => Set<Projet>();
    public DbSet<ProjetNumberSequence> ProjetNumberSequences => Set<ProjetNumberSequence>();
    public DbSet<DomaineProjet> DomainesProjet => Set<DomaineProjet>();
    public DbSet<StatutProjet> StatutsProjet => Set<StatutProjet>();
    public DbSet<StatutEtape> StatutsEtape => Set<StatutEtape>();
    public DbSet<ProjetResponsable> ProjetResponsables => Set<ProjetResponsable>();
    public DbSet<EtapeProjet> EtapesProjet => Set<EtapeProjet>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        ConfigureReferentiel<Responsable>(modelBuilder, "Responsables");
        ConfigureReferentiel<Application>(modelBuilder, "Applications");
        ConfigureReferentiel<TypeIncident>(modelBuilder, "TypesIncident");
        ConfigureReferentiel<Entite>(modelBuilder, "Entites");
        ConfigureReferentiel<Criticite>(modelBuilder, "Criticites");
        ConfigureReferentiel<Risque>(modelBuilder, "Risques");
        ConfigureReferentiel<Statut>(modelBuilder, "Statuts");
        ConfigureReferentiel<DomaineProjet>(modelBuilder, "DomainesProjet");
        ConfigureReferentiel<StatutProjet>(modelBuilder, "StatutsProjet");
        ConfigureReferentiel<StatutEtape>(modelBuilder, "StatutsEtape");

        var incident = modelBuilder.Entity<Incident>();
        incident.ToTable("Incidents");
        incident.HasKey(x => x.Id);
        incident.Property(x => x.Numero).HasMaxLength(20).IsRequired();
        incident.HasIndex(x => x.Numero).IsUnique();
        incident.HasIndex(x => x.DateDeclaration);
        incident.HasIndex(x => new { x.DateDeclaration, x.Id });
        incident.HasIndex(x => new { x.StatutId, x.DateDeclaration });
        incident.HasIndex(x => new { x.ApplicationId, x.DateDeclaration });
        incident.HasIndex(x => new { x.TypeIncidentId, x.DateDeclaration });
        incident.HasIndex(x => new { x.EntiteId, x.DateDeclaration });
        incident.HasIndex(x => new { x.CriticiteId, x.DateDeclaration });
        incident.HasIndex(x => new { x.RisqueId, x.DateDeclaration });
        incident.HasIndex(x => new { x.ResponsableN1Id, x.DateDeclaration });
        incident.HasIndex(x => new { x.ResponsableN2Id, x.DateDeclaration });
        incident.Property(x => x.Intitule).HasMaxLength(300).IsRequired();
        incident.Property(x => x.Description).HasMaxLength(10000).IsRequired();
        incident.Property(x => x.Impact).HasMaxLength(10000);
        incident.Property(x => x.Cause).HasMaxLength(10000);
        incident.Property(x => x.ActionsMenees).HasMaxLength(10000);
        incident.Property(x => x.Solution).HasMaxLength(10000);
        incident.Property(x => x.ActionsEnCours).HasMaxLength(10000);
        incident.Property(x => x.MesuresPreventives).HasMaxLength(10000);

        incident.HasOne(x => x.TypeIncident).WithMany().HasForeignKey(x => x.TypeIncidentId).OnDelete(DeleteBehavior.Restrict);
        incident.HasOne(x => x.Application).WithMany().HasForeignKey(x => x.ApplicationId).OnDelete(DeleteBehavior.Restrict);
        incident.HasOne(x => x.Entite).WithMany().HasForeignKey(x => x.EntiteId).OnDelete(DeleteBehavior.Restrict);
        incident.HasOne(x => x.Criticite).WithMany().HasForeignKey(x => x.CriticiteId).OnDelete(DeleteBehavior.Restrict);
        incident.HasOne(x => x.Risque).WithMany().HasForeignKey(x => x.RisqueId).OnDelete(DeleteBehavior.Restrict);
        incident.HasOne(x => x.ResponsableN1).WithMany().HasForeignKey(x => x.ResponsableN1Id).OnDelete(DeleteBehavior.Restrict);
        incident.HasOne(x => x.ResponsableN2).WithMany().HasForeignKey(x => x.ResponsableN2Id).OnDelete(DeleteBehavior.Restrict);
        incident.HasOne(x => x.Statut).WithMany().HasForeignKey(x => x.StatutId).OnDelete(DeleteBehavior.Restrict);

        var sequence = modelBuilder.Entity<IncidentNumberSequence>();
        sequence.ToTable("IncidentNumberSequences");
        sequence.HasKey(x => x.Year);
        sequence.Property(x => x.Year).ValueGeneratedNever();
        sequence.Property(x => x.LastValue).IsRequired();

        var projectSequence = modelBuilder.Entity<ProjetNumberSequence>();
        projectSequence.ToTable("ProjetNumberSequences");
        projectSequence.HasKey(x => x.Year);
        projectSequence.Property(x => x.Year).ValueGeneratedNever();
        projectSequence.Property(x => x.LastValue).IsRequired();

        var project = modelBuilder.Entity<Projet>();
        project.ToTable("Projets");
        project.HasKey(x => x.Id);
        project.Property(x => x.Numero).HasMaxLength(20).IsRequired();
        project.HasIndex(x => x.Numero).IsUnique();
        project.HasIndex(x => x.DateEcheance);
        project.HasIndex(x => new { x.StatutProjetId, x.DateEcheance });
        project.HasIndex(x => new { x.DomaineProjetId, x.DateEcheance });
        project.Property(x => x.Nom).HasMaxLength(300).IsRequired();
        project.Property(x => x.Description).HasMaxLength(10000);
        project.Property(x => x.TauxAvancement).HasPrecision(5, 4).IsRequired();
        project.Property(x => x.Support).HasMaxLength(5000);
        project.Property(x => x.ActeursMetiers).HasMaxLength(5000);
        project.Property(x => x.Contraintes).HasMaxLength(10000);
        project.Property(x => x.Commentaires).HasMaxLength(10000);
        project.HasOne(x => x.DomaineProjet).WithMany().HasForeignKey(x => x.DomaineProjetId).OnDelete(DeleteBehavior.Restrict);
        project.HasOne(x => x.StatutProjet).WithMany().HasForeignKey(x => x.StatutProjetId).OnDelete(DeleteBehavior.Restrict);

        var projectResponsable = modelBuilder.Entity<ProjetResponsable>();
        projectResponsable.ToTable("ProjetResponsables");
        projectResponsable.HasKey(x => new { x.ProjetId, x.ResponsableId });
        projectResponsable.HasOne(x => x.Projet).WithMany(x => x.ProjetResponsables).HasForeignKey(x => x.ProjetId).OnDelete(DeleteBehavior.Cascade);
        projectResponsable.HasOne(x => x.Responsable).WithMany().HasForeignKey(x => x.ResponsableId).OnDelete(DeleteBehavior.Restrict);

        var step = modelBuilder.Entity<EtapeProjet>();
        step.ToTable("EtapesProjet");
        step.HasKey(x => x.Id);
        step.Property(x => x.Nom).HasMaxLength(10000).IsRequired();
        step.Property(x => x.TauxAvancement).HasPrecision(5, 4).IsRequired();
        step.Property(x => x.Support).HasMaxLength(5000);
        step.Property(x => x.Contraintes).HasMaxLength(10000);
        step.Property(x => x.Commentaires).HasMaxLength(10000);
        step.HasIndex(x => new { x.ProjetId, x.Ordre });
        step.HasOne(x => x.Projet).WithMany(x => x.Etapes).HasForeignKey(x => x.ProjetId).OnDelete(DeleteBehavior.Cascade);
        step.HasOne(x => x.StatutEtape).WithMany().HasForeignKey(x => x.StatutEtapeId).OnDelete(DeleteBehavior.Restrict);

        SeedReferentiels(modelBuilder);
    }

    private static void ConfigureReferentiel<T>(ModelBuilder modelBuilder, string tableName)
        where T : ReferentielBase
    {
        var entity = modelBuilder.Entity<T>();
        entity.ToTable(tableName);
        entity.HasKey(x => x.Id);
        entity.Property(x => x.Nom).HasMaxLength(150).IsRequired();
        entity.HasIndex(x => x.Nom);
        entity.Property(x => x.Actif).IsRequired();
        entity.Property(x => x.CreatedAt).IsRequired();
        entity.Property(x => x.UpdatedAt).IsRequired();
    }

    private static void SeedReferentiels(ModelBuilder modelBuilder)
    {
        var createdAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        modelBuilder.Entity<Responsable>().HasData(
            new Responsable { Id = 1, Nom = "ARSENE", Actif = true, CreatedAt = createdAt, UpdatedAt = createdAt },
            new Responsable { Id = 2, Nom = "ARSERNE", Actif = true, CreatedAt = createdAt, UpdatedAt = createdAt },
            new Responsable { Id = 3, Nom = "DOUASSE", Actif = true, CreatedAt = createdAt, UpdatedAt = createdAt },
            new Responsable { Id = 4, Nom = "EMMANUEL", Actif = true, CreatedAt = createdAt, UpdatedAt = createdAt },
            new Responsable { Id = 5, Nom = "Equipe Exploitation", Actif = true, CreatedAt = createdAt, UpdatedAt = createdAt },
            new Responsable { Id = 6, Nom = "HOLDING", Actif = true, CreatedAt = createdAt, UpdatedAt = createdAt },
            new Responsable { Id = 7, Nom = "KARAMOKO", Actif = true, CreatedAt = createdAt, UpdatedAt = createdAt },
            new Responsable { Id = 8, Nom = "LEGRE", Actif = true, CreatedAt = createdAt, UpdatedAt = createdAt },
            new Responsable { Id = 9, Nom = "MATHURIN", Actif = true, CreatedAt = createdAt, UpdatedAt = createdAt },
            new Responsable { Id = 10, Nom = "NICAISE", Actif = true, CreatedAt = createdAt, UpdatedAt = createdAt },
            new Responsable { Id = 11, Nom = "PRESTATAIRE ELECTRIQUE", Actif = true, CreatedAt = createdAt, UpdatedAt = createdAt },
            new Responsable { Id = 12, Nom = "PRESTATAIRE SMART ACCESS", Actif = true, CreatedAt = createdAt, UpdatedAt = createdAt },
            new Responsable { Id = 13, Nom = "SOMTORE", Actif = true, CreatedAt = createdAt, UpdatedAt = createdAt },
            new Responsable { Id = 14, Nom = "BCEAO", Actif = true, CreatedAt = createdAt, UpdatedAt = createdAt });

        modelBuilder.Entity<Application>().HasData(
            new Application { Id = 1, Nom = "ABM", Actif = true, CreatedAt = createdAt, UpdatedAt = createdAt },
            new Application { Id = 2, Nom = "AMPLITUDE", Actif = true, CreatedAt = createdAt, UpdatedAt = createdAt },
            new Application { Id = 3, Nom = "CARTHAGO", Actif = true, CreatedAt = createdAt, UpdatedAt = createdAt },
            new Application { Id = 4, Nom = "SMARTACCESS", Actif = true, CreatedAt = createdAt, UpdatedAt = createdAt },
            new Application { Id = 5, Nom = "SWIFT", Actif = true, CreatedAt = createdAt, UpdatedAt = createdAt },
            new Application { Id = 6, Nom = "TOUTES LES APPLICATIONS", Actif = true, CreatedAt = createdAt, UpdatedAt = createdAt },
            new Application { Id = 7, Nom = "VNEURON", Actif = true, CreatedAt = createdAt, UpdatedAt = createdAt });

        modelBuilder.Entity<TypeIncident>().HasData(
            new TypeIncident { Id = 1, Nom = "Infrastructure / \u00e9lectrique", Actif = true, CreatedAt = createdAt, UpdatedAt = createdAt },
            new TypeIncident { Id = 2, Nom = "application", Actif = true, CreatedAt = createdAt, UpdatedAt = createdAt },
            new TypeIncident { Id = 3, Nom = "r\u00e9seaux", Actif = true, CreatedAt = createdAt, UpdatedAt = createdAt });

        modelBuilder.Entity<Entite>().HasData(
            new Entite { Id = 1, Nom = "2-PLATEAUX", Actif = true, CreatedAt = createdAt, UpdatedAt = createdAt },
            new Entite { Id = 2, Nom = "ABATTA", Actif = true, CreatedAt = createdAt, UpdatedAt = createdAt },
            new Entite { Id = 3, Nom = "ADJAME", Actif = true, CreatedAt = createdAt, UpdatedAt = createdAt },
            new Entite { Id = 4, Nom = "AGENCE ABATTA", Actif = true, CreatedAt = createdAt, UpdatedAt = createdAt },
            new Entite { Id = 5, Nom = "AGENCE ANGRE", Actif = true, CreatedAt = createdAt, UpdatedAt = createdAt },
            new Entite { Id = 6, Nom = "AGENCE DALOA", Actif = true, CreatedAt = createdAt, UpdatedAt = createdAt },
            new Entite { Id = 7, Nom = "AGENCE KOUMASSI", Actif = true, CreatedAt = createdAt, UpdatedAt = createdAt },
            new Entite { Id = 8, Nom = "ANGRE", Actif = true, CreatedAt = createdAt, UpdatedAt = createdAt },
            new Entite { Id = 9, Nom = "BANQUE PRIVEE", Actif = true, CreatedAt = createdAt, UpdatedAt = createdAt },
            new Entite { Id = 10, Nom = "BARDO", Actif = true, CreatedAt = createdAt, UpdatedAt = createdAt },
            new Entite { Id = 11, Nom = "BOUAKE", Actif = true, CreatedAt = createdAt, UpdatedAt = createdAt },
            new Entite { Id = 12, Nom = "CBICI", Actif = true, CreatedAt = createdAt, UpdatedAt = createdAt },
            new Entite { Id = 13, Nom = "DALOA", Actif = true, CreatedAt = createdAt, UpdatedAt = createdAt },
            new Entite { Id = 14, Nom = "DUEKOUE", Actif = true, CreatedAt = createdAt, UpdatedAt = createdAt },
            new Entite { Id = 15, Nom = "KOUMASSI", Actif = true, CreatedAt = createdAt, UpdatedAt = createdAt },
            new Entite { Id = 16, Nom = "PLATEAU", Actif = true, CreatedAt = createdAt, UpdatedAt = createdAt },
            new Entite { Id = 17, Nom = "SAN PEDRO BARDOT", Actif = true, CreatedAt = createdAt, UpdatedAt = createdAt },
            new Entite { Id = 18, Nom = "SAN PEDRO CITE", Actif = true, CreatedAt = createdAt, UpdatedAt = createdAt },
            new Entite { Id = 19, Nom = "TREICHVILLE", Actif = true, CreatedAt = createdAt, UpdatedAt = createdAt },
            new Entite { Id = 20, Nom = "YOP 3E PONT", Actif = true, CreatedAt = createdAt, UpdatedAt = createdAt });

        modelBuilder.Entity<Criticite>().HasData(
            new Criticite { Id = 1, Nom = "Fort", Actif = true, CreatedAt = createdAt, UpdatedAt = createdAt },
            new Criticite { Id = 2, Nom = "Moyenne", Actif = true, CreatedAt = createdAt, UpdatedAt = createdAt });

        modelBuilder.Entity<Risque>().HasData(
            new Risque { Id = 1, Nom = "Fort", Actif = true, CreatedAt = createdAt, UpdatedAt = createdAt },
            new Risque { Id = 2, Nom = "Moyen", Actif = true, CreatedAt = createdAt, UpdatedAt = createdAt });

        modelBuilder.Entity<Statut>().HasData(
            new Statut { Id = 1, Nom = "En cours", Actif = true, CreatedAt = createdAt, UpdatedAt = createdAt },
            new Statut { Id = 2, Nom = "Cl\u00f4tur\u00e9", Actif = true, CreatedAt = createdAt, UpdatedAt = createdAt });

        modelBuilder.Entity<DomaineProjet>().HasData(
            new DomaineProjet { Id = 1, Nom = "MONETIQUE", Actif = true, CreatedAt = createdAt, UpdatedAt = createdAt },
            new DomaineProjet { Id = 2, Nom = "SECURITE", Actif = true, CreatedAt = createdAt, UpdatedAt = createdAt },
            new DomaineProjet { Id = 3, Nom = "REGLEMENTAIRE", Actif = true, CreatedAt = createdAt, UpdatedAt = createdAt },
            new DomaineProjet { Id = 4, Nom = "INFRA RESEAU & SYSTÈME", Actif = true, CreatedAt = createdAt, UpdatedAt = createdAt },
            new DomaineProjet { Id = 5, Nom = "APPLICATIONS", Actif = true, CreatedAt = createdAt, UpdatedAt = createdAt },
            new DomaineProjet { Id = 6, Nom = "CORE BANKING AMPLITUDE", Actif = true, CreatedAt = createdAt, UpdatedAt = createdAt },
            new DomaineProjet { Id = 7, Nom = "FILE D'ATTENTE CLIENTELE", Actif = true, CreatedAt = createdAt, UpdatedAt = createdAt },
            new DomaineProjet { Id = 8, Nom = "PRODUCTION INFORMATIQUE", Actif = true, CreatedAt = createdAt, UpdatedAt = createdAt },
            new DomaineProjet { Id = 9, Nom = "HELPDESK & PARC INFORMATIQUE", Actif = true, CreatedAt = createdAt, UpdatedAt = createdAt },
            new DomaineProjet { Id = 10, Nom = "ETATS & REPORTING", Actif = true, CreatedAt = createdAt, UpdatedAt = createdAt },
            new DomaineProjet { Id = 11, Nom = "REPORTING", Actif = true, CreatedAt = createdAt, UpdatedAt = createdAt });

        modelBuilder.Entity<StatutProjet>().HasData(
            new StatutProjet { Id = 1, Nom = "Planifié", Actif = true, CreatedAt = createdAt, UpdatedAt = createdAt },
            new StatutProjet { Id = 2, Nom = "En cours", Actif = true, CreatedAt = createdAt, UpdatedAt = createdAt },
            new StatutProjet { Id = 3, Nom = "Clôturé", Actif = true, CreatedAt = createdAt, UpdatedAt = createdAt },
            new StatutProjet { Id = 4, Nom = "Suspendu", Actif = true, CreatedAt = createdAt, UpdatedAt = createdAt });

        modelBuilder.Entity<StatutEtape>().HasData(
            new StatutEtape { Id = 1, Nom = "Non démarrée", Actif = true, CreatedAt = createdAt, UpdatedAt = createdAt },
            new StatutEtape { Id = 2, Nom = "Démarrée", Actif = true, CreatedAt = createdAt, UpdatedAt = createdAt },
            new StatutEtape { Id = 3, Nom = "Terminée", Actif = true, CreatedAt = createdAt, UpdatedAt = createdAt });
    }
}
