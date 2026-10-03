using Microsoft.Extensions.Configuration;

namespace Bioreference.ScanningService.Infrastructure.Security;

/// <summary>
/// Helpers to enable transparent decryption of <c>ENC:</c>-prefixed configuration values.
/// </summary>
public static class EncryptedConfigurationExtensions
{
    /// <summary>
    /// Scans the current configuration for values carrying the
    /// <see cref="SecretProtector.EncryptedPrefix"/> marker and overlays their decrypted
    /// counterparts as an in-memory configuration source added last (so it takes precedence).
    /// After calling this, the rest of the application reads plain values via
    /// <see cref="IConfiguration"/> with no further changes.
    /// </summary>
    /// <typeparam name="T">
    /// A type that is both an <see cref="IConfigurationBuilder"/> and an
    /// <see cref="IConfiguration"/> (e.g. <c>ConfigurationManager</c>), so the already-loaded
    /// values can be read directly without an intermediate build.
    /// </typeparam>
    public static T AddDecryptedSecrets<T>(this T configurationManager)
        where T : IConfigurationBuilder, IConfiguration
    {
        var decrypted = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
        foreach (var kvp in configurationManager.AsEnumerable())
        {
            if (SecretProtector.IsEncrypted(kvp.Value))
            {
                decrypted[kvp.Key] = SecretProtector.Decrypt(kvp.Value);
            }
        }

        if (decrypted.Count > 0)
        {
            configurationManager.AddInMemoryCollection(decrypted);
        }

        return configurationManager;
    }

    /// <summary>
    /// Overload for a plain <see cref="IConfigurationBuilder"/> (e.g. the design-time
    /// EF factory) where the builder must be built once to discover encrypted values.
    /// </summary>
    public static IConfigurationBuilder AddDecryptedSecretsFromBuilder(this IConfigurationBuilder builder)
    {
        var snapshot = builder.Build();

        var decrypted = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
        foreach (var kvp in snapshot.AsEnumerable())
        {
            if (SecretProtector.IsEncrypted(kvp.Value))
            {
                decrypted[kvp.Key] = SecretProtector.Decrypt(kvp.Value);
            }
        }

        if (decrypted.Count > 0)
        {
            builder.AddInMemoryCollection(decrypted);
        }

        return builder;
    }
}
