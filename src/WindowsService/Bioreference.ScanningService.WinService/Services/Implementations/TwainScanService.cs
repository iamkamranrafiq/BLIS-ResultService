using Bioreference.ScanningService.WinService.Services.Interfaces;
using Bioreference.ScanningService.WinService.Settings;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Diagnostics;
using System.IO.Pipes;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Security.Principal;
using System.Text;
using System.Threading.Channels;

using NTwain;
using NTwain.Data;
using Newtonsoft.Json;
using System.Reflection;
using System.Collections;



namespace Bioreference.ScanningService.WinService.Services.Implementations
{
    public class TwainScanService : IScannerService
    {
        // Caps how long we wait for the first/next transfer before giving up and releasing
        // the device (e.g. driver stuck waiting for paper or a UI it can't display).
        private static readonly TimeSpan NoActivityTimeout = TimeSpan.FromSeconds(30);

        // TWAIN drivers permit only one active session per process/device. Keep this lease
        // until the STA thread exits, including when native cleanup is slow or blocked.
        private static readonly SemaphoreSlim TwainSessionLease = new(1, 1);

        private readonly ILogger<TwainScanService> _logger;
        private readonly ScannerSettings _scannerSettings;

        public TwainScanService(ILogger<TwainScanService> logger, IOptions<ScannerSettings> scannerSettings)
        {
            _logger = logger;
            _scannerSettings = scannerSettings.Value;
        }


        public string? GetActiveScannerName(string? preferredDeviceName = null)
        {
            if (!TwainSessionLease.Wait(0))
            {
                _logger.LogInformation("Skipping scanner-name lookup because a TWAIN scan is active.");
                return null;
            }

            string? scannerName = null;

            // TWAIN requires an STA thread with a running message pump.
            var thread = new Thread(() =>
            {
                TwainSession? session = null;
                try
                {
                    var platform = PlatformInfo.Current;
                    if (!platform.IsSupported)
                    {
                        _logger.LogError("TWAIN not supported.");
                        return;
                    }

                    var appId = TWIdentity.Create(
                        DataGroups.Image,
                        new Version(1, 0),
                        manufacturer: "Bioreference",
                        productFamily: "BLIS Scanning",
                        productName: "BLIS Scanner",
                        productDescription: "BLIS Scanner Service");

                    session = new TwainSession(appId);
                    var rc = session.Open();
                    if (rc != ReturnCode.Success)
                    {
                        _logger.LogError("Failed to open session: {RC}", rc);
                        return;
                    }

                    DataSource? source = null;
                    if (!string.IsNullOrEmpty(preferredDeviceName))
                    {
                        source = session.FirstOrDefault(s => s.Name == preferredDeviceName);
                        if (source == null)
                            _logger.LogWarning("Scanner '{Name}' not found, using default", preferredDeviceName);
                    }
                    source ??= session.FirstOrDefault();

                    if (source == null)
                    {
                        _logger.LogWarning("No TWAIN sources connected.");
                        return;
                    }

                    // source.Name is the TWAIN data source (driver) name, which is often a
                    // generic label like "HP TWAIN USB". Resolve the real device model from the
                    // WIA imaging-class registry, falling back to the TWAIN name when unavailable.
                    var twainName = string.IsNullOrEmpty(source.Name) ? "Unknown scanner" : source.Name;
                    scannerName = TryGetWindowsScannerName(preferredDeviceName, source.Manufacturer) ?? twainName;
                    //var capabilitiesJson = SerializeCapabilitiesIndented(source.Capabilities);
                    _logger.LogInformation(
                    "Active scanner resolved: {Name} (driver='{Driver}', manufacturer='{Manufacturer}', family='{Family}')",
                    scannerName, source.Name, source.Manufacturer, source.ProductFamily);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error resolving active TWAIN scanner");
                }
                finally
                {
                    try { session?.Close(); } catch { }
                    TwainSessionLease.Release();
                }
            });

            thread.SetApartmentState(ApartmentState.STA);
            thread.IsBackground = true;
            thread.Start();
            thread.Join(TimeSpan.FromSeconds(15));

            return scannerName;
        }

