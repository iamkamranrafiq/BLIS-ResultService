namespace Bioreference.ScanningService.Application.Common.Entity;

/// <summary>
/// Detector-agnostic barcode result. Both ZXing and Python detectors
/// produce this shape so BarcodeService never cares which engine is active.
/// </summary>
public class DetectedBarcode
{
    public required string Value { get; init; }
    public required string Format { get; init; }
    public float Confidence { get; init; }

    /// <summary>
    /// Corner points of the barcode bounding polygon.
    /// Used by the annotation drawer to draw the bounding rectangle.
    /// </summary>
    public BarcodePoint[] Points { get; init; } = [];
    public int DPI { get; init; }
}
public class DetectedImageBarcodes
{
    public byte[] ImageBytes { get; init; } = [];
    public List<BarcodeKeywordDoctype> DetectedBarcodes { get; init; } = new();
}
public class BarcodeKeywordDoctype : DetectedBarcode
{
    public int DoctypeId { get; init; }
    public int KeywordId { get; init; }
    public string BarcodeId { get; init; } = string.Empty;
}



public readonly record struct BarcodePoint(float X, float Y);
