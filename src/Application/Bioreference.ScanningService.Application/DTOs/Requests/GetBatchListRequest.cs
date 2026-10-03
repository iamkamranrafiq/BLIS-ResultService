namespace Bioreference.ScanningService.Application.DTOs.Requests;

public class GetBatchListRequest
{
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 10;
    public int? BatchStatusId { get; set; }
    public string? SortBy { get; set; }
}
public class BatchListRequest
{
    public int? BatchStatusId { get; set; }
    public string? BatchName { get; set; }
    public DateTime? ToDate { get; set; }
    public DateTime? FromDate { get; set; }
    public int? ScanQueueId { get; set; }
    public int? PageIndex { get; set; }
    public int? PageSize { get; set; }
    public string? OrderBy { get; set; } = "ScanDateTime";
    public string? SortBy { get; set; } = "asc";
    public QueryState? NextToken { get; set; }
}


