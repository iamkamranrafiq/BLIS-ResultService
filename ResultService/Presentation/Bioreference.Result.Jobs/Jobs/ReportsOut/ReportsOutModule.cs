using Bioreference.Contracts.Result;
using Bioreference.Jobs.Abstractions;
using Bioreference.Messaging.Kafka;
using Bioreference.ResultService.Abstractions.Application.Jobs;
using Bioreference.ResultService.Application.Model.AppSettings.ReportsOut;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Bioreference.ResultService.Jobs.ReportsOut
{
    public class ReportsOutModule : IDependencyModule
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

            services.Configure<AppSettingsReportsOut>(
                configuration.GetSection(AppSettingsReportsOut.SectionName));
            services.RegisterProducer<ReportOut>();
            services.AddTransient<IBLISJob, Application.Jobs.ReportsOutJob>();
        }
    }
}
