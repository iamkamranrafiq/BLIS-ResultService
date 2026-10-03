using Bioreference.ScanningService.Application.Common.Interfaces;
using Bioreference.ScanningService.Application.DTOs.Requests;
using Bioreference.ScanningService.Application.DTOs.Responses;
using Bioreference.ScanningService.Domain.Entities;
using Microsoft.CodeAnalysis;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Data;
using System.Data.Common;
using System.Text.Json;


namespace Bioreference.ScanningService.Infrastructure.Persistence.Repositories;

public class OnBaseRepository : Repository<Domain.Entities.Document>, IOnBaseRepository
{
    private readonly ILogger<OnBaseRepository> _logger;
    public OnBaseRepository(ScanningServiceDbContext context, ILogger<OnBaseRepository> logger) : base(context)
    {
        _logger = logger;
    }

    public async Task<OnBaseDocumentSearchResponse> SearchDocumentAsync(DocumentSearchRequest request)
    {
        _logger.LogInformation("SearchDocumentAsync OnBase called");
        try
        {
            var keywordTable = BuildKeywordTable(request.KeywordCriteria);
            var itemTypesJson = JsonSerializer.Serialize(request.DocumentTypeIds);

            var keywordJson = request.KeywordCriteria is not null ? JsonSerializer.Serialize(request.KeywordCriteria
                                                .Select(x => new
                                                {
                                                    KeywordId = x.KeywordId,
                                                    SearchValue = DateTime.TryParse(x.KeywordValue?.ToString(), out var dt) ? dt.ToString("yyyy-MM-dd") : x.KeywordValue?.ToString()
                                                }))
                                                    : "[{}]";


            var baseParameters = new List<SqlParameter>()
            {
                new SqlParameter("@Keywords", keywordTable)
                                {
                                    SqlDbType = SqlDbType.Structured,
                                    TypeName = "dbo.KeywordSearchType"
                                },
                new SqlParameter("@KeywordValues", keywordJson),
                new SqlParameter("@ItemTypes", itemTypesJson.ToString())
            };

            var pagedParams = baseParameters.ToList();
            pagedParams.Add(new SqlParameter("@FromDate", (object?)request.FromDate?.Date ?? DBNull.Value));
            pagedParams.Add(new SqlParameter("@ToDate", (object?)request.ToDate?.Date.AddDays(1) ?? DBNull.Value));

            

            _logger.LogInformation("Keyword rows: {Count}", keywordTable.Rows.Count);
            
            var totalCount = await _context.Database.SqlQueryRaw<int>(
                                                "EXEC dbo.sp_CountOnBaseDocuments @Keywords, @KeywordValues,  @ItemTypes, @FromDate, @ToDate", pagedParams.ToArray())
                                            .ToListAsync();
            //var rows = await ExecuteDynamicSPAsync("sp_SearchOnBaseDocuments", parameters);


            var offset = request.NextToken?.OnbaseOffset ?? ((request.PagedRequest.PageNumber - 1) * request.PagedRequest.PageSize);
            pagedParams.Add(new SqlParameter("@Offset", offset));
            pagedParams.Add(new SqlParameter("@PageSize", request.PagedRequest.PageSize));

            pagedParams.Add(new SqlParameter("@SortColumn", (object?)request.OrderBy ?? DBNull.Value));
            pagedParams.Add(new SqlParameter("@SortDirection", (object)request.SortBy ?? DBNull.Value));


            var resp = await _context.Database.SqlQueryRaw<OnBaseDocumentSearchResult?>(
                                                @"EXEC dbo.sp_SearchOnBaseDocuments @Keywords, @KeywordValues, @ItemTypes,
                                                                            @FromDate, @ToDate, 
                                                                            @Offset, @PageSize, 
                                                                            @SortColumn, @SortDirection", 
                                                pagedParams.ToArray())
                                            .AsNoTracking()
                                            .ToListAsync();

            _logger.LogInformation("sp_SearchOnBaseDocuments executed. Found {Count} documents matching criteria", resp.Count);

            return new OnBaseDocumentSearchResponse
            {
                documentSearchResults = resp,
                ResponseStatus = { Success = true, Message = $"Found {resp.Count} document(s)" },
                paginations = new PagedResponse
                {
                    TotalCount = totalCount.FirstOrDefault(),
                    PageNumber = request.PagedRequest.PageNumber,
                    PageSize = request.PagedRequest.PageSize,
                    TotalPages = (int)Math.Ceiling(totalCount.FirstOrDefault() / (double)request.PagedRequest.PageSize)
                }
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing sp_SearchOnBaseDocuments");
            return new OnBaseDocumentSearchResponse
            {
                documentSearchResults = new List<OnBaseDocumentSearchResult>(),
                ResponseStatus = { Message = $"Error searching documents: {ex.Message}", Success = false }
            };
        }
    }

    public async Task<DocumentDetailDto?> GetDocumentByIdAsync(long documentId)
    {
        _logger.LogInformation($"OnBaseRepository GetByIdAsync called with DocId :{documentId}");
        try
        {
            string SP_Name = "dbo.SP_GetDocumentById";
            var parameters = new[]
            {
                new SqlParameter("@DocumentId", documentId)
            };

            var result = await ExecuteDynamicSPAsync(SP_Name, parameters);

            if (result is null)
            {
                return null;
            }

            var list = new List<DocumentDetailDto>();
            foreach(var item in result)
            {
                var documentDetails = new DocumentDetailDto
                {
                    Id = Convert.ToInt64(item["DocumentId"]),
                    Name = item["DocumentName"]?.ToString(),
                    DatePosted = Convert.ToDateTime(item["DatePosted"]),
                    DocUrl = item["FilePath"]?.ToString(),
                    Keywords = item.Where(x => x.Key.StartsWith("KeyValue"))
                                    .Select(x => new KeywordValueDto
                                    {
                                        KeywordId = int.Parse(x.Key["KeyValue".Length..]),
                                        TextValue = x.Value?.ToString()
                                    }).ToList(),
                    CreatedBy = item["username"]?.ToString() ?? string.Empty,
                    CreatedDate = Convert.ToDateTime(item["DatePosted"]),
                    DocumentType = new DocumentTypeInfoDto()
                    {
                        Id = Convert.ToInt32(item["DocumentTypeId"]),
                        DisplayName = item["DocumentType"]?.ToString(),
                        Code = item["DocumentTypeCode"]?.ToString()
                    },
                    DocumentGroup = new DocumentGroupInfoDto()
                    {
                        Id = Convert.ToInt32(item["DocumentGroupId"]),
                        Name = item["DocumentGroupName"]?.ToString(),
                    },
                    Batch = new BatchInfoDto()
                    {
                        BatchNumber = item["BatchNumber"]?.ToString(),
                        Name = item["BatchName"]?.ToString()
                    }
                };

                list.Add(documentDetails);
            }

            return list.FirstOrDefault();
        }
        catch (Exception)
        {

            throw;
        }
    }

    public async Task<Domain.Entities.Document?> GetDocumentDetailByIdAsync(long documentId)
    {
        _logger.LogInformation($"OnBaseRepository GetDocumentDetailByIdAsync called with DocId :{documentId}");
        try
        {
            string SP_Name = "dbo.SP_GetDocumentById";
            var parameters = new[]
            {
                new SqlParameter("@DocumentId", documentId)
            };

            var result = await ExecuteDynamicSPAsync(SP_Name, parameters);

            if (result is null || result?.Count <= 0)
            {
                return null;
            }

            var basicObject = result.FirstOrDefault();

            var documnet = new Domain.Entities.Document();
            documnet.DocumentId = Convert.ToInt64(basicObject["DocumentId"]);
            documnet.Name = basicObject["DocumentName"]?.ToString();
            documnet.DatePosted = Convert.ToDateTime(basicObject["DatePosted"]);
            documnet.CreatedBy = basicObject["username"]?.ToString();
            documnet.CreatedDate = Convert.ToDateTime(basicObject["DatePosted"]);
            documnet.DocumentType = new DocumentType()

            {
                DocumentTypeId = Convert.ToInt32(basicObject["DocumentTypeId"]),
                DisplayName = basicObject["DocumentType"]?.ToString(),
                Code = basicObject["DocumentTypeCode"]?.ToString(),
                GroupId = Convert.ToInt32(basicObject["DocumentGroupId"]),
                Group = new DocumentTypeGroup()
                {
                    DocumentTypeGroupId = Convert.ToInt32(basicObject["DocumentGroupId"]),
                    Name = basicObject["DocumentGroupName"]?.ToString()
                }
            };

            documnet.Batch = new Batch()
            {
                BatchNumber = basicObject["BatchNumber"]?.ToString(),
                Name = basicObject["BatchName"]?.ToString()
            };

            if(basicObject.Any(x => x.Key.StartsWith("KeyValue")))
            {
                documnet.DocumentKeywordValues = basicObject.Where(x => x.Key.StartsWith("KeyValue"))
                                    .Select(x => new DocumentKeywordValue
                                    {
                                        KeywordId = int.Parse(x.Key["KeyValue".Length..]),
                                        TextValue = x.Value?.ToString()
                                    }).ToList();
            }
            
            foreach (var item in result)
            {
                documnet.DocumentPages.Add(new DocumentPage()
                {
                    SequenceNo = Convert.ToInt32(item["PageNumber"]),
                    DocumentPageId = Convert.ToInt32(item["PageNumber"]),
                    PageURL = item["FilePath"]?.ToString()
                });
            }

            return documnet;
        }
        catch (Exception)
        {

            throw;
        }
    }

    public async Task<OnBaseBatchListResponse> SearchBatchsAsync(BatchListRequest request)
    {
        _logger.LogInformation("SearchBatchsAsync OnBase called");
        try
        {

            var baseParameters = new List<SqlParameter>{
                                    new SqlParameter("@Status", request.BatchStatusId),
                                    new SqlParameter("@BatchName", (object?)request.BatchName ?? DBNull.Value),
                                    new SqlParameter("@DateFrom", (object?)request.FromDate ?? DBNull.Value),
                                    new SqlParameter("@DateTo", (object?)request.ToDate ?? DBNull.Value),
                                    new SqlParameter("@QueueNum", (object?)request.ScanQueueId ?? DBNull.Value)
            };

            var countParams = baseParameters.ToArray();
            var TotalRecords = await _context.Database.SqlQueryRaw<int>(
                                    @"EXEC dbo.sp_CountOnBaseBatches
                                    @Status = @Status,
                                    @BatchName = @BatchName,
                                    @DateFrom = @DateFrom,
                                    @DateTo = @DateTo,
                                    @QueueNum = @QueueNum",
                                countParams.ToArray())
                            .ToListAsync();



            var pagedParams = baseParameters.ToList();
            pagedParams.Add(new SqlParameter("@Offset", request.NextToken?.OnbaseOffset ?? 0));
            pagedParams.Add(new SqlParameter("@PageSize", request.PageSize != null ? request.PageSize <= 0 ? 10 : request.PageSize : 10));

            pagedParams.Add(new SqlParameter("@SortColumn", (object?)request.OrderBy ?? DBNull.Value));
            pagedParams.Add(new SqlParameter("@SortDirection", (object)request.SortBy ?? DBNull.Value));
            

            var resp = await _context.Database.SqlQueryRaw<BatchSPResult?>(
                                    @"EXEC dbo.sp_GetOnBaseBatches
                                    @Status = @Status,
                                    @Offset = @Offset,
                                    @PageSize = @PageSize,
                                    @BatchName = @BatchName,
                                    @DateFrom = @DateFrom,
                                    @DateTo = @DateTo,
                                    @QueueNum = @QueueNum,
                                    @SortColumn = @SortColumn,
                                    @SortDirection = @SortDirection",    
                                pagedParams.ToArray())
                            .AsNoTracking()
                            .ToListAsync();

            _logger.LogInformation("dbo.sp_GetOnBaseBatches executed. Found {Count} documents matching criteria", TotalRecords.FirstOrDefault());

            return new OnBaseBatchListResponse
            {
                BatchSearchResults = resp ?? new List<BatchSPResult?>(),
                ResponseStatus = { Success = true, Message = $"Found {TotalRecords.FirstOrDefault()} document(s)" },
                paginations = new PagedResponse
                {
                    TotalCount = TotalRecords.FirstOrDefault(),
                    PageNumber = request.PageIndex ?? 1,
                    PageSize = request.PageSize ?? 10,
                    TotalPages = (int)Math.Ceiling(TotalRecords.FirstOrDefault() / (double)request.PageSize)
                }
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing sp_SearchOnBaseDocuments");
            return new OnBaseBatchListResponse
            {
                BatchSearchResults = null,
                ResponseStatus = { Message = $"Error searching documents: {ex.Message}", Success = false }
            };
        }
    }

    public async Task<OnBaseBatchDocumentListResponse> GetDocumentByBatchIdAsync(int batchId)
    {
        _logger.LogInformation("GetDocumentByBatchIdAsync OnBase called ");
        try
        {

            var resp = await _context.Database.SqlQueryRaw<OnBaseBatchDocumentResult>("EXEC dbo.sp_GetBatchDocuments @BatchNumber", new SqlParameter("@BatchNumber", batchId))
                .AsNoTracking().ToListAsync();

            _logger.LogInformation("dbo.sp_GetBatchDocuments executed");

            return new OnBaseBatchDocumentListResponse
            {
                data = resp,
                ResponseStatus = { Success = true, Message = $"Found {resp.Count} document(s)" },
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing sp_SearchOnBaseDocuments");
            return new OnBaseBatchDocumentListResponse
            {
                data = null,
                ResponseStatus = { Message = $"Error searching documents: {ex.Message}", Success = false }
            };
        }
    }



    private async Task<List<Dictionary<string, object?>>> ExecuteDynamicSPAsync(string storedProcedure, IEnumerable<DbParameter>? parameters = null, CancellationToken cancellationToken = default)
    {
        try
        {
            var results = new List<Dictionary<string, object?>>();

            var connection = _context.Database.GetDbConnection();

            if (connection.State != ConnectionState.Open)
                await connection.OpenAsync(cancellationToken);

            await using var command = connection.CreateCommand();

            command.CommandText = storedProcedure;
            command.CommandType = CommandType.StoredProcedure;

            if (parameters != null)
            {
                foreach (var parameter in parameters)
                {
                    command.Parameters.Add(parameter);
                }
            }

            await using var reader = await command.ExecuteReaderAsync(cancellationToken);

            var rows = new List<Dictionary<string, object?>>();

            while (await reader.ReadAsync(cancellationToken))
            {
                var row = new Dictionary<string, object?>(
                    StringComparer.OrdinalIgnoreCase);

                for (int i = 0; i < reader.FieldCount; i++)
                {
                    row[reader.GetName(i)] = reader.IsDBNull(i)
                        ? null
                        : reader.GetValue(i);
                }

                rows.Add(row);
            }

            return rows;
        }
        catch (Exception ex)
        {

            throw;
        }
    }

    private DataTable BuildKeywordTable(IEnumerable<KeywordSearchCriteria> keywords)
    {
        var table = new DataTable();
        table.Columns.Add("KeywordId", typeof(int));
        table.Columns.Add("TableType", typeof(byte));
        
        if(keywords.Count() <= 0)
        {
            return table;
        }

        foreach (var keyword in keywords)
        {
            table.Rows.Add(keyword.KeywordId, keyword.TableType);
        }
        return table;
    }


}
