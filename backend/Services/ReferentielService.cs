using IncidentsDsi.Api.Common;
using IncidentsDsi.Api.Data;
using IncidentsDsi.Api.DTOs;
using IncidentsDsi.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace IncidentsDsi.Api.Services;

public sealed class ReferentielService(AppDbContext db) : IReferentielService
{
    public async Task<ReferentielSetDto> GetAllAsync(bool includeInactive, CancellationToken cancellationToken)
    {
        return new ReferentielSetDto
        {
            Statuts = await GetAsync(db.Statuts, includeInactive, cancellationToken),
            Criticites = await GetAsync(db.Criticites, includeInactive, cancellationToken),
            Applications = await GetAsync(db.Applications, includeInactive, cancellationToken),
            TypesIncident = await GetAsync(db.TypesIncident, includeInactive, cancellationToken),
            Entites = await GetAsync(db.Entites, includeInactive, cancellationToken),
            Risques = await GetAsync(db.Risques, includeInactive, cancellationToken),
            Responsables = await GetAsync(db.Responsables, includeInactive, cancellationToken),
            DomainesProjet = await GetAsync(db.DomainesProjet, includeInactive, cancellationToken),
            StatutsProjet = await GetAsync(db.StatutsProjet, includeInactive, cancellationToken),
            StatutsEtape = await GetAsync(db.StatutsEtape, includeInactive, cancellationToken)
        };
    }

    public async Task<IReadOnlyList<ReferentielDto>> GetAsync(string type, bool includeInactive, CancellationToken cancellationToken)
    {
        return NormalizeType(type) switch
        {
            "responsables" => await GetAsync(db.Responsables, includeInactive, cancellationToken),
            "applications" => await GetAsync(db.Applications, includeInactive, cancellationToken),
            "types-incidents" => await GetAsync(db.TypesIncident, includeInactive, cancellationToken),
            "entites" => await GetAsync(db.Entites, includeInactive, cancellationToken),
            "criticites" => await GetAsync(db.Criticites, includeInactive, cancellationToken),
            "risques" => await GetAsync(db.Risques, includeInactive, cancellationToken),
            "statuts" => await GetAsync(db.Statuts, includeInactive, cancellationToken),
            "domaines-projet" => await GetAsync(db.DomainesProjet, includeInactive, cancellationToken),
            "statuts-projet" => await GetAsync(db.StatutsProjet, includeInactive, cancellationToken),
            "statuts-etape" => await GetAsync(db.StatutsEtape, includeInactive, cancellationToken),
            _ => throw new ValidationException($"Le référentiel '{type}' n'existe pas.")
        };
    }

    public async Task<ReferentielDto?> GetByIdAsync(string type, int id, CancellationToken cancellationToken)
    {
        return NormalizeType(type) switch
        {
            "responsables" => await GetByIdAsync(db.Responsables, id, cancellationToken),
            "applications" => await GetByIdAsync(db.Applications, id, cancellationToken),
            "types-incidents" => await GetByIdAsync(db.TypesIncident, id, cancellationToken),
            "entites" => await GetByIdAsync(db.Entites, id, cancellationToken),
            "criticites" => await GetByIdAsync(db.Criticites, id, cancellationToken),
            "risques" => await GetByIdAsync(db.Risques, id, cancellationToken),
            "statuts" => await GetByIdAsync(db.Statuts, id, cancellationToken),
            "domaines-projet" => await GetByIdAsync(db.DomainesProjet, id, cancellationToken),
            "statuts-projet" => await GetByIdAsync(db.StatutsProjet, id, cancellationToken),
            "statuts-etape" => await GetByIdAsync(db.StatutsEtape, id, cancellationToken),
            _ => throw new ValidationException($"Le référentiel '{type}' n'existe pas.")
        };
    }

    public async Task<ReferentielDto> CreateAsync(string type, ReferentielWriteDto input, CancellationToken cancellationToken)
    {
        var name = input.Nom.Trim();
        if (name.Length == 0) throw new ValidationException("Le nom est obligatoire.");

        return NormalizeType(type) switch
        {
            "responsables" => await CreateAsync(db.Responsables, name, input.Actif, cancellationToken),
            "applications" => await CreateAsync(db.Applications, name, input.Actif, cancellationToken),
            "types-incidents" => await CreateAsync(db.TypesIncident, name, input.Actif, cancellationToken),
            "entites" => await CreateAsync(db.Entites, name, input.Actif, cancellationToken),
            "criticites" => await CreateAsync(db.Criticites, name, input.Actif, cancellationToken),
            "risques" => await CreateAsync(db.Risques, name, input.Actif, cancellationToken),
            "statuts" => await CreateAsync(db.Statuts, name, input.Actif, cancellationToken),
            "domaines-projet" => await CreateAsync(db.DomainesProjet, name, input.Actif, cancellationToken),
            "statuts-projet" => await CreateAsync(db.StatutsProjet, name, input.Actif, cancellationToken),
            "statuts-etape" => await CreateAsync(db.StatutsEtape, name, input.Actif, cancellationToken),
            _ => throw new ValidationException($"Le référentiel '{type}' n'existe pas.")
        };
    }

