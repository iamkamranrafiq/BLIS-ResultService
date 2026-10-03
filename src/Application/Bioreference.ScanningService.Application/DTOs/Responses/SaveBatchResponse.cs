namespace Bioreference.ScanningService.Application.DTOs.Responses;

public class SaveBatchResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public BatchDto? Data { get; set; }
}
