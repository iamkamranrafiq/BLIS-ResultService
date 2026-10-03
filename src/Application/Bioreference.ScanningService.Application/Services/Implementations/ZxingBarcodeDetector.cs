using Bioreference.ScanningService.Application.Common.Entity;
using Bioreference.ScanningService.Application.Services.Interfaces;
using Microsoft.Extensions.Logging;

using SkiaSharp;
using ZXing;
using ZXing.Common;
using ZXing.SkiaSharp;

namespace Bioreference.ScanningService.Application.Services.Implementations;

public class ZxingBarcodeDetector
{
    private readonly ZXing.SkiaSharp.BarcodeReader _fastReader;
    private readonly ZXing.SkiaSharp.BarcodeReader _thoroughReader;
    private readonly ILogger<ZxingBarcodeDetector> _logger;

    public string DisplayName => "C# ZXing";
    public bool IsAvailable => true;

    public ZxingBarcodeDetector(ILogger<ZxingBarcodeDetector> logger)
    {
        _logger = logger;
        // Most pages decode successfully on the first, cheap pass; TryHarder multiplies the
        // number of decode attempts (rotations/scales), so it's reserved as a slower fallback.
        _fastReader = new ZXing.SkiaSharp.BarcodeReader
        {
            AutoRotate = true,
            Options = new DecodingOptions
            {
                PossibleFormats = GetAllBarcodeFormats(),
                TryHarder = false,
                PureBarcode = false
            }
        };
        _thoroughReader = new ZXing.SkiaSharp.BarcodeReader
        {
            AutoRotate = true,
            Options = new DecodingOptions
            {
                PossibleFormats = GetAllBarcodeFormats(),
                TryHarder = true,
                PureBarcode = false
            }
        };
    }

    public Task<List<DetectedBarcode>> DetectAsync(byte[] imageData)
    {
        try
        {
            var dpi = GetImageDpi(imageData);
            _logger.LogWarning("DPI extracted from image: {Dpi}", dpi);
            using var bitmap = SKBitmap.Decode(imageData);
            if (bitmap == null)
            {
                _logger.LogWarning("Failed to decode image bytes to SKBitmap");
                return Task.FromResult(new List<DetectedBarcode>());
            }

            _logger.LogInformation("Image decoded: {Width}x{Height}, DPI: {Dpi}", bitmap.Width, bitmap.Height, dpi);

            var results = _fastReader.DecodeMultiple(bitmap);
            if (results == null || results.Length == 0)
            {
                results = _thoroughReader.DecodeMultiple(bitmap);
            }
            if (results == null || results.Length == 0)
                return Task.FromResult(new List<DetectedBarcode>());

            var detected = results.Select(r => new DetectedBarcode
            {
                Value = r.Text,
                Format = r.BarcodeFormat.ToString(),
                Confidence = 0.95f,
                Points = r.ResultPoints?.Select(p => new BarcodePoint(p.X, p.Y)).ToArray() ?? [],
                DPI = (int)dpi
            }).ToList();

            _logger.LogInformation("ZXing detected {Count} barcode(s)", detected.Count);
            return Task.FromResult(detected);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to decode image for barcode detection. Bytes={Bytes}", imageData.Length);
            return Task.FromResult(new List<DetectedBarcode>());
        }

    }

    /// <summary>
    /// Extracts DPI from image byte data by reading JPEG JFIF/EXIF or PNG pHYs metadata.
    /// Falls back to 96 DPI if metadata is not found.
    /// </summary>
    private float GetImageDpi(byte[] imageData)
    {
        const float defaultDpi = 96f;

        if (imageData == null || imageData.Length < 20)
            return defaultDpi;

        // Check for BMP (BITMAPFILEHEADER + DIB header)
        if (imageData[0] == 0x42 && imageData[1] == 0x4D &&
            imageData.Length >= 14 + 40)
        {
            const int dibOffset = 14;
            var dibHeaderSize = BitConverter.ToInt32(imageData, dibOffset);

            if (dibHeaderSize >= 40 && imageData.Length >= dibOffset + 28)
            {
                // BITMAPINFOHEADER: XPelsPerMeter is offset 24
                var pixelsPerMeter = BitConverter.ToInt32(imageData, dibOffset + 24);

                if (pixelsPerMeter > 0)
                {
                    return pixelsPerMeter / 39.3701f;
                }
            }

            return defaultDpi;
        }
        // Check for JPEG (starts with FF D8)
        if (imageData[0] == 0xFF && imageData[1] == 0xD8)
        {
            return GetJpegDpi(imageData);
        }

        // Check for PNG (starts with 89 50 4E 47)
        if (imageData[0] == 0x89 && imageData[1] == 0x50 && imageData[2] == 0x4E && imageData[3] == 0x47)
        {
            return GetPngDpi(imageData);
        }

        // Check for TIFF (starts with 49 49 or 4D 4D)
        if ((imageData[0] == 0x49 && imageData[1] == 0x49) || (imageData[0] == 0x4D && imageData[1] == 0x4D))
        {
            return GetTiffDpi(imageData);
        }

        return defaultDpi;
    }

