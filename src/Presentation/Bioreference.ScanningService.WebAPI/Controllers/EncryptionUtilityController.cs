using Bioreference.ScanningService.Infrastructure.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Bioreference.ScanningService.WebAPI.Controllers;

/// <summary>
/// Utility endpoints that encrypt/decrypt configuration secrets (connection strings,
/// credentials) using the <c>ENC:</c> scheme.
///
/// Both operations require a valid Azure AD (Entra ID) bearer token, which the Angular
/// application attaches via the standard <c>Authorization: Bearer &lt;token&gt;</c> header.
/// The token is validated against the <c>AzureAd</c> configuration section.
/// </summary>
[ApiController]
[Route("api/encryptionutility")]
[Authorize]
public class EncryptionUtilityController : ControllerBase
{
    /// <summary>Encrypts a plain value and returns the ENC: string.</summary>
    [HttpPost("encrypt")]
    public IActionResult Encrypt([FromBody] EncryptionRequest request)
    {
        if (string.IsNullOrEmpty(request?.Value))
        {
            return BadRequest(new { error = "Value is required." });
        }

        return Ok(new { result = SecretProtector.Encrypt(request.Value) });
    }

    /// <summary>Decrypts an ENC: value and returns the plain text.</summary>
    [HttpPost("decrypt")]
    public IActionResult Decrypt([FromBody] EncryptionRequest request)
    {
        if (string.IsNullOrEmpty(request?.Value))
        {
            return BadRequest(new { error = "Value is required." });
        }

        try
        {
            return Ok(new { result = SecretProtector.Decrypt(request.Value) });
        }
        catch (Exception)
        {
            return BadRequest(new { error = "Value could not be decrypted. Ensure it is a valid ENC: string." });
        }
    }
}

/// <summary>Request payload for the encrypt/decrypt endpoints.</summary>
public class EncryptionRequest
{
    /// <summary>The value to encrypt (plain text) or decrypt (an ENC: string).</summary>
    public string Value { get; set; } = string.Empty;
}
