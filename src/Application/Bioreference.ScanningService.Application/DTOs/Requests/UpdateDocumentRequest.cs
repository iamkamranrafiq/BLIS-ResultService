using System;
using System.Collections.Generic;
using System.Text;

namespace Bioreference.ScanningService.Application.DTOs.Requests;

public class UpdateDocumentRequest
{
    public int? DocumentTypeId { get; set; }
    public DateTime? DocumentDate { get; set; }
    public DateTime? DatePosted { get; set; }
    public string? UpdatedBy { get; set; }
    //KeyWords
    public ICollection<Documentkeyword>? keywords { get; set; } = new List<Documentkeyword>();
    // DocumentKeywordValueIds the client removed in the UI; deleted on update.
    public ICollection<int>? RemovedKeywordValueIds { get; set; } = new List<int>();
}


public record Documentkeyword(int DocumentKeywordValueId, int KeywordId, string Value);
public record DocumentNames(string FullName, string ShortName);