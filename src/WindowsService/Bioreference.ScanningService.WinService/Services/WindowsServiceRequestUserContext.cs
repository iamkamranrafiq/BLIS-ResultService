using Bioreference.ScanningService.Application.Common.Interfaces;

namespace Bioreference.ScanningService.WinService.Services;

public sealed class WindowsServiceRequestUserContext : IRequestUserContext
{
    private string? _userName = "winservice";

    public string? GetUserName() => _userName;

    public void SetUserName(string? userName) => _userName = userName;
}