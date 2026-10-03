using Bioreference.ResultService.Common;
using System.Security.Principal;
namespace Bioreference.ResultService.WebAPI.Middleware
{
    public class IdentityMiddleware
    {
        private readonly RequestDelegate _next;

        public IdentityMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            var userName = context.Request.Headers["X-UserName"].FirstOrDefault();
            var rolesHeader = context.Request.Headers["X-Roles"].FirstOrDefault();

            if (string.IsNullOrWhiteSpace(userName))
            {
                userName = "blis_Unknown";
            }
            else
            {
                string normalizedUserName = userName.Contains('@')
                    ? userName.Split('@')[0].Trim()
                    : userName.Trim();

                userName = string.IsNullOrWhiteSpace(normalizedUserName)
                    ? "blis_Unknown"
                    : $"blis_{normalizedUserName}";
            }


            string[] roles = string.IsNullOrEmpty(rolesHeader)
                              ? new[] { "Unknown" }
                              : rolesHeader.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

            SetBioreferenceIdentity(context, userName, roles);

            await _next(context);
        }

        private void SetBioreferenceIdentity(HttpContext context, string userName, string[] roles)
        {
            var identity = new BioreferenceIdentity(userName);
            identity.SetRoles(roles);

            var principal = new GenericPrincipal(identity, roles);

            // Assign to both to ensure compatibility
            Thread.CurrentPrincipal = principal;
            context.User = principal;
        }
    }


}
