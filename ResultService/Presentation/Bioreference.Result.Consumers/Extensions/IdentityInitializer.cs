using Bioreference.ResultService.Common;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Configuration;
using System.Security.Principal;

namespace Bioreference.ResultService.Consumers.Extensions
{
    public class IdentityInitializer : IHostedService
    {
        private readonly IConfiguration _configuration;
        private readonly string[] _roles = new[] { "Consumer" };

        public IdentityInitializer(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            
            string userName = _configuration["Bioreference.ConsumerLogging:LoggingName"] ?? "Consumer"; 
            SetBioreferenceIdentity(userName, _roles);
            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

        private void SetBioreferenceIdentity(string userName, string[] roles)
        {
            var identity = new BioreferenceIdentity(userName);
            identity.SetRoles(roles);

            var principal = new GenericPrincipal(identity, roles);
            AppDomain.CurrentDomain.SetThreadPrincipal(principal);
            // Assign to both to ensure compatibility
            Thread.CurrentPrincipal = principal;
        }
    }
}
