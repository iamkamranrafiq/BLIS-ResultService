namespace Bioreference.ScanningService.Application.DTOs.Requests;

public class GetLastCompletedByQueueRequest
{
    public List<int> ScanQueueIds { get; set; } = new();
}
