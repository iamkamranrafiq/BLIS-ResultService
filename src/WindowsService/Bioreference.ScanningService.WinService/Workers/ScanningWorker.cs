using Bioreference.ScanningService.Application.Common.Interfaces;
using Bioreference.ScanningService.WinService.Hubs;
using Microsoft.AspNetCore.SignalR;

namespace Bioreference.ScanningService.WinService.Workers;

/// <summary>
/// Background worker that polls for scanning work on a configurable interval
/// and broadcasts progress updates via SignalR.
/// </summary>
public class ScanningWorker : BackgroundService
{
    private readonly ILogger<ScanningWorker> _logger;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IHubContext<ScannerHub> _scannerHub;
    private readonly IConfiguration _configuration;

    public ScanningWorker(
        ILogger<ScanningWorker> logger,
        IServiceScopeFactory scopeFactory,
        IHubContext<ScannerHub> scannerHub,
        IConfiguration configuration)
    {
        _logger = logger;
        _scopeFactory = scopeFactory;
        _scannerHub = scannerHub;
        _configuration = configuration;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var intervalSeconds = _configuration.GetValue<int>("WorkerSettings:IntervalSeconds", 30);

        _logger.LogInformation("ScanningWorker started. Polling every {Interval}s", intervalSeconds);

        while (!stoppingToken.IsCancellationRequested)
        {
            _logger.LogInformation("ScanningWorker tick at {Time}", DateTimeOffset.UtcNow);

            try
            {
                // New scope per tick — IUnitOfWork is Scoped, ScanningWorker is Singleton
                await using var scope = _scopeFactory.CreateAsyncScope();
                var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

                await DoWorkAsync(unitOfWork, stoppingToken);
            }
            catch (Exception ex) when (!stoppingToken.IsCancellationRequested)
            {
                _logger.LogError(ex, "ScanningWorker tick failed — will retry after interval");

                await _scannerHub.Clients.All.SendAsync("WorkerError", new
                {
                    message   = ex.Message,
                    timestamp = DateTimeOffset.UtcNow
                }, stoppingToken);
            }

            await Task.Delay(TimeSpan.FromSeconds(intervalSeconds), stoppingToken);
        }

        _logger.LogInformation("ScanningWorker stopped");
    }

    private async Task DoWorkAsync(IUnitOfWork unitOfWork, CancellationToken cancellationToken)
    {
        // TODO: implement scanning work here using unitOfWork repositories
        // Broadcast progress to a specific batch group:
        // await _scannerHub.Clients.Group($"batch-{batchId}").SendAsync("ScanProgress", payload, cancellationToken);
        await Task.CompletedTask;
    }
}
