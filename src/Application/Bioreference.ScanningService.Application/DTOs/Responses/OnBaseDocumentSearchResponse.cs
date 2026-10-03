using System;
using System.Collections.Generic;
using System.Text;

namespace Bioreference.ScanningService.Application.DTOs.Responses;


public class OnBaseDocumentSearchResponse : ApiResponse
{
    public List<OnBaseDocumentSearchResult> documentSearchResults { get; set; }
    public PagedResponse? paginations { get; set; }

}

public class OnBaseDocumentSearchResult 
{
    //public int TotalRecords { get; set; }
    public string? DocumentName { get; set; } = string.Empty;
    public int? BatchNumber { get; set; }
    public string? BatchName { get; set; } = string.Empty;
    public string? DocumentType { get; set; }
    public DateTime? DocumentDate { get; set; }
    public int? itemnum { get; set; }
    public int? itemtypenum { get; set; }
    public DateTime? datestored { get; set; }
  
}
