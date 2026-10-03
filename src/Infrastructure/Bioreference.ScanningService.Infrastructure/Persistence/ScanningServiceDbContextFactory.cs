using Bioreference.ScanningService.Infrastructure.Security;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace Bioreference.ScanningService.Infrastructure.Persistence;

/// <summary>
/// Used by EF Core tooling (e.g. <c>dotnet ef migrations add</c>) to construct the
/// <see cref="ScanningServiceDbContext"/> at design time. Reads the connection string
/// from the WebAPI project's appsettings.json so migrations stay in sync with runtime.
/// </summary>
public class ScanningServiceDbContextFactory : IDesignTimeDbContextFactory<ScanningServiceDbContext>
{
    public ScanningServiceDbContext CreateDbContext(string[] args)
    {
        var basePath = ResolveWebApiPath();

        var configuration = new ConfigurationBuilder()
            .SetBasePath(basePath)
            .AddJsonFile("appsettings.json", optional: false)
            .AddJsonFile("appsettings.Development.json", optional: true)
            .AddEnvironmentVariables()
            .AddDecryptedSecretsFromBuilder()
            .Build();

        var connectionString = configuration.GetConnectionString("Scanning_CONSTR")
            ?? throw new InvalidOperationException(
                "Connection string 'Scanning_CONSTR' was not found in appsettings.json.");

        var options = new DbContextOptionsBuilder<ScanningServiceDbContext>()
            .UseSqlServer(connectionString, sql => sql.MigrationsAssembly(typeof(ScanningServiceDbContext).Assembly.FullName))
            .Options;

        return new ScanningServiceDbContext(options);
    }

    private static string ResolveWebApiPath()
    {
        var current = new DirectoryInfo(Directory.GetCurrentDirectory());

        while (current is not null)
        {
            var webApi = Path.Combine(current.FullName, "src", "Presentation", "Bioreference.ScanningService.WebAPI");
            if (Directory.Exists(webApi))
            {
                return webApi;
            }

            current = current.Parent;
        }

        // Fallback: current directory (works when running `dotnet ef` from the WebAPI folder directly).
        return Directory.GetCurrentDirectory();
    }
}
