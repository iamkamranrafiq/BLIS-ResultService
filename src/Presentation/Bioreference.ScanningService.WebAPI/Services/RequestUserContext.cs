using Bioreference.ScanningService.Application.Common.Interfaces;

namespace Bioreference.ScanningService.WebAPI.Services;

public sealed class RequestUserContext(IHttpContextAccessor httpContextAccessor) : IRequestUserContext
{
    public string? GetUserName()
        => httpContextAccessor.HttpContext?.Request.Headers["X-UserName"].FirstOrDefault();

    public void SetUserName(string? userName)
    {
        var httpContext = httpContextAccessor.HttpContext;
        if (httpContext is not null)
        {
            httpContext.Request.Headers["X-UserName"] = userName;
        }
    }
}