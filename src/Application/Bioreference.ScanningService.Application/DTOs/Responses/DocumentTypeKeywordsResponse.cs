namespace Bioreference.ScanningService.Application.DTOs.Responses;

public class DocumentTypeKeywordsResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public List<DocumentTypeWithKeywordsDto> Data { get; set; } = new();
}

public class DocumentTypeWithKeywordsDto
{
    public int DocumentTypeId { get; set; }
    public string Code { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public List<DocumentTypeKeywordItemDto> Keywords { get; set; } = new();
}

public class DocumentTypeKeywordItemDto
{
    public int KeywordId { get; set; }
    public string KeywordName { get; set; } = string.Empty;
    public string DataType { get; set; } = string.Empty;
    public string ControlType { get; set; } = string.Empty;
    public bool IsRequired { get; set; }
    public int DisplayOrder { get; set; }
    public int? TableType { get; set; }
}
