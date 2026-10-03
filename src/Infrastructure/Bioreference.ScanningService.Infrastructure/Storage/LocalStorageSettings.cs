namespace Bioreference.ScanningService.Infrastructure.Storage;

/// <summary>
/// Configuration for local (non-network) document storage used in test mode.
/// Bound from the "LocalStorageSettings" section of appsettings.json.
/// When <see cref="Test"/> is true, <see cref="BasePath"/> is used instead of the
/// network share configured in <see cref="StorageSettings"/>.
/// </summary>
public class LocalStorageSettings
{
    public const string SectionName = "LocalStorageSettings";

    /// <summary>
    /// When true, documents are saved to the local <see cref="BasePath"/> and
    /// no network share authentication is performed.
    /// </summary>
    public bool Test { get; set; }

    /// <summary>
    /// Local filesystem base path used when <see cref="Test"/> is true,
    /// e.g. D:\Scanning
    /// </summary>
    public string BasePath { get; set; } = string.Empty;
}
