using System.IO.Pipes;
using System.Text;
using Bioreference.ScanningService.WinService.Services.Implementations;
using Bioreference.ScanningService.WinService.Settings;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Bioreference.ScanningService.WinService.Workers;

internal static class TwainWorkerProcess
{
    // Must match TwainScanService.WorkerFrameType on the host side.
    private enum WorkerFrameType : byte
    {
        Page = 1,
        Complete = 2,
        Error = 3
    }

    public static bool IsWorker(string[] args) => args.Contains("--twain-worker", StringComparer.Ordinal);

    public static async Task<int> RunAsync(string[] args)
    {
        var pipeName = GetArgument(args, "--pipe");
        var deviceName = GetArgument(args, "--device") ?? string.Empty;
        var duplex = bool.TryParse(GetArgument(args, "--duplex"), out var requestedDuplex) && requestedDuplex;
        var dpi = int.TryParse(GetArgument(args, "--dpi"), out var requestedDpi) ? requestedDpi : 300;
        var pageSize = GetArgument(args, "--page-size") ?? "A4";
        var pixelType = int.TryParse(GetArgument(args, "--pixel-type"), out var requestedPixelType)
            ? requestedPixelType
            : 0;

        if (string.IsNullOrWhiteSpace(pipeName))
            return 2;

        using var loggerFactory = LoggerFactory.Create(builder => builder.SetMinimumLevel(LogLevel.Warning));
        var scannerSettings = Options.Create(new ScannerSettings
        {
            Dpi = dpi,
            PageSize = pageSize,
            PixelType = pixelType
        });
        var scanner = new TwainScanService(loggerFactory.CreateLogger<TwainScanService>(), scannerSettings);
        using var pipe = new NamedPipeClientStream(".", pipeName, PipeDirection.Out, PipeOptions.Asynchronous);

        try
        {
            await pipe.ConnectAsync(CancellationToken.None).ConfigureAwait(false);

            await foreach (var page in scanner.AcquireImagesInProcessAsync(deviceName, duplex).ConfigureAwait(false))
            {
                await WriteFrameAsync(pipe, WorkerFrameType.Page, page).ConfigureAwait(false);
            }

            await WriteFrameAsync(pipe, WorkerFrameType.Complete, Array.Empty<byte>()).ConfigureAwait(false);
            return 0;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine(ex);
            if (pipe.IsConnected)
            {
                await WriteFrameAsync(pipe, WorkerFrameType.Error, Encoding.UTF8.GetBytes(ex.Message)).ConfigureAwait(false);
            }
            return 1;
        }
    }

    private static string? GetArgument(string[] args, string name)
    {
        var index = Array.IndexOf(args, name);
        return index >= 0 && index + 1 < args.Length ? args[index + 1] : null;
    }

    // Frame layout: 1-byte type + 4-byte little-endian length + raw payload.
    // Raw bytes (no base64/JSON) avoid the large transient string/array allocations per page
    // that were causing OutOfMemoryException on large scans.
    private static async Task WriteFrameAsync(Stream stream, WorkerFrameType type, byte[] payload)
    {
        var header = new byte[5];
        header[0] = (byte)type;
        BitConverter.GetBytes(payload.Length).CopyTo(header, 1);
        await stream.WriteAsync(header).ConfigureAwait(false);
        if (payload.Length > 0)
            await stream.WriteAsync(payload).ConfigureAwait(false);
        await stream.FlushAsync().ConfigureAwait(false);
    }
}

