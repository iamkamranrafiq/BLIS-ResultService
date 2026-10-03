namespace Bioreference.ScanningService.Application.Common.Constants;

public static class AppConstants
{
    public const string ApplicationName = "BLIS Scanning Service";
    public const string ApplicationVersion = "1.0.0";

    /// <summary>
    /// Default scan mode label used when a batch has no scan mode configured.
    /// </summary>
    public const string DefaultScanModeLabel = "Single Page";

    /// <summary>
    /// Document status applied to a batch's documents once the batch status is updated (committed).
    /// </summary>
    public const string CommittedDocumentStatus = "Committed";

    /// <summary>
    /// Batch status name that represents a committed batch. Used by auto-commit to resolve
    /// the target BatchStatusId when a scan queue is configured for automatic commit.
    /// </summary>
    public const string CommittedBatchStatus = "Committed";

    /// <summary>
    /// Calling module identifier for Chain of Custody (CoC) requisition lookups.
    /// </summary>
    public const string CoCModule = "GetReq_COC";

    /// <summary>
    /// Calling application identifier for GENPATH requisition requests.
    /// </summary>
    public const string GenpathApplication = "GENPATH";

    /// <summary>
    /// Default look-back window (in days) used when resolving requisition images.
    /// </summary>
    public const int DefaultLookBackDays = 180;
}
