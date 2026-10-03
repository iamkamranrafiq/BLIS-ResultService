using System.Data;
using Bioreference.ScanningService.Application.Common.Entity;
using Bioreference.ScanningService.Application.Common.Enums;
using Bioreference.ScanningService.Application.Common.Interfaces;
using Bioreference.ScanningService.Domain.Entities;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Bioreference.ScanningService.Infrastructure.Persistence.Repositories;

/// <summary>
/// Locates OnBase requisition document page images by accession number using the
/// document-type sets defined by the legacy 'GetReqInPDF.ashx.vb' handler:
///   Chain-of-Custody: 104
///   Roche:            296
///   Default:          101, 170, 303
/// Runs its queries over the shared <see cref="ScanningServiceDbContext"/> connection
/// (Scanning_CONSTR) — the same connection the document search service uses to reach the
/// OnBase 'hsi' schema — so no separate connection string is required.
/// </summary>
public class RequisitionImageRepository : IRequisitionImageRepository
{
    // Document-type sets (OnBase itemtypenum values) ported from the legacy handler.
    private static readonly int[] CoCDocTypes = { 104 };
    private static readonly int[] RocheDocTypes = { 296 };
    private static readonly int[] DefaultDocTypes = { 101, 170, 303 };

    private readonly int _dateServiceInMonths;
    private readonly ScanningServiceDbContext _context;
    private readonly ILogger<RequisitionImageRepository> _logger;

    public RequisitionImageRepository(IConfiguration configuration, ScanningServiceDbContext context, ILogger<RequisitionImageRepository> logger)
    {
        _dateServiceInMonths = configuration.GetValue<int?>("RequisitionSettings:DateService_InMonths") ?? 6;
        _context = context;
        _logger = logger;
    }

    public Task<IReadOnlyList<RequisitionImage>> FindCoCAccessionImagesAsync(
        IReadOnlyList<string> accessions, DateTime dateOfService, int lookBackDays, CancellationToken cancellationToken = default)
        => FindImagesAsync(accessions, dateOfService, CoCDocTypes, lookBackDays, cancellationToken);

    public Task<IReadOnlyList<RequisitionImage>> FindRocheAccessionImagesAsync(
        IReadOnlyList<string> accessions, DateTime dateOfService, int lookBackDays, CancellationToken cancellationToken = default)
        => FindImagesAsync(accessions, dateOfService, RocheDocTypes, lookBackDays, cancellationToken);

    public Task<IReadOnlyList<RequisitionImage>> FindDefaultAccessionImagesAsync(
        IReadOnlyList<string> accessions, DateTime dateOfService, int lookBackDays, CancellationToken cancellationToken = default)
        => FindImagesAsync(accessions, dateOfService, DefaultDocTypes, lookBackDays, cancellationToken);

    /// <summary>
    /// Locates requisition pages scanned into the scanning network drive (post-cutover source).
    /// The accession number is stored in DocumentKeywordValue.Value where KeywordId = 104. The
    /// matching row's DocumentId is used to fetch the Document, and the document's file (DocUrl,
    /// or its DocumentPages) is returned as a <see cref="RequisitionImage"/> tagged with
    /// <see cref="DataSource.Scanning"/>. Each accession is matched both as-is and with the
    /// leading "10" prefix removed (and vice versa) to accommodate 10-digit vs trimmed forms.
    /// </summary>
    public async Task<IReadOnlyList<RequisitionImage>> FindScanningAccessionImagesAsync(
        IReadOnlyList<string> accessions, DateTime? dateOfService, int lookBackDays, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "FindScanningAccessionImagesAsync called for {Count} accession(s), dos {Dos}, lookBackDays {LookBack}",
            accessions.Count, dateOfService?.ToString("yyyy-MM-dd") ?? "(none)", lookBackDays);

