namespace Bioreference.ScanningService.Application.DTOs.Requests;

public class UpdateBatchStatusRequest
{
    public int statusId { get; set; }
    public string? UpdatedBy { get; set; }
}
