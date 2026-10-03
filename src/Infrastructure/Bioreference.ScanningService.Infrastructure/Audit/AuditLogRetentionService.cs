using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Bioreference.ScanningService.Infrastructure.Audit;

/// <summary>
/// Periodic background service that purges audit log records older than the configured retention period.
/// </summary>
public sealed class AuditLogRetentionService : BackgroundService
{
    private readonly string _connectionString;
    private readonly int _retentionDays;
    private readonly ILogger<AuditLogRetentionService> _logger;

    public AuditLogRetentionService(
        IOptions<AuditLogSettings> settings,
        IConfiguration configuration,
        ILogger<AuditLogRetentionService> logger)
    {
        _retentionDays = settings.Value.RetentionDays;
        _logger = logger;
        _connectionString = ResolveConnectionString(settings.Value, configuration);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await PurgeAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Audit log retention purge failed.");
            }
            await Task.Delay(TimeSpan.FromHours(24), stoppingToken);
        }
    }

    private async Task PurgeAsync(CancellationToken ct)
    {
        await using var conn = new SqlConnection(_connectionString);
        await conn.OpenAsync(ct);

        // Delete in batches to avoid long locks on large tables.
        var totalDeleted = 0;
        while (true)
        {
            await using var cmd = new SqlCommand(
                "DELETE TOP (10000) FROM AuditLog WHERE ChangedAtUtc < @Cutoff", conn);
            cmd.Parameters.AddWithValue("@Cutoff", DateTime.UtcNow.AddDays(-_retentionDays));
            var deleted = await cmd.ExecuteNonQueryAsync(ct);
            totalDeleted += deleted;
            if (deleted < 10000) break;
        }

        if (totalDeleted > 0)
            _logger.LogInformation("Audit retention: purged {Count} records older than {Days} days.", totalDeleted, _retentionDays);
    }

    private static string ResolveConnectionString(AuditLogSettings settings, IConfiguration config)
    {
        var name = !string.IsNullOrEmpty(settings.ConnectionStringName)
            ? settings.ConnectionStringName
            : DependencyInjection.ConnectionStringName;
        return config.GetConnectionString(name)!;
    }
}
