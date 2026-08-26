using ClosedXML.Excel;
using IncidentsDsi.Api.Data;
using IncidentsDsi.Api.Services;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace IncidentsDsi.Api.Tests;

public sealed class ExcelImportTests
{
    [Fact]
    public async Task HistoricalWorkbook_IsImportedWithoutMockIncidents()
    {
        var workbookPath = FindHistoricalWorkbook();
        Assert.NotNull(workbookPath);

        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(connection)
            .Options;

        await using var db = new AppDbContext(options);
        await db.Database.EnsureCreatedAsync();
        var importer = new ExcelImportService(db, NullLogger<ExcelImportService>.Instance);

        var imported = await importer.ImportIfEmptyAsync(workbookPath!, CancellationToken.None);

        Assert.True(imported >= 40, $"{imported} lignes importées au lieu d'au moins 40.");
        Assert.Equal(imported, await db.Incidents.CountAsync());
        Assert.Contains(await db.Incidents.ToListAsync(), incident => incident.Numero.StartsWith("INC-", StringComparison.Ordinal));
        Assert.All(await db.Incidents.ToListAsync(), incident => Assert.NotEmpty(incident.Description));
    }

    [Fact]
    public async Task ProjectWorkbook_IsImportedWithSeparateSteps()
    {
        var workbookPath = FindProjectWorkbook();
        Assert.NotNull(workbookPath);

        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(connection)
            .Options;

        await using var db = new AppDbContext(options);
        await db.Database.EnsureCreatedAsync();
        var importer = new ExcelImportService(db, NullLogger<ExcelImportService>.Instance);

        var imported = await importer.ImportProjectsIfEmptyAsync(workbookPath!, CancellationToken.None);

        Assert.True(imported >= 30, $"{imported} projets importés au lieu d'au moins 30.");
        Assert.Equal(imported, await db.Projets.CountAsync());
        Assert.True(await db.EtapesProjet.CountAsync() >= imported);
        Assert.Contains(await db.Projets.Include(project => project.Etapes).ToListAsync(), project => project.Nom.Contains("SD-WAN", StringComparison.OrdinalIgnoreCase) && project.Etapes.Count > 0);
    }

    private static string? FindHistoricalWorkbook()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            var file = directory.GetFiles("*.xlsx").FirstOrDefault();
            if (file is not null) return file.FullName;
            directory = directory.Parent;
        }

        return null;
    }

    private static string? FindProjectWorkbook()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            var file = directory.GetFiles("SUIVI PROJETS DSI*.xlsx").FirstOrDefault();
            if (file is not null) return file.FullName;
            directory = directory.Parent;
        }

        return null;
    }
}
