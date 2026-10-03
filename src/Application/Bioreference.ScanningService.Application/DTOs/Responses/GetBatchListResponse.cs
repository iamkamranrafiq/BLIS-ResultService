using Bioreference.ScanningService.Application.Common.Enums;
using Bioreference.ScanningService.Application.DTOs.Requests;

namespace Bioreference.ScanningService.Application.DTOs.Responses;

public class BatchDto
{
    public int BatchId { get; set; }
    public string BatchNumber { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public int BatchStatusId { get; set; }
    public string Status { get; set; } = string.Empty;
    public string ScanMode { get; set; } = string.Empty;
    public int TotalDocuments { get; set; }
    public int TotalPages { get; set; }
    public string ScanQueue { get; set; } = string.Empty;
    public int TotalDocumentNotIndexed { get; set; } 
    public DateTime? StartedDate { get; set; }
    public DateTime? CompletedDate { get; set; }
    public DataSource DataSource { get; set; }
}

public class GetBatchListResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public List<BatchDto> Data { get; set; } = [];
    public int TotalCount { get; set; }
    public int PageNumber { get; set; }
    public int PageSize { get; set; }
    public int TotalPages => PageSize > 0 ? (int)Math.Ceiling((double)TotalCount / PageSize) : 0;
    public QueryState? NextToken { get; set; }
}
