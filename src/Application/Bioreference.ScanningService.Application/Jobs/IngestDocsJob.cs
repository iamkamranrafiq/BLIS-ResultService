using Bioreference.ScanningService.Application.Common.Interfaces;
using Bioreference.ScanningService.Application.Common.Settings;
using Bioreference.ScanningService.Application.DTOs.Requests;
using Bioreference.ScanningService.Application.Services.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Globalization;

namespace Bioreference.ScanningService.Application.Jobs
{
    /// <summary>
    /// Imports eReq documents from a folder. Each PDF has a corresponding index file
    /// (CSV content with a .txt extension, one row, no header). The index columns are
    /// mapped to the document type, document date and keywords via <see cref="IngestDocsSettings"/>.
    /// On success both the index and PDF files are moved to the PROCESSED folder; on error they
    /// are moved to the ERROR_FILES folder. Path handling is OS-aware via <see cref="IPathConverter"/>.
    /// </summary>
    public class IngestDocsJob : IBLISJob
    {
        private readonly ILogger<IngestDocsJob> logger;
        private readonly IDocumentService documentService;
        private readonly IPathConverter pathConverter;
        private readonly INetworkShareConnector networkShareConnector;
        private readonly IngestDocsSettings settings;

        public IngestDocsJob(
            ILogger<IngestDocsJob> logger,
            IDocumentService documentService,
            IPathConverter pathConverter,
            INetworkShareConnector networkShareConnector,
            IOptions<IngestDocsSettings> settings)
        {
            this.logger = logger;
            this.documentService = documentService;
            this.pathConverter = pathConverter;
            this.networkShareConnector = networkShareConnector;
            this.settings = settings.Value;
        }

        public async Task<bool> Execute()
        {
            var startTime = DateTime.Now;
            int processed = 0;
            int failed = 0;

            logger.LogInformation("Entity: {Entity}; Event: {Event}; Message: {Message}", "Job", "IngestDocs", "IngestDocs job started.");

            var inputFolderSource = GetInputFolderSource();

            if (string.IsNullOrWhiteSpace(inputFolderSource))
            {
                logger.LogError("Entity: {Entity}; Event: {Event}; Message: {Message}", "Job", "IngestDocs", "InputFolder is not configured. Aborting.");
                return false;
            }

            try
            {
                using var _ = networkShareConnector.Connect();

                var inputFolder = ResolvePath(inputFolderSource);

                logger.LogInformation("OnbaseUpload File Path: {FilePath}", inputFolder);

                if (!Directory.Exists(inputFolder))
                {
                    logger.LogError("Entity: {Entity}; Event: {Event}; Message: {Message}; Folder: {Folder}", "Job", "IngestDocs", "Input folder does not exist.", inputFolder);
                    return false;
                }

                var indexFiles = Directory.GetFiles(inputFolder, $"*{settings.IndexFileExtension}", SearchOption.TopDirectoryOnly);

                logger.LogInformation("Entity: {Entity}; Event: {Event}; Message: {Message}; Count: {Count}", "Job", "IngestDocs", "Index files discovered.", indexFiles.Length);

                foreach (var indexFile in indexFiles)
                {
                    var result = await ProcessIndexFileAsync(indexFile);
                    if (result)
                    {
                        processed++;
                    }
                    else
                    {
                        failed++;
                    }
                }

                var elapsed = (DateTime.Now - startTime).TotalMilliseconds;
                logger.LogInformation("Entity: {Entity}; Event: {Event}; Message: {Message}; Processed: {Processed}; Failed: {Failed}; ElapsedTime: {ElapsedTime} ms",
                    "Job", "IngestDocs", "IngestDocs job completed.", processed, failed, elapsed);

                return failed == 0;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Entity: {Entity}; Event: {Event}; Message: {Message}", "Job", "IngestDocs", "IngestDocs job failed.");
                return false;
            }
        }

