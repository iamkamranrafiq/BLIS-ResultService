namespace Bioreference.ScanningService.Application.Common.Settings;

/// <summary>
/// Configuration for the IngestDocs background job. Defines the folders to read eReq
/// documents from and where to move files after processing, along with the mapping from
/// index-file columns to document properties and keywords.
/// </summary>
public class IngestDocsSettings
{
    public const string SectionName = "IngestDocs";

    /// <summary>
    /// Network root path (Windows UNC) where scanned upload documents live.
    /// e.g. \\nj1intranetp02\OnbaseUpload. On Linux this root is mapped to
    /// <see cref="BLISScanUploadDocs"/> (the mounted path).
    /// </summary>
    public string ScanningUploadRootPath { get; set; } = string.Empty;

    /// <summary>
    /// Path (relative to <see cref="ScanningUploadRootPath"/>) that BLIS SM reads index (.txt)
    /// and PDF files from, e.g. \IDxDocuments (or \IDxDocumentsQA for QA).
    /// </summary>
    public string ScanningUploadRelativePath { get; set; } = string.Empty;

    /// <summary>
    /// Linux mount point that maps to <see cref="ScanningUploadRootPath"/>, e.g. /blis_scan_upload_docs.
    /// Used to translate Windows UNC paths (from config and the index file) to the mounted
    /// Linux path when the job runs on Linux.
    /// </summary>
    public string BLISScanUploadDocs { get; set; } = string.Empty;

    /// <summary>
    /// Absolute folder that BLIS SM reads index (.txt) and PDF files from. When left empty the
    /// input folder is composed from <see cref="ScanningUploadRootPath"/> + <see cref="ScanningUploadRelativePath"/>.
    /// e.g. \\nj1intranetp02\OnbaseUpload\IDxDocuments (or ...IDxDocumentsQA for QA).
    /// </summary>
    public string InputFolder { get; set; } = string.Empty;

    /// <summary>
    /// Folder where both index and PDF files are moved after a successful import. When
    /// <see cref="ScanningUploadRootPath"/> is configured this is treated as a path relative to the
    /// input folder (e.g. \PROCESSED); otherwise it is used as an absolute path.
    /// </summary>
    public string ProcessedFolder { get; set; } = string.Empty;

    /// <summary>
    /// Folder where both index and PDF files are moved when processing fails. When
    /// <see cref="ScanningUploadRootPath"/> is configured this is treated as a path relative to the
    /// input folder (e.g. \ERROR_FILES); otherwise it is used as an absolute path.
    /// </summary>
    public string ErrorFolder { get; set; } = string.Empty;

    /// <summary>
    /// Extension of the index files to process (CSV content with a .txt extension).
    /// </summary>
    public string IndexFileExtension { get; set; } = ".txt";

    /// <summary>
    /// Delimiter used inside the index file. Defaults to a comma.
    /// </summary>
    public string Delimiter { get; set; } = ",";

    /// <summary>
    /// Mapping of index-file columns to document properties and keywords.
    /// </summary>
    public IngestDocsMapping Mapping { get; set; } = new();
}

/// <summary>
/// Defines how the columns of an index file row map to a document and its keywords.
/// Column indexes are zero-based.
/// </summary>
public class IngestDocsMapping
{
    /// <summary>
    /// Zero-based column index that contains the document type name (e.g. "Req - Requisition").
    /// </summary>
    public int DocumentTypeColumn { get; set; } = 0;

    /// <summary>
    /// Zero-based column index that contains the full path to the corresponding PDF file.
    /// </summary>
    public int PdfPathColumn { get; set; } = 4;

    /// <summary>
    /// Zero-based column index used to set the document date. Set to -1 to skip.
    /// </summary>
    public int DocumentDateColumn { get; set; } = 2;

    /// <summary>
    /// DocumentTypeId to import the document under. Provide the real DB id; a placeholder can be
    /// used during setup and updated later.
    /// </summary>
    public int DocumentTypeId { get; set; }

    /// <summary>
    /// Keyword mappings resolved by index-file column.
    /// </summary>
    public List<IngestKeywordMapping> Keywords { get; set; } = new();
}

/// <summary>
/// Maps a single index-file column to a keyword.
/// </summary>
public class IngestKeywordMapping
{
    /// <summary>
    /// Friendly name for the keyword (for logging/reference), e.g. "Ascession I.D.".
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Zero-based column index in the index file that holds this keyword's value.
    /// </summary>
    public int Column { get; set; }

    /// <summary>
    /// KeywordId to persist against the document. Provide the real DB id; a placeholder can be
    /// used during setup and updated later.
    /// </summary>
    public int KeywordId { get; set; }
}
