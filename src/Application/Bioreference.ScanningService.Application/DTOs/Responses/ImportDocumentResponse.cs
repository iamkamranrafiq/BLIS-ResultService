
using System;
using System.Collections.Generic;
using System.Text;

namespace Bioreference.ScanningService.Application.DTOs.Responses;

public class ImportDocumentResponse
{
    public bool Success { get; set; } = true;
    public string Message { get; set; } = string.Empty;
    public long? DocumentId { get; set; }
    public string Name { get; set; } = string.Empty;
}
