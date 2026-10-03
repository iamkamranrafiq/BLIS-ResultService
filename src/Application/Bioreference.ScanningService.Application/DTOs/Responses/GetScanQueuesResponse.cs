namespace Bioreference.ScanningService.Application.DTOs.Responses;

public class GetScanQueuesResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public List<ScanQueueDto> Data { get; set; } = new();
}
