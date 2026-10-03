using Bioreference.ScanningService.Application.Common.Enums;

namespace Bioreference.ScanningService.Application.DTOs.Responses;

/// <summary>
/// Outcome of resolving one or more accession numbers to the underlying documents that hold the
/// requisition images. The requisition service only performs the lookup/branching; the controller
/// is responsible for turning the resolved documents into a PDF via the document service.
/// </summary>
public class RequisitionResolution
{
    /// <summary>
    /// The final OnBase error code for the lookup.
    /// </summary>
    public Errors_OnBase ErrorCode { get; set; } = Errors_OnBase.Success;

    /// <summary>
    /// A plain-text validation message to return (e.g. empty accession numbers).
    /// When set, the controller returns this as text/plain.
    /// </summary>
    public string? ValidationMessage { get; set; }

    /// <summary>
    /// True when the caller (e.g. GENPATH) should receive an empty response instead of an
    /// error body when the requisition is not found.
    /// </summary>
    public bool IsEmpty { get; set; }

    /// <summary>
    /// Diagnostic detail surfaced to help troubleshoot failures. Populated only on error paths.
    /// </summary>
    public string? ErrorDetail { get; set; }

    /// <summary>
    /// Suggested inline file name for the resulting PDF response.
    /// </summary>
    public string FileName { get; set; } = "requisition.pdf";

    /// <summary>
    /// The documents (in accession input order) that should be rendered into the PDF.
    /// </summary>
    public List<RequisitionDocumentRef> Documents { get; set; } = new();
}
