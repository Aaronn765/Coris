using System.Globalization;
using System.Text;
using ClosedXML.Excel;
using IncidentsDsi.Api.Data;
using IncidentsDsi.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace IncidentsDsi.Api.Services;

public sealed class ExcelImportService(AppDbContext db, ILogger<ExcelImportService> logger) : IExcelImportService
{
    public async Task<int> ImportIfEmptyAsync(string path, CancellationToken cancellationToken)
    {
        if (await db.Incidents.AnyAsync(cancellationToken))
        {
            logger.LogInformation("Import Excel ignoré : la table des incidents contient déjà des données.");
            return 0;
        }

        if (!File.Exists(path))
        {
            logger.LogWarning("Fichier Excel historique introuvable : {Path}", path);
            return 0;
        }

        var references = await LoadReferencesAsync(cancellationToken);
        var imported = new List<Incident>();
        var nextNumbersByYear = new Dictionary<int, int>();
        using var workbook = new XLWorkbook(path);
        var worksheet = workbook.Worksheets.FirstOrDefault();

        if (worksheet is null)
        {
            logger.LogWarning("Le classeur Excel ne contient aucune feuille.");
            return 0;
        }

        foreach (var row in worksheet.RowsUsed().Skip(1))
        {
            cancellationToken.ThrowIfCancellationRequested();
            var dateDeclaration = ReadDate(row.Cell(2));
            if (!dateDeclaration.HasValue) continue;

            var dateFin = ReadDate(row.Cell(3));
            var application = Find(references.Applications, row.Cell(6).GetString());
            var typeIncident = Find(references.TypesIncident, row.Cell(5).GetString());
            var entite = Find(references.Entites, row.Cell(9).GetString());
            var criticite = Find(references.Criticites, row.Cell(10).GetString());
            var risque = Find(references.Risques, row.Cell(13).GetString());
            var responsableN1 = Find(references.Responsables, row.Cell(17).GetString());
            var responsableN2 = Find(references.Responsables, row.Cell(18).GetString());
            var statut = Find(references.Statuts, row.Cell(20).GetString());

            if (application is null || typeIncident is null || entite is null || criticite is null ||
                risque is null || responsableN1 is null || statut is null)
            {
                logger.LogWarning("Ligne Excel ignorée : un référentiel obligatoire est inconnu (ligne {Row}).", row.RowNumber());
                continue;
            }

            var year = dateDeclaration.Value.Year;
            nextNumbersByYear.TryGetValue(year, out var lastNumber);
            var number = $"INC-{year:D4}-{lastNumber + 1:D4}";
            nextNumbersByYear[year] = lastNumber + 1;
            var description = Clean(row.Cell(8).GetString());

            imported.Add(new Incident
            {
                Numero = number,
                DateDeclaration = dateDeclaration.Value,
                DateFin = dateFin,
                TypeIncidentId = typeIncident.Id,
                ApplicationId = application.Id,
                Intitule = Limit(string.IsNullOrWhiteSpace(row.Cell(7).GetString()) ? $"Incident importé {number}" : row.Cell(7).GetString(), 300),
                Description = Limit(string.IsNullOrWhiteSpace(description) ? "Incident importé depuis le fichier Excel historique." : description, 10000),
                EntiteId = entite.Id,
                CriticiteId = criticite.Id,
                Impact = Clean(row.Cell(11).GetString()),
                Cause = Clean(row.Cell(12).GetString()),
                RisqueId = risque.Id,
                ActionsMenees = Clean(row.Cell(14).GetString()),
                Solution = Clean(row.Cell(15).GetString()),
                ActionsEnCours = Clean(row.Cell(16).GetString()),
                ResponsableN1Id = responsableN1.Id,
                ResponsableN2Id = responsableN2?.Id,
                MesuresPreventives = Clean(row.Cell(19).GetString()),
                StatutId = statut.Id,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            });
        }

        if (imported.Count == 0) return 0;

        db.Incidents.AddRange(imported);
        foreach (var group in imported.GroupBy(x => x.DateDeclaration.Year))
        {
            var max = group
                .Select(x => ParseSequence(x.Numero))
                .Where(x => x.HasValue)
                .Select(x => x!.Value)
                .DefaultIfEmpty()
                .Max();
            db.IncidentNumberSequences.Add(new IncidentNumberSequence { Year = group.Key, LastValue = max });
        }

        await db.SaveChangesAsync(cancellationToken);
        logger.LogInformation("{Count} incidents historiques importés depuis {Path}.", imported.Count, path);
        return imported.Count;
    }

