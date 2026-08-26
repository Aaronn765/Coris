using IncidentsDsi.Api.DTOs;
using IncidentsDsi.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace IncidentsDsi.Api.Controllers;

[ApiController]
[Route("api/referentiels")]
public sealed class ReferentielsController(IReferentielService referentiels) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<ReferentielSetDto>> GetAll(
        [FromQuery] bool includeInactive = false,
        CancellationToken cancellationToken = default)
        => Ok(await referentiels.GetAllAsync(includeInactive, cancellationToken));

    [HttpGet("{type}")]
    public async Task<ActionResult<IReadOnlyList<ReferentielDto>>> GetAll(
        string type,
        [FromQuery] bool includeInactive = false,
        CancellationToken cancellationToken = default)
        => Ok(await referentiels.GetAsync(type, includeInactive, cancellationToken));

    [HttpGet("{type}/{id:int}")]
    public async Task<ActionResult<ReferentielDto>> GetById(
        string type,
        int id,
        CancellationToken cancellationToken)
    {
        var value = await referentiels.GetByIdAsync(type, id, cancellationToken);
        return value is null ? NotFound() : Ok(value);
    }

    [HttpPost("{type}")]
    public async Task<ActionResult<ReferentielDto>> Create(
        string type,
        ReferentielWriteDto input,
        CancellationToken cancellationToken)
    {
        var value = await referentiels.CreateAsync(type, input, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { type, id = value.Id }, value);
    }

    [HttpPut("{type}/{id:int}")]
    public async Task<ActionResult<ReferentielDto>> Update(
        string type,
        int id,
        ReferentielWriteDto input,
        CancellationToken cancellationToken)
    {
        var value = await referentiels.UpdateAsync(type, id, input, cancellationToken);
        return value is null ? NotFound() : Ok(value);
    }
}