        /// <summary>
        /// Attempts to resolve the friendly model name of a connected scanner from the WIA
        /// imaging-class registry (<c>HKLM\SYSTEM\CurrentControlSet\Control\Class\
        /// {6bdd1fc6-810f-11d0-bec7-08002be2092f}\NNNN\FriendlyName</c>). This is the same
        /// friendly name Windows Settings shows (e.g. "HP ScanJet Pro 2600 f1"), which TWAIN
        /// does not expose. When multiple scanners are installed, prefers one matching the
        /// configured device name, then one matching the TWAIN manufacturer, then the first.
        /// Returns <c>null</c> when nothing is found or the registry read fails.
        /// </summary>
        private string? TryGetWindowsScannerName(string? preferredDeviceName, string? manufacturerHint)
        {
            const string imagingClassKey =
                @"SYSTEM\CurrentControlSet\Control\Class\{6bdd1fc6-810f-11d0-bec7-08002be2092f}";

            try
            {
                using var classKey = Registry.LocalMachine.OpenSubKey(imagingClassKey);
                if (classKey == null)
                    return null;

                var names = new List<string>();
                foreach (var subKeyName in classKey.GetSubKeyNames())
                {
                    // Device instances are numeric subkeys (0000, 0001, ...).
                    if (!int.TryParse(subKeyName, out _))
                        continue;

                    using var deviceKey = classKey.OpenSubKey(subKeyName);
                    if (deviceKey?.GetValue("FriendlyName") is string friendly &&
                        !string.IsNullOrWhiteSpace(friendly))
                    {
                        names.Add(friendly.Trim());
                    }
                }

                if (names.Count == 0)
                    return null;

                // Prefer a device matching the configured/preferred scanner name.
                string? chosen = null;
                if (!string.IsNullOrEmpty(preferredDeviceName))
                {
                    chosen = names.FirstOrDefault(n =>
                        n.IndexOf(preferredDeviceName, StringComparison.OrdinalIgnoreCase) >= 0);
                }

                // Otherwise match on the TWAIN manufacturer token (e.g. "HP" from "HP Inc.").
                if (chosen == null)
                {
                    var token = FirstToken(manufacturerHint);
                    if (!string.IsNullOrEmpty(token))
                    {
                        chosen = names.FirstOrDefault(n =>
                            n.IndexOf(token, StringComparison.OrdinalIgnoreCase) >= 0);
                    }
                }

                chosen ??= names[0];
                return CleanScannerName(chosen);
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "Registry scanner name lookup failed; falling back to TWAIN identity.");
                return null;
            }
        }

        /// <summary>Returns the first whitespace-delimited token of a string (e.g. "HP" from "HP Inc.").</summary>
        private static string? FirstToken(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return null;
            var trimmed = value.Trim();
            var space = trimmed.IndexOf(' ');
            return space < 0 ? trimmed : trimmed.Substring(0, space);
        }

        /// <summary>Removes a trailing connection qualifier such as " (USB)" from a device name.</summary>
        private static string CleanScannerName(string name)
        {
            var idx = name.LastIndexOf('(');
            if (idx > 0)
                name = name.Substring(0, idx);
            return name.Trim();
        }

