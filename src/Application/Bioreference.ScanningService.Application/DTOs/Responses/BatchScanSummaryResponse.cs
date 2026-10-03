namespace Bioreference.ScanningService.Application.DTOs.Responses;

public class BatchScanSummaryResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public BatchScanSummaryDto? Data { get; set; }
}

public class BatchScanSummaryDto
{
    public int BatchId { get; set; }
    public string BatchNumber { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public DateTime? StartedDate { get; set; }
    public DateTime? CompletedDate { get; set; }

    /// <summary>Count of non-deleted documents in the batch.</summary>
    public int TotalDocuments { get; set; }

    /// <summary>Count of non-deleted document pages in the batch.</summary>
    public int TotalPages { get; set; }

    /// <summary>Scanning velocity in seconds per page (based on StartedDate/CompletedDate span).</summary>
    public double? ScanVelocitySecondsPerPage { get; set; }

    /// <summary>Estimated processing/indexing time in minutes (batch duration in minutes).</summary>
    public double? EstimatedProcessingMinutes { get; set; }

    /// <summary>Sum of on-disk sizes (in bytes) of all page files that could be read.</summary>
    public long TotalSizeBytes { get; set; }

    /// <summary>Human-readable version of <see cref="TotalSizeBytes"/>, e.g. "124.5 MB".</summary>
    public string TotalSizeDisplay { get; set; } = "0 B";

    /// <summary>Number of documents whose Status indicates an error/failure.</summary>
    public int ErrorCount { get; set; }
}
