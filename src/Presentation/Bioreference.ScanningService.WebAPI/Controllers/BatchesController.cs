using Azure.Core;
using Bioreference.ScanningService.Application.Common.Interfaces;
using Bioreference.ScanningService.Application.DTOs.Requests;
using Bioreference.ScanningService.Application.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace Bioreference.ScanningService.WebAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
public class BatchesController : ControllerBase
{
    private readonly IBatchService _batchService;
    private readonly IBatchStatusService _batchStatusService;
    private readonly IScanStatsService _scanStatsService;
    private readonly ILogger<BatchesController> _logger;
    private readonly IAuditLogService _auditLogService;

    public BatchesController(
        IBatchService batchService,
        IBatchStatusService batchStatusService,
        IScanStatsService scanStatsService,
        ILogger<BatchesController> logger,
        IAuditLogService auditLogService)
    {
        _batchService = batchService;
        _batchStatusService = batchStatusService;
        _scanStatsService = scanStatsService;
        _logger = logger;
        _auditLogService = auditLogService;
    }

    /// <summary>
    /// Returns a paginated list of batches.
    /// GET /api/batches
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetBatch([FromQuery] GetBatchListRequest request)
    {
        if (request.PageNumber < 1 || request.PageSize < 1 || request.PageSize > 100)
        {
            return BadRequest("PageNumber must be ≥ 1 and PageSize must be between 1 and 100.");
        }
        await _auditLogService.LogActivityAsync(
            this,
            "",
            "Get Batch List Requested");
        _logger.LogInformation("GET /api/batches called");

        var response = await _batchService.GetBatchListAsync(request);

        return response.Success ? Ok(response) : StatusCode(500, response);
    }

    /// <summary>
    /// Returns all distinct batch statuses.
    /// GET /api/batches/statuses?isActive=true
    /// </summary>
    [HttpGet("status")]
    public async Task<IActionResult> GetStatuses([FromQuery] GetBatchStatusesRequest request)
    {
        _logger.LogInformation("GET /api/batches/statuses called");

        var response = await _batchStatusService.GetBatchStatusesAsync(request);

        return response.Success ? Ok(response) : StatusCode(500, response);
    }

    [HttpPut("{id:int}/updatestatus")]
    public async Task<IActionResult> UpdateStatus(int id, [FromBody] UpdateBatchStatusRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }
        _logger.LogInformation("PUT /api/batches/{Id}/updatestatus called. NewStatusId={NewStaus}", id, request.statusId);
        await _auditLogService.LogActivityAsync(
            this,
            id.ToString(),
            $"Update Batch Status Requested for BatchId={id}, NewStatusId={request.statusId}");
        var response = await _batchService.UpdateBatchStatusAsync(id, request);

        if (!response.Success)
        {
            return response.Message.Contains("not found", StringComparison.OrdinalIgnoreCase)
                ? NotFound(response)
                : StatusCode(500, response);
        }

        return Ok(response);
    }

    [HttpPost]
    [Route("Search")]
    public async Task<IActionResult> GetBatchList([FromBody] BatchListRequest request)
    {

        _logger.LogInformation("POST /api/batches/Search called");
        await _auditLogService.LogActivityAsync(
            this,
            "",
            "Search Batch List Requested");

        var response = await _batchService.SearchBatchListAsync(request);

        return response.Success ? Ok(response) : StatusCode(500, response);
    }

    /// <summary>
    /// Creates a new batch.
    /// POST /api/batches
    /// </summary>
    [HttpPost]
    [Route("save")]
    public async Task<IActionResult> SaveBatch([FromBody] SaveBatchRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        _logger.LogInformation("POST /api/batches called for Name {Name}", request.Name);
        await _auditLogService.LogActivityAsync(
            this,
            request.Name,
            $"Save Batch Requested for Name={request.Name}");

        var response = await _batchService.SaveBatchAsync(request);

        if (!response.Success)
        {
            return response.Message.Contains("already exists", StringComparison.OrdinalIgnoreCase)
                ? Conflict(response)
                : StatusCode(500, response);
        }

        return CreatedAtAction(nameof(GetBatchList), new { }, response);
    }

    /// <summary>
    /// Renames an existing batch.
    /// PUT /api/batches/{id}/rename
    /// </summary>
    [HttpPut("{id:int}/rename")]
    public async Task<IActionResult> RenameBatch(int id, [FromBody] RenameBatchRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        _logger.LogInformation("PUT /api/batches/{Id}/rename called. NewName={NewName}", id, request.Name);
        await _auditLogService.LogActivityAsync(
            this,
            id.ToString(),
            $"Rename Batch Requested for Id={id}");

        var response = await _batchService.RenameBatchAsync(id, request);

        if (!response.Success)
        {
            return NotFound(response);
        }

        return Ok(response);
    }

    /// <summary>
    /// Soft-deletes multiple batches by setting IsDeleted = true.
    /// DELETE /api/batches/delete
    /// </summary>
    /// <param name="request">The request containing batch IDs to delete</param>
    /// <returns>Success response with deletion statistics</returns>
    [HttpDelete("delete")]
    public async Task<IActionResult> DeleteBatches([FromBody] DeleteBatchesRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }
        await _auditLogService.LogActivityAsync(
            this,
            string.Join(",", request.BatchIds),
            $"Delete Batches Requested for Ids={string.Join(",", request.BatchIds)}");
        _logger.LogInformation("DELETE /api/batches/delete called for {Count} batch(es)", request.BatchIds.Count);

        var response = await _batchService.DeleteBatchesAsync(request);

        return Ok(response);
    }



    /// <summary>
    /// Returns the batch scan summary (documents, pages, velocity, processing estimate, size, errors).
    /// GET /api/batches/{batchId}/summary
    /// </summary>
    [HttpGet("{batchId:int}/stats")]
    public async Task<IActionResult> GetBatchScanSummary(int batchId)
    {
        _logger.LogInformation("GET /api/batches/{BatchId}/summary called", batchId);

        var response = await _batchService.GetBatchScanSummaryAsync(batchId);
        await _auditLogService.LogActivityAsync(
            this,
            batchId.ToString(),
            $"Get Batch Scan Summary Requested for Id={batchId}");
        if (!response.Success)
        {
            // When the batch summary is unavailable (e.g. while loading a batch) do not surface a
            // 404/500. Return 204 No Content so the caller can skip the stats gracefully without
            // treating it as an error.
            _logger.LogWarning("Batch scan summary unavailable for BatchId={BatchId}: {Message}. Returning 204 No Content.",
                batchId, response.Message);
            return NoContent();
        }

        return Ok(response);
    }

    /// <summary>
    /// Returns the activity logs recorded for the given batch.
    /// GET /api/batches/{batchId}/activitylogs
    /// </summary>
    [HttpGet("{batchId:int}/activitylogs")]
    public async Task<IActionResult> GetActivityLogsByBatchId(int batchId)
    {
        _logger.LogInformation("GET /api/batches/{BatchId}/activitylogs called", batchId);

        var response = await _batchService.GetActivityLogsByBatchIdAsync(batchId);

        return response.Success ? Ok(response) : StatusCode(500, response);
    }
}
