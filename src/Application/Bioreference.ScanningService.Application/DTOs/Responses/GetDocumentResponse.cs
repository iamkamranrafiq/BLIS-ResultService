namespace Bioreference.ScanningService.Application.DTOs.Responses;

public class GetDocumentResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public DocumentDetailDto? Data { get; set; }
}

public class DocumentDetailDto
{
    public long Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string ShortName { get; set; } = string.Empty;
    public string? BarcodeValue { get; set; }
    public string? SpecimenNumber { get; set; }
    public string? IndexingValue { get; set; }
    public string? DocUrl { get; set; }
    public string? ThumbnailUrl { get; set; }
    public DateTime? DocumentDate { get; set; }
    public DateTime DatePosted { get; set; }
    public string Status { get; set; } = string.Empty;
    public bool IsIndexed { get; set; }
    public int TotalPages { get; set; }
    public bool IsActive { get; set; }

    public DocumentTypeInfoDto DocumentType { get; set; } = new();
    public DocumentGroupInfoDto DocumentGroup { get; set; } = new();
    public BatchInfoDto? Batch { get; set; }
    public List<KeywordValueDto> Keywords { get; set; } = new();

    public DateTime CreatedDate { get; set; }
    public string CreatedBy { get; set; } = string.Empty;
    public DateTime UpdatedDate { get; set; }
    public string UpdatedBy { get; set; } = string.Empty;
}

public class DocumentTypeInfoDto
{
    public int Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
}

public class DocumentGroupInfoDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
}

public class BatchInfoDto
{
    public int Id { get; set; }
    public string BatchNumber { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
}

public class KeywordValueDto
{
    public int DocumentKeywordValueId { get; set; }
    public int KeywordId { get; set; }
    public string KeywordName { get; set; } = string.Empty;
    public string? AlphanumericValue { get; set; }
    public DateTime? DateTimeValue { get; set; }
    public decimal? DecimalValue { get; set; }
    public long? LongValue { get; set; }
    public string? TextValue { get; set; }
    public string DataType { get; set; } = string.Empty;
    public string ControlType { get; set; } = string.Empty;
    public bool IsRequired { get; set; }
}
