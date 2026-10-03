using Azure;
using Bioreference.ScanningService.Application.Common.Enums;
using Bioreference.ScanningService.Application.Common.Helpers;
using Bioreference.ScanningService.Application.Common.Interfaces;
using Bioreference.ScanningService.Application.DTOs.Requests;
using Bioreference.ScanningService.Application.Services.Interfaces;
using Bioreference.ScanningService.Domain.Entities;
using Microsoft.AspNetCore.Mvc;

namespace Bioreference.ScanningService.WebAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
public class DocumentsController : ControllerBase
{
    private readonly IDocumentService _documentService;
    private readonly IAuditLogService _auditLogService;
    private readonly ILogger<DocumentsController> _logger;

    public DocumentsController(
        IDocumentService documentService,
        IAuditLogService auditLogService,
        ILogger<DocumentsController> logger)
    {
        _documentService = documentService;
        _auditLogService = auditLogService;
        _logger = logger;
    }
    /// <summary>
    /// Returns all active document types with their document type group.
    /// GET /api/documents/types
    /// </summary>
    [HttpGet("DocumentTypes")]
    public async Task<IActionResult> DocumentTypeList()
    {
        _logger.LogInformation("GET /api/documents/types called");

        var response = await _documentService.GetDocumentTypesAsync();

        return response.Success ? Ok(response) : StatusCode(500, response);
    }

    /// <summary>
    /// Returns keywords for the specified document type IDs.
    /// POST /api/documents/DocumentTypeKeywords
    /// </summary>
    [HttpPost("DocumentTypeKeywords")]
    public async Task<IActionResult> DocumentTypeKeywords([FromBody] DocumentTypeKeywordsRequest request)
    {
        _logger.LogInformation("POST /api/documents/DocumentTypeKeywords called with {Count} id(s)", request.DocumentTypeIds.Count);

        var response = await _documentService.GetDocumentTypeKeywordsAsync(request);

        return response.Success ? Ok(response) : StatusCode(400, response);
    }

    /// <summary>
    /// Returns a document by ID with full details: DocumentType, DocumentGroup, Batch, and Keyword values.
    /// GET /api/documents/1
    /// </summary>
    [HttpGet("{id:long}/{source:int}")]

    public async Task<IActionResult> GetDocumentById(long id, DataSource source = DataSource.Scanning)
    {
        await _auditLogService.LogActivityAsync(
            this,
            id.ToString(),
            "Document Fetched.");

        if (id <= 0)
        {
            return BadRequest("Document ID must be greater than 0.");
        }

        _logger.LogInformation("GET /api/documents/{Id} called", id);

        var response = await _documentService.GetDocumentByIdAsync(id, source);

        if (!response.Success)
        {
            return NotFound(response);
        }

        return Ok(response);
    }

    [HttpPost]
    [Route("Search")]
    public async Task<IActionResult> SearchDocument([FromBody] DocumentSearchRequest? request)
    {
        _logger.LogInformation("POST /api/documents/Search called");
        await _auditLogService.LogActivityAsync(
            this,
            "",
            "Document Searched Requested");

        var response = await _documentService.SearchDocumentAsync(request);

        return response.ResponseStatus.Success ? Ok(response) : StatusCode(500, response);
    }

