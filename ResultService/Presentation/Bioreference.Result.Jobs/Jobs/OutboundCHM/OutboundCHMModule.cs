using Bioreference.Contracts.Result;
using Bioreference.Jobs.Abstractions;
using Bioreference.Messaging.Kafka;
using Bioreference.ResultService.Abstractions.Application.Jobs;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Bioreference.ResultService.Jobs.OutboundCHM
{
    public class OutboundCHMModule : IDependencyModule
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

            // Register the Outbound CHM job
            services.RegisterProducer<Outbound_CHM>();
            services.AddTransient<IBLISJob, Application.Jobs.OutboundCHMJob>();
        }
    }
}
