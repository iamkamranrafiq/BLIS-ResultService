using Bioreference.ScanningService.Application.Common.Enums;

namespace Bioreference.ScanningService.Application.Common.Interfaces;

public interface IFileStorage
{
    /// <summary>
    /// Returns the configured base network path for all scanned files.
    /// </summary>
    string GetBasePath();

    /// <summary>
    /// Returns the relative folder path for a given batch and page in the format YYYY/MM/DD/BatchId/PageNumber.
    /// </summary>
    string GetFolderPath(string mainfolder);

    /// <summary>
    /// Saves the raw image bytes to the network storage path derived from <paramref name="batchId"/>
    /// and <paramref name="pageNumber"/>, then returns the full file path that was written.
    /// </summary>
    Task<string> SaveAsync(byte[] imageBytes, string mainfolder, int pageNumber, CancellationToken cancellationToken = default);

    /// <summary>
    /// Generates a thumbnail from the provided image bytes and saves it to network storage.
    /// Returns the full file path of the saved thumbnail.
    /// </summary>
    /// <param name="imageBytes">The original image bytes to generate thumbnail from</param>
    /// <param name="batchId">The batch ID for organizing storage</param>
    /// <param name="documentId">The document ID for naming the thumbnail</param>
    /// <param name="maxWidth">Maximum width of the thumbnail (default: 200)</param>
    /// <param name="maxHeight">Maximum height of the thumbnail (default: 200)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The full file path where the thumbnail was saved</returns>
    Task<string> SaveThumbnailAsync(byte[] imageBytes, string mainfolder, int maxWidth = 200, int maxHeight = 200, CancellationToken cancellationToken = default);

    /// <summary>
    /// Saves the raw image bytes to the network storage path derived from <paramref name="batchId"/>
    /// and <paramref name="pageNumber"/>, then returns the full file path that was written.
    /// </summary>
    Task<string> SavePdfAsync(byte[] PdfBytes, string mainfolder, string fileName, CancellationToken cancellationToken = default);
    Task<string> SavePdfThumbnailAsync(byte[] pdfBytes, string mainfolder, CancellationToken cancellationToken = default);

    /// <summary>
    /// Reads a file from the network storage given its full file path.
    /// Returns the file bytes if found, otherwise returns null.
    /// </summary>
    Task<byte[]?> ReadAsync(string filePath, DataSource dataSource);

    /// <summary>
    /// Encodes the provided raw image bytes to a compressed JPEG suitable for
    /// real-time preview over the wire (e.g. SignalR). Resizes to fit within
    /// <paramref name="maxDimension"/> to keep the payload small.
    /// </summary>
    byte[] CompressForPreview(byte[] imageBytes, int maxDimension = 1200, int quality = 70);
    Task<string> MoveToDeletedFolderAsync(string originalPath);

}
