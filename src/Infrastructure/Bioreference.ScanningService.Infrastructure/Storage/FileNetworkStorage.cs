using Bioreference.ScanningService.Application.Common.Enums;
using Bioreference.ScanningService.Application.Common.Interfaces;
using System.Buffers.Binary;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PDFtoImage;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Tiff;
using SixLabors.ImageSharp.Formats.Tiff.Constants;
using SkiaSharp;

namespace Bioreference.ScanningService.Infrastructure.Storage;

public class FileNetworkStorage : IFileStorage
{
    private readonly string _basePath;
    private readonly string _deletedDocumentFolder;
    private readonly string _onbaseBasePath;
    private readonly string _onbaseRootPath;

    private readonly bool _useLocalStorage;
    private readonly ILogger<FileNetworkStorage> _logger;
    private readonly INetworkShareConnector _shareConnector;
    private readonly IPathConverter _pathConverter;

    public FileNetworkStorage(
        IOptions<StorageSettings> settings,
        IOptions<LocalStorageSettings> localSettings,
        INetworkShareConnector shareConnector,
        IPathConverter pathConverter,
        ILogger<FileNetworkStorage> logger)
    {
        _shareConnector = shareConnector;
        _logger = logger;
        _pathConverter = pathConverter;
        var local = localSettings.Value;
        var setting = settings.Value;

        if (local.Test)
        {
            // Test mode: use the local storage path and bypass network share authentication.
            _useLocalStorage = true;
            _basePath = string.IsNullOrWhiteSpace(local.BasePath)
                ? throw new InvalidOperationException("LocalStorageSettings:BasePath is required when LocalStorageSettings:Test is true.")
                : local.BasePath;



            _logger.LogInformation("Local storage test mode enabled. Using BasePath {BasePath}.", _basePath);
        }
        else
        {
            _basePath = string.IsNullOrWhiteSpace(setting.BasePath)
                ? ""
                : setting.BasePath;

            _deletedDocumentFolder = string.IsNullOrWhiteSpace(setting.DeletedDocumentFolder)
                ? ""
                : setting.DeletedDocumentFolder;

        }
        _onbaseBasePath = string.IsNullOrWhiteSpace(setting.OnbaseBasePath)
                ? throw new InvalidOperationException("StorageSettings:OnbaseBasePath is required when LocalStorageSettings:Test is true.")
                : setting.OnbaseBasePath;

        _onbaseRootPath = string.IsNullOrWhiteSpace(setting.OnbaseRootPath)
                ? throw new InvalidOperationException("StorageSettings:OnbaseRootPath is required when LocalStorageSettings:Test is true.")
                : setting.OnbaseRootPath;
    }

    /// <summary>
    /// Opens an authenticated network share connection unless running in local test mode.
    /// </summary>
    private IDisposable ConnectShare()
        => _useLocalStorage ? NoOpDisposable.Instance : _shareConnector.Connect();

    private sealed class NoOpDisposable : IDisposable
    {
        public static readonly NoOpDisposable Instance = new();
        public void Dispose() { }
    }

    public string GetBasePath() => _basePath;

    public string GetFolderPath(string mainfolder)
    {
        var now = DateTime.UtcNow;
        return Path.Combine(
            now.Year.ToString("D4"),
            now.Month.ToString("D2"),
            now.Day.ToString("D2"),
            mainfolder.ToString()
            );
    }

