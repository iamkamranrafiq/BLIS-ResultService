namespace Bioreference.ScanningService.Application.DTOs.Responses;

public class RenameDocumentResponse
{
    public bool Success { get; set; } = true;
    public string Message { get; set; } = string.Empty;
    public DocumentRenameDto? Data { get; set; }
}

public class DocumentRenameDto
{
    public long DocumentId { get; set; }
    public string Name { get; set; } = string.Empty;
    public DateTime UpdatedDate { get; set; }
    public string UpdatedBy { get; set; } = string.Empty;
}
