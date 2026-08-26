using System.Net.Http.Json;
using IncidentsDsi.Api.DTOs;
using Xunit;

namespace IncidentsDsi.Api.Tests;

public sealed class ProjetTests : IClassFixture<ApiWebApplicationFactory>
{
    private readonly HttpClient client;

    public ProjetTests(ApiWebApplicationFactory factory)
    {
        factory.CreateSchema();
        client = factory.CreateClient();
    }

    [Fact]
    public async Task CreateProjectKeepsMultipleResponsibilitiesAndSteps()
    {
        var response = await client.PostAsJsonAsync("/api/projets", new
        {
            domaineProjetId = 4,
            nom = "Projet SD-WAN de test",
            description = "Déploiement de boîtiers et de switchs pour les agences.",
            tauxAvancement = 0.3m,
            dateEcheance = "2026-12-31T00:00:00Z",
            statutProjetId = 2,
            responsableIds = new[] { 8, 9 },
            etapes = new[]
            {
                new { nom = "Acquisition des équipements", tauxAvancement = 1m, statutEtapeId = 3, ordre = 0 },
                new { nom = "Configuration et installation", tauxAvancement = 0.4m, statutEtapeId = 2, ordre = 1 }
            }
        });

        response.EnsureSuccessStatusCode();
        var project = await response.Content.ReadFromJsonAsync<ProjetDto>();

        Assert.NotNull(project);
        Assert.Matches("^PRJ-2026-[0-9]{4}$", project!.Numero);
        Assert.Equal(2, project.Responsables.Count);
        Assert.Equal(2, project.Etapes.Count);
        Assert.Equal("LEGRE", project.Responsables[0].Nom);
        Assert.Equal("Terminée", project.Etapes[0].StatutEtape.Nom);

        var dashboard = await client.GetFromJsonAsync<ProjectDashboardStatsDto>("/api/dashboard/projets");
        Assert.NotNull(dashboard);
        Assert.True(dashboard!.TotalProjets >= 1);
        Assert.True(dashboard.ProjetsEnCours >= 1);
        Assert.Contains(dashboard.ProjetsParDomaine, item => item.Nom == "INFRA RESEAU & SYSTÈME");
    }
}
