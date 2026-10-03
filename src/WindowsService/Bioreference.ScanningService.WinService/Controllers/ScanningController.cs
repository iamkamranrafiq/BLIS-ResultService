using Bioreference.ScanningService.Application.DTOs.Requests;
using Bioreference.ScanningService.Application.Services.Interfaces;
using Bioreference.ScanningService.WinService.Hubs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;

namespace Bioreference.ScanningService.WinService.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ScanningController : ControllerBase
{
    private readonly IBatchService _batchService;
    private readonly IScanQueueService _scanQueueService;
    private readonly IScanFormatService _scanFormatService;
    private readonly IHubContext<ScannerHub> _scannerHub;
    private readonly ILogger<ScanningController> _logger;

    public ScanningController(
        IBatchService batchService,
        IScanQueueService scanQueueService,
        IScanFormatService scanFormatService,
        IHubContext<ScannerHub> scannerHub,
        ILogger<ScanningController> logger)
    {
        _batchService = batchService;
        _scanQueueService = scanQueueService;
        _scanFormatService = scanFormatService;
        _scannerHub = scannerHub;
        _logger = logger;
    }

    /// <summary>
    /// Returns all scan queues.
    /// GET /api/scanning/queues
    /// </summary>
    [HttpGet("queues")]
    public async Task<IActionResult> GetQueues([FromQuery] GetScanQueuesRequest request)
    {
        _logger.LogInformation("GET /api/scanning/queues called");
        var response = await _scanQueueService.GetScanQueuesAsync(request);
        return response.Success ? Ok(response) : StatusCode(500, response);
    }

    /// <summary>
    /// Returns all scan formats.
    /// GET /api/scanning/formats
    /// </summary>
    [HttpGet("formats")]
    public async Task<IActionResult> GetFormats([FromQuery] GetScanFormatsRequest request)
    {
        _logger.LogInformation("GET /api/scanning/formats called");
        var response = await _scanFormatService.GetScanFormatsAsync(request);
        return response.Success ? Ok(response) : StatusCode(500, response);
    }

    /// <summary>
    /// Returns a paginated list of batches.
    /// GET /api/scanning/batches
    /// </summary>
    [HttpGet("batches")]
    public async Task<IActionResult> GetBatches([FromQuery] GetBatchListRequest request)
    {
        _logger.LogInformation("GET /api/scanning/batches called");
        var response = await _batchService.GetBatchListAsync(request);
        return response.Success ? Ok(response) : StatusCode(500, response);
    }

    /// <summary>
    /// Creates or updates a batch and broadcasts the status to all SignalR clients.
    /// POST /api/scanning/batches
    /// </summary>
    [HttpPost("batches")]
    public async Task<IActionResult> SaveBatch([FromBody] SaveBatchRequest request)
    {
        _logger.LogInformation("POST /api/scanning/batches called");
        var response = await _batchService.SaveBatchAsync(request);

        if (response.Success)
        {
            await _scannerHub.Clients.All.SendAsync("BatchUpdated", new
            {
                batchId     = response.Data?.BatchId,
                batchNumber = response.Data?.BatchNumber,
                status      = response.Data?.Status
            });
        }

        return response.Success ? Ok(response) : StatusCode(500, response);
    }
}
