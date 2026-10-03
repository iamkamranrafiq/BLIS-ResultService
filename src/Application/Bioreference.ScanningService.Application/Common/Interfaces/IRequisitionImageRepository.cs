using Bioreference.ScanningService.Application.Common.Entity;

namespace Bioreference.ScanningService.Application.Common.Interfaces;

/// <summary>
/// Data-access abstraction for locating OnBase requisition document page images
/// by accession number. Ported from the legacy 'GetReqInPDF.ashx.vb' handler which
/// used different OnBase document-type sets depending on the calling context.
/// </summary>
public interface IRequisitionImageRepository
{
    /// <summary>
    /// Locates Chain-of-Custody requisition images (OnBase doc type 104).
    /// </summary>
    Task<IReadOnlyList<RequisitionImage>> FindCoCAccessionImagesAsync(
        IReadOnlyList<string> accessions, DateTime dateOfService, int lookBackDays, CancellationToken cancellationToken = default);

    /// <summary>
    /// Locates Roche requisition images (OnBase doc type 296).
    /// </summary>
    Task<IReadOnlyList<RequisitionImage>> FindRocheAccessionImagesAsync(
        IReadOnlyList<string> accessions, DateTime dateOfService, int lookBackDays, CancellationToken cancellationToken = default);

    /// <summary>
    /// Locates default requisition images (OnBase doc types 101, 170, 303).
    /// </summary>
    Task<IReadOnlyList<RequisitionImage>> FindDefaultAccessionImagesAsync(
        IReadOnlyList<string> accessions, DateTime dateOfService, int lookBackDays, CancellationToken cancellationToken = default);

    /// <summary>
    /// Locates requisition document pages that were scanned into the scanning network drive
    /// (used for requisitions on/after the configured cutover date). Matches documents by
    /// accession (KeywordId 104) and, when <paramref name="dateOfService"/> is supplied, by
    /// date-of-service (KeywordId 115 DateTimeValue). When no date is supplied, all matching
    /// accession documents are returned. Returns their pages ordered by sequence.
    /// </summary>
    Task<IReadOnlyList<RequisitionImage>> FindScanningAccessionImagesAsync(
        IReadOnlyList<string> accessions, DateTime? dateOfService, int lookBackDays, CancellationToken cancellationToken = default);
}
