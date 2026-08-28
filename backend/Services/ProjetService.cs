using System.Data;
using IncidentsDsi.Api.Common;
using IncidentsDsi.Api.Data;
using IncidentsDsi.Api.DTOs;
using IncidentsDsi.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace IncidentsDsi.Api.Services;

public sealed class ProjetService(AppDbContext db) : IProjetService
{
    public async Task<PagedResult<ProjetDto>> GetPagedAsync(
        ProjetQueryParameters parameters,
        CancellationToken cancellationToken)
    {
        parameters.Normalize();
        ValidateQueryParameters(parameters);

        var query = ApplyFilters(db.Projets.AsNoTracking(), parameters);
        var totalCount = await query.CountAsync(cancellationToken);
        var projects = await IncludeDetails(ApplyOrdering(query, parameters))
            .Skip((parameters.Page - 1) * parameters.PageSize)
            .Take(parameters.PageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<ProjetDto>
        {
            Items = projects.Select(ToDto).ToList(),
            Page = parameters.Page,
            PageSize = parameters.PageSize,
            TotalCount = totalCount,
            TotalPages = (int)Math.Ceiling(totalCount / (double)parameters.PageSize)
        };
    }

    public async Task<ProjetDto?> GetByIdAsync(int id, CancellationToken cancellationToken)
    {
        var project = await IncludeDetails(db.Projets.AsNoTracking())
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        return project is null ? null : ToDto(project);
    }

    public async Task<IReadOnlyList<ProjetDto>> GetForExportAsync(
        ProjetQueryParameters parameters,
        CancellationToken cancellationToken)
    {
        parameters.Normalize();
        ValidateQueryParameters(parameters);

        var query = ApplyFilters(db.Projets.AsNoTracking(), parameters);
        var projects = await IncludeDetails(ApplyOrdering(query, parameters))
            .ToListAsync(cancellationToken);

        return projects.Select(ToDto).ToList();
    }

    public async Task<ProjetDto> CreateAsync(ProjetWriteDto input, CancellationToken cancellationToken)
    {
        ValidateDates(input);
        await EnsureReferencesExistAsync(input, cancellationToken);
        var statusId = await NormalizeStatusIdAsync(input, cancellationToken);

        await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);
        var year = (input.DateDebut ?? input.DateEcheance ?? DateTime.UtcNow).Year;
        var sequence = await db.ProjetNumberSequences.SingleOrDefaultAsync(x => x.Year == year, cancellationToken);

        if (sequence is null)
        {
            sequence = new ProjetNumberSequence { Year = year, LastValue = 1 };
            db.ProjetNumberSequences.Add(sequence);
        }
        else
        {
            sequence.LastValue++;
        }

        var project = ToEntity(input, statusId);
        project.Numero = $"PRJ-{year:D4}-{sequence.LastValue:D4}";
        db.Projets.Add(project);

        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return (await GetByIdAsync(project.Id, cancellationToken))!;
    }

    public async Task<ProjetDto?> UpdateAsync(int id, ProjetWriteDto input, CancellationToken cancellationToken)
    {
        ValidateDates(input);
        await EnsureReferencesExistAsync(input, cancellationToken);
        var statusId = await NormalizeStatusIdAsync(input, cancellationToken);

        var project = await db.Projets
            .Include(x => x.ProjetResponsables)
            .Include(x => x.Etapes)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

        if (project is null) return null;

        UpdateEntity(project, input, statusId);
        db.ProjetResponsables.RemoveRange(project.ProjetResponsables);
        db.EtapesProjet.RemoveRange(project.Etapes);
        AddResponsibilitiesAndSteps(project, input);
        project.UpdatedAt = DateTime.UtcNow;

        await db.SaveChangesAsync(cancellationToken);
        return await GetByIdAsync(id, cancellationToken);
    }

    public async Task NormalizeStartedStatusesAsync(CancellationToken cancellationToken)
    {
        var statuses = await db.StatutsProjet
            .AsNoTracking()
            .Where(x => x.Actif)
            .ToListAsync(cancellationToken);
        var planned = statuses.FirstOrDefault(x => IsPlannedStatus(x.Nom));
        var inProgress = statuses.FirstOrDefault(x => IsInProgressStatus(x.Nom));

        if (planned is null || inProgress is null || planned.Id == inProgress.Id)
            return;

        var projects = await db.Projets
            .Where(x => x.StatutProjetId == planned.Id)
            .Include(x => x.Etapes)
                .ThenInclude(x => x.StatutEtape)
            .ToListAsync(cancellationToken);

        var today = DateTime.UtcNow.Date;
        var changed = false;
        foreach (var project in projects)
        {
            if (!HasStarted(project, today)) continue;
            project.StatutProjetId = inProgress.Id;
            project.UpdatedAt = DateTime.UtcNow;
            changed = true;
        }

        if (changed)
            await db.SaveChangesAsync(cancellationToken);
    }

