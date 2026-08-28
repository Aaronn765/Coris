using IncidentsDsi.Api.Common;
using IncidentsDsi.Api.DTOs;
using IncidentsDsi.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace IncidentsDsi.Api.Controllers;

[ApiController]
[Route("api/projets")]
public sealed class ProjetsController(IProjetService projets) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<PagedResult<ProjetDto>>> GetAll(
        [FromQuery] ProjetQueryParameters parameters,
        CancellationToken cancellationToken)
        => Ok(await projets.GetPagedAsync(parameters, cancellationToken));

    [HttpGet("{id:int}")]
    public async Task<ActionResult<ProjetDto>> GetById(int id, CancellationToken cancellationToken)
    {
        var project = await projets.GetByIdAsync(id, cancellationToken);
        return project is null ? NotFound() : Ok(project);
    }

    [HttpGet("export")]
    public async Task<IActionResult> Export(
        [FromQuery] ProjetQueryParameters parameters,
        CancellationToken cancellationToken)
    {
        var projects = await projets.GetForExportAsync(parameters, cancellationToken);
        return File(
            ProjetExcelDocument.Build(projects),
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            $"projets-{DateTime.UtcNow:yyyyMMddHHmmss}.xlsx");
    }

    [HttpGet("{id:int}/export")]
    public async Task<IActionResult> ExportPdf(int id, CancellationToken cancellationToken)
    {
        var project = await projets.GetByIdAsync(id, cancellationToken);
        if (project is null) return NotFound();

        return File(
            ProjetPdfDocument.Build(project),
            "application/pdf",
            $"{project.Numero}-fiche.pdf");
    }

    [HttpPost]
    public async Task<ActionResult<ProjetDto>> Create(ProjetWriteDto input, CancellationToken cancellationToken)
    {
        var project = await projets.CreateAsync(input, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = project.Id }, project);
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<ProjetDto>> Update(
        int id,
        ProjetWriteDto input,
        CancellationToken cancellationToken)
    {
        var project = await projets.UpdateAsync(id, input, cancellationToken);
        return project is null ? NotFound() : Ok(project);
    }
}
