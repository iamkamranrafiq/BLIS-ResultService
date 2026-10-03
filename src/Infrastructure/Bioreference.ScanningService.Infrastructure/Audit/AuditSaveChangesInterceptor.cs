using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Options;
using Bioreference.ScanningService.Application.Common.Interfaces;

namespace Bioreference.ScanningService.Infrastructure.Audit;

/// <summary>
/// EF Core SaveChanges interceptor that captures field-level changes for configured tables
/// and pushes them to the <see cref="AuditLogChannel"/> for async background persistence.
/// </summary>
public sealed class AuditSaveChangesInterceptor : SaveChangesInterceptor
{
    private readonly AuditLogChannel _channel;
    private readonly IRequestUserContext _userContext;
    private readonly HashSet<string> _auditedTables;
    private readonly HashSet<string> _excludedColumns;

    public AuditSaveChangesInterceptor(
        AuditLogChannel channel,
        IOptions<AuditLogSettings> settings,
        IRequestUserContext userContext)
    {
        _channel = channel;
        _userContext = userContext;
        _auditedTables = new HashSet<string>(settings.Value.AuditedTables, StringComparer.OrdinalIgnoreCase);
        _excludedColumns = new HashSet<string>(settings.Value.ExcludedColumns, StringComparer.OrdinalIgnoreCase);
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        if (eventData.Context is not null)
        {
            var entries = CaptureChanges(eventData.Context.ChangeTracker);
            if (entries.Count > 0)
                _channel.Writer.TryWrite(entries);
        }
        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    private List<AuditLogEntry> CaptureChanges(ChangeTracker changeTracker)
    {
        changeTracker.DetectChanges();
        var auditEntries = new List<AuditLogEntry>();

        foreach (var entry in changeTracker.Entries())
        {
            if (entry.State is EntityState.Detached or EntityState.Unchanged)
                continue;

            var tableName = entry.Metadata.GetTableName() ?? entry.Metadata.ClrType.Name;
            if (!_auditedTables.Contains(tableName))
                continue;

            var objectName = entry.Metadata.ClrType.FullName ?? entry.Metadata.ClrType.Name;
            var primaryKey = GetPrimaryKey(entry);
            var changedBy = GetChangedBy(entry);
            var now = DateTime.UtcNow;

            foreach (var prop in entry.Properties)
            {
                if (prop.IsTemporary) continue;
                var columnName = prop.Metadata.GetColumnName();
                if (_excludedColumns.Contains(columnName)) continue;

                switch (entry.State)
                {
                    case EntityState.Added:
                        auditEntries.Add(new AuditLogEntry
                        {
                            ObjectName = objectName,
                            Identifier = primaryKey,
                            ColumnName = columnName,
                            NewValue = prop.CurrentValue?.ToString(),
                            Action = "Insert",
                            UpdatedBy = changedBy,
                            ChangedAtUtc = now
                        });
                        break;

                    case EntityState.Deleted:
                        auditEntries.Add(new AuditLogEntry
                        {
                            ObjectName = objectName,
                            Identifier = primaryKey,
                            ColumnName = columnName,
                            OldValue = prop.OriginalValue?.ToString(),
                            Action = "Delete",
                            UpdatedBy = changedBy,
                            ChangedAtUtc = now
                        });
                        break;

                    case EntityState.Modified when prop.IsModified && !Equals(prop.OriginalValue, prop.CurrentValue):
                        auditEntries.Add(new AuditLogEntry
                        {
                            ObjectName = objectName,
                            Identifier = primaryKey,
                            ColumnName = columnName,
                            OldValue = prop.OriginalValue?.ToString(),
                            NewValue = prop.CurrentValue?.ToString(),
                            Action = "Update",
                            UpdatedBy = changedBy,
                            ChangedAtUtc = now
                        });
                        break;
                }
            }
        }
        return auditEntries;
    }

    private static string GetPrimaryKey(EntityEntry entry)
    {
        var keyValues = entry.Metadata.FindPrimaryKey()!
            .Properties.Select(p => entry.Property(p.Name).CurrentValue?.ToString() ?? "");
        return string.Join(",", keyValues);
    }

    private string? GetChangedBy(EntityEntry entry)
    {
        var updatedBy = entry.Properties.FirstOrDefault(p =>
            p.Metadata.Name.Equals("UpdatedBy", StringComparison.OrdinalIgnoreCase));
        if (updatedBy?.CurrentValue is string val && !string.IsNullOrEmpty(val))
            return val;

        var createdBy = entry.Properties.FirstOrDefault(p =>
            p.Metadata.Name.Equals("CreatedBy", StringComparison.OrdinalIgnoreCase));
        return createdBy?.CurrentValue as string ?? _userContext.GetUserName();
    }
}
