using Bioreference.Jobs.Abstractions.DTO;
using Bioreference.Jobs.Core;
using Bioreference.Logging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

var builder = new HostBuilder()
.ConfigureServices((hostContext, services) =>
{
    services.UseJobs();
    var serviceProvider = services.BuildServiceProvider();
    //IConfiguration configuration = serviceProvider.GetRequiredService<IConfiguration>();
    services.AddBioreferenceLogging("Bioreference.ScanningService.Jobs.IngestDocsJob");
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