    private IQueryable<Projet> ApplyFilters(IQueryable<Projet> query, ProjetQueryParameters parameters)
    {
        if (!string.IsNullOrWhiteSpace(parameters.Search))
        {
            var search = EscapeLikePattern(parameters.Search.Trim());
            var pattern = $"%{search}%";
            query = query.Where(x =>
                EF.Functions.Like(x.Numero, pattern) ||
                EF.Functions.Like(x.Nom, pattern) ||
                EF.Functions.Like(x.Description ?? string.Empty, pattern) ||
                EF.Functions.Like(x.DomaineProjet.Nom, pattern) ||
                EF.Functions.Like(x.Support ?? string.Empty, pattern) ||
                EF.Functions.Like(x.ActeursMetiers ?? string.Empty, pattern) ||
                EF.Functions.Like(x.Contraintes ?? string.Empty, pattern) ||
                EF.Functions.Like(x.Commentaires ?? string.Empty, pattern) ||
                x.ProjetResponsables.Any(r => EF.Functions.Like(r.Responsable.Nom, pattern)) ||
                x.Etapes.Any(step => EF.Functions.Like(step.Nom, pattern)));
        }

        if (parameters.DomaineProjetId.HasValue)
            query = query.Where(x => x.DomaineProjetId == parameters.DomaineProjetId.Value);
        if (parameters.StatutProjetId.HasValue)
            query = query.Where(x => x.StatutProjetId == parameters.StatutProjetId.Value);
        if (parameters.ResponsableId.HasValue)
            query = query.Where(x => x.ProjetResponsables.Any(r => r.ResponsableId == parameters.ResponsableId.Value));
        if (parameters.DateEcheanceApres.HasValue)
            query = query.Where(x => x.DateEcheance.HasValue && x.DateEcheance.Value >= parameters.DateEcheanceApres.Value.Date);
        if (parameters.DateEcheanceAvant.HasValue)
            query = query.Where(x => x.DateEcheance.HasValue && x.DateEcheance.Value < parameters.DateEcheanceAvant.Value.Date.AddDays(1));
        if (parameters.TauxMin.HasValue)
            query = query.Where(x => x.TauxAvancement >= parameters.TauxMin.Value);
        if (parameters.TauxMax.HasValue)
            query = query.Where(x => x.TauxAvancement <= parameters.TauxMax.Value);

        return query;
    }

    private static IOrderedQueryable<Projet> ApplyOrdering(
        IQueryable<Projet> query,
        ProjetQueryParameters parameters)
    {
        var descending = parameters.SortDirection.Equals("desc", StringComparison.OrdinalIgnoreCase);
        return (parameters.SortBy.ToLowerInvariant(), descending) switch
        {
            ("numero", false) => query.OrderBy(x => x.Numero).ThenBy(x => x.Id),
            ("numero", true) => query.OrderByDescending(x => x.Numero).ThenByDescending(x => x.Id),
            ("nom", false) => query.OrderBy(x => x.Nom).ThenBy(x => x.Id),
            ("nom", true) => query.OrderByDescending(x => x.Nom).ThenByDescending(x => x.Id),
            ("domaine", false) => query.OrderBy(x => x.DomaineProjet.Nom).ThenBy(x => x.Nom),
            ("domaine", true) => query.OrderByDescending(x => x.DomaineProjet.Nom).ThenByDescending(x => x.Nom),
            ("statut", false) => query.OrderBy(x => x.StatutProjet.Nom).ThenBy(x => x.Nom),
            ("statut", true) => query.OrderByDescending(x => x.StatutProjet.Nom).ThenByDescending(x => x.Nom),
            ("taux", false) => query.OrderBy(x => x.TauxAvancement).ThenBy(x => x.Nom),
            ("taux", true) => query.OrderByDescending(x => x.TauxAvancement).ThenByDescending(x => x.Nom),
            ("dateecheance", true) => query.OrderByDescending(x => x.DateEcheance).ThenByDescending(x => x.Id),
            ("createdat", false) => query.OrderBy(x => x.CreatedAt).ThenBy(x => x.Id),
            ("createdat", true) => query.OrderByDescending(x => x.CreatedAt).ThenByDescending(x => x.Id),
            ("dateecheance", false) => query.OrderBy(x => x.DateEcheance).ThenBy(x => x.Id),
            _ => query.OrderBy(x => x.DateEcheance).ThenBy(x => x.Id)
        };
    }

