namespace Bioreference.ScanningService.WinService.Settings;

public class ScannerSettings
{
    public const string SectionName = "ScannerSettings";

    public string DeviceName { get; set; } = string.Empty;
    public bool Duplex { get; set; } = false;
    public int Dpi { get; set; } = 300;
    public string PageSize { get; set; } = "A4";
    public int PixelType { get; set; } = 0;

    /// <summary>
    /// Output image format for saved scanned pages. TWAIN native transfer always delivers a raw
    /// BMP and most drivers do not report the format chosen in their UI, so the service converts
    /// captured pages to this format. Supported values: "Jpeg" (default) or "Bmp".
    /// </summary>
    public string OutputFormat { get; set; } = "Jpeg";
}

public class TestSettings
{
    public const string SectionName = "Test";

    public bool Mode { get; set; } = false;
    public string ImageFolderLocation { get; set; } = string.Empty;
}
