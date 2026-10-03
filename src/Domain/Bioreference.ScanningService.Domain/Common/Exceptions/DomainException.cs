namespace Bioreference.ScanningService.Domain.Common.Exceptions;

/// <summary>
/// Base exception for any domain-rule violation.
/// </summary>
public class DomainException : Exception
{
    public DomainException(string message) : base(message) { }
    public DomainException(string message, Exception innerException) : base(message, innerException) { }
}