    public async Task<int> ImportProjectsIfEmptyAsync(string path, CancellationToken cancellationToken)
    {
        if (await db.Projets.AnyAsync(cancellationToken))
        {
            logger.LogInformation("Import Excel projets ignoré : la table des projets contient déjà des données.");
            return 0;
        }

        if (!File.Exists(path))
        {
            logger.LogWarning("Fichier Excel des projets introuvable : {Path}", path);
            return 0;
        }

        using var workbook = new XLWorkbook(path);
        var worksheet = workbook.Worksheets.FirstOrDefault(x => Normalize(x.Name) == Normalize("PROJETS INFORMATIQUES"));
        if (worksheet is null)
        {
            logger.LogInformation("Aucune feuille PROJETS INFORMATIQUES trouvée dans {Path}.", path);
            return 0;
        }

        var domains = await db.DomainesProjet.ToListAsync(cancellationToken);
        var projectStatuses = await db.StatutsProjet.ToListAsync(cancellationToken);
        var stepStatuses = await db.StatutsEtape.ToListAsync(cancellationToken);
        var responsibles = await db.Responsables.ToListAsync(cancellationToken);
        var rows = worksheet.RowsUsed().Where(x => x.RowNumber() >= 4).ToList();
        var projectStarts = rows.Where(x => !string.IsNullOrWhiteSpace(x.Cell(3).GetString())).ToList();
        var imported = new List<Projet>();
        var nextNumbersByYear = new Dictionary<int, int>();

        for (var index = 0; index < projectStarts.Count; index++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var start = projectStarts[index];
            var endRow = index + 1 < projectStarts.Count
                ? projectStarts[index + 1].RowNumber()
                : int.MaxValue;
            var block = rows.Where(x => x.RowNumber() >= start.RowNumber() && x.RowNumber() < endRow).ToList();
            var domain = FindOrCreate(domains, Clean(start.Cell(2).GetString()), "Non classé", db);
            var steps = block
                .Where(x => !string.IsNullOrWhiteSpace(x.Cell(8).GetString()))
                .Select((row, stepIndex) => ToProjectStep(row, stepIndex, stepStatuses))
                .ToList();
            var projectStatus = ResolveProjectStatus(steps, projectStatuses, start.Cell(10).GetString());
            var dueDate = ReadDate(start.Cell(7));
            var year = (dueDate ?? DateTime.UtcNow).Year;
            nextNumbersByYear.TryGetValue(year, out var lastNumber);
            var project = new Projet
            {
                Numero = $"PRJ-{year:D4}-{lastNumber + 1:D4}",
                DomaineProjet = domain,
                Nom = Limit(start.Cell(3).GetString().Trim(), 300),
                Description = LimitOptional(start.Cell(6).GetString(), 10000),
                TauxAvancement = ReadRate(start.Cell(4)) ?? CalculateAverageRate(steps),
                DateEcheance = dueDate,
                StatutProjet = projectStatus,
                Support = LimitOptional(start.Cell(13).GetString(), 5000),
                ActeursMetiers = LimitOptional(start.Cell(14).GetString(), 5000),
                Contraintes = LimitOptional(start.Cell(15).GetString(), 10000),
                Commentaires = LimitOptional(start.Cell(16).GetString(), 10000),
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                Etapes = steps
            };

            foreach (var name in SplitPeople(start.Cell(5).GetString()))
            {
                var responsible = FindOrCreateResponsable(responsibles, name, db);
                project.ProjetResponsables.Add(new ProjetResponsable { Responsable = responsible });
            }

            nextNumbersByYear[year] = lastNumber + 1;
            imported.Add(project);
        }

        if (imported.Count == 0) return 0;

        db.Projets.AddRange(imported);
        foreach (var entry in nextNumbersByYear)
            db.ProjetNumberSequences.Add(new ProjetNumberSequence { Year = entry.Key, LastValue = entry.Value });

        await db.SaveChangesAsync(cancellationToken);
        logger.LogInformation("{Count} projets historiques importés depuis {Path}.", imported.Count, path);
        return imported.Count;
    }

    private async Task<ReferenceCache> LoadReferencesAsync(CancellationToken cancellationToken)
    {
        return new ReferenceCache(
            await db.Responsables.AsNoTracking().ToListAsync(cancellationToken),
            await db.Applications.AsNoTracking().ToListAsync(cancellationToken),
            await db.TypesIncident.AsNoTracking().ToListAsync(cancellationToken),
            await db.Entites.AsNoTracking().ToListAsync(cancellationToken),
            await db.Criticites.AsNoTracking().ToListAsync(cancellationToken),
            await db.Risques.AsNoTracking().ToListAsync(cancellationToken),
            await db.Statuts.AsNoTracking().ToListAsync(cancellationToken));
    }

    private static EtapeProjet ToProjectStep(IXLRow row, int order, IReadOnlyList<StatutEtape> statuses) => new()
    {
        Nom = Limit(row.Cell(8).GetString().Trim(), 10000),
        TauxAvancement = ReadRate(row.Cell(9)) ?? 0,
        StatutEtape = FindStepStatus(statuses, row.Cell(10).GetString()),
        DateDebut = ReadDate(row.Cell(11)),
        DateFin = ReadDate(row.Cell(12)),
        Support = LimitOptional(row.Cell(13).GetString(), 5000),
        Contraintes = LimitOptional(row.Cell(15).GetString(), 10000),
        Commentaires = LimitOptional(row.Cell(16).GetString(), 10000),
        Ordre = order,
        CreatedAt = DateTime.UtcNow,
        UpdatedAt = DateTime.UtcNow
    };