    /// <summary>
    /// Retrieves a file (e.g., thumbnail, page image) for a document from network storage.
    /// GET /api/documents/file?documentId=123&fileUrl=C:\path\to\file.jpg
    /// </summary>
    [HttpGet("GetDocumentThumbnail")]
    public async Task<IActionResult> GetDocumentThumbnail([FromQuery] long documentId, [FromQuery] string fileUrl)
    {
        await _auditLogService.LogActivityAsync(
            this,
            documentId.ToString(),
            "Document Thumbnail Requested");
        if (documentId <= 0)
        {
            return BadRequest("Document ID must be greater than 0.");
        }

        if (string.IsNullOrWhiteSpace(fileUrl))
        {
            return BadRequest("File URL is required.");
        }

        _logger.LogInformation("GET /api/documents/GetDocumentThumbnail called for documentId={DocumentId}, fileUrl={FileUrl}", documentId, fileUrl);

        try
        {
            var fileBytes = await _documentService.GetFileAsync(documentId, fileUrl);

            if (fileBytes == null || fileBytes.Length == 0)
            {
                // When a thumbnail is missing (e.g. while loading a batch) do not surface a 404.
                // Return 204 No Content with no body so callers can skip the preview gracefully.
                _logger.LogWarning("Thumbnail not found for documentId={DocumentId}, fileUrl={FileUrl}. Returning 204 No Content.", documentId, fileUrl);
                return NoContent();
            }

            // Determine content type based on file extension
            var extension = Path.GetExtension(fileUrl).ToLowerInvariant();
            var contentType = extension switch
            {
                ".jpg" or ".jpeg" => "image/jpeg",
                ".png" => "image/png",
                ".gif" => "image/gif",
                ".bmp" => "image/bmp",
                ".tiff" or ".tif" => "image/tiff",
                _ => "application/octet-stream"
            };

            return File(fileBytes, contentType);
        }
        catch (Exception ex)
        {
            // A missing or unreachable thumbnail file (e.g. network share unavailable) should not
            // surface as a 500 error while loading a batch. Return 204 No Content with no body so
            // callers can skip the preview gracefully.
            _logger.LogWarning(ex, "Error retrieving thumbnail for documentId={DocumentId}, fileUrl={FileUrl}. Returning 204 No Content.", documentId, fileUrl);
            return NoContent();
        }
    }

    /// <summary>
    /// Generates and returns a PDF document by merging all pages of the specified document.
    /// GET /api/documents/pdf/123
    /// </summary>
    [HttpGet("GetDocumentPdf/{documentId:long}/{source:int}")]
    public async Task<IActionResult> GetDocumentPdf(long documentId, DataSource source)
    {
        await _auditLogService.LogActivityAsync(
            this,
            documentId.ToString(),
            "Document PDF Requested");
        if (documentId <= 0)
        {
            return BadRequest("Document ID must be greater than 0.");
        }


        _logger.LogInformation("GET /api/documents/GetDocumentPdf/{DocumentId} called", documentId);

        // Resolves the document's file path so it can be reported when the PDF is unavailable.
        async Task<string?> GetCheckedPathAsync()
        {
            var doc = await _documentService.GetDocumentByIdAsync(documentId, source);
            return doc?.Data?.DocUrl;
        }

        try
        {
            var pdfBytes = await _documentService.GenerateDocumentPdfAsync(documentId, source);

            if (source == DataSource.OnBase && pdfBytes == null)
            {
                return Ok(new { Success = false, Message = "File not found" });
            }

            if (pdfBytes == null || pdfBytes.Length == 0)
            {
                // When a document's PDF is unavailable (e.g. while loading a batch) do not surface a 404.
                // Return 200 OK with Success=false plus a message and the checked path.
                var checkedPath = await GetCheckedPathAsync();
                _logger.LogWarning("PDF not available for documentId={DocumentId}. Returning message with path.", documentId);
                return Ok(new
                {
                    Success = false,
                    Message = $"PDF not available for documentId={documentId}.",
                    Path = checkedPath
                });
            }

            // Return PDF with proper content type and filename
            var document = await _documentService.GetDocumentByIdAsync(documentId, source);
            var documentName = document?.Data?.ShortName ?? $"Document_{documentId}";
            return File(pdfBytes, "application/pdf", $"{documentName}.pdf");
        }
        catch (Exception ex)
        {
            // A missing or unreachable page file (e.g. network share/domain controller unavailable)
            // should not surface as a 500 error while loading a batch. Return 200 OK with Success=false
            // plus a message and the checked path.
            var checkedPath = await GetCheckedPathAsync();
            _logger.LogWarning(ex, "Error generating PDF for documentId={DocumentId}. Returning message with path.", documentId);
            return Ok(new
            {
                Success = false,
                Message = $"Error generating PDF for documentId={documentId}.",
                Path = checkedPath
            });
        }
    }

    /// Renames a document.
    /// PUT /api/documents/{id}/rename
    /// </summary>
    /// <param name="id">The document ID to rename</param>
    /// <param name="request">The request containing the new name</param>
    /// <returns>Success response with updated document details</returns>
    [HttpPut("{id:long}/rename")]
    public async Task<IActionResult> RenameDocument(long id, [FromBody] RenameDocumentRequest request)
    {
        await _auditLogService.LogActivityAsync(
            this,
            id.ToString(),
            "Document Rename Requested");
        if (id <= 0)
        {
            return BadRequest(new { success = false, message = "Document ID must be greater than 0." });
        }

        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        _logger.LogInformation("PUT /api/documents/{Id}/rename called with new name: '{Name}'", id, request.Name);

        var response = await _documentService.RenameDocumentAsync(id, request);

        if (!response.Success)
        {
            return NotFound(response);
        }

        return Ok(response);
    }