    private float GetJpegDpi(byte[] data)
    {
        const float defaultDpi = 96f;

        // Look for JFIF APP0 marker (FF E0)
        int offset = 2;
        while (offset < data.Length - 1)
        {
            if (data[offset] != 0xFF)
                break;

            byte marker = data[offset + 1];
            offset += 2;

            if (marker == 0xD9 || marker == 0xDA) // End of image or start of scan
                break;

            if (offset + 2 > data.Length)
                break;

            int segmentLength = (data[offset] << 8) | data[offset + 1];

            // JFIF APP0 marker
            if (marker == 0xE0 && segmentLength >= 14)
            {
                int dataStart = offset + 2;
                if (dataStart + 12 <= data.Length &&
                    data[dataStart] == 0x4A && data[dataStart + 1] == 0x46 &&
                    data[dataStart + 2] == 0x49 && data[dataStart + 3] == 0x46) // "JFIF"
                {
                    byte units = data[dataStart + 7];
                    int xDensity = (data[dataStart + 8] << 8) | data[dataStart + 9];

                    if (xDensity > 0)
                    {
                        return units switch
                        {
                            1 => xDensity, // dots per inch
                            2 => xDensity * 2.54f, // dots per cm -> DPI
                            _ => defaultDpi // no units / aspect ratio only
                        };
                    }
                }
            }

            offset += segmentLength;
        }

        return defaultDpi;
    }

    private float GetPngDpi(byte[] data)
    {
        const float defaultDpi = 96f;

        // Search for pHYs chunk in PNG
        // PNG structure: 8-byte signature, then chunks (4-byte length, 4-byte type, data, 4-byte CRC)
        int offset = 8; // skip PNG signature
        while (offset + 12 <= data.Length)
        {
            int chunkLength = (data[offset] << 24) | (data[offset + 1] << 16) |
                              (data[offset + 2] << 8) | data[offset + 3];
            offset += 4;

            // Check chunk type "pHYs"
            if (data[offset] == 0x70 && data[offset + 1] == 0x48 &&
                data[offset + 2] == 0x59 && data[offset + 3] == 0x73)
            {
                offset += 4; // skip type
                if (offset + 9 <= data.Length)
                {
                    int pixelsPerUnitX = (data[offset] << 24) | (data[offset + 1] << 16) |
                                         (data[offset + 2] << 8) | data[offset + 3];
                    byte unit = data[offset + 8];

                    if (unit == 1 && pixelsPerUnitX > 0) // meter
                    {
                        return pixelsPerUnitX / 39.3701f;
                    }
                }
                break;
            }

            offset += 4; // skip type
            offset += chunkLength + 4; // skip data + CRC
        }

        return defaultDpi;
    }

    private float GetTiffDpi(byte[] data)
    {
        const float defaultDpi = 96f;

        try
        {
            bool littleEndian = data[0] == 0x49;

            int ReadInt16(int pos) => littleEndian
                ? data[pos] | (data[pos + 1] << 8)
                : (data[pos] << 8) | data[pos + 1];

            int ReadInt32(int pos) => littleEndian
                ? data[pos] | (data[pos + 1] << 8) | (data[pos + 2] << 16) | (data[pos + 3] << 24)
                : (data[pos] << 24) | (data[pos + 1] << 16) | (data[pos + 2] << 8) | data[pos + 3];

            int ifdOffset = ReadInt32(4);
            int numEntries = ReadInt16(ifdOffset);

            float xRes = 0;
            int resUnit = 2; // default: inch

            for (int i = 0; i < numEntries && ifdOffset + 2 + (i * 12) + 12 <= data.Length; i++)
            {
                int entryOffset = ifdOffset + 2 + (i * 12);
                int tag = ReadInt16(entryOffset);
                int valueOffset = ReadInt32(entryOffset + 8);

                switch (tag)
                {
                    case 0x011A: // XResolution (RATIONAL)
                        if (valueOffset + 8 <= data.Length)
                        {
                            int num = ReadInt32(valueOffset);
                            int den = ReadInt32(valueOffset + 4);
                            if (den != 0) xRes = (float)num / den;
                        }
                        break;
                    case 0x0128: // ResolutionUnit
                        resUnit = ReadInt16(entryOffset + 8);
                        break;
                }
            }

            if (xRes > 0)
            {
                return resUnit switch
                {
                    3 => xRes * 2.54f, // centimeter -> inch
                    _ => xRes // inch (default)
                };
            }
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Failed to read TIFF DPI metadata");
        }

        return defaultDpi;
    }

    private static IList<BarcodeFormat> GetAllBarcodeFormats() => new List<BarcodeFormat>
    {
        BarcodeFormat.CODE_39,
        BarcodeFormat.CODE_128,
        BarcodeFormat.CODE_93,
        BarcodeFormat.CODABAR,
    };


}
