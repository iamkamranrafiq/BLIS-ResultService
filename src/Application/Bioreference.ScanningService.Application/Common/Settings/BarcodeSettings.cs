namespace Bioreference.ScanningService.Application.Common.Settings;

public class BarcodeSettings
{
    public const string SectionName = "BarcodeSettings";

    /// <summary>
    /// Height offset in inches to add to the Y coordinate of the second barcode point.
    /// </summary>
    public float BarcodeHeightOffsetInches { get; set; } = 0f;
}
