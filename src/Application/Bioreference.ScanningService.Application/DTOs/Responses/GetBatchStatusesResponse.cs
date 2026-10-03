namespace Bioreference.ScanningService.Application.DTOs.Responses;

public class GetBatchStatusesResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public List<BatchStatusDto> Data { get; set; } = new();
}
