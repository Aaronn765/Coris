using IncidentsDsi.Api.Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace IncidentsDsi.Api.Tests;

public sealed class ApiWebApplicationFactory : WebApplicationFactory<Program>
{
    private SqliteConnection? connection;

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.ConfigureAppConfiguration((_, configuration) =>
        {
            configuration.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Database:ApplyMigrations"] = "false",
                ["Data:ImportExcelOnStartup"] = "false",
                ["Swagger:Enabled"] = "false",
                ["Cors:AllowedOrigins:0"] = "http://localhost:3000"
            });
        });

        builder.ConfigureServices(services =>
        {
            services.RemoveAll<DbContextOptions<AppDbContext>>();
            services.RemoveAll<DbContextOptions>();
            services.RemoveAll<IDbContextOptionsConfiguration<AppDbContext>>();
            connection = new SqliteConnection("Data Source=:memory:");
            connection.Open();
            services.AddDbContext<AppDbContext>(options => options.UseSqlite(connection));
        });
    }

    public void CreateSchema()
    {
        using var scope = Services.CreateScope();
        scope.ServiceProvider.GetRequiredService<AppDbContext>().Database.EnsureCreated();
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        connection?.Dispose();
    }
}
