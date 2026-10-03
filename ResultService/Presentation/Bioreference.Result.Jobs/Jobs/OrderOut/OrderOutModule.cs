using Bioreference.Jobs.Abstractions;
using Bioreference.ResultService.Abstractions.Application.ApiClient;
using Bioreference.ResultService.Abstractions.Application.Jobs;
using Bioreference.ResultService.Abstractions.Application.Setting;
using Bioreference.ResultService.Application.ApiClient;
using Bioreference.ResultService.Application.Model.AppSettings.OrderOut;
using Bioreference.ResultService.Application.Model.AppSettings.ApiClient;
using Bioreference.ResultService.Application.Setting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Bioreference.ResultService.Jobs.OrderOut
{
    public class OrderOutModule : IDependencyModule
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

            // Register the Order out job
            services.AddTransient<IBLISJob, Application.Jobs.OrderOutJob>();
            services.AddTransient<ISettingService, SettingService>();
            services.AddTransient<IPayloadSenderApiClient, PayloadSenderApiClient>();
            services.Configure<AppSettingsOrderEngine>(configuration.GetSection(AppSettingsOrderEngine.SectionName));
            services.Configure<AppSettingsPayloadSender>(configuration.GetSection(AppSettingsPayloadSender.SectionName));

        }
    }
}
