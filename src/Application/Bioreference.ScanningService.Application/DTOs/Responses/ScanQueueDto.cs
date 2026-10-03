namespace Bioreference.ScanningService.Application.DTOs.Responses;

public class ScanQueueDto
{
    public int ScanQueueId { get; set; }
    public string Name { get; set; } = string.Empty;
    public bool IsAutoCommit { get; set; }
    public DateTime? LastUsedDate { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedDate { get; set; }
    public DateTime UpdatedDate { get; set; }
}
