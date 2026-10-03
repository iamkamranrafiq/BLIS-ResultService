using Bioreference.ScanningService.Application.Common.Interfaces;
using Bioreference.ScanningService.Application.Common.Settings;
using Bioreference.ScanningService.Infrastructure.Audit;
using Bioreference.ScanningService.Infrastructure.Persistence;
using Bioreference.ScanningService.Infrastructure.Persistence.Repositories;
using Bioreference.ScanningService.Infrastructure.Storage;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Bioreference.ScanningService.Infrastructure;

public static class DependencyInjection
{
    public const string ConnectionStringName = "Scanning_CONSTR";

    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString(ConnectionStringName);

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException($"Connection string '{ConnectionStringName}' is required but not found in configuration.");
        }

        var builder = new SqlConnectionStringBuilder(connectionString);
        Serilog.Log.Information(
            "[Infrastructure] Using real DB. Server='{Server}', Catalog='{Catalog}', User='{User}'.",
            builder.DataSource,
            builder.InitialCatalog,
            builder.IntegratedSecurity ? "Integrated" : builder.UserID);

        // ── Audit Log ─────────────────────────────────────────────────────────────
        services.Configure<AuditLogSettings>(configuration.GetSection(AuditLogSettings.SectionName));
        services.AddSingleton<AuditLogChannel>();
        services.AddScoped<IAuditLogService, AuditLogService>();
        services.AddSingleton<AuditSaveChangesInterceptor>();
        services.AddHostedService<AuditLogBackgroundWriter>();
        services.AddHostedService<AuditLogRetentionService>();

        services.AddDbContext<ScanningServiceDbContext>((sp, options) =>
            options.UseSqlServer(connectionString, sql =>
                sql.MigrationsAssembly(typeof(ScanningServiceDbContext).Assembly.FullName)
                   .UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery))
            .AddInterceptors(sp.GetRequiredService<AuditSaveChangesInterceptor>()));

        services.AddMemoryCache();

        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddScoped<IBatchRepository, BatchRepository>();
        services.AddScoped<IDocumentRepository, DocumentRepository>();
        services.AddScoped<IRequisitionImageRepository, RequisitionImageRepository>();

        services.Configure<OnBaseConfiguration>(settings =>
        {
            var section = configuration.GetSection(OnBaseConfiguration.DataSource);
            if (DateOnly.TryParse(section["CutoverDate"], out var cutoverDate))
            {
                settings.CutoverDate = cutoverDate;
            }
        });

        services.Configure<StorageSettings>(settings =>
        {
            var section = configuration.GetSection(StorageSettings.SectionName);
            settings.BasePath = section["BasePath"] ?? string.Empty;
            settings.DeletedDocumentFolder = section["DocumentDeleteFolder"] ?? string.Empty;
            settings.Username = section["Username"];
            settings.Password = section["Password"];
            settings.Domain = section["Domain"];
            settings.OnbaseBasePath = section["OnbaseBasePath"] ?? string.Empty;
            settings.OnbaseRootPath = section["OnbaseRootPath"] ?? string.Empty;
        });

        services.Configure<LocalStorageSettings>(settings =>
        {
            var section = configuration.GetSection(LocalStorageSettings.SectionName);
            settings.Test = bool.TryParse(section["Test"], out var test) && test;
            settings.BasePath = section["BasePath"] ?? string.Empty;
        });
        services.AddSingleton<INetworkShareConnector, NetworkShareConnector>();
        services.AddSingleton<IFileStorage, FileNetworkStorage>();
        services.AddScoped<IOnBaseRepository, OnBaseRepository>();
        services.AddScoped<IScanQueueRepository, ScanQueueRepository>();

        return services;
    }

    public static IServiceCollection AddInfrastructureHealthChecks(this IServiceCollection services, IConfiguration configuration)
    {
        var healthChecksBuilder = services.AddHealthChecks()
            .AddCheck(
                "self",
                () => HealthCheckResult.Healthy("Application is running"),
                tags: ["live"]);

        var connectionString = configuration.GetConnectionString(ConnectionStringName);
        if (!string.IsNullOrWhiteSpace(connectionString))
        {
            healthChecksBuilder.AddDbContextCheck<ScanningServiceDbContext>(
                name: "scanning-database",
                failureStatus: HealthStatus.Unhealthy,
                tags: ["ready", "db"]);
        }

        return services;
    }
}