    private static DomaineProjet FindOrCreate(
        ICollection<DomaineProjet> values,
        string? value,
        string fallback,
        AppDbContext db)
    {
        var name = string.IsNullOrWhiteSpace(value) ? fallback : value.Trim();
        var found = values.FirstOrDefault(x => Normalize(x.Nom) == Normalize(name));
        if (found is not null) return found;

        var created = new DomaineProjet { Nom = name, Actif = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };
        values.Add(created);
        db.DomainesProjet.Add(created);
        return created;
    }

    private static Responsable FindOrCreateResponsable(
        ICollection<Responsable> values,
        string name,
        AppDbContext db)
    {
        var found = values.FirstOrDefault(x => Normalize(x.Nom) == Normalize(name));
        if (found is not null) return found;

        var created = new Responsable { Nom = Limit(name, 150), Actif = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };
        values.Add(created);
        db.Responsables.Add(created);
        return created;
    }

    private static StatutEtape FindStepStatus(IReadOnlyList<StatutEtape> statuses, string value)
    {
        var normalized = Normalize(value);
        if (normalized.Contains("termin")) return statuses.First(x => Normalize(x.Nom).Contains("termin"));
        if (normalized.Contains("non")) return statuses.First(x => Normalize(x.Nom).Contains("non"));
        if (normalized.Contains("demarrer")) return statuses.First(x => Normalize(x.Nom).Contains("demarr"));
        return statuses.First(x => Normalize(x.Nom).Contains("non") || Normalize(x.Nom).Contains("plan"));
    }

    private static StatutProjet ResolveProjectStatus(IReadOnlyList<EtapeProjet> steps, IReadOnlyList<StatutProjet> statuses, string rootStatus)
    {
        var names = steps.Select(x => Normalize(x.StatutEtape.Nom)).ToList();
        if (names.Count == 0 && !string.IsNullOrWhiteSpace(rootStatus))
            names.Add(Normalize(rootStatus));
        var name = names.Count > 0 && names.All(x => x.Contains("termin"))
            ? "cloture"
            : names.Any(x => x.Contains("demarr") && !x.Contains("non"))
                ? "en cours"
                : "planifie";
        return statuses.First(x => Normalize(x.Nom) == name);
    }

    private static decimal? ReadRate(IXLCell cell)
    {
        if (cell.IsEmpty()) return null;
        if (cell.TryGetValue<decimal>(out var decimalValue)) return Math.Clamp(decimalValue, 0, 1);
        if (cell.TryGetValue<double>(out var doubleValue)) return Math.Clamp((decimal)doubleValue, 0, 1);
        return decimal.TryParse(cell.GetString(), NumberStyles.Any, CultureInfo.InvariantCulture, out var parsed)
            ? Math.Clamp(parsed, 0, 1)
            : null;
    }

    private static decimal CalculateAverageRate(IEnumerable<EtapeProjet> steps)
    {
        var values = steps.Select(x => x.TauxAvancement).ToList();
        return values.Count == 0 ? 0 : Math.Clamp(values.Average(), 0, 1);
    }

    private static IEnumerable<string> SplitPeople(string value) => value
        .Split(['/', '\\', '\n', ';'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
        .Where(x => x.Length > 0);

    private static T? Find<T>(IEnumerable<T> values, string? value) where T : ReferentielBase =>
        values.FirstOrDefault(x => Normalize(x.Nom) == Normalize(value));

    private static string Normalize(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return string.Empty;
        var normalized = value.Trim().Normalize(NormalizationForm.FormD);
        var builder = new StringBuilder();
        foreach (var character in normalized)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(character) != UnicodeCategory.NonSpacingMark)
                builder.Append(char.ToLowerInvariant(character));
        }
        return builder.ToString().Normalize(NormalizationForm.FormC).Replace("cloture", "cloture");
    }

    private static DateTime? ReadDate(IXLCell cell)
    {
        if (cell.IsEmpty()) return null;
        if (cell.TryGetValue<DateTime>(out var date)) return DateTime.SpecifyKind(date, DateTimeKind.Utc);
        if (cell.TryGetValue<double>(out var serial)) return DateTime.SpecifyKind(DateTime.FromOADate(serial), DateTimeKind.Utc);
        return DateTime.TryParse(cell.GetString(), CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var parsed)
            ? parsed.ToUniversalTime()
            : null;
    }

    private static string? Clean(string value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static string? LimitOptional(string value, int max) => Clean(value) is { } cleaned ? Limit(cleaned, max) : null;

    private static string Limit(string value, int max) => value.Length <= max ? value : value[..max];

    private static int? ParseSequence(string number)
    {
        var lastPart = number.Split('-').LastOrDefault();
        return int.TryParse(lastPart, out var value) ? value : null;
    }

    private sealed record ReferenceCache(
        IReadOnlyList<Responsable> Responsables,
        IReadOnlyList<Application> Applications,
        IReadOnlyList<TypeIncident> TypesIncident,
        IReadOnlyList<Entite> Entites,
        IReadOnlyList<Criticite> Criticites,
        IReadOnlyList<Risque> Risques,
        IReadOnlyList<Statut> Statuts);
}
