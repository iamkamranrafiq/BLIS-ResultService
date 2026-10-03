using Bioreference.ScanningService.Application.DTOs.Requests;
using Bioreference.ScanningService.Application.DTOs.Responses;

namespace Bioreference.ScanningService.Application.Services.Interfaces;

public interface IBatchService
{
    Task<GetBatchListResponse> GetBatchListAsync(GetBatchListRequest request);
    Task<SaveBatchResponse> SaveBatchAsync(SaveBatchRequest request);
    Task<RenameBatchResponse> RenameBatchAsync(int id, RenameBatchRequest request);
    Task<GetBatchListResponse> SearchBatchListAsync(BatchListRequest request);
    Task<BatchDto?> GetBatchByIdAsync(int batchId);
    Task<DeleteBatchesResponse> DeleteBatchesAsync(DeleteBatchesRequest request);
 
    Task<BatchScanSummaryResponse> GetBatchScanSummaryAsync(int batchId);
    Task<GetActivityLogsByBatchIdResponse> GetActivityLogsByBatchIdAsync(int batchId);

    /// <summary>
    /// Writes an activity log entry for a batch (e.g. scan started/completed).
    /// </summary>
    Task LogActivityAsync(int batchId, string userId, string logLevel, string? description = null,
        int documentsScanned = 0, int pagesScanned = 0, int? documentId = null);
    Task<RenameBatchResponse> UpdateBatchStatusAsync(int id, UpdateBatchStatusRequest request);
    Task<bool> MarkBatchCompletedAsync(int batchId, string? updatedBy = null);

    /// <summary>
    /// Commits the batch automatically when its scan queue is configured with IsAutoCommit = true
    /// and the batch has no errors. Does nothing when the queue is not auto-commit, the batch has
    /// errors, or the batch/queue cannot be resolved. Returns true only when the batch was committed.
    /// </summary>
    Task<bool> TryAutoCommitBatchAsync(int batchId, string? updatedBy = null);
    /// Records the batch's StartedDate when scanning begins. Only sets it the first time so
    /// resuming a scan keeps the original start time. Returns false if the batch does not exist.
    /// </summary>
    Task<bool> MarkBatchStartedAsync(int batchId, string? updatedBy = null);

    /// <summary>
    /// Recalculates and persists the batch's TotalDocuments and TotalPages
    /// based on its current non-deleted documents and pages.
    /// Returns false if the batch does not exist.
    /// </summary>
    Task<bool> UpdateBatchCountsAsync(int batchId, string? updatedBy = null);
}