    public async Task<string> SaveAsync(byte[] imageBytes, string mainfolder, int pageNumber, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Saving page {Page} for batch {BatchId} to network storage.", pageNumber, mainfolder);
        using var _ = ConnectShare();
        _logger.LogInformation("Connected to network share for batch {BatchId}.", mainfolder);

        var relativeFolder = Path.Combine(GetFolderPath(mainfolder));
        var absoluteFolder = Path.Combine(_basePath, relativeFolder);
        var cloneimagebyte = (byte[])imageBytes.Clone();
        Directory.CreateDirectory(absoluteFolder);

        var fileName = $"page_{pageNumber:D2}.tif";
        var relativePath = Path.Combine(relativeFolder, fileName);
        var fullPath = Path.Combine(_basePath, relativePath);
        var convertedPath = _pathConverter.ConvertPath(fullPath);

        using var sourceStream = new MemoryStream(imageBytes, writable: false);
        using var image = await Image.LoadAsync(sourceStream, cancellationToken);
        await using var tiffStream = new FileStream(
            convertedPath, FileMode.Create, FileAccess.Write, FileShare.None,
            bufferSize: 81920, useAsync: true);
        var isOneBitBitmap = imageBytes.Length >= 30 &&
            imageBytes[0] == (byte)'B' && imageBytes[1] == (byte)'M' &&
            BinaryPrimitives.ReadUInt16LittleEndian(imageBytes.AsSpan(28, sizeof(ushort))) == 1;
        var encoder = isOneBitBitmap
            ? new TiffEncoder
            {
                BitsPerPixel = TiffBitsPerPixel.Bit1,
                Compression = TiffCompression.CcittGroup4Fax,
                PhotometricInterpretation = TiffPhotometricInterpretation.BlackIsZero
            }
            : new TiffEncoder
            {
                Compression = TiffCompression.Lzw
            };
        await image.SaveAsync(tiffStream, encoder, cancellationToken);
        //await File.WriteAllBytesAsync(convertedPath + ".Bmp", cloneimagebyte, cancellationToken);

        _logger.LogInformation("Saved page {Page} for batch {BatchId} to {Path}", pageNumber, mainfolder, fullPath);

        return relativePath;
    }

    public async Task<string> SaveThumbnailAsync(byte[] imageBytes, string mainfolder, int maxWidth = 200, int maxHeight = 200, CancellationToken cancellationToken = default)
    {
        try
        {
            byte[] thumbnailBytes;

            using (var originalBitmap = SKBitmap.Decode(imageBytes))
            {
                if (originalBitmap == null)
                {
                    throw new InvalidOperationException("Failed to decode image bytes");
                }

                // Calculate the resize dimensions maintaining aspect ratio
                var ratioX = (double)maxWidth / originalBitmap.Width;
                var ratioY = (double)maxHeight / originalBitmap.Height;
                var ratio = Math.Min(ratioX, ratioY);

                var newWidth = (int)(originalBitmap.Width * ratio);
                var newHeight = (int)(originalBitmap.Height * ratio);

                // Create resized bitmap with sampling options
                var imageInfo = new SKImageInfo(newWidth, newHeight);
                var resizedBitmap = originalBitmap.Resize(imageInfo, SKSamplingOptions.Default);

                if (resizedBitmap == null)
                {
                    throw new InvalidOperationException("Failed to resize image");
                }

                // Encode to JPEG
                using var image = SKImage.FromBitmap(resizedBitmap);
                using var data = image.Encode(SKEncodedImageFormat.Jpeg, 85);
                thumbnailBytes = data.ToArray();

                resizedBitmap.Dispose();
            }

            // Create thumbnail folder structure: YYYY/MM/DD/BatchId/thumbnails/
            var now = DateTime.UtcNow;
            var relativeFolder = GetFolderPath(mainfolder);

            using var _ = ConnectShare();

            var fileName = $"thumb_doc.jpg";
            var relativePath = Path.Combine(relativeFolder, fileName);
            var fullPath = Path.Combine(_basePath, relativePath);
            var convertedPath = _pathConverter.ConvertPath(fullPath);

            var convertedDirectory = Path.GetDirectoryName(convertedPath);
            if (!string.IsNullOrEmpty(convertedDirectory))
                Directory.CreateDirectory(convertedDirectory);

            await File.WriteAllBytesAsync(convertedPath, thumbnailBytes, cancellationToken);

            _logger.LogInformation("Saved thumbnail for document {DocumentId}  to {Path}", mainfolder, fullPath);

            return relativePath.Replace('/', '\\');
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate thumbnail for document {DocumentId} ", mainfolder);
            throw;
        }
    }


