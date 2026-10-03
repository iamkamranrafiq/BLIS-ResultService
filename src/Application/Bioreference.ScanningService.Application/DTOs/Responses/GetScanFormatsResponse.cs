namespace Bioreference.ScanningService.Application.DTOs.Responses;

public class GetScanFormatsResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public List<ScanFormatDto> Data { get; set; } = new();
}
