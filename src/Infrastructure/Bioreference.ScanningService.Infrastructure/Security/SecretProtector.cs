using System.Security.Cryptography;
using System.Text;

namespace Bioreference.ScanningService.Infrastructure.Security;

/// <summary>
/// Provides symmetric (AES) encryption/decryption for sensitive configuration values
/// such as connection strings and network-share credentials so they are not stored as
/// clear text in appsettings.json.
///
/// Encrypted values are stored in configuration with the <see cref="EncryptedPrefix"/>
/// marker (e.g. <c>ENC:base64...</c>). At startup the encrypted configuration provider
/// transparently decrypts any value carrying this prefix, so the rest of the application
/// keeps reading plain <see cref="Microsoft.Extensions.Configuration.IConfiguration"/>
/// values without any change.
///
/// NOTE: The key/IV are embedded in code (obfuscation-only). This keeps the secrets
/// unreadable to a casual end user inspecting appsettings.json, but is not a substitute
/// for a real secret store if a strong threat model is required.
/// </summary>
public static class SecretProtector
{
    /// <summary>Prefix that marks a configuration value as encrypted.</summary>
    public const string EncryptedPrefix = "ENC:";

    // 32 bytes = AES-256 key. 16 bytes = IV. Embedded constant (obfuscation-only).
    private static readonly byte[] Key =
    {
        0x42, 0x4C, 0x49, 0x53, 0x53, 0x63, 0x61, 0x6E,
        0x6E, 0x69, 0x6E, 0x67, 0x53, 0x76, 0x63, 0x4B,
        0x65, 0x79, 0x32, 0x30, 0x32, 0x35, 0x41, 0x45,
        0x53, 0x32, 0x35, 0x36, 0x53, 0x65, 0x63, 0x21
    };

    private static readonly byte[] Iv =
    {
        0x42, 0x4C, 0x49, 0x53, 0x49, 0x6E, 0x69, 0x74,
        0x56, 0x65, 0x63, 0x74, 0x6F, 0x72, 0x32, 0x35
    };

    /// <summary>
    /// Encrypts <paramref name="plainText"/> and returns a value prefixed with
    /// <see cref="EncryptedPrefix"/> that can be pasted directly into appsettings.json.
    /// </summary>
    public static string Encrypt(string plainText)
    {
        ArgumentNullException.ThrowIfNull(plainText);

        using var aes = CreateAes();
        using var encryptor = aes.CreateEncryptor();

        var plainBytes = Encoding.UTF8.GetBytes(plainText);
        var cipherBytes = encryptor.TransformFinalBlock(plainBytes, 0, plainBytes.Length);

        return EncryptedPrefix + Convert.ToBase64String(cipherBytes);
    }

    /// <summary>
    /// Decrypts a value previously produced by <see cref="Encrypt"/>. If the value does not
    /// start with <see cref="EncryptedPrefix"/> it is returned unchanged, so plain and
    /// encrypted values can coexist during migration.
    /// </summary>
    public static string? Decrypt(string? value)
    {
        if (string.IsNullOrEmpty(value) || !value.StartsWith(EncryptedPrefix, StringComparison.Ordinal))
        {
            return value;
        }

        var cipherBase64 = value[EncryptedPrefix.Length..];
        var cipherBytes = Convert.FromBase64String(cipherBase64);

        using var aes = CreateAes();
        using var decryptor = aes.CreateDecryptor();

        var plainBytes = decryptor.TransformFinalBlock(cipherBytes, 0, cipherBytes.Length);
        return Encoding.UTF8.GetString(plainBytes);
    }

    /// <summary>Returns true when the value is marked as encrypted.</summary>
    public static bool IsEncrypted(string? value)
        => value is not null && value.StartsWith(EncryptedPrefix, StringComparison.Ordinal);

    private static Aes CreateAes()
    {
        var aes = Aes.Create();
        aes.Key = Key;
        aes.IV = Iv;
        aes.Mode = CipherMode.CBC;
        aes.Padding = PaddingMode.PKCS7;
        return aes;
    }
}
