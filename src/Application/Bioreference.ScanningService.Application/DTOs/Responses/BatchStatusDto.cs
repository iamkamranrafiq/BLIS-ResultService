namespace Bioreference.ScanningService.Application.DTOs.Responses;

public class BatchStatusDto
{
    public int BatchStatusId { get; set; }
    public string Name { get; set; } = string.Empty;
    public bool IsActive { get; set; }
}
