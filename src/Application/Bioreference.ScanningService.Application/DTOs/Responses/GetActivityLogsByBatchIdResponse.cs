namespace Bioreference.ScanningService.Application.DTOs.Responses;

public class GetActivityLogsByBatchIdResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public List<ActivityLogDto> Data { get; set; } = new();
}

public class ActivityLogDto
{
    public long ActivityLogId { get; set; }
    public int BatchId { get; set; }
    public string UserId { get; set; } = string.Empty;
    public long? DocumentId { get; set; }
    public int DocumentsScanned { get; set; }
    public int PagesScanned { get; set; }
    public string? LogLevel { get; set; }
    public int? QueueId { get; set; }
    public string? Description { get; set; }
    public DateTime LoggedDate { get; set; }
    public DateTime CreatedDate { get; set; }
}
