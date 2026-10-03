using Bioreference.ScanningService.Application.Common.Entity;
using Bioreference.ScanningService.Application.Services.Interfaces;
using Microsoft.Extensions.Logging;
using SkiaSharp;

namespace Bioreference.ScanningService.Application.Services.Implementations;

/// <summary>
/// SkiaSharp-backed annotator. Draws colored bounding boxes, corner points, and
/// labels over the source image and writes the result to <c>outputPath</c>.
/// </summary>
public class SkiaSharpImageAnnotator : IImageAnnotator
{
    private static readonly SKColor[] Colors =
    [
        SKColors.Red, SKColors.Blue, SKColors.Green,
        SKColors.Orange, SKColors.Purple, SKColors.Cyan
    ];

    private readonly ILogger<SkiaSharpImageAnnotator> _logger;

    public SkiaSharpImageAnnotator(ILogger<SkiaSharpImageAnnotator> logger)
    {
        _logger = logger;
    }

    public string? Annotate(byte[] imageData, string outputPath, List<DetectedBarcode> codes)
    {
        try
        {
            using var source = SKBitmap.Decode(imageData);
            if (source == null || source.Width == 0 || source.Height == 0)
            {
                _logger.LogWarning("Could not decode image for annotation");
                return null;
            }

            var dir = Path.GetDirectoryName(outputPath);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                Directory.CreateDirectory(dir);

            // Force the source into a fresh Rgba8888 bitmap before drawing. Source bitmaps
            // decoded from grayscale/paletted inputs (e.g. 8bpp scanned BMPs) can trip the
            // native sk_image_new_from_bitmap call inside DrawBitmap on x86 — copying to a
            // known-good ARGB format realizes the pixels and avoids the SEH.
            using var canvasBitmap = new SKBitmap(source.Width, source.Height, SKColorType.Rgba8888, SKAlphaType.Premul);
            if (!source.CopyTo(canvasBitmap, SKColorType.Rgba8888))
            {
                // CopyTo can fail on edge cases — fall back to a draw-based conversion.
                using var convSurface = SKSurface.Create(new SKImageInfo(source.Width, source.Height, SKColorType.Rgba8888, SKAlphaType.Premul));
                convSurface.Canvas.DrawBitmap(source, 0, 0);
                using var convSnap = convSurface.Snapshot();
                using var convBmp = SKBitmap.FromImage(convSnap);
                convBmp.CopyTo(canvasBitmap, SKColorType.Rgba8888);
            }

            using var surface = SKSurface.Create(new SKImageInfo(canvasBitmap.Width, canvasBitmap.Height, SKColorType.Rgba8888, SKAlphaType.Premul));
            var canvas = surface.Canvas;
            canvas.DrawBitmap(canvasBitmap, 0, 0);

            for (int i = 0; i < codes.Count; i++)
            {
                var barcode = codes[i];
                var color = Colors[i % Colors.Length];

                if (barcode.Points.Length < 2) continue;

                var minX = barcode.Points.Min(p => p.X);
                var minY = barcode.Points.Min(p => p.Y);
                var maxX = barcode.Points.Max(p => p.X);
                var maxY = barcode.Points.Max(p => p.Y);

                using var boxPaint = new SKPaint
                    { Color = color, Style = SKPaintStyle.Stroke, StrokeWidth = 3, IsAntialias = true };
                canvas.DrawRect(minX, minY, maxX - minX, maxY - minY, boxPaint);

                using var pointPaint = new SKPaint
                    { Color = color, Style = SKPaintStyle.Fill, IsAntialias = true };
                foreach (var point in barcode.Points)
                    canvas.DrawCircle(point.X, point.Y, 5, pointPaint);

                var label = $"#{i + 1}: {barcode.Format} - {barcode.Value}";
                using var font = new SKFont { Size = 20, Edging = SKFontEdging.Antialias };
                using var textPaint = new SKPaint
                    { Color = SKColors.White, IsAntialias = true, Style = SKPaintStyle.Fill };
                using var bgPaint = new SKPaint
                    { Color = color.WithAlpha(200), Style = SKPaintStyle.Fill };

                var textWidth = font.MeasureText(label);
                var textHeight = font.Metrics.Descent - font.Metrics.Ascent;

                var labelX = minX;
                var labelY = Math.Max(minY - 10, textHeight + 5);
                canvas.DrawRect(labelX, labelY - textHeight - 5, textWidth + 10, textHeight + 10, bgPaint);
                canvas.DrawText(label, labelX + 5, labelY, font, textPaint);
            }

            using var image = surface.Snapshot();
            using var data = image.Encode(SKEncodedImageFormat.Png, 100);
            using var stream = File.OpenWrite(outputPath);
            data.SaveTo(stream);

            return outputPath;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Annotation failed for {Path}", outputPath);
            return null;
        }
    }
}