    public async Task<ReferentielDto?> UpdateAsync(string type, int id, ReferentielWriteDto input, CancellationToken cancellationToken)
    {
        var name = input.Nom.Trim();
        if (name.Length == 0) throw new ValidationException("Le nom est obligatoire.");

        return NormalizeType(type) switch
        {
            "responsables" => await UpdateAsync(db.Responsables, id, name, input.Actif, cancellationToken),
            "applications" => await UpdateAsync(db.Applications, id, name, input.Actif, cancellationToken),
            "types-incidents" => await UpdateAsync(db.TypesIncident, id, name, input.Actif, cancellationToken),
            "entites" => await UpdateAsync(db.Entites, id, name, input.Actif, cancellationToken),
            "criticites" => await UpdateAsync(db.Criticites, id, name, input.Actif, cancellationToken),
            "risques" => await UpdateAsync(db.Risques, id, name, input.Actif, cancellationToken),
            "statuts" => await UpdateAsync(db.Statuts, id, name, input.Actif, cancellationToken),
            "domaines-projet" => await UpdateAsync(db.DomainesProjet, id, name, input.Actif, cancellationToken),
            "statuts-projet" => await UpdateAsync(db.StatutsProjet, id, name, input.Actif, cancellationToken),
            "statuts-etape" => await UpdateAsync(db.StatutsEtape, id, name, input.Actif, cancellationToken),
            _ => throw new ValidationException($"Le référentiel '{type}' n'existe pas.")
        };
    }

    private async Task<IReadOnlyList<ReferentielDto>> GetAsync<T>(
        IQueryable<T> query,
        bool includeInactive,
        CancellationToken cancellationToken)
        where T : ReferentielBase
    {
        if (!includeInactive) query = query.Where(x => x.Actif);
        var values = await query.AsNoTracking().OrderBy(x => x.Nom).ToListAsync(cancellationToken);
        return values.Select(ToDto).ToList();
    }

    private static async Task<ReferentielDto?> GetByIdAsync<T>(
        IQueryable<T> query,
        int id,
        CancellationToken cancellationToken)
        where T : ReferentielBase
    {
        var value = await query.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        return value is null ? null : ToDto(value);
    }

    private async Task<ReferentielDto> CreateAsync<T>(
        DbSet<T> set,
        string name,
        bool actif,
        CancellationToken cancellationToken)
        where T : ReferentielBase, new()
    {
        if (await set.AnyAsync(x => x.Nom == name, cancellationToken))
            throw new ValidationException("Une valeur portant ce nom existe déjà dans ce référentiel.");

        var now = DateTime.UtcNow;
        var value = new T { Nom = name, Actif = actif, CreatedAt = now, UpdatedAt = now };
        set.Add(value);
        await db.SaveChangesAsync(cancellationToken);
        return ToDto(value);
    }

    private async Task<ReferentielDto?> UpdateAsync<T>(
        DbSet<T> set,
        int id,
        string name,
        bool actif,
        CancellationToken cancellationToken)
        where T : ReferentielBase
    {
        var value = await set.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (value is null) return null;

        if (await set.AnyAsync(x => x.Id != id && x.Nom == name, cancellationToken))
            throw new ValidationException("Une valeur portant ce nom existe déjà dans ce référentiel.");

        value.Nom = name;
        value.Actif = actif;
        value.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(cancellationToken);
        return ToDto(value);
    }

    private static string NormalizeType(string type) => type.Trim().ToLowerInvariant() switch
    {
        "responsable" or "responsables" => "responsables",
        "application" or "applications" => "applications",
        "typeincident" or "typesincident" or "types-incidents" or "type-incidents" => "types-incidents",
        "entite" or "entites" => "entites",
        "criticite" or "criticites" => "criticites",
        "risque" or "risques" => "risques",
        "statut" or "statuts" => "statuts",
        "domaineprojet" or "domainesprojet" or "domaines-projet" => "domaines-projet",
        "statutprojet" or "statutsprojet" or "statuts-projet" => "statuts-projet",
        "statutetape" or "statutsetape" or "statuts-etape" => "statuts-etape",
        _ => type.Trim().ToLowerInvariant()
    };

    private static ReferentielDto ToDto(ReferentielBase value) => new()
    {
        Id = value.Id,
        Nom = value.Nom,
        Actif = value.Actif,
        CreatedAt = value.CreatedAt,
        UpdatedAt = value.UpdatedAt
    };
}
