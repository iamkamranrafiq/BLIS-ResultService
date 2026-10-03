namespace Bioreference.ScanningService.Application.DTOs.Responses;

public class DeleteDocumentsResponse
{
    public bool Success { get; set; } = true;
    public string Message { get; set; } = string.Empty;
    public DeleteDocumentsResultDto? Data { get; set; }
}

public class DeleteDocumentsResultDto
{
    public int TotalRequested { get; set; }
    public int SuccessfullyDeleted { get; set; }
    public int Failed { get; set; }
    public List<long> DeletedDocumentIds { get; set; } = new();
    public List<long> FailedDocumentIds { get; set; } = new();
    public DateTime DeletedDate { get; set; }
    public string DeletedBy { get; set; } = string.Empty;
}
