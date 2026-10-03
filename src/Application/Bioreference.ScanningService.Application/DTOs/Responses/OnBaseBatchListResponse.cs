using System;
using System.Collections.Generic;
using System.Text;

namespace Bioreference.ScanningService.Application.DTOs.Responses;

public class OnBaseBatchListResponse : ApiResponse
{
    public List<BatchSPResult>? BatchSearchResults { get; set; }
    public PagedResponse? paginations { get; set; }

}

public class BatchSPResult
{
    //public int? BatchId { get; set; }
    public int batchnum { get; set; }
    public string BatchName { get; set; } = string.Empty;
    public string ScanQueue { get; set; } = string.Empty;
    public string BatchStatus { get; set; }
    public int StatusId { get; set; }
    public DateTime? ScanDateTime { get; set; }
    public int? totaldocuments { get; set; }
    //public int TotalRecords { get; set; }

}
