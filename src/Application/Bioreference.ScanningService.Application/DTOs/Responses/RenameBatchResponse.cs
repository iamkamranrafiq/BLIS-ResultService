namespace Bioreference.ScanningService.Application.DTOs.Responses;

public class RenameBatchResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public BatchDto? Data { get; set; }
}
