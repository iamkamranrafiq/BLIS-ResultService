namespace Bioreference.ScanningService.Application.Common.Enums;

/// <summary>
/// Error codes used by the requisition-image-to-PDF retrieval flow.
/// Ported from the legacy INTRANET-services 'GetReqInPDF.ashx.vb' handler.
/// </summary>
public enum Errors_OnBase
{
    Success = 0,
    RequisitionNotFound = 1,
    ErrorFetchingRequisition = 2,
    ErrorGeneratingPDF = 3,
    DatabaseError = 4,
    UnknownFileFormat = 5,
}
