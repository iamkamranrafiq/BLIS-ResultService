namespace Bioreference.ScanningService.Infrastructure.Audit;

/// <summary>
/// Represents a single audit event. Maps to the AuditLog SQL table.
/// No EF configuration — persisted via raw SQL.
/// </summary>
public class AuditLogEntry
{
    public long AuditLogId { get; set; }
    public string ObjectName { get; set; } = null!;
    public string Identifier { get; set; } = null!;
    public string ColumnName { get; set; } = null!;
    public string? OldValue { get; set; }
    public string? NewValue { get; set; }
    public string? AuditMessage { get; set; }
    public string Action { get; set; } = null!;
    public string? UpdatedBy { get; set; }
    public DateTime ChangedAtUtc { get; set; }
}
