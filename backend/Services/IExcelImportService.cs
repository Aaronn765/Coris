namespace IncidentsDsi.Api.Services;

public interface IExcelImportService
{
    Task<int> ImportIfEmptyAsync(string path, CancellationToken cancellationToken);
    Task<int> ImportProjectsIfEmptyAsync(string path, CancellationToken cancellationToken);
}
