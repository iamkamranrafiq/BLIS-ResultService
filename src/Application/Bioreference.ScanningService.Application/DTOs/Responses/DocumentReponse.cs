using Bioreference.ScanningService.Application.Common.Enums;
using Bioreference.ScanningService.Application.DTOs.Requests;
using System;
using System.Collections.Generic;
using System.Text;

namespace Bioreference.ScanningService.Application.DTOs.Responses
{
    internal class DocumentReponse
    {
    }
    
    public class DocumentSearchResponse : ApiResponse
    {
        public List<DocumentSearchResult> documentSearchResults { get; set; }
        public PagedResponse? paginations { get; set; }
        public QueryState? NextToken { get; set; }

    }
    public class DocumentSearchResult 
    {

        public long DocumentId { get; set; }
        public string? DocumentName { get; set; }
        public string? DocumentType { get; set; }
        public string? ThumbnailUrl { get; set; }
        public string? BatchNumber { get; set; }
        public DateTime? DocumentDate { get; set; }
        public DateTime? DatePosted { get; set; }
        public string? Status { get; set; }
        public DataSource DataSource { get; set; }

        public static DocumentSearchResult From(OnBaseDocumentSearchResult result)
        {
            return new DocumentSearchResult
            {
                DocumentId = result.itemnum ?? 0,
                DocumentName = result.DocumentName,
                DocumentType = result.DocumentType,
                BatchNumber = result.BatchNumber?.ToString(),
                DocumentDate = result.DocumentDate,
                DatePosted = result.datestored,
                Status = null,
                DataSource = DataSource.OnBase,
            };
        }
    }
}
