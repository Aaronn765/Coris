using System.Data;
using IncidentsDsi.Api.Common;
using IncidentsDsi.Api.Data;
using IncidentsDsi.Api.DTOs;
using IncidentsDsi.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace IncidentsDsi.Api.Services;

public sealed class IncidentService(AppDbContext db) : IIncidentService
{
    public async Task<PagedResult<IncidentDto>> GetPagedAsync(
        IncidentQueryParameters parameters,
        CancellationToken cancellationToken)
    {
        parameters.Normalize();
        ValidateQueryParameters(parameters);
        var query = ApplyFilters(db.Incidents.AsNoTracking(), parameters);

        if (IsDurationSort(parameters) && !db.Database.IsSqlServer())
        {
            var filtered = await query.ToListAsync(cancellationToken);
            var sorted = ApplyDurationOrdering(filtered, parameters);
            var durationPageIds = sorted
                .Skip((parameters.Page - 1) * parameters.PageSize)
                .Take(parameters.PageSize)
                .Select(x => x.Id)
                .ToList();
            var durationDetails = await LoadIncidentDetailsAsync(durationPageIds, cancellationToken);
            return new PagedResult<IncidentDto>
            {
                Items = durationPageIds.Select(id => ToDto(durationDetails[id])).ToList(),
                Page = parameters.Page,
                PageSize = parameters.PageSize,
                TotalCount = filtered.Count,
                TotalPages = (int)Math.Ceiling(filtered.Count / (double)parameters.PageSize)
            };
        }

        var totalCount = await query.CountAsync(cancellationToken);
        var ordered = ApplyOrdering(query, parameters);
        var pageIds = await ordered
            .Skip((parameters.Page - 1) * parameters.PageSize)
            .Take(parameters.PageSize)
            .Select(x => x.Id)
            .ToListAsync(cancellationToken);
        var details = await LoadIncidentDetailsAsync(pageIds, cancellationToken);

        return new PagedResult<IncidentDto>
        {
            Items = pageIds.Select(id => ToDto(details[id])).ToList(),
            Page = parameters.Page,
            PageSize = parameters.PageSize,
            TotalCount = totalCount,
            TotalPages = (int)Math.Ceiling(totalCount / (double)parameters.PageSize)
        };
    }

    public async Task<IncidentDto?> GetByIdAsync(int id, CancellationToken cancellationToken)
    {
        var incident = await IncludeDetails(db.Incidents.AsNoTracking())
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        return incident is null ? null : ToDto(incident);
    }

    public async Task<IncidentDto> CreateAsync(IncidentWriteDto input, CancellationToken cancellationToken)
    {
        ValidateDates(input.DateDeclaration, input.DateFin);
        await EnsureReferencesExistAsync(input, cancellationToken);

        await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);
        var year = input.DateDeclaration.Year;
        var sequence = await db.IncidentNumberSequences.SingleOrDefaultAsync(x => x.Year == year, cancellationToken);

        if (sequence is null)
        {
            sequence = new IncidentNumberSequence { Year = year, LastValue = 1 };
            db.IncidentNumberSequences.Add(sequence);
        }
        else
        {
            sequence.LastValue++;
        }

