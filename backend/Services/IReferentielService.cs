using IncidentsDsi.Api.DTOs;

namespace IncidentsDsi.Api.Services;

public interface IReferentielService
{
    Task<ReferentielSetDto> GetAllAsync(bool includeInactive, CancellationToken cancellationToken);
    Task<IReadOnlyList<ReferentielDto>> GetAsync(string type, bool includeInactive, CancellationToken cancellationToken);
    Task<ReferentielDto?> GetByIdAsync(string type, int id, CancellationToken cancellationToken);
    Task<ReferentielDto> CreateAsync(string type, ReferentielWriteDto input, CancellationToken cancellationToken);
    Task<ReferentielDto?> UpdateAsync(string type, int id, ReferentielWriteDto input, CancellationToken cancellationToken);
}
