using Bioreference.ScanningService.Application.Common.Interfaces;
using Bioreference.ScanningService.Application.DTOs.Requests;
using Bioreference.ScanningService.Application.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace Bioreference.ScanningService.WebAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ScanController : ControllerBase
{
    private readonly IScanQueueService _scanQueueService;
    private readonly IScanFormatService _scanFormatService;
    private readonly IScanStatsService _scanStatsService;
    private readonly ILogger<ScanController> _logger;
    private readonly IAuditLogService _auditLogService;

    public ScanController(
        IScanQueueService scanQueueService,
        IScanFormatService scanFormatService,
        IScanStatsService scanStatsService,
        ILogger<ScanController> logger,
        IAuditLogService auditLogService)
    {
        _scanQueueService = scanQueueService;
        _scanFormatService = scanFormatService;
        _scanStatsService = scanStatsService;
        _logger = logger;
        _auditLogService = auditLogService;
    }

    /// <summary>
    /// Returns all scan queues.
    /// GET /api/scan/queues?isActive=true
    /// </summary>
    [HttpGet("queues")]
    public async Task<IActionResult> GetQueues([FromQuery] GetScanQueuesRequest request)
    {
        _logger.LogInformation("GET /api/scan/queues called");
        await _auditLogService.LogActivityAsync(
            this,
            "NEW",
            "Get Scan Queues Requested");

        var response = await _scanQueueService.GetScanQueuesAsync(request);

        return response.Success ? Ok(response) : StatusCode(500, response);
    }

    /// <summary>
    /// Returns all scan formats.
    /// GET /api/scan/formats?isActive=true
    /// </summary>
    [HttpGet("formats")]
    public async Task<IActionResult> GetFormats([FromQuery] GetScanFormatsRequest request)
    {
        _logger.LogInformation("GET /api/scan/formats called");
        await _auditLogService.LogActivityAsync(
            this,
            "NEW",
            "Get Scan Formats Requested");

        var response = await _scanFormatService.GetScanFormatsAsync(request);

        return response.Success ? Ok(response) : StatusCode(500, response);
    }


    [HttpGet("dashboard/stats")]
    public async Task<IActionResult> GetDashboardStats()
    {
        _logger.LogInformation("GET /api/scan/dashboard/stats called");
        await _auditLogService.LogActivityAsync(
            this,
            "NEW",
            "Get Dashboard Stats Requested");

        var response = await _scanStatsService.GetDashboardStatsAsync();

        return response.Success ? Ok(response) : StatusCode(500, response);
    }

    /// <summary>
    /// Returns the most recent CompletedDate for each provided scan queue id.
    /// If a queue has no batch with a CompletedDate, returns "-" for that queue.
    /// POST /api/scan/queues/last-completed
    /// Body: { "scanQueueIds": [1, 2, 3] }
    /// </summary>
    [HttpPost("queues/last-completed")]
    public async Task<IActionResult> GetLastCompletedByQueue([FromBody] GetLastCompletedByQueueRequest request)
    {
        _logger.LogInformation("POST /api/scan/queues/last-completed called - Count: {Count}",
            request?.ScanQueueIds?.Count ?? 0);

        var response = await _scanStatsService.GetLastCompletedByQueueAsync(request ?? new GetLastCompletedByQueueRequest());

        return response.Success ? Ok(response) : StatusCode(500, response);
    }

    [HttpPost("productivityReport")]
    public async Task<IActionResult> GetProductivityReport([FromBody] GetScanProductivityReportRequest request)
    {
        _logger.LogInformation("POST /api/scan/productivityReport called");

        var response = await _scanQueueService.GetProductivityReport(request);

        return response.Success ? Ok(response) : StatusCode(500, response);
    }
}