    private static IQueryable<Projet> IncludeDetails(IQueryable<Projet> query) => query
        .Include(x => x.DomaineProjet)
        .Include(x => x.StatutProjet)
        .Include(x => x.ProjetResponsables)
            .ThenInclude(x => x.Responsable)
        .Include(x => x.Etapes)
            .ThenInclude(x => x.StatutEtape);

    private async Task EnsureReferencesExistAsync(ProjetWriteDto input, CancellationToken cancellationToken)
    {
        if (!await db.DomainesProjet.AnyAsync(x => x.Id == input.DomaineProjetId && x.Actif, cancellationToken))
            throw new ValidationException("Le domaine du projet sélectionné est introuvable.");
        if (!await db.StatutsProjet.AnyAsync(x => x.Id == input.StatutProjetId && x.Actif, cancellationToken))
            throw new ValidationException("Le statut du projet sélectionné est introuvable.");

        var responsibleIds = (input.ResponsableIds ?? []).Distinct().ToList();
        var responsibleCount = await db.Responsables.CountAsync(x => responsibleIds.Contains(x.Id) && x.Actif, cancellationToken);
        if (responsibleCount != responsibleIds.Count)
            throw new ValidationException("Un responsable sélectionné est introuvable ou inactif.");

        var stepStatusIds = (input.Etapes ?? []).Select(x => x.StatutEtapeId).Distinct().ToList();
        var stepStatusCount = await db.StatutsEtape.CountAsync(x => stepStatusIds.Contains(x.Id) && x.Actif, cancellationToken);
        if (stepStatusCount != stepStatusIds.Count)
            throw new ValidationException("Le statut d'une étape sélectionnée est introuvable ou inactif.");
    }