    /// <summary>
    /// Soft-deletes multiple documents by setting IsDeleted = true.
    /// DELETE /api/documents/delete
    /// </summary>
    /// <param name="request">The request containing document IDs to delete</param>
    /// <returns>Success response with deletion statistics</returns>
    [HttpDelete("delete")]
    public async Task<IActionResult> DeleteDocuments([FromBody] DeleteDocumentsRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }
        await _auditLogService.LogActivityAsync(
            this,
            string.Join(",", request.DocumentIds),
            "Document Delete Requested");
        _logger.LogInformation("DELETE /api/documents/delete called for {Count} document(s)", request.DocumentIds.Count);

        var response = await _documentService.DeleteDocumentsAsync(request);

        return Ok(response);
    }

    /// <summary>
    /// Gets all documents for a specific batch with their pages and thumbnail URLs.
    /// GET /api/documents/batch/123
    /// </summary>
    /// <param name="batchId">The batch ID</param>
    /// <returns>List of documents with their pages</returns>
    [HttpGet("GetDocumentsByBatchId/{batchId:int}/{source:int}")]
    public async Task<IActionResult> GetDocumentsByBatchId(int batchId, DataSource source = DataSource.Scanning)
    {
        await _auditLogService.LogActivityAsync(
            this,
            batchId.ToString(),
            "Document Batch Retrieval Requested");
        if (batchId <= 0)
        {
            return BadRequest(new { success = false, message = "Batch ID must be greater than 0." });
        }

        _logger.LogInformation("GET /api/documents/batch/{BatchId} called", batchId);

        var response = await _documentService.GetDocumentsByBatchIdAsync(batchId, source);

        if (!response.Success)
        {
            return StatusCode(500, response);
        }

        return Ok(response);
    }

    /// <summary>
    /// Gets the DocumentId and ShortName for all documents belonging to a specific batch.
    /// GET /api/documents/GetDocumentShortNamesByBatchId/123
    /// </summary>
    /// <param name="batchId">The batch ID</param>
    /// <returns>List of documents with their short names</returns>
    [HttpGet("GetDocumentShortNamesByBatchId/{batchId:int}")]
    public async Task<IActionResult> GetDocumentShortNamesByBatchId(int batchId)
    {
        if (batchId <= 0)
        {
            return BadRequest(new { success = false, message = "Batch ID must be greater than 0." });
        }


        _logger.LogInformation("GET /api/documents/GetDocumentShortNamesByBatchId/{BatchId} called", batchId);

        var response = await _documentService.GetDocumentShortNamesByBatchIdAsync(batchId);

        if (!response.Success)
        {
            return StatusCode(500, response);
        }

        return Ok(response);
    }

    [HttpPost("Import")]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> ImportDocument(IFormFile file, [FromForm] ImportDocumentRequest request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("POST /api/documents/Import called");
        await _auditLogService.LogActivityAsync(
            this,
            "NEW",
            "Import Document Requested");
        if (file == null || file.Length == 0)
            return BadRequest("File is required.");

        await using var stream = file.OpenReadStream();
        //request.fileProperties = ;
        var response = await _documentService.ImportDocumentAsync(stream, request,
                        new FileData(file.FileName, file.ContentType, Path.GetExtension(file.FileName)), cancellationToken);

        if (!response.Success)
        {
            return StatusCode(500, response);
        }

        return Ok(response);
    }


    [HttpPut("{id:long}/Update")]
    public async Task<IActionResult> UpdateDocument(long id, [FromBody] UpdateDocumentRequest request)
    {
        await _auditLogService.LogActivityAsync(
            this,
            "NEW",
            "Update Document Requested");
        if (id <= 0)
        {
            return BadRequest(new { success = false, message = "Document ID must be greater than 0." });
        }

        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        _logger.LogInformation("PUT /api/documents/{Id}/update", id);

        var response = await _documentService.UpdateDocumentAsync(id, request);

        if (!response.Success)
        {
            return NotFound(response);
        }

        return Ok(response);
    }
}

