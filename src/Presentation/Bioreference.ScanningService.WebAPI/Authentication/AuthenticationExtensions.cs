using Bioreference.ScanningService.WebAPI.Settings;

using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;

namespace Bioreference.ScanningService.WebAPI.Authentication;

/// <summary>
/// Registers Azure AD (Entra ID) JWT bearer authentication so the WebAPI verifies the
/// same access tokens that the BLIS Angular UI sends (via MSAL). Mirrors the WinService
/// configuration and reads the <c>AzureAd</c> section from configuration.
/// </summary>
public static class AuthenticationExtensions
{
    public static IServiceCollection AddScanningAuthentication(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var azureAd = new AzureAdSettings();
        configuration.GetSection(AzureAdSettings.SectionName).Bind(azureAd);
        services.Configure<AzureAdSettings>(configuration.GetSection(AzureAdSettings.SectionName));

        var hasAudience = !string.IsNullOrWhiteSpace(azureAd.Audience);

        services
            .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                // Azure AD metadata endpoint; the middleware fetches signing keys from here.
                options.Authority = azureAd.Authority;

                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    ValidateIssuer = true,
                    ValidateLifetime = true,
                    ValidateAudience = hasAudience,
                    ValidAudiences = hasAudience
                        ? new[] { azureAd.Audience, azureAd.ClientId, $"api://{azureAd.ClientId}" }
                        : null,
                    // Azure AD issues tokens from both v1 (sts.windows.net) and v2 (login.microsoftonline.com)
                    // endpoints; accept both issuer forms for the configured tenant.
                    ValidIssuers = new[]
                    {
                        $"{azureAd.Authority}/v2.0",
                        $"https://sts.windows.net/{azureAd.TenantId}/"
                    },
                    NameClaimType = "name",
                    ClockSkew = TimeSpan.FromMinutes(5)
                };
            });

        services.AddAuthorization();

        return services;
    }
}
