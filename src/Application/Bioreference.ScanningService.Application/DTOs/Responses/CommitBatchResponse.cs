namespace Bioreference.ScanningService.Application.DTOs.Responses;

public class CommitBatchResponse
{
    public bool Success { get; set; } = true;
    public string Message { get; set; } = string.Empty;
    public CommitBatchResultDto? Data { get; set; }
}

public class CommitBatchResultDto
{
    public int BatchId { get; set; }
    public string Status { get; set; } = string.Empty;
    public int BatchStatusId { get; set; }
    public int UpdatedDocuments { get; set; }
    public DateTime CommittedDate { get; set; }
    public string CommittedBy { get; set; } = string.Empty;
}
