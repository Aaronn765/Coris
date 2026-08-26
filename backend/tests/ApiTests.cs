using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using IncidentsDsi.Api.DTOs;
using Xunit;

namespace IncidentsDsi.Api.Tests;

public sealed class ApiTests : IClassFixture<ApiWebApplicationFactory>
{
    private readonly HttpClient client;

    public ApiTests(ApiWebApplicationFactory factory)
    {
        factory.CreateSchema();
        client = factory.CreateClient();
    }

    [Fact]
    public async Task Referentiels_ExposeRealValuesFromHistoricalWorkbook()
    {
        var applications = await client.GetFromJsonAsync<List<ReferentielDto>>("/api/referentiels/applications");
        var responsables = await client.GetFromJsonAsync<List<ReferentielDto>>("/api/referentiels/responsables?includeInactive=true");
        var entites = await client.GetFromJsonAsync<List<ReferentielDto>>("/api/referentiels/entites");

        Assert.Contains(applications!, item => item.Nom == "AMPLITUDE");
        Assert.DoesNotContain(applications!, item => item.Nom == "CRM");
        Assert.Contains(responsables!, item => item.Nom == "ARSENE");
        Assert.DoesNotContain(responsables!, item => item.Nom == "Alice Dupont");
        Assert.Equal(20, entites!.Count);
    }

    [Fact]
    public async Task Referentiels_CanBeFetchedInOneRequest()
    {
        var references = await client.GetFromJsonAsync<ReferentielSetDto>("/api/referentiels");

        Assert.NotNull(references);
        Assert.Contains(references!.Applications, item => item.Nom == "AMPLITUDE");
        Assert.Contains(references.Statuts, item => item.Nom == "Cl\u00f4tur\u00e9");
        Assert.NotEmpty(references.Responsables);
    }

    [Fact]
    public async Task CreateIncident_GeneratesNumberAndCalculatedDuration()
    {
        var response = await client.PostAsJsonAsync("/api/incidents", NewIncident("CREATE-DURATION"));
        response.EnsureSuccessStatusCode();
        var incident = await response.Content.ReadFromJsonAsync<IncidentDto>();

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.Matches("^INC-2026-[0-9]{4}$", incident!.Numero);
        Assert.Equal(2 * 24 * 60, incident.DureeMinutes);
        Assert.Equal(2, incident.DureeJours);
        Assert.Equal("AMPLITUDE", incident.Application.Nom);
        Assert.Equal("ARSENE", incident.ResponsableN1.Nom);
    }

    [Fact]
    public async Task List_SearchFiltersSortsAndPaginates()
    {
        await CreateIncident("QUERY-ONE-AMPLITUDE", applicationId: 2, cause: "cause-query-one");
        await CreateIncident("QUERY-TWO-ABM", applicationId: 1, cause: "cause-query-two");

        var page = await client.GetFromJsonAsync<PagedResult<IncidentDto>>(
            "/api/incidents?search=QUERY-&page=1&pageSize=1&sortBy=numero&sortDirection=asc");
        var filtered = await client.GetFromJsonAsync<PagedResult<IncidentDto>>(
            "/api/incidents?applicationId=2&search=cause-query-one&pageSize=100");
        var durationFiltered = await client.GetFromJsonAsync<PagedResult<IncidentDto>>(
            "/api/incidents?search=QUERY-&dureeMinMinutes=2880&dureeMaxMinutes=2880&pageSize=100");

        Assert.NotNull(page);
        Assert.Equal(2, page!.TotalCount);
        Assert.Single(page.Items);
        Assert.Equal(2, page.TotalPages);
        Assert.Equal("QUERY-ONE-AMPLITUDE", page.Items[0].Intitule);
        Assert.Single(filtered!.Items);
        Assert.Equal("AMPLITUDE", filtered.Items[0].Application.Nom);
        Assert.Equal(2, durationFiltered!.TotalCount);
        Assert.All(durationFiltered.Items, item => Assert.Equal(2880, item.DureeMinutes));
    }

    [Fact]
    public async Task UpdateIncident_ChangesAllEditableFields()
    {
        var created = await CreateIncident("UPDATE-BEFORE");
        var input = NewIncident(
            "UPDATE-AFTER",
            dateFin: null,
            statusId: 2,
            description: "Description modifiee avec suffisamment de caracteres.");

        var response = await client.PutAsJsonAsync($"/api/incidents/{created.Id}", input);
        response.EnsureSuccessStatusCode();
        var updated = await response.Content.ReadFromJsonAsync<IncidentDto>();

        Assert.Equal(created.Numero, updated!.Numero);
        Assert.Equal("UPDATE-AFTER", updated.Intitule);
        Assert.Null(updated.DateFin);
        Assert.Null(updated.DureeJours);
        Assert.Equal("Cl\u00f4tur\u00e9", updated.Statut.Nom);
    }

