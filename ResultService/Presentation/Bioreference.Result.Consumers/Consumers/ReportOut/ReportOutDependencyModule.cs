using Bioreference.Contracts.Result;
using Bioreference.Logging;
using Bioreference.Messaging.Abstractions;
using Bioreference.Messaging.Kafka;
using Bioreference.ResultService.Abstractions.Application.ApiClient;
using Bioreference.ResultService.Application.ApiClient;
using Bioreference.ResultService.Application.Model.AppSettings.ApiClient;
using Bioreference.ResultService.Application.Model.AppSettings.Reflex;
using Bioreference.ResultService.Application.Model.AppSettings.ReportOut;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bioreference.ResultService.Consumers
{
    public class ReportOutDependencyModule : IDependencyModule
    {
        public void RegisterServices(IServiceCollection services, IConfiguration configuration)
        {   //connection strings configuration
            var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production";

            var builder = new ConfigurationBuilder()
                .AddConfiguration(configuration)
                .AddJsonFile($"connections.{environment}.json", optional: true, reloadOnChange: true)
                .AddEnvironmentVariables();

            var newConfig = builder.Build();
            services.AddSingleton<IConfiguration>(newConfig);

            services.Configure<AppSettingsReportOut>(
                configuration.GetSection(AppSettingsReportOut.SectionName));
            services.AddTransient<IPayloadSenderApiClient, PayloadSenderApiClient>();
            services.Configure<AppSettingsPayloadSender>(configuration.GetSection(AppSettingsPayloadSender.SectionName));
        }
    }
}
