using Bioreference.ScanningService.Application.DTOs.Responses;

namespace Bioreference.ScanningService.Application.Services.Interfaces;

/// <summary>
/// Resolves OnBase/scanning requisition documents for one or more accession numbers.
/// Ported from the legacy INTRANET-services 'GetReqInPDF.ashx.vb' HTTP handler. PDF generation is
/// performed by the document service (invoked from the controller), so this service only resolves
/// accessions to their source documents.
/// </summary>
public interface IRequisitionService
{
    /// <summary>
    /// Locates the requisition document(s) for one or more accession numbers and returns the
    /// resolved document references (id + source) in accession input order. Preserves the legacy
    /// handler's branching: Chain-of-Custody vs Roche vs default lookup and the GENPATH
    /// empty-response special-case.
    /// </summary>
    /// <param name="callingApplication">Calling application name (logging; special-cased for 'GENPATH').</param>
    /// <param name="callingModule">Calling module ('GetReq_COC' selects Chain-of-Custody lookup).</param>
    /// <param name="accessionNumbers">Comma-separated list of accession numbers.</param>
    /// <param name="dateOfService">Optional date of service; defaults to today when null.</param>
    /// <param name="authenticatedUsername">Optional authenticated username to set as the DB user.</param>
    Task<RequisitionResolution> ResolveRequisitionAsync(
        string? callingApplication,
        string? callingModule,
        string? accessionNumbers,
        DateTime? dateOfService,
        string? authenticatedUsername,
        CancellationToken cancellationToken = default);
}
