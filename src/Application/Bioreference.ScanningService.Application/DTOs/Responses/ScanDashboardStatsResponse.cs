namespace Bioreference.ScanningService.Application.DTOs.Responses;

public class DocumentStatusCountDto
{
    public string Status { get; set; } = string.Empty;
    public int DocumentCount { get; set; }
    public int PageCount { get; set; }
}

public class ScanDashboardStatsResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;

    /// <summary>Total pages scanned (count of non-deleted DocumentPage rows).</summary>
    public int MonthlyPagesScanned { get; set; }

    /// <summary>Total documents scanned (count of non-deleted Document rows).</summary>
    public int MonthlyDocumentsScanned { get; set; }

 


}
