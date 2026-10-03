using Bioreference.ScanningService.Application.Common.Entity;
using Bioreference.ScanningService.Application.Common.Enums;
using Bioreference.ScanningService.Application.Common.Interfaces;
using Bioreference.ScanningService.Application.Common.Settings;
using Bioreference.ScanningService.Application.DTOs.Mappers;
using Bioreference.ScanningService.Application.DTOs.Requests;
using Bioreference.ScanningService.Application.DTOs.Responses;
using Bioreference.ScanningService.Application.Services.Interfaces;
using Bioreference.ScanningService.Domain.Entities;
using iText.IO.Image;
using iText.Kernel.Geom;
using iText.Kernel.Pdf;
using iText.Layout.Element;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SkiaSharp;
using System.Globalization;
using System.Text.RegularExpressions;
using static Microsoft.Extensions.Logging.EventSource.LoggingEventSource;

namespace Bioreference.ScanningService.Application.Services.Implementations;

public class DocumentService : IDocumentService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IFileStorage _fileStorage;
    private readonly ILogger<DocumentService> _logger;
    private readonly IDocumentRepository _documentRepository;
    private readonly IOnBaseRepository _onBaseRepository;
    private readonly OnBaseConfiguration _onBaseConfiguration;
    private readonly BarcodeSettings _barcodeSettings;
    private readonly IBatchService _batchService;
    private readonly IMemoryCache _memoryCache;

    private static readonly TimeSpan BarcodeKeywordCacheDuration = TimeSpan.FromMinutes(10);

    public DocumentService(IUnitOfWork unitOfWork, IFileStorage fileStorage, ILogger<DocumentService> logger, IDocumentRepository documentRepository, IOnBaseRepository onBaseRepository, IOptions<OnBaseConfiguration> onBaseConfiguration, IOptions<BarcodeSettings> barcodeSettings, IBatchService batchService, IMemoryCache memoryCache)
    {
        _unitOfWork = unitOfWork;
        _fileStorage = fileStorage;
        _documentRepository = documentRepository;
        _onBaseRepository = onBaseRepository;
        _logger = logger;
        _onBaseConfiguration = onBaseConfiguration.Value;
        _barcodeSettings = barcodeSettings.Value;
        _batchService = batchService;
        _memoryCache = memoryCache;
    }

    public async Task<DocumentTypeListResponse> GetDocumentTypesAsync()
    {
        _logger.LogInformation("GetDocumentTypesAsync called");

        try
        {
            var response = await _unitOfWork.Documents.GetDocumentTypesAsync();

            _logger.LogInformation("Retrieved {TypeCount} active document types and {GroupCount} groups",
                response.DocumentTypes.Count, response.DocumentTypeGroups.Count);

            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving document types");
            return new DocumentTypeListResponse
            {
                Success = false,
                Message = "An error occurred while retrieving document types."
            };
        }
    }
    public async Task<List<BarCodeFormatDocumentTypeKeyword>> GetDocumentTypesBarcodeKeywordAsync(int documentTypeId)
    {
        _logger.LogInformation("GetDocumentTypesBarcodeKeywordAsync called for documentTypeId={DocumentTypeId}", documentTypeId);

        var cacheKey = $"BarcodeKeywords_{documentTypeId}";
        if (_memoryCache.TryGetValue(cacheKey, out List<BarCodeFormatDocumentTypeKeyword>? cached) && cached != null)
        {
            return cached;
        }

        try
        {
            var barCodeFormatDocumentTypeKeywords = await _documentRepository.GetBarCodeFormatDocumentTypeKeywordsByDocumentTypeIdAsync(documentTypeId);
            // logic need to be added to select right BarCodeFormatDocumentTypeKeyword based on your criteria, for example, the first one
            var result = barCodeFormatDocumentTypeKeywords.ToList();
            _memoryCache.Set(cacheKey, result, BarcodeKeywordCacheDuration);
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving document types");
            return new List<BarCodeFormatDocumentTypeKeyword>();
        }
    }

    public async Task<DocumentType> GetDocumentTypeAsync(int documentTypeId)
    {
        _logger.LogInformation("GetDocumentTypeAsync called for documentTypeId={DocumentTypeId}", documentTypeId);

        try
        {
            var documentType = await _unitOfWork.DocumentTypes.GetByIdAsync(documentTypeId);

            if (documentType == null)
            {
                _logger.LogWarning("DocumentType with ID {DocumentTypeId} not found", documentTypeId);
                throw new KeyNotFoundException($"DocumentType with ID {documentTypeId} not found.");
            }

            _logger.LogInformation("Retrieved DocumentType: Id={DocumentTypeId}, DisplayName={DisplayName}",
                documentType.DocumentTypeId, documentType.DisplayName);

            return documentType;
        }
        catch (KeyNotFoundException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving document type with ID {DocumentTypeId}", documentTypeId);
            throw;
        }
    }

    public async Task<int?> ResolveDocumentTypeIdByNameAsync(string documentTypeName)
    {
        if (string.IsNullOrWhiteSpace(documentTypeName))
        {
            _logger.LogWarning("ResolveDocumentTypeIdByNameAsync called with an empty document type name.");
            return null;
        }

        var rawValue = documentTypeName.Trim();

        // Index values arrive as "Code - DisplayName" (e.g. "Req - Requisition"). Strip the
        // leading code segment before the first " - " and keep the remainder as the display name.
        var separatorIndex = rawValue.IndexOf(" - ", StringComparison.Ordinal);
        var displayNameValue = separatorIndex >= 0
            ? rawValue[(separatorIndex + 3)..].Trim()
            : rawValue;

        var documentTypes = await _unitOfWork.DocumentTypes.FindAsync(dt => dt.IsActive && !dt.IsDeleted);

        var match =
            documentTypes.FirstOrDefault(dt => string.Equals(dt.DisplayName, displayNameValue, StringComparison.OrdinalIgnoreCase))
            ?? documentTypes.FirstOrDefault(dt => string.Equals(dt.DisplayName, rawValue, StringComparison.OrdinalIgnoreCase))
            ?? documentTypes.FirstOrDefault(dt => string.Equals(dt.Code, rawValue, StringComparison.OrdinalIgnoreCase));

        if (match == null)
        {
            _logger.LogWarning("No DocumentType matched name '{DocumentTypeName}' (parsed display name '{DisplayName}').",
                rawValue, displayNameValue);
            return null;
        }

        _logger.LogInformation("Resolved DocumentType '{DocumentTypeName}' to Id={DocumentTypeId} (DisplayName={DisplayName}).",
            rawValue, match.DocumentTypeId, match.DisplayName);

        return match.DocumentTypeId;
    }

    public async Task<DocumentTypeKeywordsResponse> GetDocumentTypeKeywordsAsync(DocumentTypeKeywordsRequest request)
    {
        _logger.LogInformation("GetDocumentTypeKeywordsAsync called for {Count} document type(s)", request.DocumentTypeIds.Count);

        if (request.DocumentTypeIds.Count == 0)
        {
            return new DocumentTypeKeywordsResponse
            {
                Success = false,
                Message = "At least one DocumentTypeId must be provided."
            };
        }

        try
        {
            var data = await _unitOfWork.Documents.GetDocumentTypeKeywordsAsync(request.DocumentTypeIds);

            _logger.LogInformation("GetDocumentTypeKeywordsAsync retrieved {Count} document type(s) with keywords", data.Data.Count);

            return data;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving document type keywords");
            return new DocumentTypeKeywordsResponse
            {
                Success = false,
                Message = "An error occurred while retrieving document type keywords."
            };
        }
    }

    public async Task<GetDocumentResponse> GetDocumentByIdAsync(long id, DataSource source)
    {
        _logger.LogInformation("GetDocumentByIdAsync called for document id={Id}", id);

        try
        {
            var documentDto = new DocumentDetailDto();
            if (source == DataSource.Scanning)
            {
                var document = await _unitOfWork.Documents.GetByIdWithDetailsAsync(id);
                _logger.LogInformation("Document id={Id} retrieved successfully. Type: {TypeCode}, Keywords: {KeywordCount}",
                id, document.DocumentType?.Code, document.DocumentKeywordValues.Count);
                documentDto = document?.ToDetailDto();
            }
            else
            {
                documentDto = await _onBaseRepository.GetDocumentByIdAsync(id);
            }

            if (documentDto == null)
            {
                _logger.LogWarning("Document with id={Id} not found or is deleted.", id);
                return new GetDocumentResponse
                {
                    Success = false,
                    Message = $"Document with id {id} not found."
                };
            }

            return new GetDocumentResponse
            {
                Success = true,
                Message = "Document retrieved successfully.",
                Data = documentDto
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving document id={Id}", id);
            return new GetDocumentResponse
            {
                Success = false,
                Message = "An error occurred while retrieving the document."
            };
        }
    }
    public async Task<DocumentSearchResponse> SearchDocumentAsync(DocumentSearchRequest request)
    {
        _logger.LogInformation("SearchDocumentAsync called with {DocumentTypeCount} document types", request);
        var effectiveFrom = request.FromDate ?? DateTime.MinValue;
        var effectiveTo = request.ToDate ?? DateTime.UtcNow;
        DateTime? cutoffDate = _onBaseConfiguration.CutoverDate?.ToDateTime(TimeOnly.MinValue) ?? null;

        if (cutoffDate == null)
        {
            return await GetCombinedDocument(request);
        }

        if (effectiveTo < cutoffDate)
        {
            var resp = await _onBaseRepository.SearchDocumentAsync(request);
            return new DocumentSearchResponse
            {
                ResponseStatus = resp.ResponseStatus,
                documentSearchResults = resp.documentSearchResults.Select(x => DocumentSearchResult.From(x)).ToList(),
                paginations = resp.paginations,
                NextToken = new QueryState()
                {
                    OnbaseOffset = resp.documentSearchResults.Count() + (request.NextToken?.OnbaseOffset ?? 0),
                    ScanningOffset = 0
                }
            };
        }
        else if (effectiveFrom >= cutoffDate)
        {
            return await _unitOfWork.Documents.SearchDocumentAsync(request);
        }
        else
        {
            return await GetCombinedDocument(request);
        }

    }
    /// <summary>
    /// Generates document name and short name based on DocumentType format templates.
    /// Replaces placeholders: %N (DocumentType Code), %D2 (Date yyyyMMdd), %K{keywordId}.{index} (Keyword value)
    /// Example: "%K00155.0 - %K00127.1" where 00155 is keyword id and .0 is the first occurrence (0-based index).
    /// Index is 0-based: .0 = first value, .1 = second value, etc.
    /// </summary>
    /// <param name="batchId"></param>
    /// <param name="documentType"></param>
    /// <param name="Keywordval"></param>
    /// <returns></returns>
    public DocumentNames GetDocumentName(int batchId, DocumentType documentType, List<keywordValue> Keywordval)
    {
        if (Keywordval == null)
        {
            Keywordval = new List<keywordValue>();
        }
        string dt = DateTime.Now.ToString("M/d/yyyy");
        var name = documentType.DocumentNameFormat;
        var shortname = documentType.DocumentNameShortFormat;
        name = name.Replace("%N", documentType.Code);
        shortname = shortname.Replace("%N", documentType.Code);
        name = name.Replace("%D2", dt);
        shortname = shortname.Replace("%D2", dt);
        name = name.Replace("%D1", dt);
        shortname = shortname.Replace("%D1", dt);


        // Replace keyword values in format %K{keywordId}.{index}
        // Example: %K00155.0 - keyword id 155, index 0 (first occurrence)
        // Pattern matches %K followed by digits, dot and digit
        var keywordPattern = @"%K(\d+)\.(\d+)";

        // Process name
        name = Regex.Replace(name, keywordPattern, match =>
        {
            int keywordId = int.Parse(match.Groups[1].Value);
            int index = int.Parse(match.Groups[2].Value);
            index = index - 1; // convert to 0-based index

            // Find all barcodes with this keyword id
            var keywordValues = Keywordval
                .Where(b => b.KeywordId == keywordId)
                .Select(b => b.Value)
                .ToList();

            // Return the value at the specified index, or empty string if not found
            if (index < keywordValues.Count)
            {
                return keywordValues[index];
            }

            _logger.LogWarning("Keyword replacement failed for keywordId={KeywordId}, index={Index} in document name format. Found {Count} values.",
                keywordId, index, keywordValues.Count);
            return string.Empty; // Return empty string if not found
        });

        // Process shortname
        shortname = Regex.Replace(shortname, keywordPattern, match =>
        {
            int keywordId = int.Parse(match.Groups[1].Value);
            int index = int.Parse(match.Groups[2].Value);
            index = index - 1; // convert to 0-based index
            // Find all barcodes with this keyword id
            var keywordValues = Keywordval
                .Where(b => b.KeywordId == keywordId)
                .Select(b => b.Value)
                .ToList();

            // Return the value at the specified index, or empty string if not found
            if (index < keywordValues.Count)
            {
                return keywordValues[index];
            }

            _logger.LogWarning("Keyword replacement failed for keywordId={KeywordId}, index={Index} in document short name format. Found {Count} values.",
                keywordId, index, keywordValues.Count);
            return string.Empty; // Return empty string if not found
        });

        _logger.LogInformation("Generated document names - Name: '{Name}', ShortName: '{ShortName}' for batchId={BatchId}, documentType={DocumentType}",
            name, shortname, batchId, documentType.Code);

        return new DocumentNames(name, shortname);
    }

    public async Task<(long DocumentId, string ShortName)> CreateDocumentWithPagesAsync(int batchId, DocumentType documentType, List<DetectedImageBarcodes> detectedImageBarcodes, List<keywordValue> keywordValues, CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        byte[]? imgbytes = null;

        if (detectedImageBarcodes.Count > 0)
        {
            imgbytes = detectedImageBarcodes[0].ImageBytes;
        }
        var detectedImageBarcode = detectedImageBarcodes.FirstOrDefault(d => d.DetectedBarcodes.Count > 0);
        if (keywordValues == null)
        {
            keywordValues = new List<keywordValue>();
        }
        List<keywordValue> lstbarcodes = new List<keywordValue>();

        // Add barcodes from detected images
        if (detectedImageBarcode != null)
        {
            lstbarcodes.AddRange(detectedImageBarcode.DetectedBarcodes.Select(a => new keywordValue(a.KeywordId, a.Value)).ToList());
        }

        // Add keyword values from request
        lstbarcodes.AddRange(keywordValues.Select(a => new keywordValue(a.KeywordId, a.Value)).ToList());

        var documentNames = GetDocumentName(batchId, documentType, lstbarcodes);
        var document = new Domain.Entities.Document
        {
            BatchId = batchId,
            DocumentTypeId = documentType.DocumentTypeId,
            Name = documentNames.FullName.Replace("%", "-"),
            ShortName = documentNames.ShortName.Replace("%", "-"),
            DocumentDate = now,
            DatePosted = now,
            Status = "Scanning",
            IsIndexed = false,
            TotalPages = detectedImageBarcodes.Count,
            IsActive = true,
            CreatedBy = "system",
            CreatedDate = now,
            UpdatedBy = "system",
            UpdatedDate = now,
            IsDeleted = false
        };

        await _unitOfWork.BeginTransactionAsync();
        int? queueId = null;
        try
        {
            // Ensure the batch still exists and has not been soft-deleted (e.g. deleted mid-scan).
            // Creating documents against a deleted batch leaves orphaned active documents whose
            // parent batch is marked IsDeleted = true.
            var batch = await _unitOfWork.Batches.GetByIdAsync(batchId);
            if (batch is null || batch.IsDeleted)
            {
                _logger.LogWarning(
                    "Aborting document creation: batch {BatchId} was not found or has been deleted.", batchId);
                await _unitOfWork.RollbackAsync();
                throw new InvalidOperationException($"Batch {batchId} was not found or has been deleted.");
            }

            queueId = batch.ScanQueueId;

            await _unitOfWork.Documents.AddAsync(document);
            await _unitOfWork.SaveChangesAsync();

            // Save the thumbnail and every page concurrently — these are independent network
            // writes and were previously awaited one at a time, serializing several seconds per page.
            var thumbnailTask = imgbytes != null
                ? _fileStorage.SaveThumbnailAsync(imgbytes, document.DocumentId.ToString(), cancellationToken: cancellationToken)
                : null;

            var pageSaveTasks = detectedImageBarcodes
                                .Select((page, i) => _fileStorage.SaveAsync(
                                    page.ImageBytes,
                                    document.DocumentId.ToString(),
                                    i + 1,
                                    cancellationToken))
                                .ToArray();


            if (thumbnailTask != null)
            {
                var thumbnailPath = await thumbnailTask;
                document.ThumbnailUrl = thumbnailPath;
                _logger.LogInformation("Thumbnail generated and saved for document {DocumentId} at {Path}", document.DocumentId, thumbnailPath);
            }

            var pagePaths = await Task.WhenAll(pageSaveTasks);

            for (var i = 0; i < detectedImageBarcodes.Count; i++)
            {
                var pageNumber = i + 1;

                var documentPage = new Domain.Entities.DocumentPage
                {
                    DocumentId = document.DocumentId,
                    Name = $"Batch {batchId} Page {pageNumber}",
                    PageURL = pagePaths[i],
                    SequenceNo = pageNumber,
                    CreatedBy = "system",
                    CreatedDate = now,
                    UpdatedBy = "system",
                    UpdatedDate = now,
                    IsDeleted = false
                };

                await _unitOfWork.DocumentPages.AddAsync(documentPage);

            }

            // No barcode was detected on any scanned page. Instead of aborting the workflow,
            // continue so the document and its pages are still persisted, and capture the
            // missing-barcode condition in the ActivityLog for auditing.
            var barcodeMissing = detectedImageBarcode is null;
            if (barcodeMissing)
            {
                _logger.LogWarning("No barcode detected for any page of document {DocumentId} in batch {BatchId}. Continuing without barcode.",
                    document.DocumentId, batchId);
            }

            var RecuisitionValue = keywordValues.FirstOrDefault(x => x.KeywordId == 101)?.Value ?? null;
            var keyword104 = keywordValues.FirstOrDefault(x => x.KeywordId == 104);
            
            foreach (var detectedBarcode in detectedImageBarcode?.DetectedBarcodes ?? new List<BarcodeKeywordDoctype>())
            {
                // Skip barcodes that were not mapped to a real keyword (KeywordId defaults to 0
                // when no barcode keyword configuration matched). Persisting KeywordId = 0 violates
                // the FK_DocumentKeywordValue_Keyword constraint.
                if (detectedBarcode.KeywordId <= 0)
                {
                    _logger.LogWarning(
                        "Skipping detected barcode with unresolved KeywordId for document {DocumentId}, value {Value}",
                        document.DocumentId, detectedBarcode.Value);
                    continue;
                }

                var BarCodeFormatDocumentTypeKeywords = new DocumentKeywordValue()
                {
                    DocumentId = document.DocumentId,
                    KeywordId = detectedBarcode.KeywordId,
                    CreatedBy = "system",
                    CreatedDate = now,
                    UpdatedBy = "system",
                    UpdatedDate = now

                };
                await ApplyTypedKeywordValueAsync(BarCodeFormatDocumentTypeKeywords, detectedBarcode.KeywordId, $"{RecuisitionValue}{detectedBarcode.Value}");
                await _unitOfWork.DocumentKeywordValues.AddAsync(BarCodeFormatDocumentTypeKeywords);
            }

            foreach (var keyword in keywordValues)
            {
                // Skip keyword values that do not reference an existing keyword.
                if (keyword.KeywordId <= 0)
                {
                    _logger.LogWarning(
                        "Skipping keyword value with unresolved KeywordId for document {DocumentId}, value {Value}",
                        document.DocumentId, keyword.Value);
                    continue;
                }

                var DocumentTypeKeywords = new DocumentKeywordValue()
                {
                    DocumentId = document.DocumentId,
                    KeywordId = keyword.KeywordId,
                    CreatedBy = "system",
                    CreatedDate = now,
                    UpdatedBy = "system",
                    UpdatedDate = now
                };

                if (documentType.DocumentTypeId == 101)
                {
                    if (keyword.KeywordId == 101)
                    {
                        continue;
                    }
                    else if (keyword.KeywordId == 104)
                    {
                        await ApplyTypedKeywordValueAsync(DocumentTypeKeywords, keyword.KeywordId, $"{RecuisitionValue}{keyword.Value}");
                    }
                    else
                    {
                        await ApplyTypedKeywordValueAsync(DocumentTypeKeywords, keyword.KeywordId, keyword.Value);
                    }
                }
                else
                {
                    await ApplyTypedKeywordValueAsync(DocumentTypeKeywords, keyword.KeywordId, keyword.Value);
                }

                await _unitOfWork.DocumentKeywordValues.AddAsync(DocumentTypeKeywords);
            }

            if (documentType.DocumentTypeId == 101 && (keyword104 == null && detectedImageBarcode?.DetectedBarcodes == null) && !string.IsNullOrWhiteSpace(RecuisitionValue))
            {
                var documentKeywordValue = new DocumentKeywordValue
                {
                    DocumentId = document.DocumentId,
                    KeywordId = 104,
                    CreatedBy = "system",
                    CreatedDate = now,
                    UpdatedBy = "system",
                    UpdatedDate = now
                };

                await ApplyTypedKeywordValueAsync(documentKeywordValue, 104, RecuisitionValue);
                await _unitOfWork.DocumentKeywordValues.AddAsync(documentKeywordValue);
            }

            await _unitOfWork.SaveChangesAsync();

            // Record an activity log entry for this scan so scanning throughput can be audited
            // per batch/document/user. When the barcode is missing the document still moves
            // forward, but we intentionally do NOT write a per-document "Barcode is missing"
            // entry here — the missing-barcode condition is already surfaced by the per-page
            // "Error reading barcode" activity log, so this avoids duplicate document-id entries.
            if (!barcodeMissing)
            {
                await SaveActivityLogAsync(
                    batchId: batchId,
                    userId: "System",
                    documentsScanned: 1,
                    pagesScanned: detectedImageBarcodes.Count,
                    logLevel: "Info",
                    queueId: queueId,
                    loggedDate: now,
                    createdDate: now,
                    documentId: (int)document.DocumentId,
                    description: null);
            }
            await _unitOfWork.CommitAsync();

            await _batchService.UpdateBatchCountsAsync(batchId);
        }

        catch (Exception ex)
        {
            await _unitOfWork.RollbackAsync();

            // Persist an error activity log outside the rolled-back transaction so scanning
            // failures are still auditable.
            try
            {
                var errorNow = DateTime.UtcNow;
                await SaveActivityLogAsync(
                    batchId: batchId,
                    userId: "System",
                    documentsScanned: 0,
                    pagesScanned: detectedImageBarcodes.Count,
                    logLevel: "Error",
                    queueId: queueId,
                    loggedDate: errorNow,
                    createdDate: errorNow,
                    description: GetFriendlyScanErrorMessage(ex));
            }
            catch (Exception logEx)
            {
                _logger.LogError(logEx, "Failed to persist error activity log for batch {BatchId}", batchId);
            }

            _logger.LogError(ex, "Failed to create document with pages for batch {BatchId}", batchId);
            throw;
        }

        _logger.LogInformation("Document {DocumentId} created with {PageCount} page(s) for batch {BatchId}",
            document.DocumentId, detectedImageBarcodes.Count, batchId);

        return (document.DocumentId, document.ShortName);
    }


    /// <summary>
    /// Maps an exception raised during the scanning workflow to a user-friendly, auditable message
    /// for the ActivityLog. Known conditions (e.g. missing barcode) return a generic message.
    /// </summary>
    private static string GetFriendlyScanErrorMessage(Exception ex)
    {
        // A NullReferenceException in this workflow is typically caused by no barcode being
        // detected on any scanned page.
        if (ex is NullReferenceException)
        {
            return "Barcode is missing.";
        }

        var message = ex.Message ?? string.Empty;
        if (message.Contains("barcode", StringComparison.OrdinalIgnoreCase))
        {
            return "Barcode is missing.";
        }

        return string.IsNullOrWhiteSpace(message)
            ? "An error occurred during the scanning workflow."
            : message;
    }

    /// <summary>
    /// Builds and persists an <see cref="ActivityLog"/> entry so scanning activity (success or
    /// failure) is auditable per batch/document/user.
    /// </summary>
    private async Task SaveActivityLogAsync(
        int batchId,
        string? userId,
        int documentsScanned,
        int pagesScanned,
        string logLevel,
        int? queueId,
        DateTime loggedDate,
        DateTime createdDate,
        int? documentId = null,
        string? description = null)
    {
        var activityLog = new ActivityLog
        {
            BatchId = batchId,
            DocumentId = documentId,
            UserId = string.IsNullOrWhiteSpace(userId) ? "system" : userId,
            DocumentsScanned = documentsScanned,
            PagesScanned = pagesScanned,
            LogLevel = logLevel,
            QueueId = queueId,
            Description = description,
            LoggedDate = loggedDate,
            CreatedDate = createdDate
        };

        await _unitOfWork.ActivityLogs.AddAsync(activityLog);
        await _unitOfWork.SaveChangesAsync();
    }

    public BarcodeKeywordDoctype MapBarcodeToBarcodeKeywordDoctype(List<BarCodeFormatDocumentTypeKeyword> barcodeKeywords, DetectedBarcode detectedBarcode)
    {
        //chnage the logic to select the right BarCodeFormatDocumentTypeKeyword based on your criteria, for example, the first one
        _logger.LogInformation("MapBarcodeToBarcodeKeywordDoctype called with {Count} barcode keywords", barcodeKeywords?.Count ?? 0);

        if (barcodeKeywords == null || barcodeKeywords.Count == 0)
        {
            _logger.LogWarning("No barcode keywords provided");


            return null;
        }

        if (barcodeKeywords == null || barcodeKeywords.Count == 0)
        {
            _logger.LogWarning("No barcode keywords provided. Returning barcode value without keyword mapping.");
            return new BarcodeKeywordDoctype
            {
                Value = detectedBarcode.Value,
                Format = detectedBarcode.Format,
                Confidence = detectedBarcode.Confidence,
                Points = detectedBarcode.Points,
                DPI = detectedBarcode.DPI
            };
        }

        // Select keyword based on barcode position (X, Y coordinates)
        var firstKeyword = SelectKeywordByPosition(barcodeKeywords, detectedBarcode);
        if (firstKeyword == null)
            return null;

        var result = new BarcodeKeywordDoctype
        {
            Value = detectedBarcode.Value,
            Format = detectedBarcode.Format,
            Confidence = detectedBarcode.Confidence,
            Points = detectedBarcode.Points,
            DPI = detectedBarcode.DPI,
            DoctypeId = firstKeyword.DocumentTypeId,
            KeywordId = firstKeyword.KeywordId,
            BarcodeId = firstKeyword.BarCodeFormatId.ToString()
        };

        _logger.LogInformation("Mapped barcode to DoctypeId={DoctypeId}, KeywordId={KeywordId}, BarcodeId={BarcodeId}",
            result.DoctypeId, result.KeywordId, result.BarcodeId);

        return result;
    }

    private BarCodeFormatDocumentTypeKeyword SelectKeywordByPosition(
        List<BarCodeFormatDocumentTypeKeyword> barcodeKeywords,
        DetectedBarcode detectedBarcode)
    {
        // Need at least 2 points from ZXing to determine barcode position
        if (detectedBarcode.Points == null || detectedBarcode.Points.Length < 2)
        {
            _logger.LogWarning("Not enough position points available for barcode, using first keyword as fallback");
            return barcodeKeywords.First();
        }

        if (detectedBarcode.DPI <= 0)
        {
            _logger.LogWarning("Invalid DPI ({DPI}) for barcode, using first keyword as fallback", detectedBarcode.DPI);
            return barcodeKeywords.First();
        }

        // Pick first 2 points and convert from pixels to inches by dividing by DPI
        float dpi = detectedBarcode.DPI;
        float point1XInches = detectedBarcode.Points[0].X / dpi;
        float point1YInches = detectedBarcode.Points[0].Y / dpi;
        float point2XInches = detectedBarcode.Points[1].X / dpi;
        float point2YInches = (detectedBarcode.Points[1].Y / dpi) + _barcodeSettings.BarcodeHeightOffsetInches;

        _logger.LogInformation(
            "Barcode position in inches: P1=({X1}, {Y1}), P2=({X2}, {Y2}), HeightOffset={Offset}",
            point1XInches, point1YInches, point2XInches, point2YInches, _barcodeSettings.BarcodeHeightOffsetInches);

        // Find the keyword whose bounding box contains the barcode points
        foreach (var keyword in barcodeKeywords)
        {
            if (IsBarcodeInBoundingBox(point1XInches, point1YInches, point2XInches, point2YInches, keyword))
            {
                _logger.LogInformation("Barcode matched to keyword with bounds: Top={Top}, Left={Left}, Width={Width}, Height={Height}",
                    keyword.TopB, keyword.LeftB, keyword.WidthB, keyword.HeightB);
                return keyword;
            }
        }

        // If no match found, log warning and use first keyword as fallback
        _logger.LogWarning("Barcode position did not match any keyword bounding boxes. Using first keyword as fallback.");
        return null; //barcodeKeywords.First();
    }

    private bool IsBarcodeInBoundingBox(float x1Inches, float y1Inches, float x2Inches, float y2Inches, BarCodeFormatDocumentTypeKeyword keyword)
    {
        // Check if keyword has valid bounding box dimensions
        if (!keyword.TopB.HasValue || !keyword.LeftB.HasValue ||
            !keyword.WidthB.HasValue || !keyword.HeightB.HasValue)
        {
            _logger.LogDebug("Keyword KeywordId={KeywordId} has incomplete bounding box data", keyword.KeywordId);
            return false;
        }

        // Divide bounding box values by 100 to convert to inches
        var top = (float)keyword.TopB.Value / 100f;
        var left = (float)keyword.LeftB.Value / 100f;
        var width = (float)keyword.WidthB.Value / 100f;
        var height = (float)keyword.HeightB.Value / 100f;

        // Calculate bounding box boundaries in inches
        var right = left + width;
        var bottom = top + height;

        // Check if both barcode points are within the bounding box
        var point1Inside = x1Inches >= left && x1Inches <= right && y1Inches >= top && y1Inches <= bottom;
        var point2Inside = x2Inches >= left && x2Inches <= right && y2Inches >= top && y2Inches <= bottom;

        var isInside = point1Inside || point2Inside;

        if (isInside)
        {
            _logger.LogDebug("Barcode is inside box (inches): Left={Left}, Right={Right}, Top={Top}, Bottom={Bottom}",
                left, right, top, bottom);
        }

        return isInside;
    }

    public async Task<byte[]?> GetFileAsync(long documentId, string fileUrl)
    {
        _logger.LogInformation("GetFileAsync called for documentId={DocumentId}, fileUrl={FileUrl}", documentId, fileUrl);

        if (string.IsNullOrWhiteSpace(fileUrl))
        {
            _logger.LogWarning("File URL is null or empty for documentId={DocumentId}", documentId);
            return null;
        }

        try
        {
            return await _fileStorage.ReadAsync(fileUrl, DataSource.Scanning);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving file from {FileUrl} for documentId={DocumentId}", fileUrl, documentId);
            throw;
        }
    }

    public async Task<byte[]?> GenerateDocumentPdfAsync(long documentId, DataSource source = DataSource.Scanning)
    {
        _logger.LogInformation("GenerateDocumentPdfAsync called for documentId={DocumentId}", documentId);

        try
        {
            // Get document with pages
            var document = new Document();
            if (source == DataSource.OnBase)
            {
                document = await _onBaseRepository.GetDocumentDetailByIdAsync(documentId);
            }
            else
            {
                document = await _documentRepository.GetByIdWithDetailsAsync(documentId);
            }



            if (document == null)
            {
                _logger.LogWarning("Document {DocumentId} not found", documentId);
                return null;
            }

            if (document.DocumentPages == null || !document.DocumentPages.Any())
            {
                _logger.LogWarning("Document {DocumentId} has no pages", documentId);
                return null;
            }

            var orderedPages = document.DocumentPages
                .Where(p => !p.IsDeleted)
                .OrderBy(p => p.DocumentPageId)
                .ToList();

            if (!orderedPages.Any())
            {
                _logger.LogWarning("Document {DocumentId} has no valid pages", documentId);
                return null;
            }

            // Check if the first page is already a PDF
            var firstPage = orderedPages.First();
            if (string.IsNullOrWhiteSpace(firstPage.PageURL))
            {
                _logger.LogWarning("First page {PageId} has no URL", firstPage.DocumentPageId);
                return null;
            }

            var extension = System.IO.Path.GetExtension(firstPage.PageURL).ToLowerInvariant();
            if (extension == ".ctx")
            {
                _logger.LogWarning("File type is not supported");
                return null;
            }

            // Read the first file to check if it's a PDF
            var firstFileBytes = await _fileStorage.ReadAsync(firstPage.PageURL, source);

            if (firstFileBytes == null)
            {
                _logger.LogWarning("Failed to read first page {PageId} from {Url}", firstPage.DocumentPageId, firstPage.PageURL);
                return null;
            }


            // Check if file is already a PDF (PDF files start with %PDF)
            bool isPdf = firstFileBytes.Length > 4 &&
                         firstFileBytes[0] == 0x25 && // %
                         firstFileBytes[1] == 0x50 && // P
                         firstFileBytes[2] == 0x44 && // D
                         firstFileBytes[3] == 0x46;   // F

            if (isPdf)
            {
                _logger.LogInformation("Document {DocumentId} is already a PDF, returning as-is", documentId);

                if (orderedPages.Count == 1)
                {
                    _logger.LogInformation("Single page PDF, returning directly. Size: {Size} bytes", firstFileBytes.Length);
                    return firstFileBytes;
                }

                _logger.LogWarning("Document {DocumentId} has {PageCount} pages, all appear to be PDFs. Returning first page only. Consider implementing PDF merge.",
                    documentId, orderedPages.Count);
                return firstFileBytes;
            }

            _logger.LogInformation("Generating PDF for document {DocumentId} with {PageCount} pages", documentId, orderedPages.Count);

            if (extension.ToLower() == ".tif" || extension.ToLower() == ".tiff")
            {
                var pdf = await CreateTiffPdf(orderedPages, source);
                _logger.LogInformation("PDF generated successfully for document {DocumentId}, size: {Size} bytes", documentId, pdf.Length);

                return pdf;
            }


            // Create PDF in memory
            using var stream = new MemoryStream();
            using (var document1 = SKDocument.CreatePdf(stream))
            {
                foreach (var page in orderedPages)
                {
                    if (string.IsNullOrWhiteSpace(page.PageURL))
                    {
                        _logger.LogWarning("Page {PageId} has no URL, skipping", page.DocumentPageId);
                        continue;
                    }

                    // Read image from network storage
                    var imageBytes = await _fileStorage.ReadAsync(page.PageURL, source);

                    if (imageBytes == null)
                    {
                        _logger.LogWarning("Failed to read image for page {PageId} from {Url}", page.DocumentPageId, page.PageURL);
                        continue;
                    }

                    // Decode image
                    SKBitmap? bitmap;
                    try
                    {
                        bitmap = SKBitmap.Decode(imageBytes);
                    }
                    catch (Exception decodeEx)
                    {
                        throw new InvalidOperationException(
                            $"SKBitmap.Decode failed for page {page.DocumentPageId} " +
                            $"(bytes: {imageBytes.Length}, url: {page.PageURL}).", decodeEx);
                    }

                    if (bitmap == null)
                    {
                        _logger.LogWarning("Failed to decode image for page {PageId}", page.DocumentPageId);
                        continue;
                    }

                    using (bitmap)
                    {
                        try
                        {
                            // Create PDF page with image dimensions
                            using var canvas = document1.BeginPage(bitmap.Width, bitmap.Height);

                            // Draw the image on the PDF page
                            canvas.DrawBitmap(bitmap, 0, 0, SKSamplingOptions.Default, null);

                            document1.EndPage();
                        }
                        catch (Exception drawEx)
                        {
                            throw new InvalidOperationException(
                                $"PDF page render failed for page {page.DocumentPageId} " +
                                $"(dimensions: {bitmap.Width}x{bitmap.Height}, url: {page.PageURL}).", drawEx);
                        }
                    }

                    _logger.LogDebug("Added page {PageId} to PDF", page.DocumentPageId);
                }

                document1.Close();
            }

            var pdfBytes = stream.ToArray();
            _logger.LogInformation("PDF generated successfully for document {DocumentId}, size: {Size} bytes", documentId, pdfBytes.Length);

            return pdfBytes;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating PDF for document {DocumentId}", documentId);
            throw;
        }
    }

    public List<List<DetectedImageBarcodes>> GroupImagesByBarcode(List<DetectedImageBarcodes> detectedImageBarcodes)
    {
        _logger.LogInformation("GroupImagesByBarcode called with {Count} images", detectedImageBarcodes?.Count ?? 0);

        if (detectedImageBarcodes == null || !detectedImageBarcodes.Any())
        {
            _logger.LogWarning("No images provided for grouping");
            return new List<List<DetectedImageBarcodes>>();
        }

        var documentGroups = new List<List<DetectedImageBarcodes>>();
        List<DetectedImageBarcodes>? currentGroup = null;

        for (int i = 0; i < detectedImageBarcodes.Count; i++)
        {
            var currentImage = detectedImageBarcodes[i];

            // If current image has a barcode, start a new group
            if (currentImage.DetectedBarcodes != null && currentImage.DetectedBarcodes.Any())
            {
                currentGroup = new List<DetectedImageBarcodes>();
                documentGroups.Add(currentGroup);
                _logger.LogDebug("Barcode found on page {PageIndex}, starting new document group {GroupIndex}",
                    i + 1, documentGroups.Count);
            }

            // Add current image to the current group (if a group exists)
            if (currentGroup != null)
            {
                currentGroup.Add(currentImage);
            }
            else
            {
                // Orphan page(s) before the first barcode: make each its own document so every
                // page without a barcode is tracked and flagged separately (instead of merging
                // consecutive orphan pages into a single document). Leave currentGroup null so
                // the next orphan page starts its own group.
                var orphanGroup = new List<DetectedImageBarcodes> { currentImage };
                documentGroups.Add(orphanGroup);
                _logger.LogWarning("Page {PageIndex} has no barcode and no prior group, creating standalone orphan document", i + 1);
            }
        }

        _logger.LogInformation("Grouped {TotalImages} images into {GroupCount} document(s)",
            detectedImageBarcodes.Count, documentGroups.Count);

        return documentGroups;
    }

    public async Task<RenameDocumentResponse> RenameDocumentAsync(long documentId, RenameDocumentRequest request)
    {
        _logger.LogInformation("RenameDocumentAsync called for document id={DocumentId} with new name='{NewName}'",
            documentId, request.Name);

        try
        {
            var document = await _unitOfWork.Documents.RenameDocumentAsync(
                documentId,
                request.Name,
                request.UpdatedBy);

            if (document == null)
            {
                _logger.LogWarning("Document with id={DocumentId} not found or is deleted", documentId);
                return new RenameDocumentResponse
                {
                    Success = false,
                    Message = $"Document with id {documentId} not found."
                };
            }

            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation("Document id={DocumentId} successfully renamed to '{NewName}'",
                documentId, request.Name);

            return new RenameDocumentResponse
            {
                Success = true,
                Message = "Document renamed successfully.",
                Data = new DocumentRenameDto
                {
                    DocumentId = document.DocumentId,
                    Name = document.Name,
                    UpdatedDate = document.UpdatedDate,
                    UpdatedBy = document.UpdatedBy
                }
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error renaming document id={DocumentId}", documentId);
            return new RenameDocumentResponse
            {
                Success = false,
                Message = "An error occurred while renaming the document."
            };
        }
    }

    public async Task<DeleteDocumentsResponse> DeleteDocumentsAsync(DeleteDocumentsRequest request)
    {
        _logger.LogInformation("DeleteDocumentsAsync called for {Count} document(s)", request.DocumentIds.Count);

        try
        {
            var deletedBy = request.DeletedBy ?? "System";
            var documents = await _unitOfWork.Documents.GetDocumentsForDeletionAsync(request.DocumentIds);
            
            var successfulIds = new List<long>();
            var failedIds = new List<long>();

            foreach (var document in documents)
            {
                try
                {
                    var archivedThumbnailPath = await _fileStorage.MoveToDeletedFolderAsync(document.ThumbnailUrl);
                    document.ThumbnailUrl = archivedThumbnailPath;
                    
                    foreach (var page in document.DocumentPages)
                    {
                        if (string.IsNullOrWhiteSpace(page.PageURL))
                            continue;

                        // Move file and get new location
                        var archivedPath = await _fileStorage.MoveToDeletedFolderAsync(page.PageURL);
                        // Update DB path
                        page.PageURL = archivedPath;
                        
                    }

                    successfulIds.Add(document.DocumentId);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed deleting document {DocumentId}", document.DocumentId);
                    failedIds.Add(document.DocumentId);
                }
            }

            var successfulDocuments = documents.Where(x => successfulIds.Contains(x.DocumentId)).ToList();
            _unitOfWork.Documents.MarkDocumentsDeleted(successfulDocuments, deletedBy);

            await _unitOfWork.SaveChangesAsync();

            if (successfulIds.Count == 0)
            {
                _logger.LogWarning("No documents were deleted. All {Count} document(s) not found or already deleted", request.DocumentIds.Count);
                return new DeleteDocumentsResponse
                {
                    Success = false,
                    Message = "No documents were deleted. Documents not found or already deleted.",
                    Data = new DeleteDocumentsResultDto
                    {
                        TotalRequested = request.DocumentIds.Count,
                        SuccessfullyDeleted = 0,
                        Failed = failedIds.Count,
                        DeletedDocumentIds = successfulIds,
                        FailedDocumentIds = failedIds
                    }
                };
            }
                

            var message = failedIds.Count > 0
                ? $"{successfulIds.Count} document(s) deleted successfully. {failedIds.Count} document(s) failed (not found or already deleted)."
                : $"{successfulIds.Count} document(s) deleted successfully.";

            _logger.LogInformation("DeleteDocumentsAsync completed: {Success} successful, {Failed} failed",
                successfulIds.Count, failedIds.Count);

            //Update document counts in batch table (each affected batch once, resolved on DB server)
            var affectedBatchIds = await _unitOfWork.Documents.GetDistinctBatchIdsAsync(successfulIds);
            foreach (var batchId in affectedBatchIds)
            {
                await _batchService.UpdateBatchCountsAsync(batchId);
            }

            return new DeleteDocumentsResponse
            {
                Success = true,
                Message = message,
                Data = new DeleteDocumentsResultDto
                {
                    TotalRequested = request.DocumentIds.Count,
                    SuccessfullyDeleted = successfulIds.Count,
                    Failed = failedIds.Count,
                    DeletedDocumentIds = successfulIds,
                    FailedDocumentIds = failedIds,
                    DeletedDate = DateTime.UtcNow,
                    DeletedBy = request.DeletedBy ?? "System"
                }
            };

        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting documents");
            return new DeleteDocumentsResponse
            {
                Success = false,
                Message = "An error occurred while deleting documents."
            };
        }
    }

    public async Task<GetDocumentsByBatchIdResponse> GetDocumentsByBatchIdAsync(int batchId, DataSource source)
    {
        _logger.LogInformation("GetDocumentsByBatchIdAsync called for batch {BatchId} for data source {DataSource}", batchId, source);

        try
        {
            List<BatchDocumentDto> documentDtos = new List<BatchDocumentDto>();
            string scanMode = string.Empty;
            if (source == DataSource.OnBase)
            {
                var resp = await _onBaseRepository.GetDocumentByBatchIdAsync(batchId);
                scanMode = DataSource.OnBase.ToString();
                documentDtos = resp.data.Select(d => new BatchDocumentDto
                {
                    DocumentId = d.ItemNumber,
                    Name = d.DocumentName,
                    DocumentTypeId = d.ItemTypeNumber,
                    ShortName = d.DocumentName,
                    DocumentTypeName = d.DocumentType,
                    BarcodeValue = string.Empty,
                    IndexingValue = string.Empty,
                    ThumbnailUrl = string.Empty,
                    DocUrl = string.Empty,
                    DocumentDate = d.DatePosted,
                    SpecimenNumber = string.Empty,
                    Status = string.Empty,
                    TotalPages = 0,
                    CreatedDate = d.StoreDateTime,
                    DataSource = source,
                }).ToList();
            }
            else
            {
                var documents = await _unitOfWork.Documents.GetDocumentsByBatchIdAsync(batchId);

                var batch = await _unitOfWork.Batches.GetByIdAsync(batchId);
                scanMode = batch?.ScanMode ?? string.Empty;

                documentDtos = documents.Select(d => new BatchDocumentDto
                {
                    DocumentId = d.DocumentId,
                    Name = d.Name,
                    ShortName = d.ShortName ?? string.Empty,
                    DocumentTypeId = d.DocumentTypeId,
                    DocumentTypeName = d.DocumentType?.DisplayName ?? string.Empty,
                    BarcodeValue = d.BarcodeValue ?? string.Empty,
                    IndexingValue = d.IndexingValue ?? string.Empty,
                    ThumbnailUrl = d.ThumbnailUrl ?? string.Empty,
                    DocUrl = d.DocUrl ?? string.Empty,
                    DocumentDate = d.DocumentDate,
                    SpecimenNumber = d.SpecimenNumber ?? string.Empty,
                    Status = d.Status ?? string.Empty,
                    IsIndexed = d.IsIndexed,
                    TotalPages = d.TotalPages,
                    CreatedDate = d.CreatedDate,
                    DataSource = source,
                    Pages = d.DocumentPages
                        .OrderBy(p => p.CreatedDate)
                        .Select(p => new DocumentPageDto
                        {
                            DocumentPageId = p.DocumentPageId,
                            Name = p.Name ?? string.Empty,
                            PageURL = p.PageURL ?? string.Empty,
                            CreatedDate = p.CreatedDate
                        })
                        .ToList()
                }).ToList();
            }


            _logger.LogInformation("Retrieved {Count} document(s) with pages for batch {BatchId}",
                documentDtos.Count, batchId);

            return new GetDocumentsByBatchIdResponse
            {
                Success = true,
                Message = documentDtos.Count > 0
                    ? $"Retrieved {documentDtos.Count} document(s) for batch {batchId}."
                    : $"No documents found for batch {batchId}.",
                Documents = documentDtos,
                TotalDocuments = documentDtos.Count,
                BatchId = batchId,
                ScanMode = scanMode
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving documents for batch {BatchId}", batchId);
            return new GetDocumentsByBatchIdResponse
            {
                Success = false,
                Message = "An error occurred while retrieving documents."
            };
        }
    }
    
    public async Task<GetDocumentShortNamesByBatchIdResponse> GetDocumentShortNamesByBatchIdAsync(int batchId)
    {
        _logger.LogInformation("GetDocumentShortNamesByBatchIdAsync called for batch {BatchId}", batchId);

        try
        {
            var documents = await _unitOfWork.Documents.GetDocumentsByBatchIdAsync(batchId);

            var documentDtos = documents.Select(d => new DocumentShortNameDto
            {
                DocumentId = d.DocumentId,
                ShortName = d.ShortName ?? string.Empty
            }).ToList();

            _logger.LogInformation("Retrieved {Count} document short name(s) for batch {BatchId}",
                documentDtos.Count, batchId);

            return new GetDocumentShortNamesByBatchIdResponse
            {
                Success = true,
                Message = documentDtos.Count > 0
                    ? $"Retrieved {documentDtos.Count} document(s) for batch {batchId}."
                    : $"No documents found for batch {batchId}.",
                Documents = documentDtos,
                TotalDocuments = documentDtos.Count,
                BatchId = batchId
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving document short names for batch {BatchId}", batchId);
            return new GetDocumentShortNamesByBatchIdResponse
            {
                Success = false,
                Message = "An error occurred while retrieving documents."
            };
        }
    }

    public async Task<ImportDocumentResponse> ImportDocumentAsync(Stream stream, ImportDocumentRequest request, FileData fileProperties, CancellationToken cancellationToken)
    {
        _logger.LogInformation("UploadDocument called for upload the document");
        // Add keyword values from request

        var documentType = await GetDocumentTypeAsync(request.DocumentTypeId);
        var documentNames = GetDocumentName(0, documentType, request.keywords?.ToList());
        var now = DateTime.UtcNow;
        var createdBy = string.IsNullOrWhiteSpace(request.CreatedBy) ? "system" : request.CreatedBy;
        var updatedBy = string.IsNullOrWhiteSpace(request.UpdatedBy) ? createdBy : request.UpdatedBy;
        var document = new Domain.Entities.Document
        {
            BatchId = null,
            DocumentTypeId = request.DocumentTypeId,
            Name = documentNames.FullName.Replace("%", "-"),
            ShortName = documentNames.ShortName.Replace("%", "-"),
            DatePosted = now,
            Status = "import",
            DocumentDate = request.DocumentDate,
            IsIndexed = false,
            TotalPages = 1,
            IsActive = true,
            CreatedBy = createdBy,
            CreatedDate = now,
            UpdatedBy = updatedBy,
            UpdatedDate = now,
            IsDeleted = false
        };

        await _unitOfWork.BeginTransactionAsync();
        try
        {
            await _unitOfWork.Documents.AddAsync(document);
            await _unitOfWork.SaveChangesAsync();


            using var memoryStream = new MemoryStream();
            await stream.CopyToAsync(memoryStream);

            byte[] fileBytes = memoryStream.ToArray();

            if (fileBytes != null)
            {
                var ThumbnailUrl = await _fileStorage.SavePdfThumbnailAsync(fileBytes, document.DocumentId.ToString(), cancellationToken: cancellationToken);

                document.ThumbnailUrl = ThumbnailUrl;
                _logger.LogInformation("Thumbnail generated and saved for document {DocumentId} at {Path}", document.DocumentId, ThumbnailUrl);
            }

            var filePath = await _fileStorage.SavePdfAsync(fileBytes, document.DocumentId.ToString(), fileProperties.FileName, cancellationToken: cancellationToken);

            var documentPage = new Domain.Entities.DocumentPage
            {
                DocumentId = document.DocumentId,
                Name = $"import Page 1",
                PageURL = filePath,

                CreatedBy = createdBy,
                CreatedDate = now,
                UpdatedBy = updatedBy,
                UpdatedDate = now,
                IsDeleted = false
            };

            await _unitOfWork.DocumentPages.AddAsync(documentPage);
            await _unitOfWork.SaveChangesAsync();

            var keyword101 = request.keywords.FirstOrDefault(x => x.KeywordId == 101);
            var keyword104 = request.keywords.FirstOrDefault(x => x.KeywordId == 104);

            var keyword101Value = keyword101?.Value;
            var keyword104Value = keyword104?.Value;

            foreach (var keyword in request.keywords)
            {
                // don't save 101 for document type 101
                if (request.DocumentTypeId == 101 && keyword.KeywordId == 101)
                {
                    continue;
                }

                var documentKeywordValue = new DocumentKeywordValue
                {
                    DocumentId = document.DocumentId,
                    KeywordId = keyword.KeywordId,
                    CreatedBy = createdBy,
                    CreatedDate = now,
                    UpdatedBy = updatedBy,
                    UpdatedDate = now
                };

                if (request.DocumentTypeId == 101 && keyword.KeywordId == 104)
                {
                    await ApplyTypedKeywordValueAsync(documentKeywordValue, keyword.KeywordId, $"{keyword101Value}{keyword104Value}");
                }
                else
                {
                    await ApplyTypedKeywordValueAsync(documentKeywordValue, keyword.KeywordId, keyword.Value);
                }

                await _unitOfWork.DocumentKeywordValues.AddAsync(documentKeywordValue);
            }
            //Handle special case for the value of only 101
            if (request.DocumentTypeId == 101 && keyword104 == null && !string.IsNullOrWhiteSpace(keyword101Value))
            {
                var documentKeywordValue = new DocumentKeywordValue
                {
                    DocumentId = document.DocumentId,
                    KeywordId = 104,
                    CreatedBy = createdBy,
                    CreatedDate = now,
                    UpdatedBy = updatedBy,
                    UpdatedDate = now
                };

                await ApplyTypedKeywordValueAsync(documentKeywordValue, 104, keyword101Value);
                await _unitOfWork.DocumentKeywordValues.AddAsync(documentKeywordValue);
            }

            await _unitOfWork.SaveChangesAsync();

            await _unitOfWork.CommitAsync();

            return new ImportDocumentResponse
            {
                Success = true,
                Message = "Document Import Successfully",
                DocumentId = document.DocumentId,
                Name = document.Name
            };
        }
        catch (Exception ex)
        {
            await _unitOfWork.RollbackAsync();
            _logger.LogError(ex, "Error while importing document");
            return new ImportDocumentResponse
            {
                Success = false,
                Message = "Error occours while Importing a document",
                DocumentId = null
            };
        }
    }

    public async Task<ImportDocumentResponse> IngestDocumentAsync(IngestDocumentRequest request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("IngestDocumentAsync called for path {FilePath}", request.FilePath);

        var documentType = await GetDocumentTypeAsync(request.DocumentTypeId);
        var keywords = request.keywords?.ToList() ?? new List<keywordValue>();

        var now = DateTime.UtcNow;
        var createdBy = string.IsNullOrWhiteSpace(request.CreatedBy) ? "system" : request.CreatedBy;
        var updatedBy = string.IsNullOrWhiteSpace(request.UpdatedBy) ? createdBy : request.UpdatedBy;
        var name = string.IsNullOrWhiteSpace(request.Name)
            ? System.IO.Path.GetFileName(request.FilePath)
            : request.Name;

        var document = new Domain.Entities.Document
        {
            BatchId = null,
            DocumentTypeId = request.DocumentTypeId,
            Name = name,
            ShortName = name,
            DocUrl = request.FilePath,
            DatePosted = now,
            Status = "import",
            DocumentDate = request.DocumentDate,
            IsIndexed = false,
            TotalPages = 1,
            IsActive = true,
            CreatedBy = createdBy,
            CreatedDate = now,
            UpdatedBy = updatedBy,
            UpdatedDate = now,
            IsDeleted = false
        };

        await _unitOfWork.BeginTransactionAsync();
        try
        {
            await _unitOfWork.Documents.AddAsync(document);
            await _unitOfWork.SaveChangesAsync();

            var documentPage = new Domain.Entities.DocumentPage
            {
                DocumentId = document.DocumentId,
                Name = name,
                PageURL = request.FilePath,
                SequenceNo = 1,
                CreatedBy = createdBy,
                CreatedDate = now,
                UpdatedBy = updatedBy,
                UpdatedDate = now,
                IsDeleted = false
            };

            await _unitOfWork.DocumentPages.AddAsync(documentPage);
            await _unitOfWork.SaveChangesAsync();

            foreach (var keyword in keywords)
            {
                var documentKeywordValue = new DocumentKeywordValue
                {
                    DocumentId = document.DocumentId,
                    KeywordId = keyword.KeywordId,
                    CreatedBy = createdBy,
                    CreatedDate = now,
                    UpdatedBy = updatedBy,
                    UpdatedDate = now
                };
                await ApplyTypedKeywordValueAsync(documentKeywordValue, keyword.KeywordId, keyword.Value);
                await _unitOfWork.DocumentKeywordValues.AddAsync(documentKeywordValue);
                await _unitOfWork.SaveChangesAsync();
            }

            await _unitOfWork.CommitAsync();

            return new ImportDocumentResponse
            {
                Success = true,
                Message = "Document Ingested Successfully",
                DocumentId = document.DocumentId,
                Name = document.Name
            };
        }
        catch (Exception ex)
        {
            await _unitOfWork.RollbackAsync();
            _logger.LogError(ex, "Error while ingesting document for path {FilePath}", request.FilePath);
            return new ImportDocumentResponse
            {
                Success = false,
                Message = "Error occurred while ingesting a document",
                DocumentId = null
            };
        }
    }

    public async Task<UpdateDocumentResponse> UpdateDocumentAsync(long documentId, UpdateDocumentRequest request)
    {
        _logger.LogInformation("UpdateDocumentAsync called for document id={DocumentId} ", documentId);

        try
        {
            var document = await _unitOfWork.Documents.GetByIdWithKeywordsAsync(documentId);

            if (document == null)
            {
                _logger.LogWarning("Document with id={DocumentId} not found or is deleted", documentId);
                return new UpdateDocumentResponse
                {
                    Success = false,
                    Message = $"Document with id {documentId} not found."
                };
            }

            if (request.DocumentTypeId.HasValue)
                document.DocumentTypeId = request.DocumentTypeId.Value;

            if (request.DocumentDate.HasValue)
                document.DocumentDate = request.DocumentDate.Value;

            if (request.DatePosted.HasValue)
                document.DatePosted = request.DatePosted.Value;

            if (!string.IsNullOrWhiteSpace(request.UpdatedBy))
                document.UpdatedBy = request.UpdatedBy;


            document.UpdatedDate = DateTime.Now;

            await UpdateKeywords(document, request.keywords, request.RemovedKeywordValueIds);

            await _unitOfWork.Documents.UpdateAsync(document);
            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation("Document id={DocumentId} successfully updated", documentId);

            return new UpdateDocumentResponse
            {
                Success = true,
                Message = "Document updated successfully.",
                Data = new DocumentUpdateDto
                {
                    DocumentId = document.DocumentId,
                    UpdatedDate = document.UpdatedDate,
                    UpdatedBy = document.UpdatedBy
                }
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating document id={DocumentId}", documentId);
            return new UpdateDocumentResponse
            {
                Success = false,
                Message = "An error occurred while updating the document."
            };
        }

    }


    private async Task UpdateKeywords(Document document, ICollection<Documentkeyword>? requestKeywords, ICollection<int>? removedKeywordValueIds = null)
    {
        requestKeywords ??= [];

        var now = DateTime.Now;

        // Delete values explicitly removed in the UI. The client sends the real
        // DocumentKeywordValueId of each deleted row so only those are removed (other update
        // callers send none and are unaffected).
        if (removedKeywordValueIds is { Count: > 0 })
        {
            var removeIds = removedKeywordValueIds.Where(id => id > 0).ToHashSet();
            var keywordsToRemove = document.DocumentKeywordValues
                .Where(x => removeIds.Contains(x.DocumentKeywordValueId))
                .ToList();

            foreach (var keyword in keywordsToRemove)
            {
                document.DocumentKeywordValues.Remove(keyword);
            }
        }

        // Add new or update existing
        foreach (var requestKeyword in requestKeywords)
        {
            // Match on the row identity (DocumentKeywordValueId) so a single KeywordId can hold
            // multiple values. A zero/negative id (or one not present on the document) is treated
            // as a new value to add rather than an update.
            var existing = requestKeyword.DocumentKeywordValueId > 0
                ? document.DocumentKeywordValues
                    .FirstOrDefault(x => x.DocumentKeywordValueId == requestKeyword.DocumentKeywordValueId)
                : null;

            if (existing == null)
            {
                var newKeywordValue = new DocumentKeywordValue
                {
                    KeywordId = requestKeyword.KeywordId,
                    DocumentId = document.DocumentId,
                    CreatedBy = document.UpdatedBy,
                    CreatedDate = now,
                    UpdatedBy = document.UpdatedBy,
                    UpdatedDate = now
                };
                await ApplyTypedKeywordValueAsync(newKeywordValue, requestKeyword.KeywordId, requestKeyword.Value);
                document.DocumentKeywordValues.Add(newKeywordValue);
            }
            else
            {
                existing.KeywordId = requestKeyword.KeywordId;
                await ApplyTypedKeywordValueAsync(existing, requestKeyword.KeywordId, requestKeyword.Value);
                existing.UpdatedBy = document.UpdatedBy;
                existing.UpdatedDate = now;
            }
        }
    }

    /// <summary>
    /// Resolves the keyword's DataType and stores the raw value into the matching typed column
    /// (AlphanumericValue, DateTimeValue, DecimalValue, LongValue or TextValue).
    /// Throws when the value cannot be parsed into the target type.
    /// </summary>
    private async Task ApplyTypedKeywordValueAsync(DocumentKeywordValue target, int keywordId, string? rawValue, string? dataType = null)
    {
        if (string.IsNullOrWhiteSpace(dataType))
        {
            var keyword = await _unitOfWork.Keywords.GetByIdAsync(keywordId);
            if (keyword == null || string.IsNullOrWhiteSpace(keyword.DataType))
            {
                throw new InvalidOperationException($"Keyword {keywordId} not found or has no DataType configured.");
            }

            dataType = keyword.DataType;
        }

        SetTypedKeywordValue(target, dataType!, rawValue);
    }

    /// <summary>
    /// Assigns <paramref name="rawValue"/> to the correct typed column based on the keyword DataType.
    /// </summary>
    private static void SetTypedKeywordValue(DocumentKeywordValue target, string dataType, string? rawValue)
    {
        // Reset all typed columns so only the relevant one holds a value.
        target.AlphanumericValue = null;
        target.DateTimeValue = null;
        target.DecimalValue = null;
        target.LongValue = null;
        target.TextValue = null;

        switch (dataType.Trim().ToLowerInvariant())
        {
            case "alphanumeric with dropdown list":
                target.AlphanumericValue = rawValue;
                break;

            case "datetime":
                if (string.IsNullOrWhiteSpace(rawValue))
                {
                    break;
                }
                if (!DateTime.TryParse(rawValue, CultureInfo.InvariantCulture, DateTimeStyles.None, out var dateValue))
                {
                    throw new InvalidOperationException($"Value '{rawValue}' is not a valid DateTime for keyword {target.KeywordId}.");
                }
                target.DateTimeValue = dateValue;
                break;

            case "decimal":
                if (string.IsNullOrWhiteSpace(rawValue))
                {
                    break;
                }
                if (!decimal.TryParse(rawValue, NumberStyles.Number, CultureInfo.InvariantCulture, out var decimalValue))
                {
                    throw new InvalidOperationException($"Value '{rawValue}' is not a valid decimal for keyword {target.KeywordId}.");
                }
                target.DecimalValue = decimalValue;
                break;

            case "long":
                if (string.IsNullOrWhiteSpace(rawValue))
                {
                    break;
                }
                if (!long.TryParse(rawValue, NumberStyles.Integer, CultureInfo.InvariantCulture, out var longValue))
                {
                    throw new InvalidOperationException($"Value '{rawValue}' is not a valid long for keyword {target.KeywordId}.");
                }
                target.LongValue = longValue;
                break;

            case "text":
                target.TextValue = rawValue;
                break;

            default:
                throw new InvalidOperationException($"Unsupported keyword DataType '{dataType}' for keyword {target.KeywordId}.");
        }
    }

    public async Task<byte[]> CreateTiffPdf(ICollection<DocumentPage> pages, DataSource source)
    {
        try
        {
            using var outputStream = new MemoryStream();

            using var writer = new PdfWriter(outputStream);
            using var pdf = new PdfDocument(writer);
            using var document = new iText.Layout.Document(pdf);

            foreach (var page in pages)
            {
                var imageBytes = await _fileStorage.ReadAsync(page.PageURL, source);
                AddImageAsPdfPage(pdf, document, imageBytes);
            }

            document.Close();

            return outputStream.ToArray();
        }
        catch (Exception)
        {

            throw;
        }

    }

    private void AddImageAsPdfPage(PdfDocument pdf, iText.Layout.Document document, byte[] imageBytes)
    {
        try
        {
            var imageData = ImageDataFactory.Create(imageBytes);

            var page = pdf.AddNewPage(PageSize.LETTER);
            int pageNumber = pdf.GetPageNumber(page);
            var pageSize = page.GetPageSize();

            var image = new Image(imageData);

            image.ScaleToFit(
                pageSize.GetWidth() - 20,
                pageSize.GetHeight() - 20);

            image.SetFixedPosition(pageNumber, 10, 10);

            document.Add(image);
        }
        catch (Exception)
        {

            throw;
        }

    }

    private async Task<DocumentSearchResponse> GetCombinedDocument(DocumentSearchRequest request)
    {
        try
        {
            var scanningResp = await _unitOfWork.Documents.SearchDocumentAsync(request);
            var onBaseResp = await _onBaseRepository.SearchDocumentAsync(request);


            var TotalCount = (onBaseResp.paginations?.TotalCount ?? 0) + scanningResp.paginations?.TotalCount ?? 0;

            List<DocumentSearchResult> allResp = new List<DocumentSearchResult>();
            if (scanningResp.documentSearchResults.Count() > 0)
                allResp.AddRange(scanningResp.documentSearchResults);
            if (onBaseResp.documentSearchResults?.Count() > 0)
                allResp.AddRange(onBaseResp.documentSearchResults?.Select(x => DocumentSearchResult.From(x)).ToList());

            if (request.SortBy.ToLower().Trim() == "asc")
                allResp = allResp.OrderBy(x => request.OrderBy).Take(request.PagedRequest.PageSize).ToList();
            else
                allResp = allResp.OrderByDescending(x => request.OrderBy).Take(request.PagedRequest.PageSize).ToList();

            // Now Need to add manage the NextToken
            var OnBaseCount = allResp.Where(x => x.DataSource == DataSource.OnBase).Count();
            var ScanningCount = allResp.Where(x => x.DataSource == DataSource.Scanning).Count();

            var nextToken = new QueryState()
            {
                OnbaseOffset = OnBaseCount + (request.NextToken?.OnbaseOffset ?? 0),
                ScanningOffset = ScanningCount + (request.NextToken?.ScanningOffset ?? 0)
            };

            return new DocumentSearchResponse
            {
                ResponseStatus = onBaseResp.ResponseStatus,
                documentSearchResults = allResp,
                paginations = new PagedResponse()
                {
                    PageNumber = request.PagedRequest.PageNumber,
                    PageSize = request.PagedRequest.PageSize,
                    TotalCount = TotalCount,
                    TotalPages = (int)Math.Ceiling(TotalCount / (double)request.PagedRequest.PageSize)
                },
                NextToken = nextToken,
            };

        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error get combined document data");
            return new DocumentSearchResponse
            {
                ResponseStatus =
                {
                    Success = false,
                    Message = ex.Message,
                },
                documentSearchResults = null,
                paginations = new PagedResponse()
                {
                    PageNumber = request.PagedRequest.PageNumber,
                    PageSize = request.PagedRequest.PageSize,
                    TotalCount = 0,
                    TotalPages = 0
                }
            };
        }
    }
}

