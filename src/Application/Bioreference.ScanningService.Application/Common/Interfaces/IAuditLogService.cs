namespace Bioreference.ScanningService.Application.Common.Interfaces;

public interface IAuditLogService
{
    ValueTask LogActivityAsync(
        object caller,
        string identifier,
        string auditMessage);
}
