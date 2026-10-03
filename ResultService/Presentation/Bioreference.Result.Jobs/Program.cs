using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Bioreference.Jobs.Core;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using Bioreference.ResultService.Jobs.Extensions;
using Bioreference.ResultService.Abstractions.Application.Setting;
using Bioreference.ResultService.Application.Setting;
using Bioreference.Logging;
using Bioreference.ResultService.Application.Model.AppSettings.Logging;

var builder = new HostBuilder() 
.ConfigureServices((hostContext, services) =>
    {
        services.AddHostedService<IdentityInitializer>();
        services.AddHostedService<DomainLoggingInitializer>();
        services.UseJobs();
        services.AddSingleton<ISettingService, SettingService>();
        var serviceProvider = services.BuildServiceProvider();
        IConfiguration configuration = serviceProvider.GetRequiredService<IConfiguration>();
        var jobLogSettings = new LogSettings();
        configuration.GetSection(LogSettings.SectionJobLogging).Bind(jobLogSettings);
        services.AddBioreferenceLogging(jobLogSettings.LoggingName);
    })
    .UseConsoleLifetime();


var host = builder.Build();
// Register Ctrl+C handler for graceful shutdown
using var cts = new CancellationTokenSource();
Console.CancelKeyPress += (sender, e) =>
{
    var logger = host.Services.GetRequiredService<ILogger<Program>>();
    logger.LogInformation("Cancellation requested");
    e.Cancel = true;
    cts.Cancel();
};

await host.StartAsync(cts.Token);

// Run the job framework
return await host.RunJobs(cts.Token);

