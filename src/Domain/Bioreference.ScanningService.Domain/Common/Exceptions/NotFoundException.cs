namespace Bioreference.ScanningService.Domain.Common.Exceptions;

/// <summary>
/// Thrown when an entity cannot be located by its identifier.
/// </summary>
public class NotFoundException : DomainException
{
    public NotFoundException(string message) : base(message) { }

    public NotFoundException(string entityName, object key)
        : base($"Entity \"{entityName}\" ({key}) was not found.") { }
}
