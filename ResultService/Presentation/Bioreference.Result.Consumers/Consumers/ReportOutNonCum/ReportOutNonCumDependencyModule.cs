using Bioreference.Messaging.Abstractions;
using Bioreference.ResultService.Abstractions.Application.ApiClient;
using Bioreference.ResultService.Application.ApiClient;
using Bioreference.ResultService.Application.Model.AppSettings.ApiClient;
using Bioreference.ResultService.Application.Model.AppSettings.ReportOutNonCum;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Bioreference.ResultService.Consumers
{
    public class ReportOutNonCumDependencyModule : IDependencyModule
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

            services.Configure<AppSettingsReportOutNonCum>(
            configuration.GetSection(AppSettingsReportOutNonCum.SectionName));
            services.AddTransient<IPayloadSenderApiClient, PayloadSenderApiClient>();
            services.Configure<AppSettingsPayloadSender>(configuration.GetSection(AppSettingsPayloadSender.SectionName));
        }
    }
}
