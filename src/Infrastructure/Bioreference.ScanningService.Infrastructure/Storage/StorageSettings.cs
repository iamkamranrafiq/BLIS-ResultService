namespace Bioreference.ScanningService.Infrastructure.Storage;

/// <summary>
/// Configuration for the network share where scanned documents are stored.
/// Bound from the "StorageSettings" section of appsettings.json.
/// </summary>
public class StorageSettings
{
    public const string SectionName = "StorageSettings";

    /// <summary>
    /// Full UNC base path including the environment folder,
    /// e.g. \\bioreference-laboratories.com\san\BLIS_Scan_Docs\Dev
    /// </summary>
    public string BasePath { get; set; } = string.Empty;
    
    /// <summary>
    /// Path including the delete folder for documents,
    /// e.g. \RecycleBin OR \DeleteFolder OR \DeletedFiles
    /// </summary>
    public string DeletedDocumentFolder { get; set; } = string.Empty;

    /// <summary>
    /// Username used to authenticate to the network share.
    /// May be provided as "DOMAIN\\user" or plain "user" (with Domain set separately).
    /// </summary>
    public string? Username { get; set; }

    /// <summary>
    /// Password used to authenticate to the network share.
    /// </summary>
    public string? Password { get; set; }

    /// <summary>
    /// Optional domain for the credentials. Leave empty if the username already
    /// contains the domain (e.g. "DOMAIN\\user") or none is required.
    /// </summary>
    public string? Domain { get; set; }

    public string OnbaseRootPath { get; set; } = string.Empty;

    public string OnbaseBasePath { get; set; } = string.Empty;
}
