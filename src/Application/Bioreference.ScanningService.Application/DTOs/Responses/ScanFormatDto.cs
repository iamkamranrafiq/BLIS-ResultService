namespace Bioreference.ScanningService.Application.DTOs.Responses;

public class ScanFormatDto
{
    public int ScanningFormatId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public DateTime CreatedDate { get; set; }
    public DateTime UpdatedDate { get; set; }
}
