namespace Bioreference.ScanningService.Application.DTOs.Responses;

public class DocumentTypeListResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public List<DocumentTypeItemDto> DocumentTypes { get; set; } = new();
    public List<DocumentGroupInfoDto> DocumentTypeGroups { get; set; } = new();

}

public class DocumentTypeItemDto
{
    public int DocumentTypeId { get; set; }
    public string Code { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public int DocumentTypeGroupID { get; set; } = new();
    public int? DocumentTypeQueueId { get; set; }
}
