using Bioreference.Jobs.Abstractions;
using Bioreference.ScanningService.Application;
using Bioreference.ScanningService.Application.Common.Interfaces;
using Bioreference.ScanningService.Application.Common.Settings;
using Bioreference.ScanningService.Infrastructure;
using Bioreference.ScanningService.Infrastructure.Security;
using Bioreference.ScanningService.Infrastructure.Storage;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Bioreference.ScanningService.Jobs.IngestDocs
{
    public class IngestDocsModule : IDependencyModule
    {
        public void RegisterServices(IServiceCollection services, IConfiguration configuration)
        {
            // Overlay decrypted ENC: secrets so the connection string and storage credentials
            // are available in plain form (same behaviour as the WebAPI/WinService hosts).
            var decryptedConfiguration = new ConfigurationBuilder()
                .AddConfiguration(configuration)
                .AddDecryptedSecretsFromBuilder()
                .Build();

            // Ensure downstream resolution of IConfiguration returns the decrypted view.
            services.AddSingleton<IConfiguration>(decryptedConfiguration);

            // Clean Architecture layers
            services.AddApplication();
            services.AddInfrastructure(decryptedConfiguration);

            // Auditing user context for the background job host
            services.AddSingleton<IRequestUserContext, JobRequestUserContext>();

            // OS-aware path converter (Windows share vs Linux mounted path)
            if (OperatingSystem.IsLinux())
                services.AddSingleton<IPathConverter, LinuxPathConverter>();
            else
                services.AddSingleton<IPathConverter, WindowsPathConverter>();

            // IngestDocs settings (folders + index-file column mapping)
            services.Configure<IngestDocsSettings>(decryptedConfiguration.GetSection(IngestDocsSettings.SectionName));

            // Settings required by DocumentService
            services.Configure<OnBaseConfiguration>(decryptedConfiguration.GetSection(OnBaseConfiguration.DataSource));
            services.Configure<BarcodeSettings>(decryptedConfiguration.GetSection(BarcodeSettings.SectionName));

            // The job implementation invoked by the job host
            services.AddTransient<IBLISJob, Bioreference.ScanningService.Application.Jobs.IngestDocsJob>();
        }
    }
}
