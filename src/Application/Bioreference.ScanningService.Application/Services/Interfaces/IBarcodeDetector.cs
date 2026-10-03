

namespace Bioreference.ScanningService.Application.Services.Interfaces;

/// <summary>
/// Abstraction over barcode detection engines. Each implementation
/// takes raw image bytes and returns zero or more detected barcodes.
/// </summary>
public interface IBarcodeDetector
{
    /// <summary>
    /// Human-readable name for logging and UI display.
    /// </summary>
    string DisplayName { get; }

    /// <summary>
    /// Whether the underlying engine (python interpreter, libs) is available at runtime.
    /// </summary>
    bool IsAvailable { get; }

    /// <summary>
    /// Detect barcodes in the given image bytes.
    /// Returns an empty list (never null) if no barcodes are found.
    /// </summary>
    
}