        private async Task<bool> ProcessIndexFileAsync(string indexFilePath)
        {
            string? pdfFilePath = null;
            try
            {
                var lines = await File.ReadAllLinesAsync(indexFilePath);
                var row = Array.Find(lines, l => !string.IsNullOrWhiteSpace(l));

                if (string.IsNullOrWhiteSpace(row))
                {
                    logger.LogWarning("Entity: {Entity}; Event: {Event}; Message: {Message}; File: {File}", "Job", "IngestDocs", "Index file is empty.", indexFilePath);
                    MovePair(indexFilePath, null, success: false);
                    return false;
                }

                var columns = row.Split(settings.Delimiter);
                var mapping = settings.Mapping;

                // Resolve the DocumentTypeId from the index file's document type column
                // (e.g. "Req - Requisition") instead of using a hardcoded id.
                var documentTypeName = GetColumn(columns, mapping.DocumentTypeColumn);
                var documentTypeId = await documentService.ResolveDocumentTypeIdByNameAsync(documentTypeName);

                if (documentTypeId is null)
                {
                    logger.LogWarning("Entity: {Entity}; Event: {Event}; Message: {Message}; File: {File}; DocumentType: {DocumentType}", "Job", "IngestDocs", "Could not resolve DocumentType from index file.", indexFilePath, documentTypeName);
                    MovePair(indexFilePath, null, success: false);
                    return false;
                }

                pdfFilePath = ResolvePath(GetColumn(columns, mapping.PdfPathColumn));

                if (string.IsNullOrWhiteSpace(pdfFilePath) || !File.Exists(pdfFilePath))
                {
                    logger.LogWarning("Entity: {Entity}; Event: {Event}; Message: {Message}; File: {File}; Pdf: {Pdf}", "Job", "IngestDocs", "PDF file not found for index.", indexFilePath, pdfFilePath);
                    MovePair(indexFilePath, pdfFilePath, success: false);
                    return false;
                }

                var keywords = new List<keywordValue>();
                foreach (var keywordMapping in mapping.Keywords)
                {
                    var value = GetColumn(columns, keywordMapping.Column);
                    keywords.Add(new keywordValue(keywordMapping.KeywordId, value));
                }

                DateTime? documentDate = null;
                if (mapping.DocumentDateColumn >= 0)
                {
                    var dateValue = GetColumn(columns, mapping.DocumentDateColumn);
                    if (DateTime.TryParse(dateValue, CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsedDate))
                    {
                        documentDate = parsedDate;
                    }
                }

                var fileName = Path.GetFileName(pdfFilePath);

                // Read the actual PDF content from the resolved path (Windows UNC share or
                // Linux mounted path), following the same by-path read pattern used elsewhere,
                // and import the bytes through ImportDocumentAsync.
                var fileBytes = await File.ReadAllBytesAsync(pdfFilePath);

                var request = new ImportDocumentRequest
                {
                    DocumentTypeId = documentTypeId.Value,
                    DocumentDate = documentDate,
                    CreatedBy = "ingestdocs",
                    UpdatedBy = "ingestdocs",
                    keywords = keywords
                };

                using var stream = new MemoryStream(fileBytes);
                var fileProperties = new FileData(fileName, "application/pdf", Path.GetExtension(fileName));

                var response = await documentService.ImportDocumentAsync(stream, request, fileProperties);

                if (response.Success)
                {
                    MovePair(indexFilePath, pdfFilePath, success: true);
                    logger.LogInformation("Entity: {Entity}; Event: {Event}; Message: {Message}; DocumentId: {DocumentId}; File: {File}",
                        "Job", "IngestDocs", "Document ingested successfully.", response.DocumentId, fileName);
                    return true;
                }

                logger.LogWarning("Entity: {Entity}; Event: {Event}; Message: {Message}; File: {File}; Reason: {Reason}",
                    "Job", "IngestDocs", "Document ingest failed.", fileName, response.Message);
                MovePair(indexFilePath, pdfFilePath, success: false);
                return false;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Entity: {Entity}; Event: {Event}; Message: {Message}; File: {File}", "Job", "IngestDocs", "Error processing index file.", indexFilePath);
                MovePair(indexFilePath, pdfFilePath, success: false);
                return false;
            }
        }

