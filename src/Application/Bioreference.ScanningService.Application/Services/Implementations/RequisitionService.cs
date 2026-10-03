using System.Diagnostics;
using System.Text;
using Bioreference.ScanningService.Application.Common.Constants;
using Bioreference.ScanningService.Application.Common.Entity;
using Bioreference.ScanningService.Application.Common.Enums;
using Bioreference.ScanningService.Application.Common.Interfaces;
using Bioreference.ScanningService.Application.Common.Settings;
using Bioreference.ScanningService.Application.DTOs.Responses;
using Bioreference.ScanningService.Application.Services.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Bioreference.ScanningService.Application.Services.Implementations;

/// <summary>
/// Locates the requisition documents for one or more accession numbers. Ported from the legacy
/// INTRANET-services 'GetReqInPDF.ashx.vb' HTTP handler, preserving its branching, GENPATH
/// special-case, and error handling. PDF generation itself is delegated to the document service
/// by the controller, so this service only resolves accessions to their source documents.
/// </summary>
public class RequisitionService : IRequisitionService
{
    private readonly IRequisitionImageRepository _repository;
    private readonly IConfiguration _configuration;
    private readonly OnBaseConfiguration _onBaseConfiguration;
    private readonly ILogger<RequisitionService> _logger;

    public RequisitionService(
        IRequisitionImageRepository repository,
        IConfiguration configuration,
        IOptions<OnBaseConfiguration> onBaseConfiguration,
        ILogger<RequisitionService> logger)
    {
        _repository = repository;
        _configuration = configuration;
        _onBaseConfiguration = onBaseConfiguration.Value;
        _logger = logger;
    }

