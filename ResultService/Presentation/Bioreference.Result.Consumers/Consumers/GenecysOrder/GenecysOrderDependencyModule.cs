using Bioreference.Logging;
using Bioreference.Messaging.Abstractions;
using Bioreference.ResultService.Application.Model.AppSettings.Genecys;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;


namespace Bioreference.ResultService.Consumers
{
    public class GenecysOrderDependencyModule : IDependencyModule
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

            services.Configure<AppSettingsGenecysOrder>(configuration.GetSection(AppSettingsGenecysOrder.SectionName));
        }
    }
}
