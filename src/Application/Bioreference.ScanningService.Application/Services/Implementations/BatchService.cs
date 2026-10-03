using Bioreference.ScanningService.Application.Common.Constants;
using Bioreference.ScanningService.Application.Common.Interfaces;
using Bioreference.ScanningService.Application.Common.Settings;
using Bioreference.ScanningService.Application.DTOs.Requests;
using Bioreference.ScanningService.Application.DTOs.Responses;
using Bioreference.ScanningService.Application.Services.Interfaces;
using Bioreference.ScanningService.Domain.Entities;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Bioreference.ScanningService.Application.Services.Implementations;

public class BatchService : IBatchService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IOnBaseRepository _onBaseRepository;
    private readonly OnBaseConfiguration _onBaseConfiguration;
    private readonly ILogger<BatchService> _logger;

    public BatchService(IUnitOfWork unitOfWork, ILogger<BatchService> logger, IOnBaseRepository onBaseRepository, IOptions<OnBaseConfiguration> onBaseConfiguration)
    {
        _unitOfWork = unitOfWork;
        _onBaseRepository = onBaseRepository;
        _onBaseConfiguration = onBaseConfiguration.Value;
        _logger = logger;
    }

    public async Task<GetBatchListResponse> GetBatchListAsync(GetBatchListRequest request)
    {
        _logger.LogInformation("GetBatchListAsync called - Page: {Page}, PageSize: {PageSize}, BatchStatusId: {BatchStatusId}",
            request.PageNumber, request.PageSize, request.BatchStatusId);

        try
        {
            _logger.LogInformation("Repository implementation: {Repo}", _unitOfWork.Batches.GetType().FullName);

            var (batches, totalCount) = await _unitOfWork.Batches.GetPagedAsync(
                request.PageNumber,
                request.PageSize,
                request.BatchStatusId);

            _logger.LogInformation("Repository returned {Count} batch row(s) (total matching: {Total}).",
                batches.Count, totalCount);

            var dtos = batches.Select(b => new BatchDto
            {
                BatchId = b.BatchId,
                BatchNumber = b.BatchNumber,
                Name = b.Name,
                BatchStatusId = b.BatchStatusId ?? 0,
                Status = b.BatchStatus?.Name ?? string.Empty,
                ScanMode = b.ScanMode,
                TotalDocuments = b.TotalDocuments,
                TotalPages = b.TotalPages,
                StartedDate = b.StartedDate,
                CompletedDate = b.CompletedDate
            }).ToList();

            _logger.LogInformation("Returning {Count} batches (total: {Total})", dtos.Count, totalCount);

            return new GetBatchListResponse
            {
                Success = true,
                Message = "Batch list retrieved successfully.",
                Data = dtos,
                TotalCount = totalCount,
                PageNumber = request.PageNumber,
                PageSize = request.PageSize
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving batch list");
            return new GetBatchListResponse
            {
                Success = false,
                Message = "An error occurred while retrieving the batch list."
            };
        }
    }

    public async Task<SaveBatchResponse> SaveBatchAsync(SaveBatchRequest request)
    {
        _logger.LogInformation("SaveBatchAsync called - Name: {Name}", request.Name);

        try
        {
            var now = DateTime.UtcNow;

            // Auto-generate a unique batch number (timestamp-based, incrementing)
            var batchNumber = now.ToString("yyyyMMddHHmmssfff");

            var batch = new Batch
            {
                BatchNumber = batchNumber,
                Name = request.Name,
                ScanQueueId = request.ScanQueueId,
                ScannerId = request.ScannerId,
                ScanningFormatId = request.ScanningFormatId,
                ScanMode = request.ScanMode,
                BatchStatusId = request.BatchStatusId,
                TotalDocuments = request.TotalDocuments,
                TotalPages = request.TotalPages,
                StartedDate = request.StartedDate,
                CompletedDate = request.CompletedDate,
                IsActive = request.IsActive,
                CreatedBy = "system",
                CreatedDate = now,
                UpdatedBy = "system",
                UpdatedDate = now,
                IsDeleted = false
            };

            var saved = await _unitOfWork.Batches.SaveAsync(batch);

            if (saved is null)
            {
                _logger.LogWarning("Batch with number {BatchNumber} already exists.", batchNumber);
                return new SaveBatchResponse
                {
                    Success = false,
                    Message = $"A batch with number '{batchNumber}' already exists."
                };
            }

            _logger.LogInformation("Batch saved successfully with id={Id}.", saved.BatchId);

            // Load the BatchStatus to return the status name
            string statusName = string.Empty;
            if (saved.BatchStatusId.HasValue)
            {
                var batchStatus = await _unitOfWork.BatchStatuses.GetByIdAsync(saved.BatchStatusId.Value);
                statusName = batchStatus?.Name ?? string.Empty;
            }

            return new SaveBatchResponse
            {
                Success = true,
                Message = "Batch saved successfully.",
                Data = new BatchDto
                {
                    BatchId = saved.BatchId,
                    BatchNumber = saved.BatchNumber,
                    Name = saved.Name,
                    BatchStatusId = saved.BatchStatusId ?? 0,
                    Status = statusName,
                    ScanMode = saved.ScanMode,
                    TotalDocuments = saved.TotalDocuments,
                    TotalPages = saved.TotalPages,
                    StartedDate = saved.StartedDate,
                    CompletedDate = saved.CompletedDate
                }
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving batch for name {Name}", request.Name);
            return new SaveBatchResponse
            {
                Success = false,
                Message = "An error occurred while saving the batch."
            };
        }
    }

    public async Task<RenameBatchResponse> RenameBatchAsync(int id, RenameBatchRequest request)
    {
        _logger.LogInformation("RenameBatchAsync called - BatchId={Id}, NewName={NewName}, UpdatedBy={UpdatedBy}", 
            id, request.Name, request.UpdatedBy ?? "system");

        try
        {
            var updatedBy = string.IsNullOrWhiteSpace(request.UpdatedBy) ? "system" : request.UpdatedBy;
            var renamed = await _unitOfWork.Batches.RenameAsync(id, request.Name, updatedBy);

            if (renamed is null)
            {
                _logger.LogWarning("Batch not found for rename. BatchId={Id}", id);
                return new RenameBatchResponse
                {
                    Success = false,
                    Message = $"Batch with id {id} not found."
                };
            }

            _logger.LogInformation("Batch renamed successfully. BatchId={Id}, NewName={NewName}", id, request.Name);

            return new RenameBatchResponse
            {
                Success = true,
                Message = "Batch renamed successfully.",
                Data = new BatchDto
                {
                    BatchId = renamed.BatchId,
                    BatchNumber = renamed.BatchNumber,
                    Name = renamed.Name,
                    Status = renamed.BatchStatus?.Name ?? string.Empty,
                    ScanMode = renamed.ScanMode,
                    TotalDocuments = renamed.TotalDocuments,
                    TotalPages = renamed.TotalPages,
                    StartedDate = renamed.StartedDate,
                    CompletedDate = renamed.CompletedDate
                }
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error renaming batch {Id}", id);
            return new RenameBatchResponse
            {
                Success = false,
                Message = "An error occurred while renaming the batch."
            };
        }
    }

    public async Task<GetBatchListResponse> SearchBatchListAsync(BatchListRequest request)
    {
        _logger.LogInformation("SearchBatchListAsync called with {CriteriaCount} search criteria.", request);
        var effectiveFrom = request.FromDate ?? DateTime.MinValue;
        var effectiveTo = request.ToDate ?? DateTime.UtcNow;
        DateTime? cutoffDate = _onBaseConfiguration.CutoverDate?.ToDateTime(TimeOnly.MinValue) ?? null;

        if(cutoffDate == null)
        {
            return await GetCombinedBatchList(request);
        }

        if (effectiveTo < cutoffDate)
        {
            var resp = await _onBaseRepository.SearchBatchsAsync(request);
            if (!resp.ResponseStatus.Success)
            {
                return new GetBatchListResponse()
                {
                    Success = false,
                    Message = $"Error on base searching batches: {resp.ResponseStatus.Message}",
                    Data = new List<BatchDto>(),
                    TotalCount = 0
                };
            }
            var offset = resp.BatchSearchResults.Count() + ((request.PageSize ?? 10) * ((request.PageIndex ?? 1) - 1));

            return new GetBatchListResponse
            {
                Success = true,
                Message = resp.ResponseStatus.Message,
                Data = resp.BatchSearchResults.Select(x => ConvertBatchDTO(x)).ToList(),
                PageSize = resp.paginations.PageSize,
                PageNumber = resp.paginations.PageNumber,
                TotalCount = resp.paginations.TotalCount,
                NextToken = new QueryState() { OnbaseOffset = offset, ScanningOffset = 0 }
            };
        }
        else if (effectiveFrom >= cutoffDate)
        {
            return await _unitOfWork.Batches.SearchBatchListAsync(request);
        }
        else
        {
            return await GetCombinedBatchList(request);
        }

        return new GetBatchListResponse()
        {
            Success = false,
            Message = $"Error on base searching batches:",
            Data = new List<BatchDto>(),
            TotalCount = 0
        };
    }

    public async Task<BatchDto?> GetBatchByIdAsync(int batchId)
    {
        _logger.LogInformation("GetBatchByIdAsync called for batchId={BatchId}", batchId);

        var batch = await _unitOfWork.Batches.GetByIdAsync(batchId);
        if (batch is null)
        {
            _logger.LogWarning("Batch with id={BatchId} not found or deleted.", batchId);
            return null;
        }

        string statusName = string.Empty;
        if (batch.BatchStatusId.HasValue)
        {
            var batchStatus = await _unitOfWork.BatchStatuses.GetByIdAsync(batch.BatchStatusId.Value);
            statusName = batchStatus?.Name ?? string.Empty;
        }

        return new BatchDto
        {
            BatchId = batch.BatchId,
            BatchNumber = batch.BatchNumber,
            Name = batch.Name,
            BatchStatusId = batch.BatchStatusId ?? 0,
            Status = statusName,
            ScanMode = batch.ScanMode,
            TotalDocuments = batch.TotalDocuments,
            TotalPages = batch.TotalPages,
            StartedDate = batch.StartedDate,
            CompletedDate = batch.CompletedDate
        };
    }
    public async Task<DeleteBatchesResponse> DeleteBatchesAsync(DeleteBatchesRequest request)
    {
        _logger.LogInformation("DeleteBatchesAsync called for {Count} batch(es)", request.BatchIds.Count);

        try
        {
            var (successfulIds, failedIds) = await _unitOfWork.Batches.DeleteBatchesAsync(
                request.BatchIds,
                request.DeletedBy);

            if (successfulIds.Count == 0)
            {
                _logger.LogWarning("No batches were deleted. All {Count} batch(es) not found or already deleted",
                    request.BatchIds.Count);
                return new DeleteBatchesResponse
                {
                    Success = false,
                    Message = "No batches were deleted. Batches not found or already deleted.",
                    Data = new DeleteBatchesResultDto
                    {
                        TotalRequested = request.BatchIds.Count,
                        SuccessfullyDeleted = 0,
                        Failed = failedIds.Count,
                        DeletedBatchIds = successfulIds,
                        FailedBatchIds = failedIds,
                        DeletedDate = null
                    }
                };
            }

                await _unitOfWork.SaveChangesAsync();

            var message = failedIds.Count > 0
                ? $"{successfulIds.Count} batch(es) deleted successfully. {failedIds.Count} batch(es) failed (not found or already deleted)."
                : $"{successfulIds.Count} batch(es) deleted successfully.";

            _logger.LogInformation("DeleteBatchesAsync completed: {Success} successful, {Failed} failed",
                successfulIds.Count, failedIds.Count);

            return new DeleteBatchesResponse
            {
                Success = true,
                Message = message,
                Data = new DeleteBatchesResultDto
                {
                    TotalRequested = request.BatchIds.Count,
                    SuccessfullyDeleted = successfulIds.Count,
                    Failed = failedIds.Count,
                    DeletedBatchIds = successfulIds,
                    FailedBatchIds = failedIds,
                    DeletedDate = DateTime.UtcNow,
                    DeletedBy = request.DeletedBy ?? "System"
                }
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting batches");
            return new DeleteBatchesResponse
            {
                Success = false,
                Message = "An error occurred while deleting batches."
            };
        }
    }
    public async Task<BatchScanSummaryResponse> GetBatchScanSummaryAsync(int batchId)
    {
        _logger.LogInformation("GetBatchScanSummaryAsync called - BatchId: {BatchId}", batchId);

        try
        {
            var batch = await _unitOfWork.Batches.GetByIdAsync(batchId);
            if (batch is null || batch.IsDeleted)
            {
                return new BatchScanSummaryResponse
                {
                    Success = false,
                    Message = $"Batch {batchId} was not found."
                };
            }

            var documents = (await _unitOfWork.Documents.GetAllAsync())
                .Where(d => !d.IsDeleted && d.BatchId == batchId)
                .ToList();

            var documentIds = documents.Select(d => d.DocumentId).ToHashSet();

            var pages = (await _unitOfWork.DocumentPages.GetAllAsync())
                .Where(p => !p.IsDeleted && p.DocumentId.HasValue && documentIds.Contains(p.DocumentId.Value))
                .ToList();

            var totalDocuments = documents.Count;
            var totalPages = pages.Count;

            // Duration-based metrics
            double? velocitySecPerPage = null;
            double? estProcessingMinutes = null;
            if (batch.StartedDate.HasValue && batch.CompletedDate.HasValue)
            {
                var duration = batch.CompletedDate.Value - batch.StartedDate.Value;
                if (duration.TotalSeconds > 0)
                {
                    if (totalPages > 0)
                    {
                        velocitySecPerPage = Math.Round(duration.TotalSeconds / totalPages, 2);
                    }
                    estProcessingMinutes = Math.Round(duration.TotalMinutes, 2);
                }
            }

            // Total size = sum of file sizes for each unique DocumentPage.PageURL as stored in the DB.
            long totalBytes = 0;
            var countedPaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var page in pages)
            {
                if (string.IsNullOrWhiteSpace(page.PageURL) || !countedPaths.Add(page.PageURL))
                {
                    continue;
                }

                try
                {
                    var fi = new FileInfo(page.PageURL);
                    if (fi.Exists)
                    {
                        totalBytes += fi.Length;
                        _logger.LogInformation("Counted page file {Path} - Size: {Bytes} bytes (running total: {Total})",
                            page.PageURL, fi.Length, totalBytes);
                    }
                    else
                    {
                        _logger.LogWarning("Page file not found on disk: {Path}", page.PageURL);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to stat page file {Path}", page.PageURL);
                }
            }

            var errorCount = documents.Count(d =>
                !string.IsNullOrWhiteSpace(d.Status) &&
                (d.Status.Contains("Fail", StringComparison.OrdinalIgnoreCase) ||
                 d.Status.Contains("Error", StringComparison.OrdinalIgnoreCase)));

            // Include batch activity log errors (e.g. missing-barcode documents) so the
            // integrity status reflects issues recorded during scanning, not just document status.
            // Only count per-document error entries (those carrying a DocumentId) to avoid
            // double-counting the batch-level "Finished scanning with errors" summary line.
            var activityLogErrorCount = (await _unitOfWork.ActivityLogs.GetAllAsync())
                .Count(a => a.BatchId == batchId &&
                            a.DocumentId.HasValue &&
                            !string.IsNullOrWhiteSpace(a.LogLevel) &&
                            a.LogLevel.Equals("Error", StringComparison.OrdinalIgnoreCase));

            errorCount += activityLogErrorCount;

            _logger.LogInformation("Batch {BatchId} - TotalDocuments: {TotalDocs}, TotalPages: {TotalPages}, UniquePageFilesCounted: {Unique}, TotalSizeBytes: {TotalSize}, VelocitySecPerPage: {Velocity}, EstProcessingMinutes: {EstMinutes}",
                batchId, totalDocuments, totalPages, countedPaths.Count, totalBytes, velocitySecPerPage, estProcessingMinutes);

            var dto = new BatchScanSummaryDto
            {
                BatchId = batch.BatchId,
                BatchNumber = batch.BatchNumber ?? string.Empty,
                Name = batch.Name ?? string.Empty,
                StartedDate = batch.StartedDate,
                CompletedDate = batch.CompletedDate,
                TotalDocuments = totalDocuments,
                TotalPages = totalPages,
                ScanVelocitySecondsPerPage = velocitySecPerPage,
                EstimatedProcessingMinutes = estProcessingMinutes,
                TotalSizeBytes = totalBytes,
                TotalSizeDisplay = FormatBytes(totalBytes),
                ErrorCount = errorCount
            };

            return new BatchScanSummaryResponse
            {
                Success = true,
                Message = "Batch scan summary retrieved successfully.",
                Data = dto
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving batch scan summary for BatchId {BatchId}", batchId);
            return new BatchScanSummaryResponse
            {
                Success = false,
                Message = "An error occurred while retrieving the batch scan summary."
            };
        }
    }

    public async Task<GetActivityLogsByBatchIdResponse> GetActivityLogsByBatchIdAsync(int batchId)
    {
        _logger.LogInformation("GetActivityLogsByBatchIdAsync called - BatchId: {BatchId}", batchId);

        try
        {
            var logs = (await _unitOfWork.ActivityLogs.FindAsync(a => a.BatchId == batchId))
                .OrderByDescending(a => a.LoggedDate)
                .Select(a => new ActivityLogDto
                {
                    ActivityLogId = a.ActivityLogId,
                    BatchId = a.BatchId,
                    UserId = a.UserId,
                    DocumentId = a.DocumentId,
                    DocumentsScanned = a.DocumentsScanned,
                    PagesScanned = a.PagesScanned,
                    LogLevel = a.LogLevel,
                    QueueId = a.QueueId,
                    Description = a.Description,
                    LoggedDate = a.LoggedDate,
                    CreatedDate = a.CreatedDate
                })
                .ToList();

            _logger.LogInformation("Retrieved {Count} activity log(s) for BatchId {BatchId}", logs.Count, batchId);

            return new GetActivityLogsByBatchIdResponse
            {
                Success = true,
                Message = "Activity logs retrieved successfully.",
                Data = logs
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving activity logs for BatchId {BatchId}", batchId);
            return new GetActivityLogsByBatchIdResponse
            {
                Success = false,
                Message = "An error occurred while retrieving the activity logs."
            };
        }
    }

    public async Task LogActivityAsync(int batchId, string userId, string logLevel, string? description = null,
        int documentsScanned = 0, int pagesScanned = 0, int? documentId = null)
    {
        try
        {
            var now = DateTime.UtcNow;
            var batch = await _unitOfWork.Batches.GetByIdAsync(batchId);

            var activityLog = new ActivityLog
            {
                BatchId = batchId,
                DocumentId = documentId,
                UserId = string.IsNullOrWhiteSpace(userId) ? "system" : userId,
                DocumentsScanned = documentsScanned,
                PagesScanned = pagesScanned,
                LogLevel = logLevel,
                QueueId = batch?.ScanQueueId,
                Description = description,
                LoggedDate = now,
                CreatedDate = now
            };

            await _unitOfWork.ActivityLogs.AddAsync(activityLog);
            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation("Activity logged for BatchId {BatchId} - Level: {LogLevel}", batchId, logLevel);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to write activity log for BatchId {BatchId}", batchId);
        }
    }

    public async Task<RenameBatchResponse> UpdateBatchStatusAsync(int id, UpdateBatchStatusRequest request)
    {
        _logger.LogInformation("UpdateBatchStatusAsync called - BatchId={Id}, StatusId={StatusId}, UpdatedBy={UpdatedBy}",
            id, request.statusId, request.UpdatedBy ?? "system");

        try
        {
            var updatedBy = string.IsNullOrWhiteSpace(request.UpdatedBy) ? "system" : request.UpdatedBy;

            // Validate the target status exists before attempting the update so an invalid
            // statusId returns a clear message instead of a foreign-key violation.
            var status = await _unitOfWork.BatchStatuses.GetByIdAsync(request.statusId);
            if (status is null)
            {
                _logger.LogWarning("Invalid status for update. BatchId={Id}, StatusId={StatusId}", id, request.statusId);
                return new RenameBatchResponse
                {
                    Success = false,
                    Message = $"Batch status with id {request.statusId} not found."
                };
            }

            var renamed = await _unitOfWork.Batches.UpdateStatusAsync(id, request.statusId, updatedBy);

            if (renamed is null)
            {
                _logger.LogWarning("Batch not found for update status. BatchId={Id}", id);
                return new RenameBatchResponse
                {
                    Success = false,
                    Message = $"Batch with id {id} not found."
                };
            }

            // Mark all documents belonging to this batch as committed against the batch.
            // Uses a bulk DB update (no change tracking) so shared navigation entities such as
            // DocumentType are not attached, avoiding duplicate-key tracking errors.
            var affected = await _unitOfWork.Documents.UpdateDocumentsStatusByBatchIdAsync(
                id, AppConstants.CommittedDocumentStatus, updatedBy);

            if (affected > 0)
            {
                _logger.LogInformation("Marked {Count} document(s) as {Status} for BatchId={Id}",
                    affected, AppConstants.CommittedDocumentStatus, id);
            }

            _logger.LogInformation("Batch renamed successfully. BatchId={Id}, NewStatusId={statusId}", id, request.statusId);

            return new RenameBatchResponse
            {
                Success = true,
                Message = "Batch status updated successfully.",
                Data = new BatchDto
                {
                    BatchId = renamed.BatchId,
                    BatchNumber = renamed.BatchNumber,
                    Name = renamed.Name,
                    Status = renamed.BatchStatus?.Name ?? string.Empty,
                    ScanMode = renamed.ScanMode,
                    TotalDocuments = renamed.TotalDocuments,
                    TotalPages = renamed.TotalPages,
                    StartedDate = renamed.StartedDate,
                    CompletedDate = renamed.CompletedDate
                }
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating status batch {Id}", id);
            return new RenameBatchResponse
            {
                Success = false,
                Message = "An error occurred while updating the batch status."
            };
        }
    }

    public async Task<bool> MarkBatchCompletedAsync(int batchId, string? updatedBy = null)
    {
        _logger.LogInformation("MarkBatchCompletedAsync called - BatchId={BatchId}", batchId);

        try
        {
            var by = string.IsNullOrWhiteSpace(updatedBy) ? "system" : updatedBy;
            var batch = await _unitOfWork.Batches.SetCompletedDateAsync(batchId, DateTime.UtcNow, by);

            if (batch is null)
            {
                _logger.LogWarning("Batch not found while marking completed. BatchId={BatchId}", batchId);
                return false;
            }

            _logger.LogInformation("Batch {BatchId} marked completed at {CompletedDate}", batchId, batch.CompletedDate);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error marking batch completed. BatchId={BatchId}", batchId);
            return false;
        }
    }

    public async Task<bool> TryAutoCommitBatchAsync(int batchId, string? updatedBy = null)
    {
        _logger.LogInformation("TryAutoCommitBatchAsync called - BatchId={BatchId}", batchId);

        try
        {
            var batch = await _unitOfWork.Batches.GetByIdAsync(batchId);
            if (batch is null || batch.IsDeleted)
            {
                _logger.LogWarning("Batch not found while attempting auto-commit. BatchId={BatchId}", batchId);
                return false;
            }

            if (!batch.ScanQueueId.HasValue)
            {
                _logger.LogInformation("Batch {BatchId} has no scan queue; skipping auto-commit.", batchId);
                return false;
            }

            var scanQueue = await _unitOfWork.ScanQueues.GetByIdAsync(batch.ScanQueueId.Value);
            if (scanQueue is null || !scanQueue.IsAutoCommit)
            {
                _logger.LogInformation("Scan queue {ScanQueueId} for batch {BatchId} is not configured for auto-commit; skipping.",
                    batch.ScanQueueId, batchId);
                return false;
            }

            // Only auto-commit when the batch has no errors.
            var errorCount = await GetBatchErrorCountAsync(batchId);
            if (errorCount > 0)
            {
                _logger.LogInformation("Batch {BatchId} has {ErrorCount} error(s); skipping auto-commit.", batchId, errorCount);
                return false;
            }

            var committedStatus = (await _unitOfWork.BatchStatuses.GetAllAsync())
                .FirstOrDefault(s => !string.IsNullOrWhiteSpace(s.Name) &&
                                     s.Name.Equals(AppConstants.CommittedBatchStatus, StringComparison.OrdinalIgnoreCase));

            if (committedStatus is null)
            {
                _logger.LogWarning("Committed batch status '{Status}' not found; cannot auto-commit BatchId={BatchId}.",
                    AppConstants.CommittedBatchStatus, batchId);
                return false;
            }

            var response = await UpdateBatchStatusAsync(batchId, new UpdateBatchStatusRequest
            {
                statusId = committedStatus.BatchStatusId,
                UpdatedBy = updatedBy
            });

            if (response.Success)
            {
                _logger.LogInformation("Batch {BatchId} auto-committed successfully via scan queue {ScanQueueId}.",
                    batchId, batch.ScanQueueId);
            }

            return response.Success;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during auto-commit for BatchId={BatchId}", batchId);
            return false;
        }
    }
    
            
    public async Task<bool> MarkBatchStartedAsync(int batchId, string? updatedBy = null)
    {
        _logger.LogInformation("MarkBatchStartedAsync called - BatchId={BatchId}", batchId);

        try
        {
            var by = string.IsNullOrWhiteSpace(updatedBy) ? "system" : updatedBy;
            var batch = await _unitOfWork.Batches.SetStartedDateAsync(batchId, DateTime.UtcNow, by);

            if (batch is null)
            {
                _logger.LogWarning("Batch not found while marking started. BatchId={BatchId}", batchId);
                return false;
            }

            _logger.LogInformation("Batch {BatchId} started date is {StartedDate}", batchId, batch.StartedDate);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error marking batch started. BatchId={BatchId}", batchId);
            return false;
        }
    }

    /// <summary>
    /// Counts the errors for a batch based on document status and per-document activity log errors.
    /// </summary>
    private async Task<int> GetBatchErrorCountAsync(int batchId)
    {
        var documents = (await _unitOfWork.Documents.GetAllAsync())
            .Where(d => !d.IsDeleted && d.BatchId == batchId)
            .ToList();

        var errorCount = documents.Count(d =>
            !string.IsNullOrWhiteSpace(d.Status) &&
            (d.Status.Contains("Fail", StringComparison.OrdinalIgnoreCase) ||
             d.Status.Contains("Error", StringComparison.OrdinalIgnoreCase)));

        var activityLogErrorCount = (await _unitOfWork.ActivityLogs.GetAllAsync())
            .Count(a => a.BatchId == batchId &&
                        a.DocumentId.HasValue &&
                        !string.IsNullOrWhiteSpace(a.LogLevel) &&
                        a.LogLevel.Equals("Error", StringComparison.OrdinalIgnoreCase));

        return errorCount + activityLogErrorCount;
    }

    public async Task<bool> UpdateBatchCountsAsync(int batchId, string? updatedBy = null)
    {
        _logger.LogInformation("UpdateBatchCountsAsync called - BatchId={BatchId}", batchId);

        try
        {
            var batch = await _unitOfWork.Batches.GetByIdAsync(batchId);
            if (batch is null || batch.IsDeleted)
            {
                _logger.LogWarning("Batch not found while updating counts. BatchId={BatchId}", batchId);
                return false;
            }

            var documents = (await _unitOfWork.Documents.GetAllAsync())
                .Where(d => !d.IsDeleted && d.IsActive && d.BatchId == batchId)
                .ToList();

            var documentIds = documents.Select(d => d.DocumentId).ToHashSet();

            var totalPages = (await _unitOfWork.DocumentPages.GetAllAsync())
                .Count(p => !p.IsDeleted && p.DocumentId.HasValue && documentIds.Contains(p.DocumentId.Value));

            batch.TotalDocuments = documents.Count;
            batch.TotalPages = totalPages;
            batch.UpdatedBy = string.IsNullOrWhiteSpace(updatedBy) ? "system" : updatedBy;
            batch.UpdatedDate = DateTime.Now;

            await _unitOfWork.Batches.UpdateAsync(batch);
            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation("Batch {BatchId} counts updated - TotalDocuments={TotalDocuments}, TotalPages={TotalPages}",
                batchId, batch.TotalDocuments, batch.TotalPages);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating batch counts. BatchId={BatchId}", batchId);
            return false;
        }
    }

    
    private static string FormatBytes(long bytes)
    {
        string[] units = { "B", "KB", "MB", "GB", "TB" };
        double value = bytes;
        var unit = 0;
        while (value >= 1024 && unit < units.Length - 1)
        {
            value /= 1024;
            unit++;
        }
        return $"{value:0.##} {units[unit]}";
    }

    private async Task<GetBatchListResponse> GetCombinedBatchList(BatchListRequest request)
    {
        try
        {
            var onBaseResp = await _onBaseRepository.SearchBatchsAsync(request);
            var scanningResp = await _unitOfWork.Batches.SearchBatchListAsync(request);
            var TotalCount = (onBaseResp.paginations?.TotalCount ?? 0) + scanningResp.TotalCount;


            List<BatchDto> allResp = new List<BatchDto>();
            allResp.AddRange(scanningResp.Data);
            allResp.AddRange(onBaseResp.BatchSearchResults?.Select(x => ConvertBatchDTO(x)).ToList() ?? new List<BatchDto>());

            // Now need to sort out the List and paginated the data 
            allResp = request.OrderBy?.ToLower() switch
            {
                "scanqueue" => request.SortBy.ToLower() == "desc"
                    ? allResp.OrderByDescending(x => x.ScanQueue).ToList()
                    : allResp.OrderBy(x => x.ScanQueue).ToList(),

                "batchnumber" => request.SortBy.ToLower() == "desc"
                    ? allResp.OrderByDescending(x => x.BatchNumber).ToList()
                    : allResp.OrderBy(x => x.BatchNumber).ToList(),

                "name" => request.SortBy.ToLower() == "desc"
                    ? allResp.OrderByDescending(x => x.Name).ToList()
                    : allResp.OrderBy(x => x.Name).ToList(),

                "scandatetime" => request.SortBy.ToLower() == "desc"
                    ? allResp.OrderByDescending(x => x.StartedDate).ToList()
                    : allResp.OrderBy(x => x.StartedDate).ToList(),

                "totaldocuments" => request.SortBy.ToLower() == "desc"
                    ? allResp.OrderByDescending(x => x.TotalDocuments).ToList()
                    : allResp.OrderBy(x => x.TotalDocuments).ToList(),

                _ => allResp
            };


            if (request.SortBy.ToLower().Trim() == "asc")
                allResp = allResp.OrderBy(x => request.OrderBy).Take(request.PageSize ?? 10).ToList();
            else
                allResp = allResp.OrderByDescending(x => request.OrderBy).Take(request.PageSize ?? 10).ToList();




            // Now Need to add manage the NextToken
            var OnBaseCount = allResp.Where(x => x.DataSource == Common.Enums.DataSource.OnBase).Count();
            var ScanningCount = allResp.Where(x => x.DataSource == Common.Enums.DataSource.Scanning).Count();

            var nextToken = new QueryState()
            {
                OnbaseOffset = request.NextToken.OnbaseOffset + OnBaseCount,
                ScanningOffset = request.NextToken.ScanningOffset + ScanningCount
            };


            return new GetBatchListResponse
            {
                Success = true,
                Message = "",
                Data = allResp,
                PageSize = request.PageSize ?? 10,
                PageNumber = request.PageIndex ?? 1,
                TotalCount = TotalCount,
                NextToken = nextToken
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error combined batch list data");
            return new GetBatchListResponse()
            {
                Success = false,
                Message = $"Error on base searching batches:",
                Data = new List<BatchDto>(),
                TotalCount = 0
            };
        }
    }

    private BatchDto ConvertBatchDTO(BatchSPResult req)
    {
        return new BatchDto
        {
            BatchId = req.batchnum,
            BatchNumber = req.batchnum.ToString(),
            Name = req.BatchName,
            BatchStatusId = req.StatusId,
            Status = req.BatchStatus,
            ScanQueue = req.ScanQueue,
            TotalDocuments = req.totaldocuments ?? 0,
            StartedDate = req.ScanDateTime,
            DataSource = Common.Enums.DataSource.OnBase
        };
    }

    
}
