using System.Text.Json.Serialization;
using IncidentsDsi.Api.Common;
using IncidentsDsi.Api.Data;
using IncidentsDsi.Api.Services;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
        options.JsonSerializerOptions.DictionaryKeyPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
        options.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
    });

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("La chaîne de connexion 'DefaultConnection' est absente.");

builder.Services.AddDbContext<AppDbContext>(options => options.UseSqlServer(connectionString));
builder.Services.AddScoped<IIncidentService, IncidentService>();
builder.Services.AddScoped<IReferentielService, ReferentielService>();
builder.Services.AddScoped<IDashboardService, DashboardService>();
builder.Services.AddScoped<IProjetService, ProjetService>();
builder.Services.AddScoped<IProjetDashboardService, ProjetDashboardService>();
builder.Services.AddScoped<IExcelImportService, ExcelImportService>();

var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
    ?? ["http://localhost:3000"];

builder.Services.AddCors(options =>
{
    options.AddPolicy("Frontend", policy => policy
        .WithOrigins(allowedOrigins)
        .AllowAnyHeader()
        .AllowAnyMethod());
});

var app = builder.Build();

app.UseMiddleware<ExceptionHandlingMiddleware>();

if (app.Environment.IsDevelopment() || app.Configuration.GetValue<bool>("Swagger:Enabled"))
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

if (!app.Environment.IsEnvironment("Testing")
    && app.Configuration.GetValue("HttpsRedirection:Enabled", true))
{
    app.UseHttpsRedirection();
}
app.UseCors("Frontend");
app.MapControllers();

if (app.Configuration.GetValue("Database:ApplyMigrations", true))
{
    await using var scope = app.Services.CreateAsyncScope();
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await db.Database.MigrateAsync();

    if (app.Configuration.GetValue("Data:ImportExcelOnStartup", false))
    {
        var configuredPath = app.Configuration["Data:ExcelPath"];
        var path = string.IsNullOrWhiteSpace(configuredPath)
            ? null
            : Path.IsPathRooted(configuredPath)
                ? configuredPath
                : Path.GetFullPath(Path.Combine(app.Environment.ContentRootPath, configuredPath));

        if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
        {
            var directories = new[]
            {
                app.Environment.ContentRootPath,
                Path.GetFullPath(Path.Combine(app.Environment.ContentRootPath, ".."))
            };
            path = directories
                .Where(Directory.Exists)
                .SelectMany(directory => Directory.GetFiles(directory, "*.xlsx"))
                .FirstOrDefault();
        }

        if (!string.IsNullOrWhiteSpace(path))
        {
            await scope.ServiceProvider.GetRequiredService<IExcelImportService>()
                .ImportIfEmptyAsync(path, CancellationToken.None);
            await scope.ServiceProvider.GetRequiredService<IExcelImportService>()
                .ImportProjectsIfEmptyAsync(path, CancellationToken.None);
        }
    }

    await scope.ServiceProvider.GetRequiredService<IProjetService>()
        .NormalizeStartedStatusesAsync(CancellationToken.None);
}

await app.RunAsync();

public partial class Program;
