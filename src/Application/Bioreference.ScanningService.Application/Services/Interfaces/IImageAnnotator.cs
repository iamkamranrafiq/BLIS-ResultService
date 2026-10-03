using Bioreference.ScanningService.Application.Common.Entity;

namespace Bioreference.ScanningService.Application.Services.Interfaces;

/// <summary>
/// Draws bounding-box annotations over an image and writes the result to disk.
/// Implementations live in Bioreference.ScanningService.Application.Services.Implementations (SkiaSharp) so that
/// Bioreference.ScanningService.Application has no SkiaSharp dependency.
/// </summary>
public interface IImageAnnotator
{
    /// <summary>
    /// Draws the given detected barcodes on top of <paramref name="imageData"/> and
    /// writes a PNG to <paramref name="outputPath"/>. Returns null if the source
    /// image cannot be decoded.
    /// </summary>
    string? Annotate(byte[] imageData, string outputPath, List<DetectedBarcode> codes);
}