    public async Task<RequisitionResolution> ResolveRequisitionAsync(
        string? callingApplication,
        string? callingModule,
        string? accessionNumbers,
        DateTime? dateOfService,
        string? authenticatedUsername,
        CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        _logger.LogInformation(
            "Starting requisition resolution for accession(s) {AccessionNumbers} (App={CallingApplication}, Module={CallingModule})",
            accessionNumbers, callingApplication, callingModule);
        var errorCode = Errors_OnBase.Success;

        // Parse date of service; default to today on failure/missing.
        var dtOfService = dateOfService ?? DateTime.Today;

        var logStr = new StringBuilder(
            $"GetReqInPDF|{callingApplication}|{callingModule}|{accessionNumbers}|{dtOfService}");

        var result = new RequisitionResolution { ErrorCode = errorCode };

        try
        {
            if (!string.IsNullOrWhiteSpace(authenticatedUsername))
            {
                _logger.LogInformation("Setting DB user to {User} for requisition retrieval", authenticatedUsername);
            }

            // Validate accession numbers.
            if (string.IsNullOrWhiteSpace(accessionNumbers))
            {
                result.ValidationMessage = "AccessionNumber(s) can not be null or empty.";
                return result;
            }

            // Split into a list of accession numbers.
            var accessions = accessionNumbers
                .Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(a => a.Trim())
                .Where(a => a.Length > 0)
                .ToList();

            if (accessions.Count == 0)
            {
                result.ValidationMessage = "AccessionNumber(s) can not be null or empty.";
                return result;
            }

            // Prefix-expand accessions for the OnBase lookup. A 7-char accession may be stored with
            // a '10' or '11' prefix, so include those variants. The mapping from an expanded value
            // back to the original accession is retained so grouping (which uses the original input
            // order) still works.
            var expandedToOriginal = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            var expandedAccessions = new List<string>();
            foreach (var accession in accessions)
            {
                AddExpanded(expandedAccessions, expandedToOriginal, accession, accession);
                if (accession.Length == 7)
                {
                    AddExpanded(expandedAccessions, expandedToOriginal, "10" + accession, accession);
                    AddExpanded(expandedAccessions, expandedToOriginal, "11" + accession, accession);
                }
            }

            // Compute the look-back window from configuration (default 180 days).
            var lookBackDays = _configuration.GetValue<int?>("RequisitionSettings:RequisitionLookBackAndForthDays")
                ?? AppConstants.DefaultLookBackDays;

            // Select the image lookup strategy. Requisitions dated on/after the configured cutover
            // date are retrieved from the scanning network drive; anything before the cutover is
            // retrieved from OnBase.
            IReadOnlyList<RequisitionImage> reqs;
            var cutoverDate = _onBaseConfiguration.CutoverDate;
            var isAfterCutover = cutoverDate != default &&
                DateOnly.FromDateTime(dtOfService.Date) >= cutoverDate;

            if (isAfterCutover)
            {
                _logger.LogInformation(
                    "Date of service {Dos:yyyy-MM-dd} is on/after cutover {Cutover:yyyy-MM-dd}; using scanning network drive source",
                    dtOfService, cutoverDate);
                reqs = await _repository.FindScanningAccessionImagesAsync(expandedAccessions, dateOfService, lookBackDays, cancellationToken);
            }
            else if(string.Equals(callingModule?.Trim(), AppConstants.CoCModule, StringComparison.OrdinalIgnoreCase))
            {
                reqs = await _repository.FindCoCAccessionImagesAsync(expandedAccessions, dtOfService, lookBackDays, cancellationToken);
            }
            else if (accessions.Count == 1 && accessions[0].Any(char.IsLetter))
            {
                reqs = await _repository.FindRocheAccessionImagesAsync(expandedAccessions, dtOfService, lookBackDays, cancellationToken);
            }
            else
            {
                reqs = await _repository.FindDefaultAccessionImagesAsync(expandedAccessions, dtOfService, lookBackDays, cancellationToken);
            }

            // Map located images back to the original accession so grouping matches input order.
            reqs = reqs
                .Select(r =>
                {
                    if (expandedToOriginal.TryGetValue(r.KeyValue, out var original) &&
                        !string.Equals(original, r.KeyValue, StringComparison.OrdinalIgnoreCase))
                    {
                        r.KeyValue = original;
                    }
                    return r;
                })
                .ToList();

            if (reqs.Count == 0)
            {
                errorCode = Errors_OnBase.RequisitionNotFound;
            }

            // GENPATH callers get an empty response when the requisition is not found.
            if (errorCode == Errors_OnBase.RequisitionNotFound &&
                string.Equals(callingApplication?.Trim(), AppConstants.GenpathApplication, StringComparison.OrdinalIgnoreCase))
            {
                result.ErrorCode = errorCode;
                result.IsEmpty = true;
                logStr.Append($"|{errorCode}");
                return result;
            }

            // Resolve each accession (in input order) to its candidate source documents. An
            // accession can map to several candidate documents (test data or re-scans); they are
            // ordered most-recent first by the repository, so the controller can try each in turn.
            var documents = new List<RequisitionDocumentRef>();
            foreach (var accession in accessions)
            {
                var accessionImages = reqs
                    .Where(r => string.Equals(r.KeyValue, accession, StringComparison.OrdinalIgnoreCase))
                    .ToList();

                foreach (var group in accessionImages
                    .GroupBy(r => new { r.DocumentId, r.Source })
                    .OrderBy(g => accessionImages.FindIndex(i => i.DocumentId == g.Key.DocumentId && i.Source == g.Key.Source)))
                {
                    documents.Add(new RequisitionDocumentRef
                    {
                        Accession = accession,
                        DocumentId = group.Key.DocumentId,
                        Source = group.Key.Source
                    });
                }
            }

            result.ErrorCode = errorCode;
            result.Documents = documents;
            result.FileName = accessions.Count == 1 ? $"{accessions[0]}.pdf" : "requisitions.pdf";

            if (errorCode != Errors_OnBase.Success)
            {
                logStr.Append($"|{errorCode}");
            }

            return result;
        }
        catch (Exception ex)
        {
            // Database failures are classified separately so they are diagnosable.
            errorCode = IsDatabaseException(ex) ? Errors_OnBase.DatabaseError : Errors_OnBase.ErrorGeneratingPDF;
            result.ErrorCode = errorCode;
            result.IsEmpty = false;
            result.ErrorDetail = FlattenExceptionMessages(ex);
            logStr.Append($"|{errorCode}|{ex.Message}|{ex.StackTrace}");
            _logger.LogError(ex, "Requisition resolution failed with {ErrorCode}", errorCode);
            return result;
        }
        finally
        {
            stopwatch.Stop();
            logStr.Append($"|Resolve(ms)|{stopwatch.ElapsedMilliseconds} ms");
            _logger.LogInformation("{RequisitionLog}", logStr.ToString());
        }
    }

    /// <summary>
    /// Adds an expanded accession variant to the lookup list and records its mapping back to
    /// the original accession, avoiding duplicate entries.
    /// </summary>
    private static void AddExpanded(
        List<string> expandedAccessions,
        Dictionary<string, string> expandedToOriginal,
        string expanded,
        string original)
    {
        if (expandedToOriginal.ContainsKey(expanded))
        {
            return;
        }
        expandedToOriginal[expanded] = original;
        expandedAccessions.Add(expanded);
    }

    /// <summary>
    /// Determines whether the given exception (or any inner exception) originated from the
    /// database provider, so it can be classified as a <see cref="Errors_OnBase.DatabaseError"/>.
    /// </summary>
    private static bool IsDatabaseException(Exception ex)
    {
        for (var current = ex; current is not null; current = current.InnerException)
        {
            var typeName = current.GetType().FullName ?? string.Empty;
            if (typeName.Contains("SqlException", StringComparison.OrdinalIgnoreCase) ||
                current is System.Data.Common.DbException)
            {
                return true;
            }
        }
        return false;
    }

    /// <summary>
    /// Concatenates the messages of an exception and all of its inner exceptions so the
    /// underlying cause (e.g. the SQL error text) is visible for troubleshooting.
    /// </summary>
    private static string FlattenExceptionMessages(Exception ex)
    {
        var messages = new List<string>();
        for (var current = ex; current is not null; current = current.InnerException)
        {
            messages.Add($"{current.GetType().Name}: {current.Message}");
        }
        return string.Join(" | ", messages);
    }
}
