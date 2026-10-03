namespace Bioreference.ScanningService.Infrastructure.Audit;

/// <summary>
/// Configuration for the audit log system. Bound from appsettings "AuditLog" section.
/// </summary>
public class AuditLogSettings
{
    public const string SectionName = "AuditLog";

    /// <summary>Tables to audit. If empty, no tables are audited.</summary>
    public List<string> AuditedTables { get; set; } = [];

    /// <summary>Column names to exclude across all tables (e.g., binary columns).</summary>
    public List<string> ExcludedColumns { get; set; } = [];

    /// <summary>Retention period in days. Default: 365.</summary>
    public int RetentionDays { get; set; } = 365;

    /// <summary>Connection string name for audit DB. If null, uses main DB.</summary>
    public string? ConnectionStringName { get; set; }
}
