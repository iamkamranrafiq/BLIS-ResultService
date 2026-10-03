using Bioreference.ScanningService.Application.Common.Enums;

namespace Bioreference.ScanningService.Application.DTOs.Responses;

public class GetDocumentsByBatchIdResponse
{
    public bool Success { get; set; } = true;
    public string Message { get; set; } = string.Empty;
    public List<BatchDocumentDto> Documents { get; set; } = new();
    public int TotalDocuments { get; set; }
    public int BatchId { get; set; }
    public string ScanMode { get; set; } = string.Empty;
}

public class BatchDocumentDto
{
    public long DocumentId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string ShortName { get; set; } = string.Empty;
    public int DocumentTypeId { get; set; }
    public string DocumentTypeName { get; set; } = string.Empty;
    public string BarcodeValue { get; set; } = string.Empty;
    public string IndexingValue { get; set; } = string.Empty;
    public string ThumbnailUrl { get; set; } = string.Empty;
    public string DocUrl { get; set; } = string.Empty;
    public DateTime? DocumentDate { get; set; }
    public string SpecimenNumber { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public bool IsIndexed { get; set; }
    public int TotalPages { get; set; }
    public DateTime CreatedDate { get; set; }
    public List<DocumentPageDto> Pages { get; set; } = new();
    public DataSource DataSource { get; set; }
}

public class DocumentPageDto
{
    public long DocumentPageId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string PageURL { get; set; } = string.Empty;
    public DateTime CreatedDate { get; set; }
}