    public async Task<string> SavePdfAsync(byte[] PdfBytes, string mainfolder, string fileName, CancellationToken cancellationToken = default)
    {
        using var _ = ConnectShare();

        var relativeFolder = Path.Combine(GetFolderPath(mainfolder));
        var absoluteFolder = Path.Combine(_basePath, relativeFolder);

        var relativePath = Path.Combine(relativeFolder, fileName);
        var fullPath = Path.Combine(_basePath, relativePath);
        var convertedPath = _pathConverter.ConvertPath(fullPath);

        var convertedDirectory = Path.GetDirectoryName(convertedPath);
        if (!string.IsNullOrEmpty(convertedDirectory))
            Directory.CreateDirectory(convertedDirectory);

        await File.WriteAllBytesAsync(convertedPath, PdfBytes, cancellationToken);

        _logger.LogInformation("Saved Pdf File to {Path}", fullPath);

        return relativePath.Replace('/', '\\');
    }

    public async Task<string> SavePdfThumbnailAsync(byte[] pdfBytes, string mainfolder, CancellationToken cancellationToken = default)
    {
        try
        {
            using var memoryStream = new MemoryStream(pdfBytes);
            using SKBitmap bitmap = Conversion.ToImage(memoryStream, page: 0);

            // Encode to PNG
            using SKImage image = SKImage.FromBitmap(bitmap);
            using SKData data = image.Encode(SKEncodedImageFormat.Png, 90);


            return await SaveThumbnailAsync(data.ToArray(), mainfolder);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate image for import document {DocumentId} ", mainfolder);
            throw;
        }
    }



    public async Task<byte[]?> ReadAsync(string filePath, DataSource dataSource)
    {
        if (string.IsNullOrEmpty(filePath))
        {
            return null;
        }

        var fullpath = string.Empty;

        if (dataSource == DataSource.OnBase)
        {
            fullpath = _pathConverter.ConvertOnbasePath(filePath, _onbaseRootPath, _onbaseBasePath);
        }
        else
        {
            // Always resolve the file as basePath + the path stored in the DB. The DB value may
            // arrive rooted (leading "\" or "/"), which would make Path.Combine discard _basePath
            // and return just the DB path. Trim any leading separators so the DB value is always
            // treated as relative to _basePath.
            var relativePath = filePath.TrimStart('\\', '/');

            fullpath = Path.Combine(_basePath, relativePath);

            fullpath = _pathConverter.ConvertPath(fullpath);
        }


        if (string.IsNullOrWhiteSpace(fullpath))
        {
            _logger.LogWarning("File path is null or empty");
            return null;
        }

        // Trim stray whitespace that can arrive via query strings. A leading space stops the
        // path from being recognised as a rooted UNC path, causing File.* to treat it as
        // relative and prepend the app content root (e.g. "...\WebAPI\ \10.30.80.150\...").
        fullpath = fullpath.Trim();

        _logger.LogInformation(
            "ReadAsync resolving {Path}. Running identity: {Identity}",
            fullpath, Environment.UserName);

        // NOTE: Do NOT gate the read on File.Exists(). On SMB shares File.Exists() returns
        // false for BOTH "missing" and "access denied"/"no session", which hides the real
        // cause. Instead we attempt the actual read and let the exception tell us what went
        // wrong. First try with the ambient identity; if that fails, open an authenticated
        // share session with the configured credentials and retry.
        var ambient = await TryReadBytesAsync(fullpath);
        if (ambient.Bytes is not null)
        {
            _logger.LogInformation(
                "Successfully read file from {Path} using ambient identity, size: {Size} bytes",
                fullpath, ambient.Bytes.Length);
            return ambient.Bytes;
        }

        // If the file genuinely does not exist, an authenticated session won't help.
        if (ambient.Exception is FileNotFoundException or DirectoryNotFoundException)
        {
            _logger.LogWarning("File not found at path: {Path}", fullpath);
            return null;
        }

        _logger.LogWarning(
            ambient.Exception,
            "Ambient read of {Path} failed ({Reason}); retrying with an authenticated share session.",
            fullpath, ambient.Exception?.GetType().Name ?? "unknown");
        using var _ = ConnectShare();

        var authenticated = await TryReadBytesAsync(fullpath);
        if (authenticated.Bytes is not null)
        {
            _logger.LogInformation(
                "Successfully read file from {Path} using authenticated share session, size: {Size} bytes",
                fullpath, authenticated.Bytes.Length);
            return authenticated.Bytes;
        }

        if (authenticated.Exception is FileNotFoundException or DirectoryNotFoundException)
        {
            _logger.LogWarning("File not found at path: {Path}", fullpath);
            return null;
        }

        _logger.LogError(
            authenticated.Exception,
            "Failed to read {Path} even after opening an authenticated share session ({Reason}). " +
            "Verify the configured share account has NTFS read access to this folder.",
            fullpath, authenticated.Exception?.GetType().Name ?? "unknown");
        throw authenticated.Exception ?? new IOException($"Unable to read file at {fullpath}.");
    }