        /// <summary>
        /// Stream raw BMP bytes for each scanned page the moment the hardware transfers it.
        ///
        /// Pacing is strict one-page-ahead: the STA callback writes the BMP to a single-slot
        /// channel and then waits on a ManualResetEventSlim until the consumer has read it.
        /// The wait happens INSIDE the STA callback, so while it's blocked the STA message
        /// pump is paused — TWAIN cannot fire the next <c>DataTransferred</c> until we return.
        /// That gives the user visible page-by-page feedback instead of an initial burst.
        /// </summary>
        public async IAsyncEnumerable<byte[]> AcquireImagesAsync(
            string deviceName,
            bool duplex = false,
            [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            if (!await TwainSessionLease.WaitAsync(0, cancellationToken).ConfigureAwait(false))
            {
                throw new InvalidOperationException(
                    "The scanner is still releasing a previous scan. Wait a moment before starting another scan.");
            }

            try
            {
                await foreach (var page in AcquireImagesFromWorkerAsync(
                    deviceName, duplex, _scannerSettings.Dpi, _scannerSettings.PageSize,
                    _scannerSettings.PixelType, cancellationToken)
                    .ConfigureAwait(false))
                {
                    yield return page;
                }
            }
            finally
            {
                TwainSessionLease.Release();
            }
        }

        internal async IAsyncEnumerable<byte[]> AcquireImagesInProcessAsync(
            string deviceName,
            bool duplex = false,
            [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("[Acquire] STEP A: AcquireImagesAsync started. Device='{Device}', duplex={Duplex}.", deviceName, duplex);

            // Single-slot channel — capacity 1 because the STA callback itself enforces
            // strict pacing via the pageConsumed handshake (no need for extra buffering).
            var channel = Channel.CreateBounded<byte[]>(new BoundedChannelOptions(1)
            {
                FullMode = BoundedChannelFullMode.Wait,
                SingleReader = true,
                SingleWriter = true
            });

            // Signaled by the consumer after it reads from the channel. The STA callback
            // waits on this BEFORE returning so TWAIN doesn't queue the next transfer.
            var pageConsumed = new ManualResetEventSlim(false);

            var captureComplete = new ManualResetEventSlim(false);

            _logger.LogInformation("[Acquire] STEP B: Starting STA capture thread.");
            var staThread = new Thread(() =>
            {
                try
                {
                    _logger.LogInformation("[Acquire] STEP B1: STA thread running RunTwainCapture.");
                    RunTwainCapture(deviceName, duplex, channel.Writer, pageConsumed, captureComplete, cancellationToken);
                    _logger.LogInformation("[Acquire] STEP B2: RunTwainCapture returned normally.");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "STA capture thread crashed");
                    channel.Writer.TryComplete(ex);
                }
                finally
                {
                    _logger.LogInformation("[Acquire] STEP B3: STA thread completing channel writer.");
                    channel.Writer.TryComplete();
                    captureComplete.Set();
                }
            });
            staThread.SetApartmentState(ApartmentState.STA);
            staThread.IsBackground = true;
            staThread.Start();

            _logger.LogInformation("[Acquire] STEP C: Reading pages from channel as they are captured.");
            var yielded = 0;
            await foreach (var bmpBytes in channel.Reader.ReadAllAsync(cancellationToken).ConfigureAwait(false))
            {
                yielded++;
                _logger.LogInformation("[Acquire] STEP C1: Yielding page {Page} ({Bytes} bytes) to hub.", yielded, bmpBytes?.Length ?? 0);
                // Signal the STA callback that we've taken this page — it can return and
                // allow TWAIN to send the next one. Set BEFORE yield so the STA unblocks
                // immediately, even if the hub takes a while to ask us for the next page.
                pageConsumed.Set();
                yield return bmpBytes;
            }

            _logger.LogInformation("[Acquire] STEP D: Channel drained. Total pages yielded={Count}. Waiting for capture completion.", yielded);
            captureComplete.Wait(TimeSpan.FromSeconds(2));
            _logger.LogInformation("[Acquire] STEP D done: AcquireImagesAsync finished for device '{Device}'.", deviceName);
        }

        // Frame layout on the wire: 1-byte WorkerFrameType + 4-byte little-endian length + raw payload.
        // Raw bytes (no base64/JSON) avoid the ~3x transient string/array overhead per page that
        // was causing OutOfMemoryException on large scans.
        private const int MaxFramePayloadBytes = 64 * 1024 * 1024;

        private enum WorkerFrameType : byte
        {
            Page = 1,
            Complete = 2,
            Error = 3
        }

        private sealed record WorkerFrame(WorkerFrameType Type, byte[] Payload);

        private async IAsyncEnumerable<byte[]> AcquireImagesFromWorkerAsync(
            string deviceName,
            bool duplex,
            int dpi,
            string pageSize,
            int pixelType,
            [EnumeratorCancellation] CancellationToken cancellationToken)
        {
            const int workerTimeoutSeconds = 45;
            var pipeName = $"blis-twain-{Guid.NewGuid():N}";
            _logger.LogInformation("[Acquire] STEP C: Starting worker process for device '{Device}' with pipe '{PipeName}'.", deviceName, pipeName);
            using var pipe = new NamedPipeServerStream(pipeName, PipeDirection.In, 1,
                PipeTransmissionMode.Byte, PipeOptions.Asynchronous);
            using var worker = StartWorkerProcess(pipeName, deviceName, duplex, dpi, pageSize, pixelType);
            _logger.LogInformation("[Acquire] STEP C: Worker process started for device '{Device}' with pipe '{PipeName}'.", deviceName, pipeName);
            var workerErrors = new System.Collections.Concurrent.ConcurrentQueue<string>();
            worker.ErrorDataReceived += (_, eventArgs) =>
            {
                if (!string.IsNullOrWhiteSpace(eventArgs.Data))
                    workerErrors.Enqueue(eventArgs.Data);
            };
            worker.BeginErrorReadLine();
            using var inactivityCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            inactivityCts.CancelAfter(TimeSpan.FromSeconds(workerTimeoutSeconds));

            try
            {
                await WaitForWorkerConnectionAsync(pipe, worker, workerErrors, inactivityCts.Token,
                        cancellationToken, workerTimeoutSeconds)
                    .ConfigureAwait(false);

                while (true)
                {
                    var frame = await ReadFrameAsync(pipe, inactivityCts.Token, cancellationToken, workerTimeoutSeconds)
                        .ConfigureAwait(false);
                    if (frame == null)
                        break;

                    inactivityCts.CancelAfter(TimeSpan.FromSeconds(workerTimeoutSeconds));

                    switch (frame.Type)
                    {
                        case WorkerFrameType.Page:
                            yield return frame.Payload;
                            break;
                        case WorkerFrameType.Error:
                            throw new InvalidOperationException(
                                frame.Payload.Length > 0 ? Encoding.UTF8.GetString(frame.Payload) : "Scanner worker failed.");
                        case WorkerFrameType.Complete:
                            yield break;
                        default:
                            throw new InvalidOperationException($"Scanner worker sent an unknown frame type ({(byte)frame.Type}).");
                    }
                }

                if (worker.ExitCode != 0)
                    throw new InvalidOperationException($"Scanner worker exited with code {worker.ExitCode}.");
            }
            finally
            {
                if (!worker.HasExited)
                {
                    _logger.LogWarning("Terminating scanner worker process {ProcessId}.", worker.Id);
                    try { worker.Kill(entireProcessTree: true); } catch (InvalidOperationException) { }
                }
            }
        }

        private static async Task<WorkerFrame?> ReadFrameAsync(
            Stream stream,
            CancellationToken inactivityToken,
            CancellationToken cancellationToken,
            int workerTimeoutSeconds)
        {
            try
            {
                var header = new byte[5];
                var headerRead = await ReadExactlyAsync(stream, header, inactivityToken).ConfigureAwait(false);
                if (headerRead == 0)
                    return null;
                if (headerRead != header.Length)
                    throw new InvalidOperationException("Scanner worker closed the pipe mid-frame.");

                var type = (WorkerFrameType)header[0];
                var length = BitConverter.ToInt32(header, 1);
                if (length < 0 || length > MaxFramePayloadBytes)
                    throw new InvalidOperationException(
                        $"Scanner worker sent an oversized message ({length} bytes > {MaxFramePayloadBytes}).");

                var payload = Array.Empty<byte>();
                if (length > 0)
                {
                    payload = new byte[length];
                    var payloadRead = await ReadExactlyAsync(stream, payload, inactivityToken).ConfigureAwait(false);
                    if (payloadRead != length)
                        throw new InvalidOperationException("Scanner worker closed the pipe mid-frame.");
                }

                return new WorkerFrame(type, payload);
            }
            catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
            {
                throw new TimeoutException(
                    $"Scanner worker did not report activity within {workerTimeoutSeconds} seconds and was restarted.");
            }
        }

        private static async Task<int> ReadExactlyAsync(Stream stream, byte[] buffer, CancellationToken cancellationToken)
        {
            var offset = 0;
            while (offset < buffer.Length)
            {
                var read = await stream.ReadAsync(buffer.AsMemory(offset, buffer.Length - offset), cancellationToken)
                    .ConfigureAwait(false);
                if (read == 0)
                    break;
                offset += read;
            }
            return offset;
        }

        private static async Task WaitForWorkerConnectionAsync(
            NamedPipeServerStream pipe,
            Process worker,
            System.Collections.Concurrent.ConcurrentQueue<string> workerErrors,
            CancellationToken inactivityToken,
            CancellationToken cancellationToken,
            int workerTimeoutSeconds)
        {
            try
            {
                var connectionTask = pipe.WaitForConnectionAsync(inactivityToken);
                var exitTask = worker.WaitForExitAsync(CancellationToken.None);
                var completedTask = await Task.WhenAny(connectionTask, exitTask).ConfigureAwait(false);

                if (completedTask == exitTask)
                {
                    var details = string.Join(Environment.NewLine, workerErrors);
                    throw new InvalidOperationException(
                        $"Scanner worker exited before connecting to the host pipe (exit code {worker.ExitCode}). " +
                        (string.IsNullOrWhiteSpace(details) ? "No worker error output was captured." : details));
                }

                await connectionTask.ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
            {
                throw new TimeoutException(
                    $"Scanner worker did not report activity within {workerTimeoutSeconds} seconds and was restarted.");
            }
        }

        private static Process StartWorkerProcess(
            string pipeName, string deviceName, bool duplex, int dpi, string pageSize, int pixelType)
        {
            var executablePath = Environment.ProcessPath ?? throw new InvalidOperationException("Cannot locate the scanner host executable.");
            var isDotnetHost = string.Equals(
                Path.GetFileNameWithoutExtension(executablePath),
                "dotnet",
                StringComparison.OrdinalIgnoreCase);
            var startInfo = new ProcessStartInfo
            {
                FileName = executablePath,
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardError = true,
                WorkingDirectory = AppContext.BaseDirectory
            };

            if (isDotnetHost)
                startInfo.ArgumentList.Add(System.Reflection.Assembly.GetEntryAssembly()?.Location
                    ?? throw new InvalidOperationException("Cannot locate the scanner host assembly."));

            startInfo.ArgumentList.Add("--twain-worker");
            startInfo.ArgumentList.Add("--pipe");
            startInfo.ArgumentList.Add(pipeName);
            startInfo.ArgumentList.Add("--device");
            startInfo.ArgumentList.Add(deviceName);
            startInfo.ArgumentList.Add("--duplex");
            startInfo.ArgumentList.Add(duplex.ToString());
            startInfo.ArgumentList.Add("--dpi");
            startInfo.ArgumentList.Add(dpi.ToString(System.Globalization.CultureInfo.InvariantCulture));
            startInfo.ArgumentList.Add("--page-size");
            startInfo.ArgumentList.Add(pageSize);
            startInfo.ArgumentList.Add("--pixel-type");
            startInfo.ArgumentList.Add(pixelType.ToString(System.Globalization.CultureInfo.InvariantCulture));

            return Process.Start(startInfo) ?? throw new InvalidOperationException("Could not start the scanner worker process.");
        }

        /// <summary>
        /// STA-thread entry point. Runs the full TWAIN acquire lifecycle. For each
        /// <c>DataTransferred</c>: capture raw BMP bytes, write to the channel, then WAIT
        /// on <paramref name="pageConsumed"/> until the consumer has read them. This is the
        /// strict pacing handshake — the wait blocks the message pump, preventing TWAIN from
        /// firing the next transfer until we're ready.
        /// </summary>
        private void RunTwainCapture(
            string deviceName,
            bool duplex,
            ChannelWriter<byte[]> writer,
            ManualResetEventSlim pageConsumed,
            ManualResetEventSlim captureComplete,
            CancellationToken cancellationToken)
        {
            // Set on every TWAIN transfer event so the wait loop below only times out when
            // there is truly no activity (not just because a multi-page job is running long).
            var activitySignal = new ManualResetEventSlim(false);
            // Log the execution context up-front. This is usually the single most useful
            // piece of diagnostic information when a scan fails on a customer machine: it
            // tells us whether we are running in an interactive session (where the driver
            // can open the device) or in Session 0 (headless service, where desktop-bound
            // drivers like EPSON Scan 2 cannot open the device).
            LogScanEnvironment(deviceName, duplex);

            var platform = PlatformInfo.Current;
            if (!platform.IsSupported)
            {
                _logger.LogError("TWAIN platform not supported. ProcessBitness={Bitness}",
                    Environment.Is64BitProcess ? "x64" : "x86");
                return;
            }

            var appId = TWIdentity.Create(
                DataGroups.Image,
                new Version(1, 0),
                manufacturer: "Bioreference",
                productFamily: "BLIS Scanning",
                productName: "BLIS Scanner",
                productDescription: "BLIS Scanner Service");
            var session = new TwainSession(appId);

            session.DataTransferred += (_, e) =>
            {
                activitySignal.Set();
                _logger.LogInformation("[Transfer] STEP T1: DataTransferred fired (native={Native}, file='{File}').",
                    e.NativeData != IntPtr.Zero, e.FileDataPath ?? "none");
                try
                {
                    var bmpBytes = CaptureRawBmpBytes(e.NativeData, e.FileDataPath);
                    if (bmpBytes == null)
                    {
                        _logger.LogWarning("[Transfer] STEP T2: No bytes captured from this transfer (native and file both empty).");
                        return;
                    }

                    _logger.LogInformation("[Transfer] STEP T2: Captured {Bytes} bytes, writing page to channel.", bmpBytes.Length);

                    // Arm the handshake: consumer must call Set() before we can return.
                    pageConsumed.Reset();

                    // Write BMP to channel. Single-slot channel + the handshake below together
                    // guarantee the channel is empty when we get here (previous page was consumed).
                    writer.WriteAsync(bmpBytes).AsTask().GetAwaiter().GetResult();
                    _logger.LogInformation("[Transfer] STEP T3: Page written to channel, waiting for consumer to take it.");

                    // Wait for the consumer to take this page before returning. While we wait,
                    // the STA message pump is paused — TWAIN cannot send the next page yet.
                    // This is what enforces strict one-page-ahead pacing.
                    pageConsumed.Wait(cancellationToken);
                    _logger.LogInformation("[Transfer] STEP T4: Consumer took the page, returning to allow next transfer.");
                }
                catch (OperationCanceledException)
                {
                    _logger.LogInformation("Capture cancelled mid-page");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error capturing transferred image1" + ex.ToString());
                }
            };

            session.TransferReady += (_, _) =>
            {
                activitySignal.Set();
                _logger.LogInformation("[Transfer] TransferReady fired.");
            };

            session.TransferCanceled += (_, _) =>
            {
                _logger.LogWarning("[Transfer] TransferCanceled fired.");
                captureComplete.Set();
            };

            session.TransferError += (_, e) =>
            {
                _logger.LogError("[Transfer] TransferError fired: {Error}", e);
                captureComplete.Set();
            };

            session.SourceDisabled += (_, _) =>
            {
                _logger.LogInformation("[Transfer] STEP T5: SourceDisabled fired. Source disabled, capture complete.");
                captureComplete.Set();
            };

            _logger.LogInformation("[Capture] STEP 1: Opening TWAIN session.");
            var rc = session.Open();
            if (rc != ReturnCode.Success)
            {
                _logger.LogError("Failed to open session: {RC}", rc);
                return;
            }

            DataSource? source = null;
            _logger.LogInformation("[Capture] STEP 2: Session opened. Selecting source (requested='{Device}').", deviceName);
            if (!string.IsNullOrEmpty(deviceName))
            {
                source = session.FirstOrDefault(s => s.Name == deviceName);
                if (source == null) _logger.LogWarning("Scanner '{Name}' not found, using default", deviceName);
            }
            source ??= session.FirstOrDefault();
            if (source == null)
            {
                _logger.LogError("No TWAIN sources.");
                session.Close();
                return;
            }

            _logger.LogInformation("[Capture] STEP 3: Opening source '{Source}'.", source.Name);
            var openRc = source.Open();
            if (openRc != ReturnCode.Success)
            {
                // Try to surface the underlying TWAIN condition code so the failure is actionable
                // instead of just the generic "Failure" return code.
                string conditionCode = "unknown";
                try
                {
                    var status = session.GetStatus();
                    if (status != null)
                    {
                        conditionCode = status.ConditionCode.ToString();
                    }
                }
                catch (Exception statusEx)
                {
                    _logger.LogDebug(statusEx, "Unable to retrieve TWAIN status after failed source.Open()");
                }

                // Capture the device's own identity so we can see exactly which driver
                // refused to open (helps distinguish EPSON Scan 2 from HP/WIA-backed drivers).
                string deviceManufacturer = "unknown";
                string deviceProductFamily = "unknown";
                try
                {
                    deviceManufacturer = source.Manufacturer ?? "unknown";
                    deviceProductFamily = source.ProductFamily ?? "unknown";
                }
                catch (Exception idEx)
                {
                    _logger.LogDebug(idEx, "Unable to read data source identity after failed source.Open()");
                }

                _logger.LogError(
                    "Failed to open source '{Source}': ReturnCode={RC}, ConditionCode={ConditionCode}. " +
                    "Device: manufacturer='{Manufacturer}', family='{Family}'.",
                    source.Name,
                    openRc,
                    conditionCode,
                    deviceManufacturer,
                    deviceProductFamily);

                // EPSON Scan 2 (and most consumer TWAIN drivers) require an interactive desktop
                // session. When the process runs as a Windows Service in Session 0
                // (Environment.UserInteractive == false), the driver cannot access the device UI
                // and source.Open() fails with a generic Failure. Make that cause explicit.
                if (!Environment.UserInteractive)
                {
                    _logger.LogError(
                        "The scanning process is running in a non-interactive session (Session 0 / Windows Service). " +
                        "The TWAIN driver for '{Source}' likely cannot open the device without an interactive desktop. " +
                        "Run the scanning host in the logged-in user's session (tray/desktop app or a Scheduled Task " +
                        "that runs only when the user is logged on), or use a network scan protocol (eSCL/WSD) that " +
                        "does not require a desktop.",
                        source.Name);
                }

                session.Close();
                return;
            }

            _logger.LogInformation("[Capture] STEP 4: Source '{Source}' opened. Configuring capabilities.", source.Name);
            ConfigureCapabilities(source, duplex);
            _logger.LogInformation("[Capture] STEP 5: Enabling source '{Source}' (NoUI) to start acquisition.", source.Name);
            var enableRc = source.Enable(SourceEnableMode.NoUI, false, IntPtr.Zero);
            if (enableRc != ReturnCode.Success)
            {
                _logger.LogError("Failed to enable source: {RC}", enableRc);
                source.Close();
                session.Close();
                return;
            }

            _logger.LogInformation("[Capture] STEP 6: Source enabled. Waiting for TransferReady/DataTransferred.");
            try
            {
                // Bounded wait: some drivers block on a hidden UI dialog (e.g. empty feeder)
                // that can never be dismissed under a non-interactive service session, which
                // would otherwise hang this STA thread — and the locked device — forever. The
                // timeout resets on every transfer event so a long multi-page job isn't aborted,
                // only a driver that has gone silent.
                var handles = new[] { captureComplete.WaitHandle, cancellationToken.WaitHandle, activitySignal.WaitHandle };
                var timedOut = false;
                while (true)
                {
                    var signaledIndex = WaitHandle.WaitAny(handles, NoActivityTimeout);
                    if (signaledIndex == WaitHandle.WaitTimeout)
                    {
                        timedOut = true;
                        break;
                    }
                    if (signaledIndex != 2)
                        break; // captureComplete or cancellationToken signaled

                    activitySignal.Reset();
                }

                if (timedOut)
                {
                    _logger.LogWarning(
                        "[Capture] STEP 6b: No transfer activity for source '{Source}' within {Timeout}. " +
                        "Likely no paper loaded or the driver is stuck on a UI it cannot show; aborting capture.",
                        deviceName, NoActivityTimeout);
                }

                _logger.LogInformation("[Capture] STEP 7: Capture wait ended (complete={Complete}, cancelled={Cancelled}).",
                    captureComplete.IsSet, cancellationToken.IsCancellationRequested);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unhandled exception while waiting for TWAIN capture for source '{Source}'", deviceName);
            }
            finally
            {
                // CRITICAL: always drive the TWAIN state machine back down so the physical
                // device is released. The SourceDisabled event only fires on a normal
                // end-of-job; on stop/cancel/timeout (or when TransferReady never fires) it
                // does NOT, leaving the source enabled+open and the scanner locked until the
                // driver is restarted. Explicitly disable and close here to guarantee release.
                try
                {
                    _logger.LogInformation("[Capture] STEP 8: Releasing TWAIN source/session (cleanup).");

                    // State 5 -> 4 -> 3: close the source. NTwain's Close() drives the required
                    // down-transitions (disable if still enabled, then close the source).
                    if (source != null && source.IsOpen)
                    {
                        try
                        {
                            var closeSrcRc = source.Close();
                            _logger.LogInformation("[Capture] STEP 8.1: Source '{Source}' closed: {RC}", source.Name, closeSrcRc);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning(ex, "[Capture] STEP 8.1: Failed to close source '{Source}'", source.Name);
                        }
                    }

                    // State 3 -> 2/1: close the DSM/session.
                    try
                    {
                        var closeRc = session.Close();
                        _logger.LogInformation("[Capture] STEP 8.2: Session closed: {RC}", closeRc);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "[Capture] STEP 8.2: Failed to close session");
                    }
                }
                finally
                {
                    // Unblock the consumer/await-foreach even if no page was ever produced.
                    captureComplete.Set();
                    _logger.LogInformation("[Capture] STEP 8 done: Cleanup complete, device should be released.");
                }
            }
        }

        /// <summary>
        /// Logs the process execution context that most often explains scan failures on
        /// customer machines: interactive vs. Session 0, the Windows session id, the
        /// identity the process runs under, process bitness, and how many interactive
        /// sessions exist. Desktop-bound TWAIN drivers (e.g. EPSON Scan 2) require an
        /// interactive session; this makes the "why" visible directly in the log.
        /// </summary>
        private void LogScanEnvironment(string deviceName, bool duplex)
        {
            try
            {
                int sessionId = Process.GetCurrentProcess().SessionId;
                string identity = "unknown";
                try { identity = WindowsIdentity.GetCurrent()?.Name ?? "unknown"; } catch { }

                _logger.LogInformation(
                    "Scan attempt starting. Device='{Device}', duplex={Duplex}. " +
                    "Environment: UserInteractive={Interactive}, SessionId={SessionId}, " +
                    "RunningAs='{Identity}', ProcessBitness={Bitness}, OS='{OS}'.",
                    deviceName,
                    duplex,
                    Environment.UserInteractive,
                    sessionId,
                    identity,
                    Environment.Is64BitProcess ? "x64" : "x86",
                    Environment.OSVersion.VersionString);

                if (!Environment.UserInteractive || sessionId == 0)
                {
                    _logger.LogWarning(
                        "Process is running in a non-interactive context (UserInteractive={Interactive}, SessionId={SessionId}). " +
                        "Desktop-bound scanner drivers such as EPSON Scan 2 may fail to open the device here.",
                        Environment.UserInteractive,
                        sessionId);
                }
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "Failed to log scan environment diagnostics");
            }
        }