    [Fact]
    public async Task Referentiel_CanBeCreatedAndSoftDeactivated()
    {
        var create = await client.PostAsJsonAsync("/api/referentiels/applications", new { nom = "APPLICATION-TEST", actif = true });
        create.EnsureSuccessStatusCode();
        var created = await create.Content.ReadFromJsonAsync<ReferentielDto>();

        var update = await client.PutAsJsonAsync($"/api/referentiels/applications/{created!.Id}", new { nom = created.Nom, actif = false });
        update.EnsureSuccessStatusCode();
        var activeOnly = await client.GetFromJsonAsync<List<ReferentielDto>>("/api/referentiels/applications");
        var all = await client.GetFromJsonAsync<List<ReferentielDto>>("/api/referentiels/applications?includeInactive=true");

        Assert.DoesNotContain(activeOnly!, item => item.Id == created.Id);
        Assert.Contains(all!, item => item.Id == created.Id && !item.Actif);
    }

    [Fact]
    public async Task Dashboard_ReturnsAggregatesAndEvolution()
    {
        await CreateIncident("DASHBOARD-CLOSED", statusId: 2);
        await CreateIncident("DASHBOARD-OPEN", statusId: 1);

        var stats = await client.GetFromJsonAsync<DashboardStatsDto>("/api/dashboard?dateDebut=2026-01-01&dateFin=2026-12-31");

        Assert.NotNull(stats);
        Assert.True(stats!.TotalIncidents >= 2);
        Assert.True(stats.IncidentsClotures >= 1);
        Assert.True(stats.IncidentsEnCours >= 1);
        Assert.Contains(stats.IncidentsParApplication, item => item.Nom == "AMPLITUDE");
        Assert.Contains(stats.Evolution, item => item.Annee == 2026 && item.Mois == 1);
    }

    [Fact]
    public async Task Export_ReturnsAnExcelWorkbookRespectingSearch()
    {
        await CreateIncident("EXPORT-UNIQUE");
        var response = await client.GetAsync("/api/incidents/export?search=EXPORT-UNIQUE");
        var content = await response.Content.ReadAsByteArrayAsync();

        response.EnsureSuccessStatusCode();
        Assert.Equal("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", response.Content.Headers.ContentType?.MediaType);
        Assert.True(content.Length > 1000);
        Assert.Equal((byte)'P', content[0]);
        Assert.Equal((byte)'K', content[1]);
        Assert.Contains(".xlsx", response.Content.Headers.ContentDisposition?.FileName ?? string.Empty);
    }

    [Fact]
    public async Task Validation_RejectsAnEndDateBeforeDeclaration()
    {
        var response = await client.PostAsJsonAsync("/api/incidents", NewIncident(
            "INVALID-DATES",
            dateDeclaration: "2026-02-02T00:00:00Z",
            dateFin: "2026-02-01T00:00:00Z"));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("date de fin", body, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Validation_RejectsDefaultDeclarationDate()
    {
        var response = await client.PostAsJsonAsync("/api/incidents", new
        {
            typeIncidentId = 2,
            applicationId = 2,
            intitule = "MISSING-DATE",
            description = "Description de test suffisamment detaillee.",
            entiteId = 12,
            criticiteId = 1,
            risqueId = 1,
            responsableN1Id = 1,
            statutId = 1
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Validation_RejectsInactiveReferenceForIncidentCreation()
    {
        var create = await client.PostAsJsonAsync(
            "/api/referentiels/applications",
            new { nom = $"INACTIVE-{Guid.NewGuid():N}", actif = false });
        create.EnsureSuccessStatusCode();
        var inactive = await create.Content.ReadFromJsonAsync<ReferentielDto>();

        var response = await client.PostAsJsonAsync(
            "/api/incidents",
            NewIncident("INACTIVE-REFERENCE", applicationId: inactive!.Id));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task QueryValidation_RejectsInvalidDateRange()
    {
        var response = await client.GetAsync("/api/incidents?dateDebut=2027-01-01&dateFin=2026-01-01");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task MissingResourcesReturnNotFound()
    {
        var response = await client.GetAsync("/api/incidents/999999");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    private async Task<IncidentDto> CreateIncident(
        string title,
        int applicationId = 2,
        string cause = "Cause de test",
        int statusId = 1)
    {
        var response = await client.PostAsJsonAsync("/api/incidents", NewIncident(title, applicationId: applicationId, cause: cause, statusId: statusId));
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<IncidentDto>())!;
    }

    private static IncidentWriteDto NewIncident(
        string title,
        int applicationId = 2,
        string cause = "Cause de test",
        string dateDeclaration = "2026-01-01T00:00:00Z",
        string? dateFin = "2026-01-03T00:00:00Z",
        int statusId = 1,
        string description = "Description de test suffisamment détaillée.") => new()
        {
            DateDeclaration = DateTime.Parse(dateDeclaration),
            DateFin = dateFin is null ? null : DateTime.Parse(dateFin),
            TypeIncidentId = 2,
            ApplicationId = applicationId,
            Intitule = title,
            Description = description,
            EntiteId = 12,
            CriticiteId = 1,
            Impact = "Impact de test",
            Cause = cause,
            RisqueId = 1,
            ActionsMenees = "Actions menees de test",
            Solution = "Solution de test",
            ActionsEnCours = "Actions en cours de test",
            ResponsableN1Id = 1,
            ResponsableN2Id = 3,
            MesuresPreventives = "Mesures preventives de test",
            StatutId = statusId
        };
}