    private static Projet ToEntity(ProjetWriteDto input, int statusId)
    {
        var project = new Projet
        {
            DomaineProjetId = input.DomaineProjetId,
            Nom = input.Nom.Trim(),
            Description = CleanOptional(input.Description),
            TauxAvancement = input.TauxAvancement,
            DateDebut = input.DateDebut,
            DateFin = input.DateFin,
            DateEcheance = input.DateEcheance,
            StatutProjetId = statusId,
            Support = CleanOptional(input.Support),
            ActeursMetiers = CleanOptional(input.ActeursMetiers),
            Contraintes = CleanOptional(input.Contraintes),
            Commentaires = CleanOptional(input.Commentaires),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        AddResponsibilitiesAndSteps(project, input);
        return project;
    }

    private static void UpdateEntity(Projet project, ProjetWriteDto input, int statusId)
    {
        project.DomaineProjetId = input.DomaineProjetId;
        project.Nom = input.Nom.Trim();
        project.Description = CleanOptional(input.Description);
        project.TauxAvancement = input.TauxAvancement;
        project.DateDebut = input.DateDebut;
        project.DateFin = input.DateFin;
        project.DateEcheance = input.DateEcheance;
        project.StatutProjetId = statusId;
        project.Support = CleanOptional(input.Support);
        project.ActeursMetiers = CleanOptional(input.ActeursMetiers);
        project.Contraintes = CleanOptional(input.Contraintes);
        project.Commentaires = CleanOptional(input.Commentaires);
    }

    private static void AddResponsibilitiesAndSteps(Projet project, ProjetWriteDto input)
    {
        foreach (var responsibleId in (input.ResponsableIds ?? []).Distinct())
            project.ProjetResponsables.Add(new ProjetResponsable { ResponsableId = responsibleId });

        var order = 0;
        foreach (var item in input.Etapes ?? [])
        {
            project.Etapes.Add(new EtapeProjet
            {
                Nom = item.Nom.Trim(),
                TauxAvancement = item.TauxAvancement,
                StatutEtapeId = item.StatutEtapeId,
                DateDebut = item.DateDebut,
                DateFin = item.DateFin,
                Support = CleanOptional(item.Support),
                Contraintes = CleanOptional(item.Contraintes),
                Commentaires = CleanOptional(item.Commentaires),
                Ordre = order++,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            });
        }
    }

    private static void ValidateDates(ProjetWriteDto input)
    {
        if (input.DateDebut.HasValue && input.DateFin.HasValue && input.DateFin.Value < input.DateDebut.Value)
            throw new ValidationException("La date de fin doit être postérieure ou égale à la date de début.");
        if (input.DateEcheance.HasValue && input.DateDebut.HasValue && input.DateEcheance.Value < input.DateDebut.Value)
            throw new ValidationException("La date d'échéance doit être postérieure ou égale à la date de début.");
        if ((input.Etapes ?? []).Any(x => x.DateDebut.HasValue && x.DateFin.HasValue && x.DateFin.Value < x.DateDebut.Value))
            throw new ValidationException("La date de fin d'une étape doit être postérieure ou égale à sa date de début.");
    }

    private static void ValidateQueryParameters(ProjetQueryParameters parameters)
    {
        if (parameters.HasInvalidDateRange)
            throw new ValidationException("La date d'échéance de début doit être antérieure ou égale à la date d'échéance de fin.");
        if (parameters.TauxMin.HasValue && parameters.TauxMax.HasValue && parameters.TauxMin.Value > parameters.TauxMax.Value)
            throw new ValidationException("Le taux minimal doit être inférieur ou égal au taux maximal.");
    }

    private async Task<int> NormalizeStatusIdAsync(ProjetWriteDto input, CancellationToken cancellationToken)
    {
        var selectedStatus = await db.StatutsProjet
            .AsNoTracking()
            .FirstAsync(x => x.Id == input.StatutProjetId, cancellationToken);

        if (!IsPlannedStatus(selectedStatus.Nom) || !await HasStartedAsync(input, cancellationToken))
            return selectedStatus.Id;

        var inProgressId = (await db.StatutsProjet
            .AsNoTracking()
            .Where(x => x.Actif)
            .ToListAsync(cancellationToken))
            .FirstOrDefault(x => IsInProgressStatus(x.Nom))?.Id ?? 0;

        return inProgressId > 0 ? inProgressId : selectedStatus.Id;
    }

    private async Task<bool> HasStartedAsync(ProjetWriteDto input, CancellationToken cancellationToken)
    {
        var today = DateTime.UtcNow.Date;
        if (input.TauxAvancement > 0 || HasStartedDate(input.DateDebut, today))
            return true;

        var statusIds = (input.Etapes ?? []).Select(x => x.StatutEtapeId).Distinct().ToList();
        var statuses = await db.StatutsEtape
            .AsNoTracking()
            .Where(x => statusIds.Contains(x.Id))
            .ToDictionaryAsync(x => x.Id, x => x.Nom, cancellationToken);

        return (input.Etapes ?? []).Any(step =>
            step.TauxAvancement > 0 ||
            HasStartedDate(step.DateDebut, today) ||
            (statuses.TryGetValue(step.StatutEtapeId, out var name) && IsStartedStepStatus(name)));
    }

    private static bool HasStarted(Projet project, DateTime today) =>
        project.TauxAvancement > 0 ||
        HasStartedDate(project.DateDebut, today) ||
        project.Etapes.Any(step =>
            step.TauxAvancement > 0 ||
            HasStartedDate(step.DateDebut, today) ||
            IsStartedStepStatus(step.StatutEtape.Nom));

    private static bool HasStartedDate(DateTime? date, DateTime today) => date.HasValue && date.Value.Date <= today;

    private static bool IsStartedStepStatus(string? value)
    {
        var normalized = Normalize(value);
        return !string.IsNullOrWhiteSpace(normalized)
            && !normalized.Contains("non")
            && !normalized.Contains("planif")
            && !normalized.Contains("a faire");
    }

    private static bool IsPlannedStatus(string? value) => Normalize(value).Contains("planif");

    private static bool IsInProgressStatus(string? value) => Normalize(value).Contains("en cours");

    private static ProjetDto ToDto(Projet project) => new()
    {
        Id = project.Id,
        Numero = project.Numero,
        DomaineProjetId = project.DomaineProjetId,
        DomaineProjet = ToReferenceDto(project.DomaineProjet),
        Nom = project.Nom,
        Description = project.Description,
        TauxAvancement = project.TauxAvancement,
        DateDebut = project.DateDebut,
        DateFin = project.DateFin,
        DateEcheance = project.DateEcheance,
        StatutProjetId = project.StatutProjetId,
        StatutProjet = ToReferenceDto(project.StatutProjet),
        Support = project.Support,
        ActeursMetiers = project.ActeursMetiers,
        Contraintes = project.Contraintes,
        Commentaires = project.Commentaires,
        Responsables = project.ProjetResponsables
            .OrderBy(x => x.Responsable.Nom)
            .Select(x => ToReferenceDto(x.Responsable))
            .ToList(),
        Etapes = project.Etapes
            .OrderBy(x => x.Ordre)
            .Select(x => new EtapeProjetDto
            {
                Id = x.Id,
                Nom = x.Nom,
                TauxAvancement = x.TauxAvancement,
                StatutEtapeId = x.StatutEtapeId,
                StatutEtape = ToReferenceDto(x.StatutEtape),
                DateDebut = x.DateDebut,
                DateFin = x.DateFin,
                Support = x.Support,
                Contraintes = x.Contraintes,
                Commentaires = x.Commentaires,
                Ordre = x.Ordre
            })
            .ToList(),
        CreatedAt = project.CreatedAt,
        UpdatedAt = project.UpdatedAt
    };

    private static ReferentielDto ToReferenceDto(ReferentielBase reference) => new()
    {
        Id = reference.Id,
        Nom = reference.Nom,
        Actif = reference.Actif,
        CreatedAt = reference.CreatedAt,
        UpdatedAt = reference.UpdatedAt
    };

    private static string? CleanOptional(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static string Normalize(string? value) => string.IsNullOrWhiteSpace(value)
        ? string.Empty
        : new string(value.Trim().Normalize(System.Text.NormalizationForm.FormD)
            .Where(ch => System.Globalization.CharUnicodeInfo.GetUnicodeCategory(ch) != System.Globalization.UnicodeCategory.NonSpacingMark)
            .Select(char.ToLowerInvariant)
            .ToArray());

    private static string EscapeLikePattern(string value) => value
        .Replace("[", "[[]", StringComparison.Ordinal)
        .Replace("%", "[%]", StringComparison.Ordinal)
        .Replace("_", "[_]", StringComparison.Ordinal);
}

public sealed class ProjetDashboardService(AppDbContext db) : IProjetDashboardService
{
    public async Task<ProjectDashboardStatsDto> GetAsync(CancellationToken cancellationToken)
    {
        var query = db.Projets.AsNoTracking();
        var total = await query.CountAsync(cancellationToken);
        var byStatus = await query
            .GroupBy(x => new { x.StatutProjetId, x.StatutProjet.Nom })
            .Select(x => new DashboardCountDto { Id = x.Key.StatutProjetId, Nom = x.Key.Nom, Count = x.Count() })
            .OrderByDescending(x => x.Count)
            .ThenBy(x => x.Nom)
            .ToListAsync(cancellationToken);
        var byDomain = await query
            .GroupBy(x => new { x.DomaineProjetId, x.DomaineProjet.Nom })
            .Select(x => new DashboardCountDto { Id = x.Key.DomaineProjetId, Nom = x.Key.Nom, Count = x.Count() })
            .OrderByDescending(x => x.Count)
            .ThenBy(x => x.Nom)
            .ToListAsync(cancellationToken);
        var average = total == 0 ? 0 : await query.AverageAsync(x => x.TauxAvancement, cancellationToken);

        return new ProjectDashboardStatsDto
        {
            TotalProjets = total,
            ProjetsEnCours = CountStatus(byStatus, "en cours"),
            ProjetsPlanifies = CountStatus(byStatus, "planifie"),
            ProjetsClotures = CountStatus(byStatus, "cloture"),
            TauxMoyen = Math.Round(average, 4),
            ProjetsParDomaine = byDomain,
            ProjetsParStatut = byStatus
        };
    }

    private static int CountStatus(IEnumerable<DashboardCountDto> values, string expected)
    {
        return values
            .Where(x => Normalize(x.Nom) == expected)
            .Sum(x => x.Count);
    }

    private static string Normalize(string value) => value
        .Normalize(System.Text.NormalizationForm.FormD)
        .Where(ch => System.Globalization.CharUnicodeInfo.GetUnicodeCategory(ch) != System.Globalization.UnicodeCategory.NonSpacingMark)
        .Aggregate(new System.Text.StringBuilder(), (builder, ch) => builder.Append(char.ToLowerInvariant(ch)), builder => builder.ToString());
}
