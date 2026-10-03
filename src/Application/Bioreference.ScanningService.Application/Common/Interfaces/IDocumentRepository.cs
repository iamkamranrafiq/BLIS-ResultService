using Bioreference.ScanningService.Application.DTOs.Requests;
using Bioreference.ScanningService.Application.DTOs.Responses;
using Bioreference.ScanningService.Domain.Entities;

namespace Bioreference.ScanningService.Application.Common.Interfaces;

public interface IDocumentRepository : IRepository<Document>
{
    /// <summary>
    /// Gets a document by ID with all related entities: DocumentType, DocumentTypeGroup, Batch, and KeywordValues.
    /// Returns null if the document doesn't exist or is soft-deleted.
    /// </summary>
    Task<Document?> GetByIdWithDetailsAsync(long id, CancellationToken cancellationToken = default);
    Task<DocumentSearchResponse> SearchDocumentAsync(DocumentSearchRequest request);

    /// <summary>
    /// Gets all active document types with their document type groups.
    /// </summary>
    Task<DocumentTypeListResponse> GetDocumentTypesAsync();

    /// <summary>
    /// Gets active document types with their keywords, filtered to the provided DocumentTypeIds.
    /// </summary>
    Task<DocumentTypeKeywordsResponse> GetDocumentTypeKeywordsAsync(List<int> documentTypeIds);

    /// <summary>
    /// Gets all active barcode format document type keywords for a specific document type.
    /// Includes related BarCodeFormat, DocumentType, and Keyword entities.
    /// </summary>
    Task<List<BarCodeFormatDocumentTypeKeyword>> GetBarCodeFormatDocumentTypeKeywordsByDocumentTypeIdAsync(int documentTypeId, CancellationToken cancellationToken = default);
    /// Renames a document by updating its Name property.
    /// Returns null if the document doesn't exist or is soft-deleted.
    /// </summary>
    Task<Document?> RenameDocumentAsync(long documentId, string newName, string? updatedBy = null, CancellationToken cancellationToken = default);
    /// <summary>
    /// Gets all documents for a specific batch with their pages.
    /// Returns empty list if batch has no documents.
    /// </summary>
    Task<List<Document>> GetDocumentsByBatchIdAsync(int batchId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Bulk-updates the status of all non-deleted documents belonging to the given batch.
    /// Executes directly against the database (no change tracking) and returns the number of affected rows.
    /// </summary>
    Task<int> UpdateDocumentsStatusByBatchIdAsync(int batchId, string status, string updatedBy, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a document by ID with related entities: KeywordValues.
    /// Returns null if the document doesn't exist or is soft-deleted.
    /// </summary>
    Task<Document?> GetByIdWithKeywordsAsync(long id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Discards all documents for a batch by setting IsActive = false and IsDeleted = true.
    /// Returns the list of affected document IDs.
    /// </summary>
    //Task<List<long>> DiscardDocumentsByBatchIdAsync(int batchId, string? updatedBy = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates the Status of all non-deleted documents for a batch to the given status.
    /// Returns the number of documents updated.
    /// </summary>

    /// <summary>
    /// Gets the distinct BatchIds for the given document ids, evaluated on the database server.
    /// </summary>
    Task<List<int>> GetDistinctBatchIdsAsync(IEnumerable<long> documentIds, CancellationToken cancellationToken = default);
    Task<List<Document>> GetDocumentsForDeletionAsync(List<long> documentIds, CancellationToken cancellationToken = default);
    void MarkDocumentsDeleted(IEnumerable<Document> documents, string deletedBy);
}
