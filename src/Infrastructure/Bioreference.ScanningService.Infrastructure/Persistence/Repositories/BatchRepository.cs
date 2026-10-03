using Bioreference.ScanningService.Application.Common.Interfaces;
using Bioreference.ScanningService.Application.DTOs.Requests;
using Bioreference.ScanningService.Application.DTOs.Responses;
using Bioreference.ScanningService.Domain.Entities;
using Microsoft.Extensions.Logging;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using System.Data;
using System.Numerics;

namespace Bioreference.ScanningService.Infrastructure.Persistence.Repositories;

public class BatchRepository : Repository<Batch>, IBatchRepository
{
    private readonly ILogger<BatchRepository> _logger;

    public BatchRepository(ScanningServiceDbContext context, ILogger<BatchRepository> logger) : base(context)
    {
        _logger = logger;
    }

    public async Task<(IReadOnlyList<Batch> Items, int TotalCount)> GetPagedAsync(
        int pageNumber,
        int pageSize,
        int? batchStatusId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("GetPagedAsync starting - PageNumber={PageNumber}, PageSize={PageSize}, BatchStatusId={BatchStatusId}",
                pageNumber, pageSize, batchStatusId);

            // First, check if we have any batches at all
            var allBatchesCount = await _context.Batch.CountAsync(cancellationToken);
            _logger.LogInformation("Total batches in database: {Count}", allBatchesCount);

            var nonDeletedCount = await _context.Batch.Where(b => !b.IsDeleted).CountAsync(cancellationToken);
            _logger.LogInformation("Non-deleted batches: {Count}", nonDeletedCount);

            // Build the base query - start without includes to avoid filtering
            var baseQuery = _context.Batch.Where(b => !b.IsDeleted);

            // Apply status filter if provided and valid (> 0)
            if (batchStatusId.HasValue && batchStatusId.Value > 0)
            {
                baseQuery = baseQuery.Where(b => b.BatchStatusId == batchStatusId.Value);
                _logger.LogInformation("Applied BatchStatusId filter: {StatusId}", batchStatusId.Value);
            }

            // Get total count before pagination
            var totalCount = await baseQuery.CountAsync(cancellationToken);
            _logger.LogInformation("Total count after filters: {Count}", totalCount);

            // Apply pagination, ordering, and includes
            var items = await baseQuery
                .OrderByDescending(b => b.CreatedDate)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .Include(b => b.BatchStatus)
                .Include(b => b.ScanQueue)
                .Include(b => b.ScanningFormat)
                .AsNoTracking()
                .ToListAsync(cancellationToken);

            _logger.LogInformation("Retrieved {Count} batches after includes for page {Page}",
                items.Count, pageNumber);

            return (items, totalCount);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in GetPagedAsync. PageNumber={PageNumber}, PageSize={PageSize}, BatchStatusId={BatchStatusId}",
                pageNumber, pageSize, batchStatusId);
            throw;
        }
    }

    public async Task<Batch?> GetByBatchNumberAsync(string batchNumber, CancellationToken cancellationToken = default)
    {
        try
        {
            return await _dbSet.AsNoTracking()
                .FirstOrDefaultAsync(b => b.BatchNumber == batchNumber, cancellationToken);
        }
        catch (SqlException ex)
        {
            _logger.LogError(ex, "SQL error in GetByBatchNumberAsync. BatchNumber={BatchNumber}", batchNumber);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error in GetByBatchNumberAsync. BatchNumber={BatchNumber}", batchNumber);
            throw;
        }
    }

    public async Task<Batch?> SaveAsync(Batch batch, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(batch);

        try
        {
            // Check for duplicate batch number
            var existingBatch = await _context.Batch
                .AsNoTracking()
                .FirstOrDefaultAsync(b => b.BatchNumber == batch.BatchNumber, cancellationToken);

            if (existingBatch != null)
            {
                _logger.LogWarning("Duplicate batch number detected in SaveAsync. BatchNumber={BatchNumber}", batch.BatchNumber);
                return null;
            }

            // Set timestamps
            var now = DateTime.Now;
            batch.CreatedDate = now;
            batch.UpdatedDate = now;

            // Add the batch to the context
            await _context.Batch.AddAsync(batch, cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Batch saved successfully. BatchId={BatchId}, BatchNumber={BatchNumber}", 
                batch.BatchId, batch.BatchNumber);

            // Return the saved batch with generated ID
            return batch;
        }
        catch (DbUpdateException ex) when (IsDuplicateKeyException(ex))
        {
            _logger.LogWarning(ex, "Duplicate batch number detected in SaveAsync. BatchNumber={BatchNumber}", batch.BatchNumber);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error in SaveAsync. BatchNumber={BatchNumber}", batch.BatchNumber);
            throw;
        }
    }

    private static bool IsDuplicateKeyException(DbUpdateException ex)
    {
        // Check if the inner exception is a SqlException with duplicate key error
        if (ex.InnerException is SqlException sqlException)
        {
            // SQL Server error codes for unique constraint violations:
            // 2601 - Cannot insert duplicate key row with unique index
            // 2627 - Violation of UNIQUE KEY constraint
            return sqlException.Number == 2601 || sqlException.Number == 2627;
        }
        return false;
    }

    public Task<GetBatchListResponse> SearchBatchListAsync(BatchListRequest request)
    {
        try
        {
            _logger.LogInformation("SearchBatchListAsync called with BatchStatusId={StatusId}, BatchName={Name}, ScannedDate={Date}, ScanQueueId={QueueId}",
                request?.BatchStatusId, request?.BatchName, request?.ToDate, request?.ScanQueueId);

            // Start with base query - active and not deleted batches
            IQueryable<Batch> query = _context.Batch
                .Where(b => b.IsActive && !b.IsDeleted);

            // Log base count before filters
            var baseCount = query.Count();
            _logger.LogInformation("Base query (IsActive && !IsDeleted) found {Count} batches", baseCount);

            // Log sample data for debugging
            if (baseCount > 0)
            {
                var sample = query
                    .Include(b => b.BatchStatus)
                    .Select(b => new { 
                        b.Name, 
                        StatusName = b.BatchStatus != null ? b.BatchStatus.Name : "NULL",
                        CreatedDate = b.CreatedDate,
                        b.ScanQueueId 
                    })
                    .Take(5)
                    .ToList();

                _logger.LogInformation("Sample batches in DB: {Samples}", 
                    string.Join("; ", sample.Select(s => 
                        $"Name='{s.Name}', Status='{s.StatusName}', Date={s.CreatedDate:yyyy-MM-dd}, QueueId={s.ScanQueueId}")));
            }

            // Apply filters based on request criteria
            if (request != null)
            {
                // Filter by Batch Status ID (if provided and > 0)
                if (request.BatchStatusId.HasValue && request.BatchStatusId.Value > 0)
                {
                    var batchStatusId = request.BatchStatusId.Value;
                    query = query.Where(b => b.BatchStatusId == batchStatusId);
                    var statusCount = query.Count();
                    _logger.LogInformation("After BatchStatusId filter '{StatusId}': {Count} batches", batchStatusId, statusCount);
                }

                // Filter by Batch Name (partial match, if provided and not empty/default) - case-insensitive
                if (!string.IsNullOrWhiteSpace(request.BatchName) && 
                    !request.BatchName.Equals("string", StringComparison.OrdinalIgnoreCase))
                {
                    var batchName = request.BatchName.Trim();
                    query = query.Where(b => b.Name.ToLower().Contains(batchName.ToLower()));
                    var nameCount = query.Count();
                    _logger.LogInformation("After BatchName filter '{Name}': {Count} batches", batchName, nameCount);
                }

                if (request.FromDate.HasValue)
                {
                    var fromDate = request.FromDate.Value.Date;
                    query = query.Where(b => b.CreatedDate >= fromDate);
                }

                if (request.ToDate.HasValue)
                {
                    var toDateExclusive = request.ToDate.Value.Date.AddDays(1);
                    query = query.Where(b => b.CreatedDate < toDateExclusive);
                }

                var dateCount = query.Count();

                _logger.LogInformation(
                    "After ScannedDate range filter {From} - {To}: {Count} batches",
                    request.FromDate,
                    request.ToDate,
                    dateCount);


                // Filter by Scan Queue ID (if provided and > 0)
                if (request.ScanQueueId.HasValue && request.ScanQueueId.Value > 0)
                {
                    var scanQueueId = request.ScanQueueId.Value;
                    query = query.Where(b => b.ScanQueueId == scanQueueId);
                    var queueCount = query.Count();
                    _logger.LogInformation("After ScanQueueId filter '{QueueId}': {Count} batches", scanQueueId, queueCount);
                }
            }

            query = query
                .Include(b => b.BatchStatus)
                .Include(b => b.ScanQueue);

            var totalCount = query.Count();

            switch (request.OrderBy?.ToLower())
            {
                case "scanqueue":
                    query = request.SortBy?.ToLower() == "asc"
                        ? query.OrderBy(x => x.ScanQueue)
                        : query.OrderByDescending(x => x.ScanQueue);
                    break;

                case "batchnumber":
                    query = request.SortBy?.ToLower() == "asc"
                        ? query.OrderBy(x => x.BatchNumber)
                        : query.OrderByDescending(x => x.BatchNumber);
                    break;
                case "name":
                    query = request.SortBy?.ToLower() == "asc"
                        ? query.OrderBy(x => x.Name)
                        : query.OrderByDescending(x => x.Name);
                    break;

                case "scandatetime":
                    query = request.SortBy?.ToLower() == "asc"
                        ? query.OrderBy(x => x.StartedDate)
                        : query.OrderByDescending(x => x.StartedDate);
                    break;

                case "totaldocuments":
                    query = request.SortBy?.ToLower() == "asc"
                        ? query.OrderBy(x => x.TotalDocuments)
                        : query.OrderByDescending(x => x.TotalDocuments);
                    break;
            }


            if (request.PageIndex.HasValue && request.PageSize.HasValue)
            {
                query = query = query.Skip(request.NextToken?.ScanningOffset ?? 0).Take(request.PageSize ?? 10);
            }

            
            // Execute query with includes and projection
            var dto = query
                .Select(b => new BatchDto
                {
                    BatchNumber = b.BatchNumber,
                    Name = b.Name,
                    BatchId = b.BatchId,
                    CompletedDate = b.CompletedDate,
                    ScanMode = b.ScanMode,
                    StartedDate = b.StartedDate,
                    BatchStatusId = b.BatchStatusId ?? 0,
                    Status = b.BatchStatus != null ? b.BatchStatus.Name : string.Empty,
                    TotalDocuments = b.TotalDocuments,
                    TotalPages = b.TotalPages,
                    ScanQueue = b.ScanQueue != null ? b.ScanQueue.Name : string.Empty,
                    TotalDocumentNotIndexed = 0,
                    DataSource = Application.Common.Enums.DataSource.Scanning
                })
                .ToList();

            _logger.LogInformation("SearchBatchListAsync completed. Final result: {Count} batches", dto.Count);

            var offset = dto.Count + ((request.PageSize ?? 10) * ((request.PageIndex ?? 1) -1));

            return Task.FromResult(new GetBatchListResponse
            {
                Success = true,
                Message = $"Found {dto.Count} batch(es)",
                Data = dto,
                TotalCount = totalCount,
                PageNumber = request.PageIndex ?? 1,
                PageSize = request.PageSize ?? 10,
                NextToken = new QueryState() { OnbaseOffset = 0 , ScanningOffset = offset },
            });
        }
        catch (SqlException ex)
        {
            _logger.LogError(ex, "SQL error in SearchBatchListAsync");
            return Task.FromResult(new GetBatchListResponse
            {
                Success = false,
                Message = $"Database error: {ex.Message}",
                Data = new List<BatchDto>(),
                TotalCount = 0
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error in SearchBatchListAsync");
            return Task.FromResult(new GetBatchListResponse
            {
                Success = false,
                Message = $"Error searching batches: {ex.Message}",
                Data = new List<BatchDto>(),
                TotalCount = 0
            });
        }
    }

    public async Task<Batch?> RenameAsync(int id, string newName, string updatedBy, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("RenameAsync called - BatchId={Id}, NewName={NewName}", id, newName);

        try
        {
            var batch = await _context.Batch
                .Include(b => b.BatchStatus)
                .FirstOrDefaultAsync(b => b.BatchId == id && !b.IsDeleted, cancellationToken);

            if (batch is null)
            {
                _logger.LogWarning("Batch not found for rename. BatchId={Id}", id);
                return null;
            }

            batch.Name = newName;
            batch.UpdatedBy = updatedBy;
            batch.UpdatedDate = DateTime.UtcNow;

            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Batch renamed successfully. BatchId={Id}, NewName={NewName}", id, newName);
            return batch;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error renaming batch. BatchId={Id}", id);
            throw;
        }
    }

    public async Task<(List<int> SuccessfulIds, List<int> FailedIds)> DeleteBatchesAsync(
        List<int> batchIds,
        string? deletedBy = null,
        CancellationToken cancellationToken = default)
    {
        var successfulIds = new List<int>();
        var failedIds = new List<int>();

        try
        {
            var batches = await _context.Batch
                .Where(b => batchIds.Contains(b.BatchId) && !b.IsDeleted)
                .ToListAsync(cancellationToken);

            var documents = await _context.Document
                .Where(d => batchIds.Contains((int)d.BatchId) && !d.IsDeleted)
                .ToListAsync(cancellationToken);

            var foundIds = batches.Select(b => b.BatchId).ToList();

            failedIds.AddRange(batchIds.Except(foundIds));

            var deletionDate = DateTime.UtcNow;
            var deletedByUser = deletedBy ?? "System";

            // Soft delete batches
            foreach (var batch in batches)
            {
                batch.IsActive = false;
                batch.IsDeleted = true;
                batch.UpdatedBy = deletedByUser;
                batch.UpdatedDate = deletionDate;

                successfulIds.Add(batch.BatchId);
            }

            // Soft delete documents belonging to those batches
            foreach (var document in documents)
            {
                document.IsActive = false;
                document.IsDeleted = true;
                document.UpdatedBy = deletedByUser;
                document.UpdatedDate = deletionDate;
            }

            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                "Soft-deleted {SuccessCount} batch(es) and {DocumentCount} document(s) out of {RequestedCount} requested batches by {DeletedBy}. Failed: {FailedCount}",
                successfulIds.Count,
                documents.Count,
                batchIds.Count,
                deletedByUser,
                failedIds.Count);

            return (successfulIds, failedIds);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error deleting batches. Requested IDs: {BatchIds}",
                string.Join(", ", batchIds));

            throw;
        }
    }

    public async Task<IReadOnlyDictionary<int, DateTime>> GetLastCompletedDatesByQueueAsync(
        IReadOnlyCollection<int> scanQueueIds,
        CancellationToken cancellationToken = default)
    {
        if (scanQueueIds is null || scanQueueIds.Count == 0)
        {
            return new Dictionary<int, DateTime>();
        }

        try
        {
            // Project only the columns we need to avoid materializing the full Batch entity,
            // which can throw SqlNullValueException when non-nullable string columns contain NULL.
            var rows = await _context.Batch
                .AsNoTracking()
                .Where(b => !b.IsDeleted)
                .Where(b => b.ScanQueueId.HasValue && scanQueueIds.Contains(b.ScanQueueId.Value))
                .Where(b => b.CompletedDate.HasValue)
                .Select(b => new { ScanQueueId = b.ScanQueueId!.Value, CompletedDate = b.CompletedDate!.Value })
                .ToListAsync(cancellationToken);

            return rows
                .GroupBy(r => r.ScanQueueId)
                .ToDictionary(g => g.Key, g => g.Max(x => x.CompletedDate));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in GetLastCompletedDatesByQueueAsync. QueueIds: {Ids}",
                string.Join(",", scanQueueIds));
            throw;
        }
    }

    public async Task<Batch?> UpdateStatusAsync(int id, int statusId, string updatedBy, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("UpdateStatusAsync called - BatchId={Id}, NewStatus={statusID}", id, statusId);

        try
        {
            var batch = await _context.Batch
                .Include(b => b.BatchStatus)
                .FirstOrDefaultAsync(b => b.BatchId == id && !b.IsDeleted, cancellationToken);

            if (batch is null)
            {
                _logger.LogWarning("Batch not found for Update status. BatchId={Id}", id);
                return null;
            }

            batch.BatchStatusId = statusId;
            batch.UpdatedBy = updatedBy;
            batch.UpdatedDate = DateTime.UtcNow;

            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Batch status update successfully. BatchId={Id}, NewStatus={stausId}", id, statusId);
            return batch;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error Updating batch status. BatchId={Id}", id);
            throw;
        }
    }
    public async Task<Batch?> SetStartedDateAsync(int id, DateTime startedDate, string updatedBy, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("SetStartedDateAsync called - BatchId={Id}, StartedDate={StartedDate}", id, startedDate);

        try
        {
            var batch = await _context.Batch
                .Include(b => b.BatchStatus)
                .FirstOrDefaultAsync(b => b.BatchId == id && !b.IsDeleted, cancellationToken);

            if (batch is null)
            {
                _logger.LogWarning("Batch not found for set started date. BatchId={Id}", id);
                return null;
            }

            // Only record the start once so resuming a scan does not overwrite the original start time.
            if (batch.StartedDate.HasValue)
            {
                _logger.LogInformation("Batch StartedDate already set; leaving unchanged. BatchId={Id}", id);
                return batch;
            }

            batch.StartedDate = startedDate;
            batch.UpdatedBy = updatedBy;
            batch.UpdatedDate = DateTime.UtcNow;

            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Batch started date set successfully. BatchId={Id}", id);
            return batch;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error setting batch started date. BatchId={Id}", id);
            throw;
        }
    }

    public async Task<Batch?> SetCompletedDateAsync(int id, DateTime completedDate, string updatedBy, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("SetCompletedDateAsync called - BatchId={Id}, CompletedDate={CompletedDate}", id, completedDate);

        try
        {
            var batch = await _context.Batch
                .Include(b => b.BatchStatus)
                .FirstOrDefaultAsync(b => b.BatchId == id && !b.IsDeleted, cancellationToken);

            if (batch is null)
            {
                _logger.LogWarning("Batch not found for set completed date. BatchId={Id}", id);
                return null;
            }

            batch.CompletedDate = completedDate;
            batch.UpdatedBy = updatedBy;
            batch.UpdatedDate = DateTime.UtcNow;

            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Batch completed date set successfully. BatchId={Id}", id);
            return batch;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error setting batch completed date. BatchId={Id}", id);
            throw;
        }
    }

}