        private static string GetColumn(string[] columns, int index)
        {
            if (index < 0 || index >= columns.Length)
            {
                return string.Empty;
            }

            return columns[index].Trim();
        }

        private void MovePair(string indexFilePath, string? pdfFilePath, bool success)
        {
            var targetFolder = ResolveMoveFolder(success ? settings.ProcessedFolder : settings.ErrorFolder);

            if (string.IsNullOrWhiteSpace(targetFolder))
            {
                logger.LogWarning("Entity: {Entity}; Event: {Event}; Message: {Message}", "Job", "IngestDocs", "Target folder is not configured; files left in place.");
                return;
            }

            MoveFile(indexFilePath, targetFolder);
            if (!string.IsNullOrWhiteSpace(pdfFilePath))
            {
                MoveFile(pdfFilePath, targetFolder);
            }
        }

        /// <summary>
        /// Returns the configured input folder or, when the root-based scheme is used,
        /// composes it from <see cref="IngestDocsSettings.ScanningUploadRootPath"/> +
        /// <see cref="IngestDocsSettings.ScanningUploadRelativePath"/>.
        /// </summary>
        private string GetInputFolderSource()
        {
            if (!string.IsNullOrWhiteSpace(settings.InputFolder))
            {
                return settings.InputFolder;
            }

            if (!string.IsNullOrWhiteSpace(settings.ScanningUploadRootPath))
            {
                return settings.ScanningUploadRootPath.TrimEnd('\\') + settings.ScanningUploadRelativePath;
            }

            return string.Empty;
        }

        /// <summary>
        /// Resolves a move target folder. When the root-based scheme is used the configured
        /// value is treated as a path relative to the input folder (e.g. \PROCESSED); otherwise
        /// it is used as an absolute path. The result is OS-normalized (Linux mount mapping included).
        /// </summary>
        private string ResolveMoveFolder(string folder)
        {
            if (string.IsNullOrWhiteSpace(folder))
            {
                return string.Empty;
            }

            if (string.IsNullOrWhiteSpace(settings.InputFolder) && !string.IsNullOrWhiteSpace(settings.ScanningUploadRootPath))
            {
                var inputFolderSource = GetInputFolderSource();
                var combined = inputFolderSource.TrimEnd('\\') + "\\" + folder.TrimStart('\\');
                return ResolvePath(combined);
            }

            return ResolvePath(folder);
        }

        /// <summary>
        /// Normalizes a path for the current OS. On Linux, maps the configured Windows network
        /// root (<see cref="IngestDocsSettings.ScanningUploadRootPath"/>) to the mounted path
        /// (<see cref="IngestDocsSettings.BLISScanUploadDocs"/>) before converting separators;
        /// on Windows it simply normalizes separators / preserves the UNC path.
        /// </summary>
        private string ResolvePath(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                return string.Empty;
            }

            if (!string.IsNullOrWhiteSpace(settings.ScanningUploadRootPath) &&
                !string.IsNullOrWhiteSpace(settings.BLISScanUploadDocs) &&
                path.StartsWith(settings.ScanningUploadRootPath, StringComparison.OrdinalIgnoreCase))
            {
                return pathConverter.ConvertOnbasePath(path, settings.ScanningUploadRootPath, settings.BLISScanUploadDocs);
            }

            return pathConverter.ConvertPath(path);
        }

        private void MoveFile(string sourceFilePath, string targetFolder)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(sourceFilePath) || !File.Exists(sourceFilePath))
                {
                    return;
                }

                Directory.CreateDirectory(targetFolder);

                var destination = pathConverter.CombinePath(targetFolder, Path.GetFileName(sourceFilePath));

                if (File.Exists(destination))
                {
                    File.Delete(destination);
                }

                File.Move(sourceFilePath, destination);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Entity: {Entity}; Event: {Event}; Message: {Message}; Source: {Source}; Target: {Target}",
                    "Job", "IngestDocs", "Failed to move file.", sourceFilePath, targetFolder);
            }
        }
    }
}
