namespace Bioreference.ScanningService.Application.Common.Interfaces;

public interface IRequestUserContext
{
    string? GetUserName();

    void SetUserName(string? userName);
}