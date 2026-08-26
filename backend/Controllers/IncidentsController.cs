using ClosedXML.Excel;
using IncidentsDsi.Api.DTOs;
using IncidentsDsi.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace IncidentsDsi.Api.Controllers;

[ApiController]
[Route("api/incidents")]
public sealed class IncidentsController(IIncidentService incidents) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<PagedResult<IncidentDto>>> GetAll(
        [FromQuery] IncidentQueryParameters parameters,
        CancellationToken cancellationToken)
        => Ok(await incidents.GetPagedAsync(parameters, cancellationToken));

    [HttpGet("{id:int}")]
    public async Task<ActionResult<IncidentDto>> GetById(int id, CancellationToken cancellationToken)
    {
        var incident = await incidents.GetByIdAsync(id, cancellationToken);
        return incident is null ? NotFound() : Ok(incident);
    }

    [HttpPost]
    public async Task<ActionResult<IncidentDto>> Create(IncidentWriteDto input, CancellationToken cancellationToken)
    {
        var incident = await incidents.CreateAsync(input, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = incident.Id }, incident);
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<IncidentDto>> Update(
        int id,
        IncidentWriteDto input,
        CancellationToken cancellationToken)
    {
        var incident = await incidents.UpdateAsync(id, input, cancellationToken);
        return incident is null ? NotFound() : Ok(incident);
    }

    [HttpGet("export")]
    public async Task<IActionResult> Export(
        [FromQuery] IncidentQueryParameters parameters,
        CancellationToken cancellationToken)
    {
        var incidentsToExport = await incidents.GetForExportAsync(parameters, cancellationToken);
        using var workbook = new XLWorkbook();
        var worksheet = workbook.Worksheets.Add("Incidents");
        var headers = new[]
        {
            "N°", "DATE DECLARATION", "DATE FIN", "DUREE (JOURS)", "TYPE INCIDENT", "APPLICATIF",
            "INTITULE INCIDENT", "DESCRIPTION", "PERIMETRE / ENTITE", "CRITICITE", "IMPACT", "CAUSE INCIDENT",
            "RISQUE POUR LA BANQUE", "ACTIONS MENEES", "SOLUTIONS", "ACTIONS EN COURS", "RESPONSABLE N1",
            "RESPONSABLE N2", "MESURES PREVENTIVES", "STATUT", "CREATED AT", "UPDATED AT"
        };

        for (var column = 0; column < headers.Length; column++)
        {
            var cell = worksheet.Cell(1, column + 1);
            cell.Value = headers[column];
            cell.Style.Font.Bold = true;
            cell.Style.Fill.BackgroundColor = XLColor.FromHtml("#E8EEF7");
        }

        for (var index = 0; index < incidentsToExport.Count; index++)
        {
            var incident = incidentsToExport[index];
            var row = index + 2;
            var values = new object?[]
            {
                incident.Numero, incident.DateDeclaration, incident.DateFin, incident.DureeJours,
                incident.TypeIncident.Nom, incident.Application.Nom, incident.Intitule, incident.Description,
                incident.Entite.Nom, incident.Criticite.Nom, incident.Impact, incident.Cause, incident.Risque.Nom,
                incident.ActionsMenees, incident.Solution, incident.ActionsEnCours, incident.ResponsableN1.Nom,
                incident.ResponsableN2?.Nom, incident.MesuresPreventives, incident.Statut.Nom,
                incident.CreatedAt, incident.UpdatedAt
            };

            for (var column = 0; column < values.Length; column++)
                worksheet.Cell(row, column + 1).Value = values[column]?.ToString() ?? string.Empty;
        }

        worksheet.SheetView.FreezeRows(1);
        worksheet.Columns().AdjustToContents();
        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return File(
            stream.ToArray(),
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            $"incidents-{DateTime.UtcNow:yyyyMMddHHmmss}.xlsx");
    }
}
