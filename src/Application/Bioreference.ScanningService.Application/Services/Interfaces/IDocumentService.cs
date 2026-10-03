using Bioreference.ScanningService.Application.Common.Entity;
using Bioreference.ScanningService.Application.Common.Enums;
using Bioreference.ScanningService.Application.DTOs.Requests;
using Bioreference.ScanningService.Application.DTOs.Responses;
using Bioreference.ScanningService.Domain.Entities;
using System.Xml.Linq;

namespace Bioreference.ScanningService.Application.Services.Interfaces;

public interface IDocumentService
{
    Task<DocumentTypeListResponse> GetDocumentTypesAsync();
    Task<DocumentTypeKeywordsResponse> GetDocumentTypeKeywordsAsync(DocumentTypeKeywordsRequest request);
    Task<GetDocumentResponse> GetDocumentByIdAsync(long id, DataSource source = DataSource.Scanning);
    Task<DocumentSearchResponse> SearchDocumentAsync(DocumentSearchRequest request);
    Task<List<BarCodeFormatDocumentTypeKeyword>> GetDocumentTypesBarcodeKeywordAsync(int documentTypeId);
    Task<DocumentType> GetDocumentTypeAsync(int documentTypeId);

    /// <summary>
    /// Resolves a <see cref="DocumentType"/> id from a name taken from an index file
    /// (e.g. "Req - Requisition"). The leading code segment before a " - " separator is stripped
    /// and the remainder is matched (case-insensitive) against the DocumentType DisplayName; the
    /// raw value is also matched against Code and DisplayName as a fallback. Returns null when no
    /// active, non-deleted DocumentType matches.
    /// </summary>
    Task<int?> ResolveDocumentTypeIdByNameAsync(string documentTypeName);
    /// <summary>
    /// Maps detected barcode to BarcodeKeywordDoctype using barcode format keywords configuration.
    /// Returns the first matching barcode keyword configuration with the detected barcode value.
    /// </summary>
    BarcodeKeywordDoctype MapBarcodeToBarcodeKeywordDoctype(List<BarCodeFormatDocumentTypeKeyword> barcodeKeywords, DetectedBarcode detectedBarcode);

    /// <summary>
    /// Generates the document full name and short name based on the DocumentType format templates.
    /// </summary>
    DocumentNames GetDocumentName(int batchId, DocumentType documentType, List<keywordValue> Keywordval);

    /// <summary>
    /// Creates a new Document for the given batch, saves each page's image bytes to network
    /// storage, and persists all DocumentPage rows in a single transaction.
    /// Returns the generated DocumentId and ShortName so callers don't need a follow-up lookup.
    /// </summary>
    Task<(long DocumentId, string ShortName)> CreateDocumentWithPagesAsync(int batchId, DocumentType documentType, List<DetectedImageBarcodes> detectedImageBarcodes, List<keywordValue> keywordValues, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves the file from network storage based on the provided file URL.
    /// Returns the file bytes if found, otherwise returns null.
    /// </summary>
    /// <param name="documentId">The document ID for future use and logging</param>
    /// <param name="fileUrl">The full file path to retrieve</param>
    Task<byte[]?> GetFileAsync(long documentId, string fileUrl);

    /// <summary>
    /// Generates a PDF document from all pages of the specified document.
    /// Fetches all page images from network storage and merges them into a single PDF in memory.
    /// </summary>
    /// <param name="documentId">The document ID</param>
    /// <returns>PDF bytes or null if document not found</returns>
    Task<byte[]?> GenerateDocumentPdfAsync(long documentId, DataSource source);

    /// <summary>
    /// Groups scanned images into separate documents based on barcode detection.
    /// Each group represents a single document. A new document starts when a page with at least one barcode is found.
    /// All previous pages without barcodes are grouped together with the page that has the barcode.
    /// </summary>
    /// <param name="detectedImageBarcodes">List of scanned images with detected barcodes</param>
    /// <returns>List of grouped images, where each group represents one document</returns>
    List<List<DetectedImageBarcodes>> GroupImagesByBarcode(List<DetectedImageBarcodes> detectedImageBarcodes);

    Task<RenameDocumentResponse> RenameDocumentAsync(long documentId, RenameDocumentRequest request);
    Task<DeleteDocumentsResponse> DeleteDocumentsAsync(DeleteDocumentsRequest request);
    Task<GetDocumentsByBatchIdResponse> GetDocumentsByBatchIdAsync(int batchId, DataSource source);

    /// <summary>
    /// Gets the DocumentId and ShortName for all documents belonging to a specific batch.
    /// </summary>
    Task<GetDocumentShortNamesByBatchIdResponse> GetDocumentShortNamesByBatchIdAsync(int batchId);

    Task<ImportDocumentResponse> ImportDocumentAsync(Stream stream, ImportDocumentRequest request, FileData fileProperties, CancellationToken cancellationToken = default);
    Task<UpdateDocumentResponse> UpdateDocumentAsync(long documentId, UpdateDocumentRequest request);

    /// <summary>
    /// Creates a document entry that references an already-existing file by its path
    /// (no file bytes are copied). Persists the document, a single page pointing at the
    /// existing file path, and any provided keyword values in one transaction.
    /// </summary>
    Task<ImportDocumentResponse> IngestDocumentAsync(IngestDocumentRequest request, CancellationToken cancellationToken = default);
}
