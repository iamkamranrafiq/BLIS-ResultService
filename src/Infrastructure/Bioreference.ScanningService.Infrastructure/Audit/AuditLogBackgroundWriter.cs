using System.Threading.Channels;
using Bioreference.ScanningService.Application.Common.Interfaces;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Bioreference.ScanningService.Infrastructure.Audit;

/// <summary>
/// In-memory channel that buffers audit entries for background writing.
/// </summary>
public sealed class AuditLogChannel
{
    private readonly Channel<List<AuditLogEntry>> _channel =
        Channel.CreateBounded<List<AuditLogEntry>>(new BoundedChannelOptions(1024)
        {
            FullMode = BoundedChannelFullMode.DropOldest,
            SingleReader = true
        });

    public ChannelWriter<List<AuditLogEntry>> Writer => _channel.Writer;
    public ChannelReader<List<AuditLogEntry>> Reader => _channel.Reader;
}

public sealed class AuditLogService : IAuditLogService
{
    private readonly AuditLogChannel _channel;
    private readonly IRequestUserContext _userContext;

    public AuditLogService(AuditLogChannel channel, IRequestUserContext userContext)
    {
        _channel = channel;
        _userContext = userContext;
    }



    public ValueTask LogActivityAsync(
        object caller,
        string identifier,
        string auditMessage)
    {


        string? updatedBy = _userContext.GetUserName();
        _channel.Writer.TryWrite(
        [
            new AuditLogEntry
            {
                ObjectName = caller.GetType().FullName ?? caller.GetType().Name,
                Identifier = identifier,
                ColumnName = "Activity",
                AuditMessage = auditMessage,
                Action = "Activity",
                UpdatedBy = updatedBy,
                ChangedAtUtc = DateTime.UtcNow
            }
        ]);

        return ValueTask.CompletedTask;
    }
}

/// <summary>
/// Background service that reads audit entries from the channel and bulk-inserts them
/// into the AuditLog table using raw SQL for maximum throughput.
/// </summary>
public sealed class AuditLogBackgroundWriter : BackgroundService
{
    private readonly AuditLogChannel _channel;
    private readonly string _connectionString;
    private readonly ILogger<AuditLogBackgroundWriter> _logger;
    private readonly IRequestUserContext _userContext;


    public AuditLogBackgroundWriter(
        AuditLogChannel channel,
        IOptions<AuditLogSettings> settings,
        IConfiguration configuration,
        ILogger<AuditLogBackgroundWriter> logger,
        IRequestUserContext userContext)
    {
        _channel = channel;
        _logger = logger;
        _connectionString = ResolveConnectionString(settings.Value, configuration);
        _userContext = userContext;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("AuditLog background writer started.");
        await foreach (var batch in _channel.Reader.ReadAllAsync(stoppingToken))
        {
            try
            {
                await InsertBatchAsync(batch, stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to write {Count} audit entries.", batch.Count);
            }
        }
    }

    private async Task InsertBatchAsync(List<AuditLogEntry> entries, CancellationToken ct)
    {
        if (entries.Count == 0) return;

        const string sql = """
            INSERT INTO AuditLog (ObjectName, Identifier, ColumnName, OldValue, NewValue, AuditMessage, Action, UpdatedBy, ChangedAtUtc)
            VALUES (@ObjectName, @Identifier, @ColumnName, @OldValue, @NewValue, @AuditMessage, @Action, @UpdatedBy, @ChangedAtUtc)
            """;

        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(ct);
        await using var transaction = connection.BeginTransaction();

        foreach (var entry in entries)
        {
            await using var cmd = new SqlCommand(sql, connection, transaction);
            cmd.Parameters.AddWithValue("@ObjectName", entry.ObjectName);
            cmd.Parameters.AddWithValue("@Identifier", entry.Identifier);
            cmd.Parameters.AddWithValue("@ColumnName", entry.ColumnName);
            cmd.Parameters.AddWithValue("@OldValue", (object?)entry.OldValue ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@NewValue", (object?)entry.NewValue ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@AuditMessage", (object?)entry.AuditMessage ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@Action", entry.Action);
            cmd.Parameters.AddWithValue("@UpdatedBy", (object?)_userContext.GetUserName() ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@ChangedAtUtc", entry.ChangedAtUtc);
            await cmd.ExecuteNonQueryAsync(ct);
        }

        await transaction.CommitAsync(ct);
        _logger.LogDebug("Wrote {Count} audit log entries.", entries.Count);
    }

    private static string ResolveConnectionString(AuditLogSettings settings, IConfiguration config)
    {
        var name = !string.IsNullOrEmpty(settings.ConnectionStringName)
            ? settings.ConnectionStringName
            : DependencyInjection.ConnectionStringName;
        return config.GetConnectionString(name)
            ?? throw new InvalidOperationException($"Connection string '{name}' not found for AuditLog.");
    }
}
