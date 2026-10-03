using System;
using System.Collections.Generic;
using System.Text;

namespace Bioreference.ScanningService.WinService.Services.Interfaces
{
    public interface IScannerService
    {
        /// <summary>
        /// Synchronously enumerate available TWAIN devices.
        /// </summary>
        //List<ScannerInfo> DiscoverScanners();

        /// <summary>
        /// Returns the name of the active/connected scanner. When
        /// <paramref name="preferredDeviceName"/> is supplied and that device is connected,
        /// its name is returned; otherwise the first available connected scanner is returned.
        /// Returns <c>null</c> when no scanner is connected or TWAIN is unavailable.
        /// </summary>
        string? GetActiveScannerName(string? preferredDeviceName = null);

        /// <summary>
        /// Stream acquired images (BMP bytes) one at a time as the scanner captures them.
        /// Each yielded item is a complete BMP, ready to save/process. The caller should
        /// process each item promptly — capture pauses (backpressure) when the caller is slow.
        /// Pass a <see cref="CancellationToken"/> (e.g. <c>Context.ConnectionAbortedToken</c>
        /// in a SignalR hub) to stop acquisition when the consumer disconnects.
        /// </summary>
        /// <param name="duplex">
        /// When <c>true</c>, scan both sides of each page. When <c>false</c> (default), only
        /// the front side is scanned.
        /// </param>
        IAsyncEnumerable<byte[]> AcquireImagesAsync(string deviceName, bool duplex = false, CancellationToken cancellationToken = default);
    }
}
