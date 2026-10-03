using Bioreference.ScanningService.Application.DTOs.Responses;

namespace Bioreference.ScanningService.Application.DTOs.Requests;


public class StartScanRequest
{
    public int DocumentTypeId { get; set; } = 1;
    public int? BatchId { get; set; }
    public List<keywordValue>? KeywordValue { get; set; } = new List<keywordValue>();
    public bool SinglePageDocument { get; set; } =false;
}

