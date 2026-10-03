namespace Bioreference.ScanningService.WebAPI.Settings;

/// <summary>
/// Azure AD (Entra ID) settings used to validate the JWT access tokens that the
/// Angular BLIS UI already attaches to its requests (via MSAL). These values mirror
/// the Angular environment's <c>azureAd</c> configuration and the WinService settings.
/// </summary>
public class AzureAdSettings
{
    public const string SectionName = "AzureAd";

    /// <summary>Base login instance, e.g. https://login.microsoftonline.com/.</summary>
    public string Instance { get; set; } = "https://login.microsoftonline.com/";

    /// <summary>Directory (tenant) id.</summary>
    public string TenantId { get; set; } = string.Empty;

    /// <summary>Application (client) id of the API/app the tokens are issued for.</summary>
    public string ClientId { get; set; } = string.Empty;

    /// <summary>
    /// Optional audience to validate against. When empty, audience validation is skipped
    /// (we only verify the token signature, issuer/tenant and lifetime).
    /// </summary>
    public string Audience { get; set; } = string.Empty;

    /// <summary>
    /// The STS authority derived from <see cref="Instance"/> and <see cref="TenantId"/>.
    /// </summary>
    public string Authority => $"{Instance.TrimEnd('/')}/{TenantId}";
}
