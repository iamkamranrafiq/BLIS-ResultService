using Bioreference.ScanningService.Application.Common.Enums;

namespace Bioreference.ScanningService.Application.Common.Entity;

/// <summary>
/// Represents a single requisition document page image located in OnBase.
/// Ported from the legacy INTRANET-services 'GetReqInPDF.ashx.vb' handler.
/// </summary>
public class RequisitionImage
{
    /// <summary>
    /// The storage source the page image should be read from. Requisitions dated before the
    /// configured cutover come from OnBase; those on/after the cutover come from the scanning
    /// network drive.
    /// </summary>
    public DataSource Source { get; set; } = DataSource.OnBase;

    /// <summary>
    /// The accession number (keyword value) the image belongs to.
    /// </summary>
    public string KeyValue { get; set; } = string.Empty;

    /// <summary>
    /// The scanning document id the page belongs to. Used to group pages by their source
    /// document so an accession that maps to several candidate documents can try each
    /// document in turn until one has readable files.
    /// </summary>
    public long DocumentId { get; set; }

    /// <summary>
    /// The date the document was created/stored in OnBase.
    /// </summary>
    public DateTime CreationDate { get; set; }

    /// <summary>
    /// The 1-based page number of the image within the document.
    /// </summary>
    public int PageNumber { get; set; }

    /// <summary>
    /// The physical/relative file path of the page image.
    /// </summary>
    public string FilePath { get; set; } = string.Empty;
}
