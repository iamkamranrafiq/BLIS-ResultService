namespace Bioreference.ScanningService.Application.DTOs.Responses;

public class DeleteBatchesResponse
{
    public bool Success { get; set; } = true;
    public string Message { get; set; } = string.Empty;
    public DeleteBatchesResultDto? Data { get; set; }
}

public class DeleteBatchesResultDto
{
    public int TotalRequested { get; set; }
    public int SuccessfullyDeleted { get; set; }
    public int Failed { get; set; }
    public List<int> DeletedBatchIds { get; set; } = new();
    public List<int> FailedBatchIds { get; set; } = new();
    public DateTime? DeletedDate { get; set; }
    public string DeletedBy { get; set; } = string.Empty;
}
