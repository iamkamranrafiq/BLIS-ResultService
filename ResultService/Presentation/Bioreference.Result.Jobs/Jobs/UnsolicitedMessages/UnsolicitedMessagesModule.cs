using Bioreference.Contracts.Result;
using Bioreference.Jobs.Abstractions;
using Bioreference.Messaging.Kafka;
using Bioreference.ResultService.Abstractions.Application.Jobs;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Bioreference.ResultService.Jobs.UnsolicitedMessages
{
    public class UnsolicitedMessagesModule : IDependencyModule
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

            services.RegisterProducer<CreateManifest>();
            services.AddTransient<IBLISJob, Application.Jobs.UnsolicitedMessagesJob>();
        }
    }
}
