using Bioreference.ScanningService.Application.Common.Enums;

namespace Bioreference.ScanningService.Application.DTOs.Responses;

/// <summary>
/// A single document that the controller should render to PDF through the document service.
/// </summary>
public class RequisitionDocumentRef
{
    /// <summary>
    /// The accession number the document belongs to.
    /// </summary>
    public string Accession { get; set; } = string.Empty;

    /// <summary>
    /// The document id (scanning DocumentId or OnBase itemnum) to generate a PDF for.
    /// </summary>
    public long DocumentId { get; set; }

    /// <summary>
    /// The storage source the document is read from.
    /// </summary>
    public DataSource Source { get; set; } = DataSource.OnBase;
}
