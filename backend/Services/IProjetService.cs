using IncidentsDsi.Api.Common;
using IncidentsDsi.Api.DTOs;

namespace IncidentsDsi.Api.Services;

public interface IProjetService
{
    Task<PagedResult<ProjetDto>> GetPagedAsync(ProjetQueryParameters parameters, CancellationToken cancellationToken);
    Task<ProjetDto?> GetByIdAsync(int id, CancellationToken cancellationToken);
    Task<ProjetDto> CreateAsync(ProjetWriteDto input, CancellationToken cancellationToken);
    Task<ProjetDto?> UpdateAsync(int id, ProjetWriteDto input, CancellationToken cancellationToken);
}