        /// <summary>
        /// Fast memcpy of the TWAIN transfer raw data into a self-contained BMP byte array
        /// (BITMAPFILEHEADER + DIB). Returns null if the transfer had neither native data nor a file path.
        /// </summary>
        private byte[]? CaptureRawBmpBytes(IntPtr nativeData, string? fileDataPath)
        {
            _logger.LogInformation("[Capture] STEP R1: CaptureRawBmpBytes started (native={Native}, file='{File}').",
                nativeData != IntPtr.Zero, fileDataPath ?? "none");
            if (nativeData != IntPtr.Zero)
            {
                var ptr = PlatformInfo.Current.MemoryManager.Lock(nativeData);
                try
                {
                    var headerSize = (uint)Marshal.ReadInt32(ptr, 0);
                    var width = Marshal.ReadInt32(ptr, 4);
                    var height = Marshal.ReadInt32(ptr, 8);
                    var planes = (ushort)Marshal.ReadInt16(ptr, 12);
                    var bitCount = (ushort)Marshal.ReadInt16(ptr, 14);
                    var compression = (uint)Marshal.ReadInt32(ptr, 16);
                    var imageSize = (uint)Marshal.ReadInt32(ptr, 20);
                    var colorsUsed = (uint)Marshal.ReadInt32(ptr, 32);

                    const uint biRgb = 0;
                    const uint biBitfields = 3;
                    var validBitCount = bitCount is 1 or 4 or 8 or 24 or 32;
                    var validDimensions = width > 0 && width <= 10000 &&
                        height != 0 && Math.Abs((long)height) <= 10000;

                    if (headerSize < 40 || headerSize > 124 ||
                        planes != 1 || !validBitCount || !validDimensions ||
                        (compression != biRgb && compression != biBitfields))
                    {
                        _logger.LogError(
                            "Invalid TWAIN DIB header: HeaderSize={HeaderSize}, Width={Width}, " +
                            "Height={Height}, Planes={Planes}, Bpp={Bpp}, Compression={Compression}, " +
                            "ImageSize={ImageSize}, ColorsUsed={ColorsUsed}",
                            headerSize, width, height, planes, bitCount, compression, imageSize, colorsUsed);
                        return null;
                    }

                    var colorTableEntries = bitCount <= 8
                        ? (colorsUsed == 0 ? 1U << bitCount : Math.Min(colorsUsed, 1U << bitCount))
                        : 0;
                    var maskBytes = compression == biBitfields && headerSize == 40 ? 12U : 0U;

                    try
                    {
                        var stride = checked((((long)width * bitCount) + 31) / 32 * 4);
                        var expectedPixelBytes = checked(Math.Abs((long)height) * stride);

                        // BI_RGB and BI_BITFIELDS are uncompressed here. Do not trust a
                        // driver-reported image size to determine how much memory to copy.
                        if (imageSize != 0 && imageSize < expectedPixelBytes)
                        {
                            _logger.LogError(
                                "Truncated TWAIN DIB: ReportedImageSize={ImageSize}, ExpectedPixelBytes={ExpectedBytes}",
                                imageSize, expectedPixelBytes);
                            return null;
                        }

                        var dibSize = checked((int)(headerSize + maskBytes +
                            (colorTableEntries * 4L) + expectedPixelBytes));

                        _logger.LogInformation(
                            "TWAIN native DIB validated: {Width}x{Height} {Bpp}bpp, " +
                            "Compression={Compression}, HeaderSize={HeaderSize}, ImageSize={ImageSize}, " +
                            "ExpectedBytes={ExpectedBytes}, CopyBytes={CopyBytes}",
                            width, height, bitCount, compression, headerSize, imageSize,
                            expectedPixelBytes, dibSize);

                        const int fileHeaderSize = 14;
                        var bmpBytes = new byte[checked(fileHeaderSize + dibSize)];
                        bmpBytes[0] = (byte)'B'; bmpBytes[1] = (byte)'M';
                        BitConverter.GetBytes((uint)bmpBytes.Length).CopyTo(bmpBytes, 2);
                        BitConverter.GetBytes(checked(fileHeaderSize + (int)(headerSize + maskBytes + colorTableEntries * 4L))).CopyTo(bmpBytes, 10);
                        Marshal.Copy(ptr, bmpBytes, fileHeaderSize, dibSize);
                        _logger.LogInformation("Captured page: {W}x{H} {Bpp}bpp {Size} bytes",
                            width, height, bitCount, bmpBytes.Length);
                        return bmpBytes;
                    }
                    catch (OverflowException ex)
                    {
                        _logger.LogError(ex, "TWAIN image dimensions overflowed while calculating the BMP size.");
                        return null;
                    }
                }
                finally
                {
                    PlatformInfo.Current.MemoryManager.Unlock(nativeData);
                }
            }
            if (fileDataPath != null && File.Exists(fileDataPath))
            {
                var bytes = File.ReadAllBytes(fileDataPath);
                _logger.LogInformation("Captured page from file: {Path}", fileDataPath);
                return bytes;
            }
            _logger.LogWarning("[Capture] STEP R2: CaptureRawBmpBytes found no native data and no file — returning null.");
            return null;
        }

