using Bioreference.ScanningService.Application.DTOs.Requests;
using Bioreference.ScanningService.Application.DTOs.Responses;
using Bioreference.ScanningService.Domain.Entities;

namespace Bioreference.ScanningService.Application.Common.Interfaces;

public interface IBatchRepository : IRepository<Batch>
{
    /// <summary>
    /// Returns a page of batches, optionally filtered by BatchStatusId, plus the total matching count.
    /// </summary>
    Task<(IReadOnlyList<Batch> Items, int TotalCount)> GetPagedAsync(
        int pageNumber,
        int pageSize,
        int? batchStatusId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns the batch matching the given unique batch number, or null if none exists.
    /// </summary>
    Task<Batch?> GetByBatchNumberAsync(string batchNumber, CancellationToken cancellationToken = default);

    /// <summary>
    /// Inserts a new batch using Entity Framework and returns the freshly-inserted entity.
    /// Returns null when a duplicate batch number is detected.
    /// </summary>
    Task<Batch?> SaveAsync(Batch batch, CancellationToken cancellationToken = default);

    /// <summary>
    /// Renames an existing batch via the dbo.sp_Batch_Rename stored procedure.
    /// Returns the updated batch, or null if the batch doesn't exist.
    /// </summary>
    Task<Batch?> RenameAsync(int id, string newName, string updatedBy, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Update status of an existing batch.
    /// Returns the updated batch, or null if the batch doesn't exist.
    /// </summary>
    Task<Batch?> UpdateStatusAsync(int id, int statusId, string updatedBy, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sets the StartedDate of a batch (e.g. when scanning starts).
    /// Only sets the value when it has not already been set so resumed scans keep the original start.
    /// Returns the updated batch, or null if the batch doesn't exist.
    /// </summary>
    Task<Batch?> SetStartedDateAsync(int id, DateTime startedDate, string updatedBy, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sets the CompletedDate of a batch (e.g. when scanning finishes).
    /// Returns the updated batch, or null if the batch doesn't exist.
    /// </summary>
    Task<Batch?> SetCompletedDateAsync(int id, DateTime completedDate, string updatedBy, CancellationToken cancellationToken = default);

    Task<GetBatchListResponse> SearchBatchListAsync(BatchListRequest request);

    /// <summary>
    /// Soft-deletes multiple batches by setting IsDeleted = true.
    /// Returns a tuple containing lists of successfully deleted and failed batch IDs.
    /// </summary>
    Task<(List<int> SuccessfulIds, List<int> FailedIds)> DeleteBatchesAsync(List<int> batchIds, string? deletedBy = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// For the given scan queue ids, returns the most recent CompletedDate per queue.
    /// Only non-deleted batches with a non-null CompletedDate are considered.
    /// Queues with no completed batches are omitted from the result.
    /// </summary>
    Task<IReadOnlyDictionary<int, DateTime>> GetLastCompletedDatesByQueueAsync(
        IReadOnlyCollection<int> scanQueueIds,
        CancellationToken cancellationToken = default);
}
