using System;
using System.Collections.Generic;
using System.Text;

namespace Bioreference.ScanningService.Application.DTOs.Responses;

public class OnBaseBatchDocumentListResponse : ApiResponse
{
    public List<OnBaseBatchDocumentResult> data { get; set; }
     
}

public class OnBaseBatchDocumentResult
{
    public string DocumentName { get; set; }
    public int BatchNumber { get; set; }
    public string BatchName { get; set; }
    public string DocumentType { get; set; }
    public DateTime DatePosted { get; set; }
    public int ItemNumber { get; set; }
    public int ItemTypeNumber { get; set; }
    public DateTime StoreDateTime { get; set; }
}

