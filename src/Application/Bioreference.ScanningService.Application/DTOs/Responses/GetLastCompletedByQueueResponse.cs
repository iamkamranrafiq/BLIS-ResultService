namespace Bioreference.ScanningService.Application.DTOs.Responses;

public class ScanQueueLastCompletedDto
{
    public int ScanQueueId { get; set; }

    /// <summary>Most recent CompletedDate for the queue's batches, or "-" when none.</summary>
    public string LastCompletedDate { get; set; } = "-";
}

public class GetLastCompletedByQueueResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public List<ScanQueueLastCompletedDto> Data { get; set; } = new();
}