        var incident = ToEntity(input);
        incident.Numero = $"INC-{year:D4}-{sequence.LastValue:D4}";
        db.Incidents.Add(incident);

        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return (await GetByIdAsync(incident.Id, cancellationToken))!;
    }

    public async Task<IncidentDto?> UpdateAsync(int id, IncidentWriteDto input, CancellationToken cancellationToken)
    {
        ValidateDates(input.DateDeclaration, input.DateFin);
        await EnsureReferencesExistAsync(input, cancellationToken);

        var incident = await db.Incidents.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (incident is null)
        {
            return null;
        }

        UpdateEntity(incident, input);
        incident.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(cancellationToken);

        return await GetByIdAsync(id, cancellationToken);
    }

    public async Task<IReadOnlyList<IncidentDto>> GetForExportAsync(
        IncidentQueryParameters parameters,
        CancellationToken cancellationToken)
    {
        parameters.Normalize();
        ValidateQueryParameters(parameters);
        var query = ApplyFilters(db.Incidents.AsNoTracking(), parameters);

        if (IsDurationSort(parameters) && !db.Database.IsSqlServer())
        {
            var filtered = await query.ToListAsync(cancellationToken);
            var ids = ApplyDurationOrdering(filtered, parameters).Select(x => x.Id).ToList();
            var details = await LoadIncidentDetailsAsync(ids, cancellationToken);
            return ids.Select(id => ToDto(details[id])).ToList();
        }

        var incidents = await IncludeDetails(ApplyOrdering(query, parameters))
            .ToListAsync(cancellationToken);

        return incidents.Select(ToDto).ToList();
    }

    private IQueryable<Incident> ApplyFilters(IQueryable<Incident> query, IncidentQueryParameters parameters)
    {
        if (!string.IsNullOrWhiteSpace(parameters.Search))
        {
            var search = EscapeLikePattern(parameters.Search.Trim());
            var pattern = $"%{search}%";
            query = query.Where(x =>
                EF.Functions.Like(x.Numero, pattern) ||
                EF.Functions.Like(x.Intitule, pattern) ||
                EF.Functions.Like(x.Description, pattern) ||
                EF.Functions.Like(x.Application.Nom, pattern) ||
                EF.Functions.Like(x.Entite.Nom, pattern) ||
                EF.Functions.Like(x.ResponsableN1.Nom, pattern) ||
                (x.ResponsableN2 != null && EF.Functions.Like(x.ResponsableN2.Nom, pattern)) ||
                EF.Functions.Like(x.Cause ?? string.Empty, pattern) ||
                EF.Functions.Like(x.Solution ?? string.Empty, pattern) ||
                EF.Functions.Like(x.ActionsMenees ?? string.Empty, pattern));
        }

        if (parameters.DateDebut.HasValue)
        {
            query = query.Where(x => x.DateDeclaration >= parameters.DateDebut.Value.Date);
        }

        if (parameters.DateFin.HasValue)
        {
            var endExclusive = parameters.DateFin.Value.Date.AddDays(1);
            query = query.Where(x => x.DateDeclaration < endExclusive);
        }

        if (parameters.StatutId.HasValue)
        {
            query = query.Where(x => x.StatutId == parameters.StatutId.Value);
        }

        if (parameters.TypeIncidentId.HasValue)
        {
            query = query.Where(x => x.TypeIncidentId == parameters.TypeIncidentId.Value);
        }

        if (parameters.ApplicationId.HasValue)
        {
            query = query.Where(x => x.ApplicationId == parameters.ApplicationId.Value);
        }

        if (parameters.EntiteId.HasValue)
        {
            query = query.Where(x => x.EntiteId == parameters.EntiteId.Value);
        }

        if (parameters.CriticiteId.HasValue)
        {
            query = query.Where(x => x.CriticiteId == parameters.CriticiteId.Value);
        }

        if (parameters.RisqueId.HasValue)
        {
            query = query.Where(x => x.RisqueId == parameters.RisqueId.Value);
        }

        if (parameters.ResponsableId.HasValue)
        {
            query = query.Where(x => x.ResponsableN1Id == parameters.ResponsableId.Value ||
                                     x.ResponsableN2Id == parameters.ResponsableId.Value);
        }

        if (parameters.DureeMinMinutes.HasValue || parameters.DureeMaxMinutes.HasValue)
        {
            query = query.Where(x => x.DateFin.HasValue);

            if (parameters.DureeMinMinutes.HasValue)
            {
                var minMinutes = parameters.DureeMinMinutes.Value;
                query = query.Where(x => x.DateFin!.Value >= x.DateDeclaration.AddMinutes(minMinutes));
            }

            if (parameters.DureeMaxMinutes.HasValue)
            {
                var maxExclusiveMinutes = parameters.DureeMaxMinutes.Value + 1;
                query = query.Where(x => x.DateFin!.Value < x.DateDeclaration.AddMinutes(maxExclusiveMinutes));
            }
        }

        return query;
    }

    private IOrderedQueryable<Incident> ApplyOrdering(
        IQueryable<Incident> query,
        IncidentQueryParameters parameters)
    {
        var descending = parameters.SortDirection.Equals("desc", StringComparison.OrdinalIgnoreCase);

        if (parameters.SortBy.Equals("duree", StringComparison.OrdinalIgnoreCase))
        {
            if (db.Database.IsSqlServer())
            {
                return descending
                    ? query.OrderByDescending(x => x.DateFin.HasValue
                        ? EF.Functions.DateDiffMinute(x.DateDeclaration, x.DateFin!.Value)
                        : -1).ThenByDescending(x => x.Id)
                    : query.OrderBy(x => x.DateFin.HasValue
                        ? EF.Functions.DateDiffMinute(x.DateDeclaration, x.DateFin!.Value)
                        : -1).ThenBy(x => x.Id);
            }

            throw new InvalidOperationException("Le tri par duree doit etre traite en memoire avec ce fournisseur de base de donnees.");
        }

        return (parameters.SortBy.ToLowerInvariant(), descending) switch
        {
            ("numero", false) => query.OrderBy(x => x.Numero).ThenBy(x => x.Id),
            ("numero", true) => query.OrderByDescending(x => x.Numero).ThenByDescending(x => x.Id),
            ("datefin", false) => query.OrderBy(x => x.DateFin).ThenBy(x => x.Id),
            ("datefin", true) => query.OrderByDescending(x => x.DateFin).ThenByDescending(x => x.Id),
            ("intitule", false) => query.OrderBy(x => x.Intitule).ThenBy(x => x.Id),
            ("intitule", true) => query.OrderByDescending(x => x.Intitule).ThenByDescending(x => x.Id),
            ("application", false) => query.OrderBy(x => x.Application.Nom).ThenBy(x => x.Id),
            ("application", true) => query.OrderByDescending(x => x.Application.Nom).ThenByDescending(x => x.Id),
            ("typeincident", false) => query.OrderBy(x => x.TypeIncident.Nom).ThenBy(x => x.Id),
            ("typeincident", true) => query.OrderByDescending(x => x.TypeIncident.Nom).ThenByDescending(x => x.Id),
            ("criticite", false) => query.OrderBy(x => x.Criticite.Nom).ThenBy(x => x.Id),
            ("criticite", true) => query.OrderByDescending(x => x.Criticite.Nom).ThenByDescending(x => x.Id),
            ("statut", false) => query.OrderBy(x => x.Statut.Nom).ThenBy(x => x.Id),
            ("statut", true) => query.OrderByDescending(x => x.Statut.Nom).ThenByDescending(x => x.Id),
            ("createdat", false) => query.OrderBy(x => x.CreatedAt).ThenBy(x => x.Id),
            ("createdat", true) => query.OrderByDescending(x => x.CreatedAt).ThenByDescending(x => x.Id),
            ("datedeclaration", false) => query.OrderBy(x => x.DateDeclaration).ThenBy(x => x.Id),
            _ => query.OrderByDescending(x => x.DateDeclaration).ThenByDescending(x => x.Id)
        };
    }

    private static IQueryable<Incident> IncludeDetails(IQueryable<Incident> query) => query
        .Include(x => x.TypeIncident)
        .Include(x => x.Application)
        .Include(x => x.Entite)
        .Include(x => x.Criticite)
        .Include(x => x.Risque)
        .Include(x => x.ResponsableN1)
        .Include(x => x.ResponsableN2)
        .Include(x => x.Statut);

    private async Task<Dictionary<int, Incident>> LoadIncidentDetailsAsync(
        IReadOnlyCollection<int> ids,
        CancellationToken cancellationToken)
    {
        if (ids.Count == 0)
            return [];

        return await IncludeDetails(db.Incidents.AsNoTracking())
            .Where(x => ids.Contains(x.Id))
            .ToDictionaryAsync(x => x.Id, cancellationToken);
    }

    private async Task EnsureReferencesExistAsync(IncidentWriteDto input, CancellationToken cancellationToken)
    {
        if (!await db.TypesIncident.AnyAsync(x => x.Id == input.TypeIncidentId && x.Actif, cancellationToken))
            throw new ValidationException("Le type d'incident sélectionné est introuvable.");
        if (!await db.Applications.AnyAsync(x => x.Id == input.ApplicationId && x.Actif, cancellationToken))
            throw new ValidationException("L'application sélectionnée est introuvable.");
        if (!await db.Entites.AnyAsync(x => x.Id == input.EntiteId && x.Actif, cancellationToken))
            throw new ValidationException("L'entité sélectionnée est introuvable.");
        if (!await db.Criticites.AnyAsync(x => x.Id == input.CriticiteId && x.Actif, cancellationToken))
            throw new ValidationException("La criticité sélectionnée est introuvable.");
        if (!await db.Risques.AnyAsync(x => x.Id == input.RisqueId && x.Actif, cancellationToken))
            throw new ValidationException("Le risque sélectionné est introuvable.");
        if (!await db.Responsables.AnyAsync(x => x.Id == input.ResponsableN1Id && x.Actif, cancellationToken))
            throw new ValidationException("Le responsable N1 sélectionné est introuvable.");
        if (input.ResponsableN2Id.HasValue &&
            !await db.Responsables.AnyAsync(x => x.Id == input.ResponsableN2Id.Value && x.Actif, cancellationToken))
            throw new ValidationException("Le responsable N2 sélectionné est introuvable.");
        if (!await db.Statuts.AnyAsync(x => x.Id == input.StatutId && x.Actif, cancellationToken))
            throw new ValidationException("Le statut sélectionné est introuvable.");
    }

    private static Incident ToEntity(IncidentWriteDto input) => new()
    {
        DateDeclaration = input.DateDeclaration,
        DateFin = input.DateFin,
        TypeIncidentId = input.TypeIncidentId,
        ApplicationId = input.ApplicationId,
        Intitule = input.Intitule.Trim(),
        Description = input.Description.Trim(),
        EntiteId = input.EntiteId,
        CriticiteId = input.CriticiteId,
        Impact = CleanOptional(input.Impact),
        Cause = CleanOptional(input.Cause),
        RisqueId = input.RisqueId,
        ActionsMenees = CleanOptional(input.ActionsMenees),
        Solution = CleanOptional(input.Solution),
        ActionsEnCours = CleanOptional(input.ActionsEnCours),
        ResponsableN1Id = input.ResponsableN1Id,
        ResponsableN2Id = input.ResponsableN2Id,
        MesuresPreventives = CleanOptional(input.MesuresPreventives),
        StatutId = input.StatutId,
        CreatedAt = DateTime.UtcNow,
        UpdatedAt = DateTime.UtcNow
    };

    private static void UpdateEntity(Incident incident, IncidentWriteDto input)
    {
        incident.DateDeclaration = input.DateDeclaration;
        incident.DateFin = input.DateFin;
        incident.TypeIncidentId = input.TypeIncidentId;
        incident.ApplicationId = input.ApplicationId;
        incident.Intitule = input.Intitule.Trim();
        incident.Description = input.Description.Trim();
        incident.EntiteId = input.EntiteId;
        incident.CriticiteId = input.CriticiteId;
        incident.Impact = CleanOptional(input.Impact);
        incident.Cause = CleanOptional(input.Cause);
        incident.RisqueId = input.RisqueId;
        incident.ActionsMenees = CleanOptional(input.ActionsMenees);
        incident.Solution = CleanOptional(input.Solution);
        incident.ActionsEnCours = CleanOptional(input.ActionsEnCours);
        incident.ResponsableN1Id = input.ResponsableN1Id;
        incident.ResponsableN2Id = input.ResponsableN2Id;
        incident.MesuresPreventives = CleanOptional(input.MesuresPreventives);
        incident.StatutId = input.StatutId;
    }

    private static void ValidateDates(DateTime dateDeclaration, DateTime? dateFin)
    {
        if (dateDeclaration == default)
            throw new ValidationException("La date de declaration est obligatoire.");

        if (dateFin.HasValue && dateFin.Value < dateDeclaration)
            throw new ValidationException("La date de fin doit être postérieure ou égale à la date de déclaration.");
    }

    private static void ValidateQueryParameters(IncidentQueryParameters parameters)
    {
        if (parameters.HasInvalidDateRange)
            throw new ValidationException("La date de debut doit etre anterieure ou egale a la date de fin.");

        if (parameters.DureeMinMinutes.HasValue &&
            parameters.DureeMaxMinutes.HasValue &&
            parameters.DureeMinMinutes.Value > parameters.DureeMaxMinutes.Value)
            throw new ValidationException("La duree minimale doit etre inferieure ou egale a la duree maximale.");
    }

    private static bool IsDurationSort(IncidentQueryParameters parameters) =>
        parameters.SortBy.Equals("duree", StringComparison.OrdinalIgnoreCase);

    private static IEnumerable<Incident> ApplyDurationOrdering(
        IEnumerable<Incident> incidents,
        IncidentQueryParameters parameters)
    {
        var descending = parameters.SortDirection.Equals("desc", StringComparison.OrdinalIgnoreCase);
        return descending
            ? incidents.OrderByDescending(x => x.DureeMinutes ?? -1).ThenByDescending(x => x.Id)
            : incidents.OrderBy(x => x.DureeMinutes ?? -1).ThenBy(x => x.Id);
    }

    private static string? CleanOptional(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static string EscapeLikePattern(string value) => value
        .Replace("[", "[[]", StringComparison.Ordinal)
        .Replace("%", "[%]", StringComparison.Ordinal)
        .Replace("_", "[_]", StringComparison.Ordinal);

    private static IncidentDto ToDto(Incident incident) => new()
    {
        Id = incident.Id,
        Numero = incident.Numero,
        DateDeclaration = incident.DateDeclaration,
        DateFin = incident.DateFin,
        DureeMinutes = incident.DureeMinutes,
        DureeJours = incident.DureeJours,
        TypeIncidentId = incident.TypeIncidentId,
        TypeIncident = ToReferenceDto(incident.TypeIncident),
        ApplicationId = incident.ApplicationId,
        Application = ToReferenceDto(incident.Application),
        Intitule = incident.Intitule,
        Description = incident.Description,
        EntiteId = incident.EntiteId,
        Entite = ToReferenceDto(incident.Entite),
        CriticiteId = incident.CriticiteId,
        Criticite = ToReferenceDto(incident.Criticite),
        Impact = incident.Impact,
        Cause = incident.Cause,
        RisqueId = incident.RisqueId,
        Risque = ToReferenceDto(incident.Risque),
        ActionsMenees = incident.ActionsMenees,
        Solution = incident.Solution,
        ActionsEnCours = incident.ActionsEnCours,
        ResponsableN1Id = incident.ResponsableN1Id,
        ResponsableN1 = ToReferenceDto(incident.ResponsableN1),
        ResponsableN2Id = incident.ResponsableN2Id,
        ResponsableN2 = incident.ResponsableN2 is null ? null : ToReferenceDto(incident.ResponsableN2),
        MesuresPreventives = incident.MesuresPreventives,
        StatutId = incident.StatutId,
        Statut = ToReferenceDto(incident.Statut),
        CreatedAt = incident.CreatedAt,
        UpdatedAt = incident.UpdatedAt
    };

    private static ReferentielDto ToReferenceDto(ReferentielBase reference) => new()
    {
        Id = reference.Id,
        Nom = reference.Nom,
        Actif = reference.Actif,
        CreatedAt = reference.CreatedAt,
        UpdatedAt = reference.UpdatedAt
    };
}
