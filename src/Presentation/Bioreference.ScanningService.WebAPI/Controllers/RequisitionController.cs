using Bioreference.ScanningService.Application.Common.Enums;
using Bioreference.ScanningService.Application.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace Bioreference.ScanningService.WebAPI.Controllers;

/// <summary>
/// Retrieves the requisition document for an accession number and streams it as a PDF.
/// The requisition service resolves the accession to a document id/source; the document service
/// generates the PDF.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class RequisitionController : ControllerBase
{
    private readonly IRequisitionService _requisitionService;
    private readonly IDocumentService _documentService;
    private readonly ILogger<RequisitionController> _logger;

    public RequisitionController(
        IRequisitionService requisitionService,
        IDocumentService documentService,
        ILogger<RequisitionController> logger)
    {
        _requisitionService = requisitionService;
        _documentService = documentService;
        _logger = logger;
    }

    /// <summary>
    /// GET /api/requisition/image
    /// Returns the requisition PDF inline, or a plain-text message when not found.
    /// </summary>
    [HttpGet("image")]
    [Produces("application/pdf", "text/plain")]
    public async Task<IActionResult> GetRequisitionImage(
        [FromQuery] string? callingApplication,
        [FromQuery] string? callingModule,
        [FromQuery] string? accessionNumber,
        [FromQuery] string? accessions,
        [FromQuery] string? dateOfService,
        [FromQuery] string? authenticatedUsername,
        CancellationToken cancellationToken)
    {
        var accessionNumbers = string.IsNullOrWhiteSpace(accessionNumber) ? accessions : accessionNumber;
        DateTime? dos = DateTime.TryParse(dateOfService, out var parsed) ? parsed : null;

        _logger.LogInformation(
            "GET /api/requisition/image app={App} module={Module} accessions={Accessions}",
            callingApplication, callingModule, accessionNumbers);

        // 1. Resolve the accession to a document id + source.
        var resolution = await _requisitionService.ResolveRequisitionAsync(
            callingApplication, callingModule, accessionNumbers, dos, authenticatedUsername, cancellationToken);

        if (resolution.ValidationMessage is not null)
        {
            return Content(resolution.ValidationMessage, "text/plain");
        }

        var document = resolution.Documents.FirstOrDefault();
        if (document is null)
        {
            if (resolution.ErrorDetail is not null)
            {
                _logger.LogError(
                    "Requisition lookup failed with {ErrorCode}: {ErrorDetail}",
                    resolution.ErrorCode, resolution.ErrorDetail);
                return Content($"{resolution.ErrorCode}: {resolution.ErrorDetail}", "text/plain");
            }

            return Content(resolution.ErrorCode.ToString(), "text/plain");
        }

        // 2. Generate the PDF for that document via the document service.
        var pdfBytes = await _documentService.GenerateDocumentPdfAsync(document.DocumentId, document.Source);
        if (pdfBytes is null || pdfBytes.Length == 0)
        {
            return Content(Errors_OnBase.RequisitionNotFound.ToString(), "text/plain");
        }

        Response.Headers.ContentDisposition = $"inline; filename=\"{resolution.FileName}\"";
        return File(pdfBytes, "application/pdf");
    }
}
