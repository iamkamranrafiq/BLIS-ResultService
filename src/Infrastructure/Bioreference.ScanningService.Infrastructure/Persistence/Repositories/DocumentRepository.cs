using Bioreference.ScanningService.Application.Common.Interfaces;
using Bioreference.ScanningService.Application.DTOs.Requests;
using Bioreference.ScanningService.Application.DTOs.Responses;
using Bioreference.ScanningService.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Data;
using System.Globalization;

namespace Bioreference.ScanningService.Infrastructure.Persistence.Repositories;

public class DocumentRepository : Repository<Document>, IDocumentRepository
{
    private readonly ILogger<DocumentRepository> _logger;
    public DocumentRepository(ScanningServiceDbContext context, ILogger<DocumentRepository> logger) : base(context)
    {
        _logger = logger;
    }

    public async Task<Document?> GetByIdWithDetailsAsync(long id, CancellationToken cancellationToken = default)
    {

        try
        {
            var document = await _context.Document
                .Include(d => d.DocumentType)
                    .ThenInclude(dt => dt.Group)
                .Include(d => d.Batch)
                .Include(d => d.DocumentKeywordValues)
                    .ThenInclude(dkv => dkv.Keyword)
                .Include(d => d.DocumentPages)
                .Where(d => d.DocumentId == id && !d.IsDeleted)
                .FirstOrDefaultAsync(cancellationToken);

            return document;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching document {DocumentId} with details", id);
            throw;
        }
    }
    public async Task<DocumentSearchResponse> SearchDocumentAsync(DocumentSearchRequest request)
    {
        try
        {
            // Start with base query - active and not deleted documents
            IQueryable<Document> query = _context.Document
                .Include(d => d.DocumentType)
                    .ThenInclude(dt => dt.Group)
                .Include(d => d.Batch)
                .Where(d => d.IsActive && !d.IsDeleted);

            // Apply filters based on request criteria
            if (request != null)
            {
                // Filter by Document Type Group ID (if selected)
                if (request.DocumentTypeGroupId.HasValue && request.DocumentTypeGroupId.Value > 0)
                {
                    var groupId = request.DocumentTypeGroupId.Value;
                    query = query.Where(d => d.DocumentType.GroupId == groupId);
                    _logger.LogInformation("Filtering by DocumentTypeGroupId={GroupId}", groupId);
                }

                // Filter by Document Type IDs (multiple checkboxes - OR logic)
                if (request.DocumentTypeIds != null && request.DocumentTypeIds.Any())
                {
                    // Filter out invalid IDs (0 or negative)
                    var validTypeIds = request.DocumentTypeIds.Where(id => id > 0).ToList();

                    if (validTypeIds.Any())
                    {
                        query = query.Where(d => validTypeIds.Contains(d.DocumentTypeId));
                        _logger.LogInformation("Filtering by {Count} document type(s): {Ids}",
                            validTypeIds.Count,
                            string.Join(", ", validTypeIds));
                    }
                }

                // Filter by Date Range (From/To dates)
                if (request.FromDate.HasValue)
                {
                    var fromDate = request.FromDate.Value;
                    query = query.Where(d => d.DocumentDate >= fromDate);
                    _logger.LogInformation("Filtering by FromDate >= {FromDate}", fromDate);
                }

                if (request.ToDate.HasValue)
                {
                    var toDate = request.ToDate.Value;
                    query = query.Where(d => d.DocumentDate <= toDate);
                    _logger.LogInformation("Filtering by ToDate <= {ToDate}", toDate);
                }

                // Filter by Keyword Criteria (dynamic keyword fields - AND logic)
                // Examples: EOB Batch Number, Deposit Date, Initials, Date Posted, etc.
                if (request.KeywordCriteria != null && request.KeywordCriteria.Any())
                {
                    // The value for a keyword is stored in a type-specific column based on the
                    // keyword's DataType in the Keyword table (text/long/decimal/DateTime).
                    // Resolve each requested keyword's DataType up-front so the search targets the
                    // correct column instead of always checking TextValue.
                    var requestedKeywordIds = request.KeywordCriteria
                        .Where(k => k.KeywordId > 0)
                        .Select(k => k.KeywordId)
                        .Distinct()
                        .ToList();

                    var keywordDataTypes = await _context.Keyword
                        .Where(k => requestedKeywordIds.Contains(k.KeywordId))
                        .ToDictionaryAsync(k => k.KeywordId, k => k.DataType);

                    foreach (var keywordCriteria in request.KeywordCriteria)
                    {
                        if (keywordCriteria.KeywordId > 0)
                        {
                            var keywordId = keywordCriteria.KeywordId;
                            var searchValue = keywordCriteria.KeywordValue ?? string.Empty;

                            if (!string.IsNullOrWhiteSpace(searchValue))
                            {
                                var dataType = keywordDataTypes.TryGetValue(keywordId, out var dt) ? dt : null;

                                switch (dataType?.Trim().ToLowerInvariant())
                                {
                                    case "long":
                                        if (long.TryParse(searchValue, NumberStyles.Any, CultureInfo.InvariantCulture, out var longValue))
                                        {
                                            query = query.Where(d =>
                                                d.DocumentKeywordValues.Any(dkv =>
                                                    dkv.KeywordId == keywordId && dkv.LongValue == longValue));
                                        }
                                        break;

                                    case "decimal":
                                        if (decimal.TryParse(searchValue, NumberStyles.Any, CultureInfo.InvariantCulture, out var decimalValue))
                                        {
                                            query = query.Where(d =>
                                                d.DocumentKeywordValues.Any(dkv =>
                                                    dkv.KeywordId == keywordId && dkv.DecimalValue == decimalValue));
                                        }
                                        break;

                                    case "datetime":
                                        if (DateTime.TryParse(searchValue, CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal, out var dateValue))
                                        {
                                            var dateOnly = dateValue.Date;
                                            query = query.Where(d =>
                                                d.DocumentKeywordValues.Any(dkv =>
                                                    dkv.KeywordId == keywordId &&
                                                    dkv.DateTimeValue != null &&
                                                    dkv.DateTimeValue.Value.Date == dateOnly));
                                        }
                                        break;

                                    default: // "text" (and any unknown type) - partial match on the text columns
                                        query = query.Where(d =>
                                            d.DocumentKeywordValues.Any(dkv =>
                                                dkv.KeywordId == keywordId &&
                                                (
                                                    (dkv.TextValue != null && dkv.TextValue.Contains(searchValue)) ||
                                                    (dkv.AlphanumericValue != null && dkv.AlphanumericValue.Contains(searchValue))
                                                )));
                                        break;
                                }

                                _logger.LogInformation("Filtering by KeywordId={KeywordId} (DataType={DataType}) with value '{Value}'",
                                    keywordId,
                                    dataType,
                                    searchValue);
                            }
                            else
                            {
                                // Search by KeywordId only (document must have this keyword)
                                query = query.Where(d =>
                                    d.DocumentKeywordValues.Any(dkv => dkv.KeywordId == keywordId)
                                );
                                _logger.LogInformation("Filtering by KeywordId={KeywordId} - any value",
                                    keywordId);
                            }
                        }
                    }
                }
            }
            var paginationsRequest = request.PagedRequest ?? null;
            var totalRecords = await query.CountAsync();


            switch (request.OrderBy?.ToLower())
            {
                case "dateposted":
                    query = request.SortBy?.ToLower() == "asc"
                        ? query.OrderBy(x => x.DatePosted)
                        : query.OrderByDescending(x => x.DatePosted);
                    break;

                case "documentdate":
                    query = request.SortBy?.ToLower() == "asc"
                        ? query.OrderBy(x => x.DocumentDate)
                        : query.OrderByDescending(x => x.DocumentDate);
                    break;

                case "batchnumber":
                    query = request.SortBy?.ToLower() == "asc"
                        ? query.OrderBy(x => x.Batch.BatchNumber)
                        : query.OrderByDescending(x => x.Batch.BatchNumber);
                    break;

                case "documenttype":
                    query = request.SortBy?.ToLower() == "asc"
                        ? query.OrderBy(x => x.DocumentType)
                        : query.OrderByDescending(x => x.DocumentType);
                    break;

                case "documentname":
                    query = request.SortBy?.ToLower() == "asc"
                        ? query.OrderBy(x => x.Name)
                        : query.OrderByDescending(x => x.Name);
                    break;
            }

            if (paginationsRequest != null)
            {
                var skip = request.NextToken?.ScanningOffset ?? ((paginationsRequest.PageNumber - 1) * paginationsRequest.PageSize);
                query = query.Skip(skip).Take(paginationsRequest.PageSize);
            }

            // Execute query and project to result DTOs
            var res = query
                .Select(d => new DocumentSearchResult()
                {
                    DocumentId = d.DocumentId,
                    BatchNumber = d.Batch != null ? d.Batch.BatchNumber : "",
                    DocumentDate = d.DocumentDate,
                    DatePosted = d.DatePosted,
                    ThumbnailUrl = d.ThumbnailUrl,
                    DocumentName = d.Name,
                    DocumentType = d.DocumentType.DisplayName,
                    Status = d.Status,
                    DataSource = Application.Common.Enums.DataSource.Scanning
                })
                .ToList();

            _logger.LogInformation("Document search completed. Found {Count} documents matching criteria", res.Count);

            return new DocumentSearchResponse
            {
                documentSearchResults = res,
                ResponseStatus = { Success = true, Message = $"Found {res.Count} document(s)" },
                NextToken = new QueryState()
                {
                    OnbaseOffset = 0,
                    ScanningOffset = res.Count() + (request.NextToken?.ScanningOffset ?? 0)
                },
                paginations = new PagedResponse
                {
                    TotalCount = totalRecords,
                    PageNumber = paginationsRequest.PageNumber,
                    PageSize = paginationsRequest.PageSize,
                    TotalPages = (int)Math.Ceiling(totalRecords / (double)paginationsRequest.PageSize)
                }
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching documents");
            return new DocumentSearchResponse
            {
                documentSearchResults = new List<DocumentSearchResult>(),
                ResponseStatus = { Message = $"Error searching documents: {ex.Message}", Success = false }
            };
        }
    }
    public async Task<DocumentTypeListResponse> GetDocumentTypesAsync()
    {

        _logger.LogInformation("-----------------------Started Item" + DateTime.Now.ToLocalTime());
        var items = _context.DocumentType
            .AsNoTracking()
            .Where(dt => dt.IsActive && !dt.IsDeleted)
            .OrderBy(dt => dt.DisplayName)
            .Select(dt => new DocumentTypeItemDto
            {
                DocumentTypeId = dt.DocumentTypeId,
                Code = dt.Code,
                DisplayName = dt.DisplayName,
                DocumentTypeGroupID = dt.GroupId,
                DocumentTypeQueueId = _context.Set<ScanQueueDocumentType>()
                    .Where(sq => sq.DocumentTypeId == dt.DocumentTypeId)
                    .Select(sq => (int?)sq.ScanQueueId)
                    .FirstOrDefault()
            })
            .ToList();
        _logger.LogInformation("-------------------Started GP" + DateTime.Now.ToLocalTime());
        var groups = _context.DocumentTypeGroup
            .Where(dt => dt.IsActive && !dt.IsDeleted)
            .AsNoTracking()
            .OrderBy(g => g.Name)
            .Select(g => new DocumentGroupInfoDto
            {
                Id = g.DocumentTypeGroupId,
                Name = g.Name
            })
            .ToList();

        return new DocumentTypeListResponse
        {
            Success = true,
            Message = "Document types retrieved successfully.",
            DocumentTypes = items,
            DocumentTypeGroups = groups
        };
    }

    public async Task<DocumentTypeKeywordsResponse> GetDocumentTypeKeywordsAsync(List<int> documentTypeIds)
    {
        _logger.LogInformation("GetDocumentTypeKeywordsAsync querying {Count} document type(s)", documentTypeIds.Count);

        var data = await _context.DocumentType
            .AsNoTracking()
            .Where(dt => dt.IsActive && !dt.IsDeleted && documentTypeIds.Contains(dt.DocumentTypeId))
            .OrderBy(dt => dt.DisplayName)
            .Select(dt => new DocumentTypeWithKeywordsDto
            {
                DocumentTypeId = dt.DocumentTypeId,
                Code = dt.Code,
                DisplayName = dt.DisplayName,
                Keywords = dt.DocumentTypeKeywords
                    .Where(dtk => dtk.IsActive)
                    .OrderBy(dtk => dtk.DisplayOrder)
                    .Select(dtk => new DocumentTypeKeywordItemDto
                    {
                        KeywordId = dtk.KeywordId,
                        KeywordName = dtk.Keyword.Name,
                        DataType = dtk.Keyword.DataType,
                        ControlType = dtk.Keyword.ControlType,
                        IsRequired = dtk.IsRequired,
                        DisplayOrder = dtk.DisplayOrder,
                        TableType = dtk.Keyword.OnBaseTableType,
                    })
                    .ToList()
            })
            .ToListAsync();

        return new DocumentTypeKeywordsResponse
        {
            Success = true,
            Message = "Document type keywords retrieved successfully.",
            Data = data
        };
    }

    private static Document MapDocument(IDataRecord r) => new()
    {
        DocumentId = r.GetInt64(r.GetOrdinal("DocumentId")),
        BatchId = GetNullable<int>(r, "batch_id"),
        DocumentTypeId = r.GetInt32(r.GetOrdinal("document_type_id")),
        Name = r.GetString(r.GetOrdinal("name")),
        BarcodeValue = GetNullableString(r, "barcode_value"),
        SpecimenNumber = GetNullableString(r, "specimen_number"),
        IndexingValue = GetNullableString(r, "indexing_value"),
        DocUrl = GetNullableString(r, "doc_url"),
        DocumentDate = GetNullable<DateTime>(r, "document_date"),
        DatePosted = r.GetDateTime(r.GetOrdinal("date_posted")),
        Status = r.GetString(r.GetOrdinal("status")),
        IsIndexed = r.GetBoolean(r.GetOrdinal("is_indexed")),
        TotalPages = r.GetInt32(r.GetOrdinal("total_pages")),
        IsActive = r.GetBoolean(r.GetOrdinal("is_active")),
        CreatedBy = r.GetString(r.GetOrdinal("CreatedBy")),
        CreatedDate = r.GetDateTime(r.GetOrdinal("CreatedDate")),
        UpdatedBy = r.GetString(r.GetOrdinal("updated_by")),
        UpdatedDate = r.GetDateTime(r.GetOrdinal("UpdatedBy")),
        IsDeleted = r.GetBoolean(r.GetOrdinal("IsDeleted"))
    };

    private static DocumentType MapDocumentType(IDataRecord r) => new()
    {
        DocumentTypeId = r.GetInt32(r.GetOrdinal("DocumentTypeId")),
        GroupId = r.GetInt32(r.GetOrdinal("group_id")),
        Code = r.GetString(r.GetOrdinal("code")),
        DisplayName = r.GetString(r.GetOrdinal("display_name")),
        IsActive = r.GetBoolean(r.GetOrdinal("is_active")),
        CreatedBy = r.GetString(r.GetOrdinal("CreatedBy")),
        CreatedDate = r.GetDateTime(r.GetOrdinal("CreatedDate")),
        UpdatedBy = r.GetString(r.GetOrdinal("updated_by")),
        UpdatedDate = r.GetDateTime(r.GetOrdinal("UpdatedBy")),
        IsDeleted = r.GetBoolean(r.GetOrdinal("IsDeleted"))
    };

    private static DocumentTypeGroup MapDocumentTypeGroup(IDataRecord r) => new()
    {
        DocumentTypeGroupId = r.GetInt32(r.GetOrdinal("DocumentTypeGroupId")),
        Name = r.GetString(r.GetOrdinal("name")),
        IsActive = r.GetBoolean(r.GetOrdinal("is_active")),
        CreatedBy = r.GetString(r.GetOrdinal("CreatedBy")),
        CreatedDate = r.GetDateTime(r.GetOrdinal("CreatedDate")),
        UpdatedBy = r.GetString(r.GetOrdinal("updated_by")),
        UpdatedDate = r.GetDateTime(r.GetOrdinal("UpdatedBy")),
        IsDeleted = r.GetBoolean(r.GetOrdinal("IsDeleted"))
    };

    private static Batch MapBatch(IDataRecord r) => new()
    {
        BatchId = r.GetInt32(r.GetOrdinal("BatchId")),
        BatchNumber = r.GetString(r.GetOrdinal("batch_number")),
        Name = r.GetString(r.GetOrdinal("name")),
        ScanQueueId = GetNullable<int>(r, "scan_queue_id"),
        ScannerId = r.GetInt32(r.GetOrdinal("scanner_id")),
        ScanningFormatId = r.GetInt32(r.GetOrdinal("scanning_format_id")),
        ScanMode = r.GetString(r.GetOrdinal("scan_mode")),
        BatchStatusId = r.GetInt32(r.GetOrdinal("BatchStatusID")),
        TotalDocuments = r.GetInt32(r.GetOrdinal("total_documents")),
        TotalPages = r.GetInt32(r.GetOrdinal("total_pages")),
        StartedDate = GetNullable<DateTime>(r, "StartedDate"),
        CompletedDate = GetNullable<DateTime>(r, "CompletedDate"),
        IsActive = r.GetBoolean(r.GetOrdinal("is_active")),
        CreatedBy = r.GetString(r.GetOrdinal("CreatedBy")),
        CreatedDate = r.GetDateTime(r.GetOrdinal("CreatedDate")),
        UpdatedBy = r.GetString(r.GetOrdinal("updated_by")),
        UpdatedDate = r.GetDateTime(r.GetOrdinal("UpdatedBy")),
        IsDeleted = r.GetBoolean(r.GetOrdinal("IsDeleted"))
    };

    private static DocumentKeywordValue MapDocumentKeywordValue(IDataRecord r) => new()
    {
        DocumentKeywordValueId = r.GetInt32(r.GetOrdinal("DocumentKeywordValueId")),
        DocumentId = r.GetInt64(r.GetOrdinal("document_id")),
        KeywordId = r.GetInt32(r.GetOrdinal("keyword_id")),
        TextValue = r.GetString(r.GetOrdinal("value")),
        CreatedBy = r.GetString(r.GetOrdinal("dkv_CreatedBy")),
        CreatedDate = r.GetDateTime(r.GetOrdinal("dkv_CreatedDate")),
        UpdatedBy = r.GetString(r.GetOrdinal("dkv_updated_by")),
        UpdatedDate = r.GetDateTime(r.GetOrdinal("dkv_UpdatedBy"))
    };

    private static Keyword MapKeywordFromDkv(IDataRecord r) => new()
    {
        KeywordId = r.GetInt32(r.GetOrdinal("keyword_id_full")),
        Name = r.GetString(r.GetOrdinal("keyword_name")),
        DataType = r.GetString(r.GetOrdinal("data_type")),
        ControlType = r.GetString(r.GetOrdinal("control_type")),
        IsRequired = r.GetBoolean(r.GetOrdinal("is_required")),
        IsActive = r.GetBoolean(r.GetOrdinal("keyword_is_active"))
    };

    private static T? GetNullable<T>(IDataRecord r, string column) where T : struct
    {
        var ordinal = r.GetOrdinal(column);
        return r.IsDBNull(ordinal) ? null : (T)r.GetValue(ordinal);
    }

    private static DateOnly? GetNullableDateOnly(IDataRecord r, string column)
    {
        var ordinal = r.GetOrdinal(column);
        return r.IsDBNull(ordinal) ? null : DateOnly.FromDateTime(r.GetDateTime(ordinal));
    }

    private static string? GetNullableString(IDataRecord r, string column)
    {
        var ordinal = r.GetOrdinal(column);
        return r.IsDBNull(ordinal) ? null : r.GetString(ordinal);
    }

    public async Task<List<BarCodeFormatDocumentTypeKeyword>> GetBarCodeFormatDocumentTypeKeywordsByDocumentTypeIdAsync(int documentTypeId, CancellationToken cancellationToken = default)
    {
        try
        {
            return await _context.BarCodeFormatDocumentTypeKeyword
                .AsNoTracking()
                .Where(b => b.DocumentTypeId == documentTypeId &&
                            b.IsActive &&
                            b.IsDeleted != true)
                .OrderBy(b => b.KeywordId)
                .ToListAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching BarCodeFormatDocumentTypeKeywords for DocumentTypeId {DocumentTypeId}", documentTypeId);
        }
        return null;
    }
    public async Task<Document?> RenameDocumentAsync(long documentId, string newName, string? updatedBy = null, CancellationToken cancellationToken = default)
    {
        try
        {
            var document = await _context.Document
                .Where(d => d.DocumentId == documentId && !d.IsDeleted)
                .FirstOrDefaultAsync(cancellationToken);

            if (document == null)
            {
                _logger.LogWarning("Document with ID {DocumentId} not found or is deleted", documentId);
                return null;
            }

            document.Name = newName;
            document.UpdatedBy = updatedBy ?? "System";
            document.UpdatedDate = DateTime.UtcNow;

            _logger.LogInformation("Document {DocumentId} prepared for rename to '{NewName}' by {UpdatedBy}",
                documentId, newName, document.UpdatedBy);

            return document;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error renaming document {DocumentId}", documentId);
            throw;
        }
    }

    public async Task<List<Document>> GetDocumentsByBatchIdAsync(int batchId, CancellationToken cancellationToken = default)
    {
        try
        {
            var documents = await _context.Document
                .Where(d => d.BatchId == batchId && !d.IsDeleted)
                .Include(d => d.DocumentType)
                .Include(d => d.DocumentPages.Where(p => !p.IsDeleted))
                .OrderBy(d => d.CreatedDate)
                .AsNoTracking()
                .ToListAsync(cancellationToken);

            _logger.LogInformation("Retrieved {Count} document(s) for batch {BatchId}", documents.Count, batchId);

            return documents;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving documents for batch {BatchId}", batchId);
            throw;
        }
    }

    public async Task<int> UpdateDocumentsStatusByBatchIdAsync(int batchId, string status, string updatedBy, CancellationToken cancellationToken = default)
    {
        try
        {
            var affected = await _context.Document
                .Where(d => d.BatchId == batchId && !d.IsDeleted)
                .ExecuteUpdateAsync(setters => setters
                    .SetProperty(d => d.Status, status)
                    .SetProperty(d => d.UpdatedBy, updatedBy)
                    .SetProperty(d => d.UpdatedDate, DateTime.UtcNow),
                    cancellationToken);

            _logger.LogInformation("Updated status to {Status} for {Count} document(s) in batch {BatchId}",
                status, affected, batchId);

            return affected;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating document status for batch {BatchId}", batchId);
            throw;
        }
    }

    public async Task<Document?> GetByIdWithKeywordsAsync(long id, CancellationToken cancellationToken = default)
    {
        try
        {
            var document = await _context.Document
                .Include(d => d.DocumentKeywordValues)
                .Include(d => d.DocumentPages)
                .Where(d => d.DocumentId == id && !d.IsDeleted)
                .FirstOrDefaultAsync(cancellationToken);

            return document;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching document {DocumentId} with details", id);
            throw;
        }
    }

    public async Task<List<int>> GetDistinctBatchIdsAsync(IEnumerable<long> documentIds, CancellationToken cancellationToken = default)
    {
        var ids = documentIds.ToList();
        if (ids.Count == 0)
        {
            return new List<int>();
        }

        return await _context.Document
            .Where(d => ids.Contains(d.DocumentId) && d.BatchId.HasValue)
            .Select(d => d.BatchId!.Value)
            .Distinct()
            .ToListAsync(cancellationToken);
    }

    public async Task<List<Document>> GetDocumentsForDeletionAsync(List<long> documentIds, CancellationToken cancellationToken = default)
    {
        return await _context.Document
            .Where(d => documentIds.Contains(d.DocumentId) && !d.IsDeleted)
            .Include(d => d.DocumentPages)
            .ToListAsync(cancellationToken);
    }

    public void MarkDocumentsDeleted(IEnumerable<Document> documents, string deletedBy)
    {
        var deletionDate = DateTime.UtcNow;

        foreach (var document in documents)
        {
            document.IsDeleted = true;
            document.UpdatedBy = deletedBy;
            document.UpdatedDate = deletionDate;
        }
    }

}
