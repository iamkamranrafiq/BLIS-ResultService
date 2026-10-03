using System;
using System.Collections.Generic;
using System.Text;

namespace Bioreference.ScanningService.Application.DTOs.Responses;

public class UpdateDocumentResponse
{
    public bool Success { get; set; } = true;
    public string Message { get; set; } = string.Empty;
    public DocumentUpdateDto? Data { get; set; }
}

public class DocumentUpdateDto
{
    public long DocumentId { get; set; }
    public DateTime UpdatedDate { get; set; }
    public string UpdatedBy { get; set; } = string.Empty;
}