        private void ConfigureCapabilities(DataSource source, bool duplex)
        {
            _logger.LogInformation("[Config] STEP CFG1: Configuring capabilities for '{Source}' (duplex={Duplex}).", source.Name, duplex);
            try
            {
                var pixelTypeCap = source.Capabilities.ICapPixelType;
                if (pixelTypeCap.IsSupported && pixelTypeCap.CanSet)
                {
                    var requestedPixelType = _scannerSettings.PixelType switch
                    {
                        0 => PixelType.BlackWhite,
                        1 => PixelType.Gray,
                        2 => PixelType.RGB,
                        _ => PixelType.BlackWhite
                    };

                    if (_scannerSettings.PixelType is < 0 or > 2)
                    {
                        _logger.LogWarning(
                            "Unsupported PixelType setting {PixelType}; falling back to BlackWhite.",
                            _scannerSettings.PixelType);
                    }

                    var rc = pixelTypeCap.SetValue(requestedPixelType);
                    _logger.LogInformation("Set PixelType to {PixelType}: {RC}", requestedPixelType, rc);
                }
                else
                {
                    _logger.LogWarning("Scanner does not support setting pixel type.");
                }

                TWFix32 targetRes = _scannerSettings.Dpi;

                var xResCap = source.Capabilities.ICapXResolution;
                if (xResCap.IsSupported && xResCap.CanSet)
                {
                    var rcX = xResCap.SetValue(targetRes);
                    _logger.LogInformation("Set XResolution to {Dpi} DPI: {RC}", _scannerSettings.Dpi, rcX);
                }

                var yResCap = source.Capabilities.ICapYResolution;
                if (yResCap.IsSupported && yResCap.CanSet)
                {
                    var rcY = yResCap.SetValue(targetRes);
                    _logger.LogInformation("Set YResolution to {Dpi} DPI: {RC}", _scannerSettings.Dpi, rcY);
                }

                var pageSizeCap = source.Capabilities.ICapSupportedSizes;
                if (pageSizeCap.IsSupported && pageSizeCap.CanSet)
                {
                    if (Enum.TryParse<SupportedSize>(_scannerSettings.PageSize, true, out var pageSize))
                    {
                        var rcPageSize = pageSizeCap.SetValue(pageSize);
                        _logger.LogInformation("Set page size to {PageSize}: {RC}", _scannerSettings.PageSize, rcPageSize);
                    }
                    else
                    {
                        _logger.LogWarning("Configured scanner page size '{PageSize}' is not a valid TWAIN supported size.", _scannerSettings.PageSize);
                    }
                }
                else
                {
                    _logger.LogInformation("Scanner does not support setting page size.");
                }

                // Duplex toggle: when enabled the scanner captures both sides of each sheet;
                // when disabled (default) only the front side is scanned.
                var duplexCap = source.Capabilities.CapDuplexEnabled;
                if (duplexCap.IsSupported && duplexCap.CanSet)
                {
                    var rcDuplex = duplexCap.SetValue(duplex ? BoolType.True : BoolType.False);
                    _logger.LogInformation("Set DuplexEnabled to {Value}: {RC}", duplex, rcDuplex);
                }


                _logger.LogInformation("[Config] STEP CFG2: Capabilities configured for '{Source}'.", source.Name);
            }
            catch (Exception ex) { _logger.LogWarning(ex, "Could not configure capabilities"); }
        }
    }
}
