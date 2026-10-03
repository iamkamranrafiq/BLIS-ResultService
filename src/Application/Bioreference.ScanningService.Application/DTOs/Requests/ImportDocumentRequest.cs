using Bioreference.ScanningService.Application.DTOs.Responses;
using Bioreference.ScanningService.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Text;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace Bioreference.ScanningService.Application.DTOs.Requests;

public class ImportDocumentRequest
{
    public DateTime DatePosted { get; set; }

    //Documnet Metadata 
    public int DocumentTypeId { get; set; }
    public DateTime? DocumentDate { get; set; }
    //Audit
    public string? CreatedBy { get; set; }
    public string? UpdatedBy { get; set; }
    //KeyWords
    public ICollection<keywordValue>? keywords { get; set; } = new List<keywordValue>();
}

public record FileData(string FileName, string ContentType, string Extension);

public record keywordValue(int KeywordId, string Value);

/// <summary>
/// Request to create a document entry that references an already-existing file on disk/network
/// (by its path) rather than uploading file bytes. Used by folder-based ingestion (IngestDocs).
/// </summary>
public class IngestDocumentRequest
{
    public int DocumentTypeId { get; set; }
    public DateTime? DocumentDate { get; set; }

    /// <summary>
    /// Full path to the existing document (e.g. the PDF referenced by the index file).
    /// </summary>
    public string FilePath { get; set; } = string.Empty;

    /// <summary>
    /// Display name for the document. Defaults to the file name when not provided.
    /// </summary>
    public string? Name { get; set; }

    public string? CreatedBy { get; set; }
    public string? UpdatedBy { get; set; }

    public ICollection<keywordValue>? keywords { get; set; } = new List<keywordValue>();
}
