namespace Bioreference.ScanningService.Application.DTOs.Responses;

public class GetDocumentShortNamesByBatchIdResponse
{
    public bool Success { get; set; } = true;
    public string Message { get; set; } = string.Empty;
    public List<DocumentShortNameDto> Documents { get; set; } = new();
    public int TotalDocuments { get; set; }
    public int BatchId { get; set; }
}

public class DocumentShortNameDto
{
    public long DocumentId { get; set; }
    public string ShortName { get; set; } = string.Empty;
}