    /// <summary>
    /// Attempts to read all bytes of a file, returning either the bytes or the exception that
    /// prevented the read (instead of relying on File.Exists, which cannot distinguish a
    /// missing file from an access/authentication failure on an SMB share).
    /// </summary>
    private static async Task<(byte[]? Bytes, Exception? Exception)> TryReadBytesAsync(string fullpath)
    {
        try
        {
            var bytes = await File.ReadAllBytesAsync(fullpath);
            return (bytes, null);
        }
        catch (Exception ex)
        {
            return (null, ex);
        }
    }

    public byte[] CompressForPreview(byte[] imageBytes, int maxDimension = 1200, int quality = 70)
    {
        using var originalBitmap = SKBitmap.Decode(imageBytes);
        if (originalBitmap == null)
        {
            throw new InvalidOperationException("Failed to decode image bytes for preview compression.");
        }

        var largestSide = Math.Max(originalBitmap.Width, originalBitmap.Height);
        var ratio = largestSide > maxDimension ? (double)maxDimension / largestSide : 1.0;

        var newWidth = (int)(originalBitmap.Width * ratio);
        var newHeight = (int)(originalBitmap.Height * ratio);

        using var resizedBitmap = ratio < 1.0
            ? originalBitmap.Resize(new SKImageInfo(newWidth, newHeight), SKSamplingOptions.Default)
            : originalBitmap.Copy();

        using var image = SKImage.FromBitmap(resizedBitmap ?? originalBitmap);
        using var data = image.Encode(SKEncodedImageFormat.Jpeg, quality);
        return data.ToArray();
    }

    public async Task<string> MoveToDeletedFolderAsync(string originalPath)
    {
        if (string.IsNullOrWhiteSpace(originalPath))
            throw new ArgumentException("Original file path cannot be null or empty.", nameof(originalPath));

        var filePath = _pathConverter.ConvertPath(Path.Combine(_basePath, originalPath));
        var deletedRoot = Path.Combine(_deletedDocumentFolder, originalPath);

        try
        {
            Directory.CreateDirectory(deletedRoot);
            var destinationPath = _pathConverter.ConvertPath(Path.Combine(_basePath, deletedRoot));
            _logger.LogInformation("Moving deleted document from {OriginalPath} to {DestinationPath}", originalPath, destinationPath);
            File.Move(filePath, destinationPath, true);
            _logger.LogInformation("Document successfully moved to {DestinationPath}", destinationPath);

            return destinationPath;
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogError(ex, "Access denied while moving file {OriginalPath}", originalPath);
            throw;
        }
        catch (IOException ex)
        {
            _logger.LogError(ex, "IO error while moving file {OriginalPath}", originalPath);
            throw new IOException($"Failed to move file '{originalPath}' to deleted folder.", ex);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error while moving file {OriginalPath}", originalPath);
            throw;
        }

    }
}