namespace Bioreference.ScanningService.Application.DTOs.Requests;

public class GetScanProductivityReportRequest
{
    public DateTime? StartDateTime { get; set; }
    public DateTime? EndDateTime { get; set; }
    public TimeSpan? StartTime { get; set; }
    public TimeSpan? EndTime { get; set; }
    public IEnumerable<string>? UserNames { get; set; }
}
