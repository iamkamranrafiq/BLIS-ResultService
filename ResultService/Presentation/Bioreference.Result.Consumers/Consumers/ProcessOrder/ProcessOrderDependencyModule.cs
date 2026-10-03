using Bioreference.Messaging.Abstractions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Bioreference.ResultService.Application.Model.AppSettings.QueueOrder;

namespace Bioreference.ResultService.Processors
{
    public class ProcessOrderDependencyModule : IDependencyModule
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

            // Register HttpClient for API calls
            services.AddHttpClient<ProcessOrder>(client =>
            {
                client.Timeout = TimeSpan.FromMinutes(5);
            });

            services.Configure<AppSettingsQueueOrderInProcessor>(configuration.GetSection(AppSettingsQueueOrderInProcessor.SectionName));
        }
    }
}
