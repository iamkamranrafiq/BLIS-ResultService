using Bioreference.ScanningService.Application.Common.Entity;
using Bioreference.ScanningService.Application.Services.Interfaces;
using Microsoft.Extensions.Logging;


namespace Bioreference.ScanningService.Application.Services.Implementations;

/// <summary>
/// Orchestrates barcode processing: resolve the active detector, run detection,
/// then delegate annotation to <see cref="IImageAnnotator"/>. No SkiaSharp or
/// ZXing dependency lives here — both are pushed into BarcodeExtraction.
/// </summary>
public class BarcodeService
{
    private readonly ILogger<BarcodeService> _logger;

    private readonly IImageAnnotator _annotator;
    private ZxingBarcodeDetector _detector;
    public BarcodeService(
        ILogger<BarcodeService> logger,
        ZxingBarcodeDetector detector,
        IImageAnnotator annotator)
    {
        _logger = logger;
        _detector = detector;
        _annotator = annotator;
    }

    /// <summary>
    /// Run detection + annotation on in-memory image bytes.
    /// Returns the list of codes (value/format/confidence) and the path to the
    /// annotated PNG (null when no barcodes were found).
    /// </summary>
    public async Task<(List<(string Value, string Format, float Confidence)> Codes, string? ProcessedImagePath)> ProcessImageFromDataAsync(
        byte[] imageData, string outputDir, string fileName)
    {

        _logger.LogInformation("Processing {FileName} for barcodes with engine {Engine}",
            fileName, _detector.DisplayName);

        var detected = await _detector.DetectAsync(imageData);

        _logger.LogInformation("Barcode scan found {Count} code(s) in {FileName} using {Engine}",
            detected.Count, fileName, _detector.DisplayName);

        string? annotatedPath = null;
        if (detected.Count > 0)
        {
            var processedPath = Path.Combine(outputDir,
                $"processed_{Path.GetFileNameWithoutExtension(fileName)}.png");
            annotatedPath = _annotator.Annotate(imageData, processedPath, detected);
        }

        var codes = detected.Select(r => (r.Value, r.Format, r.Confidence)).ToList();
        return (codes, annotatedPath);
    }
    public async Task<List<DetectedBarcode>> ProcessImageFromDataAsync(
        byte[] imageData)
    {

        _logger.LogInformation("Processing image for barcodes with engine {Engine}",
            _detector.DisplayName);

        var detected = await _detector.DetectAsync(imageData);

        _logger.LogInformation("Barcode scan found {Count} code(s) using {Engine}",
            detected.Count, _detector.DisplayName);
        return detected;

    }
}
