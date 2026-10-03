using Bioreference.Contracts.Result;
using Bioreference.Logging;
using Bioreference.Messaging.Abstractions;
using Bioreference.Messaging.Kafka;
using Bioreference.ResultService.Application.Common;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Bioreference.ResultService.Consumers
{
    public class ResultReportingDependencyModule : IDependencyModule
    {
        public void RegisterServices(IServiceCollection services, IConfiguration configuration)
        {
            //connection strings configuration
            var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production";

            var builder = new ConfigurationBuilder()
                .AddConfiguration(configuration)
                .AddJsonFile($"connections.{environment}.json", optional: true, reloadOnChange: true);

            var newConfig = builder.Build();
            services.AddSingleton<IConfiguration>(newConfig);
            services.AddAutoMapper(_ => { }, typeof(CommonResultService).Assembly);
            services.RegisterProducer<AuditOut>();
        }
    }
}