        try
        {
            // Build the set of accession variants to search for in DocumentKeywordValue.Value:
            // the value as provided, plus a "10"-prefix-removed variant, plus a "10"-prefixed
            // variant, so a 10-digit accession and its trimmed form both resolve. Each variant is
            // mapped back to the original searched accession so the returned KeyValue matches the
            // caller's input (required for correct PDF grouping).
            var variantToOriginal = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (var accession in accessions)
            {
                var trimmed = accession?.Trim();
                if (string.IsNullOrEmpty(trimmed))
                {
                    continue;
                }

                variantToOriginal[trimmed] = trimmed;

                if (trimmed.StartsWith("10", StringComparison.Ordinal) && trimmed.Length > 2)
                {
                    variantToOriginal[trimmed.Substring(2)] = trimmed;
                }
                else
                {
                    variantToOriginal["10" + trimmed] = trimmed;
                }
            }

            var searchValues = variantToOriginal.Keys.ToList();
            var searchValueSet = new HashSet<string>(searchValues, StringComparer.OrdinalIgnoreCase);

            // Two-condition scanning match (mirrors document search):
            //   1. Accession: a KeywordId = 104 keyword whose text value STRICTLY equals an
            //      accession variant (TextValue, with AlphanumericValue as a fallback).
            //   2. Date-of-service: a KeywordId = 115 keyword whose DateTimeValue falls on the
            //      requested date-of-service.
            // A document must satisfy BOTH conditions to be selected, so only the requisition
            // indexed with this precise accession AND date is returned.
            const int accessionKeywordId = 104;
            const int dateKeywordId = 115;
            var dosDate = dateOfService?.Date;

            // Filter by KeywordId in SQL only, then match the accession value in memory. Comparing
            // a parameter against both TextValue (varchar(max)) and AlphanumericValue (varchar(1000))
            // in the same SQL query makes EF infer conflicting type mappings for the column, so the
            // value comparison is done client-side after materialization.
            var accessionRows = await _context.DocumentKeywordValue
                .AsNoTracking()
                .Where(kv => kv.KeywordId == accessionKeywordId)
                .Select(kv => new { kv.DocumentId, kv.TextValue, kv.AlphanumericValue })
                .ToListAsync(cancellationToken);

            var accessionMatches = accessionRows
                .Where(kv =>
                    (kv.TextValue != null && searchValueSet.Contains(kv.TextValue))
                    || (kv.AlphanumericValue != null && searchValueSet.Contains(kv.AlphanumericValue)))
                .ToList();

            if (accessionMatches.Count == 0)
            {
                _logger.LogInformation("No scanning documents matched accession KeywordId {KeywordId} for the given accession(s)", accessionKeywordId);
                return Array.Empty<RequisitionImage>();
            }

            var accessionDocumentIds = accessionMatches.Select(m => m.DocumentId).Distinct().ToList();

            List<long> matchedDocumentIds;
            if (dosDate.HasValue)
            {
                // Condition 2: from the accession-matched documents, keep only those whose
                // KeywordId = 115 DateTimeValue matches the requested date-of-service.
                var dateValue = dosDate.Value;
                var dateMatchedDocumentIds = await _context.DocumentKeywordValue
                    .AsNoTracking()
                    .Where(kv => kv.KeywordId == dateKeywordId
                        && accessionDocumentIds.Contains(kv.DocumentId)
                        && kv.DateTimeValue != null
                        && kv.DateTimeValue.Value.Date == dateValue)
                    .Select(kv => kv.DocumentId)
                    .Distinct()
                    .ToListAsync(cancellationToken);

                var dateMatchedSet = new HashSet<long>(dateMatchedDocumentIds);
                matchedDocumentIds = accessionMatches
                    .Where(m => dateMatchedSet.Contains(m.DocumentId))
                    .Select(m => m.DocumentId)
                    .Distinct()
                    .ToList();
            }
            else
            {
                // No date-of-service supplied: return every document matching the accession.
                matchedDocumentIds = accessionDocumentIds;
            }

            var matchedDocumentSet = new HashSet<long>(matchedDocumentIds);
            var matches = accessionMatches
                .Where(m => matchedDocumentSet.Contains(m.DocumentId))
                .Select(m => new { m.DocumentId, m.TextValue, m.AlphanumericValue })
                .ToList();

            if (matches.Count == 0)
            {
                _logger.LogInformation(
                    "No scanning documents matched both accession (KeywordId {AccKw}) and date-of-service {Dos} (KeywordId {DateKw})",
                    accessionKeywordId, dosDate?.ToString("yyyy-MM-dd") ?? "(none)", dateKeywordId);
                return Array.Empty<RequisitionImage>();
            }

            // Map each matched keyword value back to the original searched accession. The stored
            // value may contain the accession as a substring, so resolve by checking which
            // searched variant is contained in it.
            static string ResolveOriginal(string? value, Dictionary<string, string> map)
            {
                if (string.IsNullOrEmpty(value))
                {
                    return string.Empty;
                }

                if (map.TryGetValue(value, out var exact))
                {
                    return exact;
                }

                foreach (var kvp in map)
                {
                    if (value.Contains(kvp.Key, StringComparison.OrdinalIgnoreCase))
                    {
                        return kvp.Value;
                    }
                }

                return value;
            }

            // The same accession can be indexed on many documents. Map every matched DocumentId
            // to its original searched accession; the correct document is chosen later by
            // date-of-service (mirroring document search), not by keyword CreatedDate.
            var documentIdToAccession = new Dictionary<long, string>();
            foreach (var match in matches)
            {
                var matchValue = match.TextValue ?? match.AlphanumericValue;
                documentIdToAccession[match.DocumentId] = ResolveOriginal(matchValue, variantToOriginal);
            }

            var documentIds = documentIdToAccession.Keys.ToList();

            var documents = await _context.Document
                .AsNoTracking()
                .Include(d => d.DocumentPages)
                .Where(d => !d.IsDeleted && documentIds.Contains(d.DocumentId))
                .ToListAsync(cancellationToken);

            _logger.LogInformation(
                "Scanning candidate documents: {Candidates}",
                string.Join(" | ", documents.Select(d =>
                    $"DocId={d.DocumentId},DocDate={d.DocumentDate:yyyy-MM-dd},Pages={d.DocumentPages.Count},FirstPageUrl={d.DocumentPages.OrderBy(p => p.DocumentPageId).FirstOrDefault()?.PageURL}")));

            // The accession was matched strictly against KeywordId 115, so every returned document
            // genuinely belongs to this accession. Return ALL matching documents (there can be
            // several when the requisition was scanned more than once), ordered most-recent first.
            // No cross-date fallback is applied, so unrelated requisitions are never pulled in.
            var selectedDocuments = documents
                .OrderByDescending(d => d.DocumentDate.GetValueOrDefault(d.DatePosted))
                .ThenByDescending(d => d.DocumentId)
                .ToList();

            _logger.LogInformation(
                "Scanning selected document ids: {Selected}",
                string.Join(",", selectedDocuments.Select(d => d.DocumentId)));

            var images = new List<RequisitionImage>();
            foreach (var document in selectedDocuments)
            {
                var keyValue = documentIdToAccession.TryGetValue(document.DocumentId, out var mv) && !string.IsNullOrEmpty(mv)
                    ? mv
                    : document.SpecimenNumber ?? string.Empty;

                // Mirror the document-view/search file resolution (GenerateDocumentPdfAsync):
                // each scanned page is a separate file referenced by DocumentPage.PageURL and is
                // read via IFileStorage.ReadAsync(pageUrl, DataSource.Scanning). Emit one image
                // per page (ordered like document-view) so the requisition PDF is built from the
                // exact same files the document viewer renders. DocUrl is not used here because
                // the document viewer never reads it.
                var orderedPages = document.DocumentPages
                    .Where(p => !p.IsDeleted && !string.IsNullOrWhiteSpace(p.PageURL))
                    .OrderBy(p => p.DocumentPageId)
                    .ToList();

                if (orderedPages.Count == 0)
                {
                    _logger.LogWarning("Scanning document {DocumentId} has no readable pages", document.DocumentId);
                    continue;
                }

                var pageNumber = 1;
                foreach (var page in orderedPages)
                {
                    images.Add(new RequisitionImage
                    {
                        KeyValue = keyValue,
                        DocumentId = document.DocumentId,
                        CreationDate = document.DocumentDate ?? document.DatePosted,
                        PageNumber = pageNumber++,
                        FilePath = page.PageURL,
                        Source = DataSource.Scanning
                    });
                }
            }

            _logger.LogInformation("Scanning requisition query returned {Count} document(s)", images.Count);
            return images;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing scanning requisition query");
            throw;
        }
    }

    private async Task<IReadOnlyList<RequisitionImage>> FindImagesAsync(
        IReadOnlyList<string> accessions,
        DateTime dateOfService,
        int[] docTypes,
        int lookBackDays,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "FindImagesAsync called for {Count} accession(s), docTypes [{DocTypes}], dos {Dos:yyyy-MM-dd}, lookBackDays {LookBack}",
            accessions.Count, string.Join(",", docTypes), dateOfService, lookBackDays);

        try
        {
            // Reuse the shared ScanningServiceDbContext connection (Scanning_CONSTR) — the same
            // connection the document search service uses to reach the OnBase 'hsi' schema —
            // instead of opening a separate SqlConnection. The connection is owned by the
            // DbContext, so it is opened if needed but never disposed here.
            var connection = _context.Database.GetDbConnection();
            if (connection.State != ConnectionState.Open)
            {
                await connection.OpenAsync(cancellationToken);
            }

            await using var command = connection.CreateCommand();
            command.CommandType = CommandType.Text;
            command.CommandText = BuildQuery(accessions, docTypes, (SqlCommand)command);

            command.Parameters.Add(new SqlParameter("@DateOfService", SqlDbType.Date) { Value = dateOfService.Date });
            command.Parameters.Add(new SqlParameter("@LookBackDays", SqlDbType.Int) { Value = lookBackDays });
            command.Parameters.Add(new SqlParameter("@DateServiceInMonths", SqlDbType.Int) { Value = _dateServiceInMonths });

            var images = new List<RequisitionImage>();
            await using var reader = await command.ExecuteReaderAsync(cancellationToken);

            var keyValueOrdinal = reader.GetOrdinal("KeyValue");
            var documentIdOrdinal = reader.GetOrdinal("DocumentId");
            var creationDateOrdinal = reader.GetOrdinal("CreationDate");
            var pageNumberOrdinal = reader.GetOrdinal("PageNumber");
            var filePathOrdinal = reader.GetOrdinal("FilePath");

            while (await reader.ReadAsync(cancellationToken))
            {
                images.Add(new RequisitionImage
                {
                    KeyValue = reader.IsDBNull(keyValueOrdinal) ? string.Empty : reader.GetString(keyValueOrdinal),
                    DocumentId = reader.IsDBNull(documentIdOrdinal) ? 0 : Convert.ToInt64(reader.GetValue(documentIdOrdinal)),
                    CreationDate = reader.IsDBNull(creationDateOrdinal) ? dateOfService : reader.GetDateTime(creationDateOrdinal),
                    PageNumber = reader.IsDBNull(pageNumberOrdinal) ? 0 : reader.GetInt32(pageNumberOrdinal),
                    FilePath = reader.IsDBNull(filePathOrdinal) ? string.Empty : reader.GetString(filePathOrdinal)
                });
            }

            _logger.LogInformation("Requisition image query returned {Count} image row(s)", images.Count);
            foreach (var img in images)
            {
                _logger.LogInformation(
                    "OnBase image row: KeyValue={KeyValue}, PageNumber={PageNumber}, FilePath={FilePath}",
                    img.KeyValue, img.PageNumber, img.FilePath);
            }
            return images;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing requisition image query");
            throw;
        }
    }

    /// <summary>
    /// Builds the requisition-image lookup query inline (no stored procedure required).
    /// References the OnBase 'hsi' schema on the same SQL Server via three-part names so
    /// it resolves regardless of which catalog ONBASE_CONSTR points at.
    /// Roche (@DocTypes = {296}) uses hsi.keyitem161; every other set uses
    /// hsi.keytable104 + hsi.keyxitem104, matching the legacy handler behavior.
    /// </summary>
    private static string BuildQuery(IReadOnlyList<string> accessions, int[] docTypes, SqlCommand command)
    {
        var accessionParams = new List<string>(accessions.Count);
        for (var i = 0; i < accessions.Count; i++)
        {
            var name = "@acc" + i;
            accessionParams.Add(name);
            command.Parameters.Add(new SqlParameter(name, SqlDbType.NVarChar, 50) { Value = accessions[i] });
        }

        var docTypeParams = new List<string>(docTypes.Length);
        for (var i = 0; i < docTypes.Length; i++)
        {
            var name = "@dt" + i;
            docTypeParams.Add(name);
            command.Parameters.Add(new SqlParameter(name, SqlDbType.Int) { Value = docTypes[i] });
        }

        var accessionList = accessionParams.Count > 0 ? string.Join(", ", accessionParams) : "NULL";
        var docTypeList = docTypeParams.Count > 0 ? string.Join(", ", docTypeParams) : "NULL";

        var isRoche = docTypes.Length == 1 && docTypes[0] == 296;

        // Roche branch keys off hsi.keyitem161; default/CoC branch off hsi.keytable104 + hsi.keyxitem104.
        var keySource = isRoche
            ? @"OnBase.hsi.keyitem161 A
            JOIN OnBase.hsi.itemdatapage C     ON A.itemnum = C.itemnum"
            : @"OnBase.hsi.keytable104 A
            JOIN OnBase.hsi.keyxitem104 B      ON A.keywordnum = B.keywordnum
            JOIN OnBase.hsi.itemdatapage C     ON B.itemnum = C.itemnum";

        return $@"
        DECLARE @IncludeSecondaryPlatter BIT = 0;
        IF (@DateOfService < DATEADD(MONTH, -@DateServiceInMonths, CAST(GETDATE() AS DATE)))
            SET @IncludeSecondaryPlatter = 1;

        SELECT
            RTRIM(A.keyvaluechar)                       AS KeyValue,
            C.itemnum                                   AS DocumentId,
            E.itemdate                                  AS CreationDate,
            C.itempagenum                               AS PageNumber,
            RTRIM(D.lastuseddrive) + RTRIM(C.filepath)  AS FilePath
        FROM {keySource}
            JOIN OnBase.hsi.physicalplatter D  ON C.diskgroupnum = D.diskgroupnum
                                              AND C.logicalplatternum = D.logicalplatternum
            JOIN OnBase.hsi.itemdata E         ON C.itemnum = E.itemnum
        WHERE RTRIM(A.keyvaluechar) IN ({accessionList})
          AND C.filepath NOT LIKE '%.CTX%'
          AND (D.physicalplatternum = 1 OR (@IncludeSecondaryPlatter = 1 AND D.physicalplatternum IN (1, 2)))
          AND ISNULL(D.lastuseddrive, '') NOT IN ('', 'NOT CREATED')
          AND E.itemtypenum IN ({docTypeList})
          AND ISNULL(E.status, 0) = 0
          AND (@LookBackDays <= 0
               OR E.itemdate BETWEEN DATEADD(DAY, -@LookBackDays, @DateOfService)
                                 AND DATEADD(DAY,  @LookBackDays, @DateOfService))
        ORDER BY E.itemdate, C.itempagenum;";
    }
}
