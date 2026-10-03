using Bioreference.ScanningService.WinService.Settings;

using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;

namespace Bioreference.ScanningService.WinService.Authentication;

/// <summary>
/// Registers Azure AD (Entra ID) JWT bearer authentication so the Windows Service
/// verifies the same access tokens that the BLIS Angular UI sends to the API.
/// </summary>
public static class AuthenticationExtensions
{
    /// <summary>Path of the SignalR scanner hub (tokens arrive via query string here).</summary>
    private const string ScannerHubPath = "/hubs/scanner";

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

                // Per requirement we "just verify the JWT" (signature + issuer/tenant + lifetime)
                // and do not restrict on a specific audience unless one is configured.
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

                // SignalR sends the token in the "access_token" query-string parameter for
                // WebSocket/SSE connections (browsers cannot set Authorization headers on those).
                options.Events = new JwtBearerEvents
                {
                    OnMessageReceived = context =>
                    {
                        var accessToken = context.Request.Query["access_token"];
                        var path = context.HttpContext.Request.Path;
                        if (!string.IsNullOrEmpty(accessToken) &&
                            path.StartsWithSegments(ScannerHubPath, StringComparison.OrdinalIgnoreCase))
                        {
                            context.Token = accessToken;
                        }

                        var logger = context.HttpContext.RequestServices
                            .GetRequiredService<ILoggerFactory>()
                            .CreateLogger("JwtAuth");
                        var hasHeader = context.Request.Headers.ContainsKey("Authorization");
                        logger.LogInformation(
                            "[JwtAuth] OnMessageReceived path={Path} hasAuthHeader={HasHeader} hasQueryToken={HasQuery} tokenResolved={Resolved}",
                            path.Value, hasHeader, !string.IsNullOrEmpty(accessToken), !string.IsNullOrEmpty(context.Token));
                        return Task.CompletedTask;
                    },
                    OnAuthenticationFailed = context =>
                    {
                        var logger = context.HttpContext.RequestServices
                            .GetRequiredService<ILoggerFactory>()
                            .CreateLogger("JwtAuth");
                        logger.LogError(context.Exception,
                            "[JwtAuth] Authentication FAILED: {Message}", context.Exception.Message);
                        return Task.CompletedTask;
                    },
                    OnChallenge = context =>
                    {
                        var logger = context.HttpContext.RequestServices
                            .GetRequiredService<ILoggerFactory>()
                            .CreateLogger("JwtAuth");
                        logger.LogWarning(
                            "[JwtAuth] Challenge issued (401). error={Error} description={Description}",
                            context.Error, context.ErrorDescription);
                        return Task.CompletedTask;
                    },
                    OnTokenValidated = context =>
                    {
                        var logger = context.HttpContext.RequestServices
                            .GetRequiredService<ILoggerFactory>()
                            .CreateLogger("JwtAuth");
                        logger.LogInformation(
                            "[JwtAuth] Token VALIDATED for '{Name}'. Issuer={Issuer}",
                            context.Principal?.Identity?.Name ?? "(no name)",
                            (context.SecurityToken as System.IdentityModel.Tokens.Jwt.JwtSecurityToken)?.Issuer ?? "(unknown)");
                        return Task.CompletedTask;
                    }
                };
            });

        services.AddAuthorization();

        return services;
    }
}
