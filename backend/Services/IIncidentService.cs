using IncidentsDsi.Api.DTOs;

namespace IncidentsDsi.Api.Services;

public interface IIncidentService
{
    Task<PagedResult<IncidentDto>> GetPagedAsync(IncidentQueryParameters parameters, CancellationToken cancellationToken);
    Task<IncidentDto?> GetByIdAsync(int id, CancellationToken cancellationToken);
    Task<IncidentDto> CreateAsync(IncidentWriteDto input, CancellationToken cancellationToken);
    Task<IncidentDto?> UpdateAsync(int id, IncidentWriteDto input, CancellationToken cancellationToken);
    Task<IReadOnlyList<IncidentDto>> GetForExportAsync(IncidentQueryParameters parameters, CancellationToken cancellationToken);
}
