using Bioreference.Logging;
using Bioreference.Messaging.Abstractions;
using Bioreference.ResultService.Application.Model.AppSettings.Reflex;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bioreference.ResultService.Consumers
{
    public class ReflexDependencyModule : IDependencyModule
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

            services.Configure<AppSettingsReflex>(
                configuration.GetSection(AppSettingsReflex.SectionName));
        }
    }
}
