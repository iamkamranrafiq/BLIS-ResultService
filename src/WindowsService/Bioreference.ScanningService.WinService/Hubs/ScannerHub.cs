using System.Drawing;
using System.Drawing.Imaging;
using Bioreference.ScanningService.Application.Common.Constants;
using Bioreference.ScanningService.Application.Common.Entity;
using Bioreference.ScanningService.Application.Common.Interfaces;
using Bioreference.ScanningService.Application.DTOs.Requests;
using Bioreference.ScanningService.Application.DTOs.Responses;
using Bioreference.ScanningService.Application.Services.Implementations;
using Bioreference.ScanningService.Application.Services.Interfaces;
using Bioreference.ScanningService.Domain.Entities;
using Bioreference.ScanningService.WinService.Services.Interfaces;
using Bioreference.ScanningService.WinService.Settings;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Bioreference.ScanningService.WinService.Hubs;

/// <summary>
/// SignalR hub for real-time scanner status notifications.
/// Clients connect at /hubs/scanner.
/// </summary>
[Authorize]
public class ScannerHub : Hub
{
    private readonly ILogger<ScannerHub> _logger;
    private readonly IScannerService _scannerService;
    private readonly IBatchService _batchService;
    private readonly IDocumentService _documentService;
    private readonly BarcodeService _barcodeService;
    private readonly ScannerSettings _scannerSettings;
    private readonly TestSettings _testSettings;
    private readonly IFileStorage _fileStorage;
    private readonly IServiceScopeFactory _serviceScopeFactory;

    /// <summary>
    /// Tracks in-progress scan sessions keyed by batch id so they can be paused,
    /// resumed, or stopped from separate hub invocations.
    /// </summary>
    private static readonly System.Collections.Concurrent.ConcurrentDictionary<int, ScanControl> _scanControls = new();

    /// <summary>
    /// Holds the pause gate and cancellation source for a single running scan session.
    /// </summary>
    private sealed class ScanControl : IDisposable
    {
        private readonly object _sync = new();
        // When null, the scan is running. When non-null, the scan is paused and callers
        // await this task; it completes when the scan is resumed.
        private TaskCompletionSource<bool>? _pauseTcs;

        public CancellationTokenSource CancellationSource { get; } = new();

        // Set to true when the scan is explicitly stopped (as opposed to a normal
        // finish or a client disconnect). When stopped, all post-scan processing
        // (document creation, completion notifications) is skipped.
        public bool Stopped { get; set; }

        /// <summary>Marks the scan as paused so the loop will await resumption.</summary>
        public void Pause()
        {
            lock (_sync)
            {
                _pauseTcs ??= new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            }
        }

        /// <summary>Resumes a paused scan, releasing any awaiter.</summary>
        public void Resume()
        {
            lock (_sync)
            {
                _pauseTcs?.TrySetResult(true);
                _pauseTcs = null;
            }
        }

        /// <summary>
        /// Asynchronously waits while the scan is paused. Returns immediately when running.
        /// Throws <see cref="OperationCanceledException"/> if cancelled (e.g. stopped/disconnected).
        /// </summary>
        public async Task WaitIfPausedAsync(CancellationToken cancellationToken)
        {
            Task pauseTask;
            lock (_sync)
            {
                if (_pauseTcs is null)
                {
                    return;
                }
                pauseTask = _pauseTcs.Task;
            }

            // Await either resume (pauseTask completes) or cancellation.
            var cancelTcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            using (cancellationToken.Register(static s => ((TaskCompletionSource<bool>)s!).TrySetResult(true), cancelTcs))
            {
                await Task.WhenAny(pauseTask, cancelTcs.Task);
            }

            cancellationToken.ThrowIfCancellationRequested();
        }

        public void Dispose()
        {
            // Release any awaiter so it doesn't hang forever.
            Resume();
            CancellationSource.Dispose();
        }
    }

    public ScannerHub(
        ILogger<ScannerHub> logger,
        IScannerService scanner,
        IBatchService batchService,
        IDocumentService documentService,
        BarcodeService barcodeService,
        IOptions<ScannerSettings> scannerSettings,
        IOptions<TestSettings> testSettings,
        IFileStorage fileStorage,
        IServiceScopeFactory serviceScopeFactory)
    {
        _logger = logger;
        _scannerService = scanner;
        _batchService = batchService;
        _documentService = documentService;
        _barcodeService = barcodeService;
        _scannerSettings = scannerSettings.Value;
        _testSettings = testSettings.Value;
        _fileStorage = fileStorage;
        _serviceScopeFactory = serviceScopeFactory;

    }

    public override async Task OnConnectedAsync()
    {
        _logger.LogInformation("ScannerHub client connected: {ConnectionId}", Context.ConnectionId);
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        _logger.LogInformation("ScannerHub client disconnected: {ConnectionId}", Context.ConnectionId);
        await base.OnDisconnectedAsync(exception);
    }

    /// <summary>
    /// Client joins a group to receive updates scoped to a specific batch.
    /// </summary>
    public async Task JoinBatchGroup(string batchId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, $"batch-{batchId}");
        _logger.LogInformation("Connection {ConnectionId} joined batch group {BatchId}", Context.ConnectionId, batchId);
    }

    /// <summary>
    /// Client leaves a batch-specific group.
    /// </summary>
    public async Task LeaveBatchGroup(string batchId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"batch-{batchId}");
        _logger.LogInformation("Connection {ConnectionId} left batch group {BatchId}", Context.ConnectionId, batchId);
    }

    /// <summary>
    /// Returns the name of the currently connected/active scanner to the calling client.
    /// Resolves the configured device when connected, otherwise the first available scanner.
    /// Also pushes a "ScannerName" event to the caller for real-time UI updates.
    /// </summary>
    [HubMethodName("GetActiveScannerName")]
    public async Task<string?> GetActiveScannerName()
    {
        var scannerName = _scannerService.GetActiveScannerName(_scannerSettings.DeviceName);

        if (string.IsNullOrEmpty(scannerName))
        {
            _logger.LogWarning("No active scanner detected for connection {ConnectionId}", Context.ConnectionId);
        }
        else
        {
            _logger.LogInformation("Active scanner '{ScannerName}' reported to connection {ConnectionId}",
                scannerName, Context.ConnectionId);
        }

        await Clients.Caller.SendAsync("ScannerName", scannerName);
        return scannerName;
    }
    /// <summary>
    /// Starts a scan session. If <paramref name="batchId"/> is provided the scan is added to
    /// that existing batch; otherwise a new batch is created from <paramref name="batchRequest"/>.
    /// Collects all scanned pages, then persists a Document with its DocumentPages in one call.
    /// </summary>
    [HubMethodName("StartScan")]
    public async Task StartScan(int DocumentTypeId = 1, int? batchId = null)
    {
        await StartScanProcess(new StartScanRequest() { DocumentTypeId = DocumentTypeId, BatchId = batchId });
    }

    [HubMethodName("StartScanProcess")]
    public async Task StartScanProcess(StartScanRequest request)
    {
        if (request.KeywordValue == null)
        {
            request.KeywordValue = new List<keywordValue>();
        }

        // Resolve the acting user once at the entry point and reuse it throughout the scan workflow.
        var userId = Context.UserIdentifier ?? "system";

        // Diagnostic: log the incoming request so we can see whether a BatchId was supplied. When
        // BatchId is null the existing-batch start-stamp branch is skipped and StartedDate must be
        // set via the new-batch creation path below. This helps explain a NULL StartedDate.
        _logger.LogInformation(
            "[Scan][StartedDate] StartScanProcess entry - HasBatchId={HasBatchId}, BatchId={BatchId}, DocumentTypeId={DocumentTypeId}, User={User}.",
            request.BatchId.HasValue, request.BatchId, request.DocumentTypeId, userId);

        // Persist the scan start time at the very start of the scan. For an existing batch we can
        // stamp it immediately; new batches are created with StartedDate set below. MarkBatchStartedAsync
        // only sets StartedDate the first time, so resuming keeps the original start time.
        if (request.BatchId.HasValue)
        {
            _logger.LogInformation("[Scan][StartedDate] Existing batch path - marking batch {BatchId} started.", request.BatchId.Value);

            var started = await _batchService.MarkBatchStartedAsync(request.BatchId.Value, userId);

            // The result was previously ignored. Log failures explicitly so a missing StartedDate
            // (e.g. batch not found or a persistence error) is visible instead of being swallowed.
            if (!started)
            {
                _logger.LogWarning(
                    "[Scan][StartedDate] MarkBatchStartedAsync returned false for batch {BatchId}; StartedDate may remain NULL.",
                    request.BatchId.Value);
            }
            else
            {
                _logger.LogInformation("[Scan][StartedDate] Batch {BatchId} StartedDate stamped successfully.", request.BatchId.Value);
            }
        }
        else
        {
            _logger.LogInformation("[Scan][StartedDate] No BatchId supplied - StartedDate will be set on the new batch during creation.");
        }

        _logger.LogInformation("StartScan invoked: batchId={BatchId} device={Device} duplex={Duplex}",
            request.BatchId, _scannerSettings.DeviceName, _scannerSettings.Duplex);

        _logger.LogInformation("[Scan] STEP 1: Resolving barcode keywords for DocumentTypeId={DocumentTypeId}.", request.DocumentTypeId);
        BatchDto? batch;
        var barcodeKeywords = await _documentService.GetDocumentTypesBarcodeKeywordAsync(request.DocumentTypeId);
        _logger.LogInformation("[Scan] STEP 1 done: retrieved {Count} barcode keyword(s).", barcodeKeywords?.Count ?? 0);

        _logger.LogInformation("[Scan] STEP 2: Resolving batch (BatchId={BatchId}).", request.BatchId);
        if (request.BatchId.HasValue)
        {
            batch = await _batchService.GetBatchByIdAsync(request.BatchId.Value);
            if (batch is null)
            {
                await Clients.Caller.SendAsync("Error", $"Batch {request.BatchId.Value} not found.");
                return;
            }
            _logger.LogInformation("Using existing batch: id={BatchId} number={BatchNumber}", batch.BatchId, batch.BatchNumber);
        }
        else
        {
            SaveBatchRequest? batchRequest = new SaveBatchRequest() { StartedDate = DateTime.UtcNow };

            _logger.LogInformation("[Scan][StartedDate] Creating new batch with StartedDate={StartedDate}.", batchRequest.StartedDate);

            var saveResponse = await _batchService.SaveBatchAsync(batchRequest);
            if (!saveResponse.Success || saveResponse.Data is null)
            {
                await Clients.Caller.SendAsync("Error", saveResponse.Message);
                return;
            }

            batch = saveResponse.Data;
            _logger.LogInformation("Batch created: id={BatchId} number={BatchNumber}", batch.BatchId, batch.BatchNumber);
            _logger.LogInformation("[Scan][StartedDate] New batch {BatchId} persisted StartedDate={StartedDate}.", batch.BatchId, batch.StartedDate);
        }

        _logger.LogInformation("[Scan] STEP 3: Resolving document type (DocumentTypeId={DocumentTypeId}).", request.DocumentTypeId);
        var documentType = await _documentService.GetDocumentTypeAsync(request.DocumentTypeId);
        _logger.LogInformation("[Scan] STEP 3 done: DocumentType Id={Id}, Name='{Name}'.", documentType?.DocumentTypeId, documentType?.DisplayName);

        _logger.LogInformation("[Scan] STEP 4: Adding connection {ConnectionId} to batch group {BatchId}.", Context.ConnectionId, batch.BatchId);
        await Groups.AddToGroupAsync(Context.ConnectionId, BatchGroup(batch.BatchId));

        // Log that scanning has started for this batch.
        var scanModeLabel = string.IsNullOrWhiteSpace(batch.ScanMode) ? AppConstants.DefaultScanModeLabel : batch.ScanMode;
        await _batchService.LogActivityAsync(batch.BatchId, Context.UserIdentifier ?? "system", "Info",
            $"Starting scan - Batch #{batch.BatchId} [{scanModeLabel}].");

        // Fetch document short names for the batch so they can be emitted alongside scanned pages.
        _logger.LogInformation("[Scan] STEP 5: Fetching document short names for batch {BatchId}.", batch.BatchId);
        var shortNamesResponse = await _documentService.GetDocumentShortNamesByBatchIdAsync(batch.BatchId);
        var shortNames = shortNamesResponse.Success ? shortNamesResponse.Documents : new();

        var pages = new List<byte[]>();
        var group = new List<DetectedImageBarcodes>();

        // Register a scan-control entry so this session can be paused/resumed/stopped
        // from separate hub invocations (PauseScanProcess/ResumeScanProcess/StopScanProcess).
        var scanControl = new ScanControl();
        _scanControls[batch.BatchId] = scanControl;

        // Combine the client's connection-abort token with the stop token so either can cancel.
        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(
            Context.ConnectionAborted, scanControl.CancellationSource.Token);
        var scanToken = linkedCts.Token;

        // Tracks whether the scan was explicitly stopped so post-scan processing can be skipped.
        var stopped = false;

        // Determine whether this batch is a multi-page scan and track page-level barcode errors.
        var isMultiPage = !string.IsNullOrWhiteSpace(batch.ScanMode) &&
            batch.ScanMode.Contains("Multi", StringComparison.OrdinalIgnoreCase);
        var hasErrors = false;

        _logger.LogInformation("[Scan] STEP 6: Beginning acquisition loop. TestMode={TestMode}, MultiPage={MultiPage}, Device='{Device}', Duplex={Duplex}.",
            _testSettings.Mode, isMultiPage, _scannerSettings.DeviceName, _scannerSettings.Duplex);

        try
        {

            if (_testSettings.Mode == true)
            {
                _logger.LogInformation("[Scan] STEP 6a: TEST MODE - loading test images.");
                var testImages = await GetTestImages();
                _logger.LogInformation("[Scan] STEP 6a: {Count} test image(s) loaded.", testImages.Count);
                foreach (var imageBytes in testImages)
                {
                    // Await here while the scan is paused; throws if stopped/disconnected.
                    await scanControl.WaitIfPausedAsync(scanToken);
                    hasErrors |= await ProcessScannedPageAsync(imageBytes, pages, group, barcodeKeywords, documentType, batch.BatchId, shortNames, request.KeywordValue, isMultiPage, userId);
                }
            }
            else
            {
                _logger.LogInformation("[Scan] STEP 6b: LIVE MODE - awaiting images from scanner '{Device}'.", _scannerSettings.DeviceName);
                var filescannedlist = new List<byte[]>();
                int counter = 0;
                await foreach (var imageBytes in _scannerService.AcquireImagesAsync(_scannerSettings.DeviceName, _scannerSettings.Duplex, scanToken))
                {
                    var bitmapBytes = Array.Empty<byte>();
                    counter++;

                    _logger.LogInformation("[Scan] STEP 6b: Writing scanned {Counter} image to temporary storage.", counter);
                    try
                    {

                        //using var bitmap = new Bitmap(new MemoryStream(imageBytes));
                        _logger.LogInformation("[Scan] STEP 6b: Bitmap size before saving to temporary storage={Size} KB.", imageBytes.Length / 1024);

                        using var sourceStream = new MemoryStream(imageBytes, writable: false);
                        using var source = new Bitmap(sourceStream);


                        // 50% resize for temp storage / preview
                        using var resized = new Bitmap(source.Width / 2, source.Height / 2, PixelFormat.Format24bppRgb);
                        using var g = Graphics.FromImage(resized);
                        g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
                        g.DrawImage(source, new Rectangle(0, 0, resized.Width, resized.Height));

                        using var ms = new MemoryStream();
                        resized.Save(ms, System.Drawing.Imaging.ImageFormat.Jpeg);
                        bitmapBytes = ms.ToArray();

                        //using var mssource = new MemoryStream();
                        //source.Save(mssource, System.Drawing.Imaging.ImageFormat.Bmp);
                        //var sourceImageBytes = mssource.ToArray();



                        _logger.LogInformation("[Scan] STEP 6b: Bitmap saved to temporary storage, size={Size} KB.", bitmapBytes.Length / 1024);

                        filescannedlist.Add(imageBytes);
                        await Clients.Caller.SendAsync("PageScanned", new
                        {
                            PageNumber = filescannedlist.Count,
                            ImageBase64 = Convert.ToBase64String(bitmapBytes),
                            BatchId = batch.BatchId,
                            ShortNames = "",
                            ShortName = ""
                        });
                    }
                    catch (Exception ex)
                    {

                        _logger.LogError(ex, "Failed to bitmap.Save scanned {Counter} image to temporary storage.", counter);
                    }
                }
                await Clients.Caller.SendAsync("Scannerlogs", new
                {
                    PageNumber = pages.Count,
                    Message = "Indexing Started",
                });
                _logger.LogInformation("[Scan] STEP 6b: {Count} image(s) received from scanner '{Device}'.", filescannedlist.Count, _scannerSettings.DeviceName);
                foreach (var imageBytes in filescannedlist)
                {
                    _logger.LogInformation("[Scan] STEP 6b: Received image of {Bytes} byte(s) from scanner (page {Page}).", imageBytes?.Length ?? 0, pages.Count + 1);
                    // Await here while the scan is paused; throws if stopped/disconnected.
                    await scanControl.WaitIfPausedAsync(scanToken);
                    hasErrors |= await ProcessScannedPageAsync(imageBytes, pages, group, barcodeKeywords, documentType, batch.BatchId, shortNames, request.KeywordValue, isMultiPage, userId);
                }
                _logger.LogInformation("[Scan] STEP 6b done: acquisition loop ended. Total pages received={Pages}.", pages.Count);
            }

        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Scan cancelled for batch {BatchId} after {Pages} page(s)", batch.BatchId, pages.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "TWAIN acquisition failed for batch {BatchId}", batch.BatchId);

            // Store the scanning-workflow failure in the activity log.
            await _batchService.LogActivityAsync(batch.BatchId, Context.UserIdentifier ?? "system", "Error",
                $"Scanner failed: {ex.Message}", pagesScanned: pages.Count);

            await Clients.Caller.SendAsync("Error", $"Scanner failed: {ex.Message}");
            return;
        }
        finally
        {
            // Remove and dispose the control entry once acquisition has ended.
            if (_scanControls.TryRemove(batch.BatchId, out var finishedControl))
            {
                stopped = finishedControl.Stopped;
                finishedControl.Dispose();
            }
        }

        // If the scan was explicitly stopped, abort all remaining processing:
        // no barcode grouping, no document creation, no completion notification.
        if (stopped)
        {
            _logger.LogInformation("Scan stopped for batch {BatchId}; skipping document creation.", batch.BatchId);
            return;
        }


        if (pages.Count == 0)
        {
            _logger.LogWarning("[Scan] STEP 7: No images captured from scanner for batch {BatchId}; aborting.", batch.BatchId);
            // No images captured from the scanner - store this in the activity log.
            await _batchService.LogActivityAsync(batch.BatchId, userId, "Error",
                "No images captured from scanner.");

            await Clients.Caller.SendAsync("Error", "No images captured from scanner1");
            return;
        }

        _logger.LogInformation("[Scan] STEP 7: Acquisition complete. {Pages} page(s) captured for batch {BatchId}.", pages.Count, batch.BatchId);

        //DocumentTypeKeyword
        //reading barcodes and grouping  barcodes  
        // TODO: Re-enable barcode-based grouping later. For now create a single document with all scanned pages.
        _logger.LogInformation("[Scan] STEP 8: Grouping {PageCount} page(s) into documents (SinglePageDocument={SinglePage}).", group.Count, request.SinglePageDocument);

        List<List<DetectedImageBarcodes>> groupedimages = new List<List<DetectedImageBarcodes>>();
        if (!request.SinglePageDocument)
            groupedimages = _documentService.GroupImagesByBarcode(group);
        else
        {
            foreach (var item in group)
            {
                var x = new List<DetectedImageBarcodes>();
                x.Add(item);
                groupedimages.Add(x);
            }
        }

        _logger.LogInformation("[Scan] STEP 8 done: {DocumentCount} document group(s) formed.", groupedimages.Count);
        await Clients.Caller.SendAsync("Scannerlogs", new
        {
            PageNumber = pages.Count,
            Message = "Documents Saving Started",
        });
        var documentId = 0;
        var createdDocuments = new List<object>();
        _logger.LogInformation("[Scan] STEP 9: Creating documents for batch {BatchId}.", batch.BatchId);
        var docseq = 0;
        var documentCreationTasks = groupedimages.Select(async (groupimage, index) =>
        {
            await using var scope = _serviceScopeFactory.CreateAsyncScope();
            var documentService = scope.ServiceProvider.GetRequiredService<IDocumentService>();

            _logger.LogInformation("[Scan] STEP 9: Creating document with {Pages} page(s).", groupimage.Count);
            // Persist the captured document independently of the SignalR connection lifetime.
            // Using Context.ConnectionAborted here would cancel the save if the client's
            // connection dropped during/after scanning, losing already-scanned pages.

            var createResult = await documentService.CreateDocumentWithPagesAsync(batch.BatchId, documentType, groupimage, request.KeywordValue, CancellationToken.None);
            var messagebarcodenotdetected = "(Barcode Not Detected)";
            await Clients.Caller.SendAsync("Scannerlogs", new
            {
                PageNumber = pages.Count,
                Message = $"Document Saved {(groupimage.Any(d => d.DetectedBarcodes.Count > 0) ? createResult.ShortName : messagebarcodenotdetected)}"
            });
            _logger.LogInformation("[Scan] STEP 9: Document {DocumentId} created.", createResult.DocumentId);
            return new
            {
                DocumentId = (int)createResult.DocumentId,
                ShortName = createResult.ShortName
            };
        }).ToArray();

        var createdDocumentResults = await Task.WhenAll(documentCreationTasks);
        createdDocuments.AddRange(createdDocumentResults);
        documentId = createdDocumentResults.LastOrDefault()?.DocumentId ?? 0;
        _logger.LogInformation("[Scan] STEP 9 done: {Count} document(s) created for batch {BatchId}.", createdDocuments.Count, batch.BatchId);

        //var documentId = (int)await _documentService.CreateDocumentWithPagesAsync(batch.BatchId, documentType, group, request.KeywordValue, Context.ConnectionAborted);


        _logger.LogInformation("Document {DocumentId} saved with {PageCount} page(s) for batch {BatchId}", documentId, pages.Count, batch.BatchId);

        // Record the batch completion time now that scanning and document creation are done.
        _logger.LogInformation("[Scan] STEP 10: Marking batch {BatchId} completed.", batch.BatchId);
        await _batchService.MarkBatchCompletedAsync(batch.BatchId);

        // Log that scanning has finished, reflecting whether any page-level errors occurred,
        // along with a summary of pages scanned and documents saved.
        await _batchService.LogActivityAsync(batch.BatchId, userId, hasErrors ? "Error" : "Info",
            $"{(hasErrors ? "Finished scanning with errors, please review." : "Finished scanning.")}");
        await _batchService.LogActivityAsync(batch.BatchId, userId, "Info",
            $"Pages scanned: {pages.Count}; Documents saved: {createdDocuments.Count}.",
            documentsScanned: createdDocuments.Count, pagesScanned: pages.Count);

        var scanCompletedPayload = new
        {
            batch.BatchId,
            DocumentId = documentId,
            TotalPages = pages.Count,
            Documents = createdDocuments
        };

        await _batchService.UpdateBatchCountsAsync(batch.BatchId);

        // If the batch's scan queue is configured for auto-commit and no errors occurred during
        // scanning, commit the batch automatically. Auto-commit is skipped when errors are present.
        if (!hasErrors)
        {
            var autoCommitted = await _batchService.TryAutoCommitBatchAsync(batch.BatchId, userId);
            if (autoCommitted)
            {
                _logger.LogInformation("[Scan] Batch {BatchId} auto-committed (scan queue IsAutoCommit=true, no errors).", batch.BatchId);
                await _batchService.LogActivityAsync(batch.BatchId, userId, "Info", "Batch auto-committed (no errors).");
            }
        }
        else
        {
            _logger.LogInformation("[Scan] Skipping auto-commit for batch {BatchId} because errors were detected.", batch.BatchId);
        }

        // Notify the caller directly (reliable even if the connection auto-reconnected and lost
        // its group membership during the long-running scan) and the batch group (for any other
        // observers scoped to this batch).
        _logger.LogInformation("[Scan] STEP 11: Sending ScanCompleted for batch {BatchId} (Documents={Docs}, Pages={Pages}).", batch.BatchId, createdDocuments.Count, pages.Count);
        await Clients.Caller.SendAsync("ScanCompleted", scanCompletedPayload);
        await Clients.Group(BatchGroup(batch.BatchId)).SendAsync("ScanCompleted", scanCompletedPayload);
        _logger.LogInformation("[Scan] STEP 11 done: scan process finished for batch {BatchId}.", batch.BatchId);
    }

    /// <summary>
    /// Pauses an in-progress scan for the given batch. Acquisition blocks before the next page
    /// until <see cref="ResumeScanProcess"/> or <see cref="StopScanProcess"/> is called.
    /// </summary>
    [HubMethodName("PauseScanProcess")]
    public async Task PauseScanProcess(int batchId)
    {
        _logger.LogInformation("PauseScanProcess invoked for batch {BatchId}", batchId);

        if (!_scanControls.TryGetValue(batchId, out var control))
        {
            await Clients.Caller.SendAsync("Error", $"No active scan found for batch {batchId}.");
            return;
        }

        // Mark the scan as paused so the loop awaits before processing the next page.
        control.Pause();

        await Clients.Caller.SendAsync("ScanPaused", new { BatchId = batchId });
        await Clients.Group(BatchGroup(batchId)).SendAsync("ScanPaused", new { BatchId = batchId });
    }

    /// <summary>
    /// Resumes a previously paused scan for the given batch.
    /// </summary>
    [HubMethodName("ResumeScanProcess")]
    public async Task ResumeScanProcess(int batchId)
    {
        _logger.LogInformation("ResumeScanProcess invoked for batch {BatchId}", batchId);

        if (!_scanControls.TryGetValue(batchId, out var control))
        {
            await Clients.Caller.SendAsync("Error", $"No active scan found for batch {batchId}.");
            return;
        }

        // Resume the loop so it continues processing pages from where it paused.
        control.Resume();

        await Clients.Caller.SendAsync("ScanResumed", new { BatchId = batchId });
        await Clients.Group(BatchGroup(batchId)).SendAsync("ScanResumed", new { BatchId = batchId });
    }

    /// <summary>
    /// Stops an in-progress scan for the given batch by cancelling acquisition. Any pages
    /// captured before the stop are still persisted by the running <see cref="StartScanProcess"/> call.
    /// </summary>
    [HubMethodName("StopScanProcess")]
    public async Task StopScanProcess(int batchId)
    {
        _logger.LogInformation("StopScanProcess invoked for batch {BatchId}", batchId);

        if (!_scanControls.TryGetValue(batchId, out var control))
        {
            await Clients.Caller.SendAsync("Error", $"No active scan found for batch {batchId}.");
            return;
        }

        // Mark the scan as explicitly stopped so post-scan processing (document creation and
        // completion notification) is skipped. Ensure a paused scan can observe the cancellation,
        // then cancel acquisition.
        control.Stopped = true;
        control.Resume();
        control.CancellationSource.Cancel();

        await Clients.Caller.SendAsync("ScanStopped", new { BatchId = batchId });
        await Clients.Group(BatchGroup(batchId)).SendAsync("ScanStopped", new { BatchId = batchId });
    }

    /// <summary>
    /// Processes a single scanned page: reads its barcodes, generates the document short name
    /// from the detected barcode values, and emits a PageScanned event with the image and short name.
    /// Returns true if reading the page's barcode failed (single-page mode only).
    /// </summary>
    private async Task<bool> ProcessScannedPageAsync(
        byte[] imageBytes,
        List<byte[]> pages,
        List<DetectedImageBarcodes> group,
        List<BarCodeFormatDocumentTypeKeyword> barcodeKeywords,
        DocumentType documentType,
        int batchId,
        object shortNames,
        List<keywordValue> requestKeywordValues,
        bool isMultiPage,
        string userId)
    {
        pages.Add(imageBytes);
        var pageNumber = pages.Count;
        _logger.LogInformation("[Page {Page}] Processing scanned page ({Bytes} bytes) for batch {BatchId}.", pageNumber, imageBytes?.Length ?? 0, batchId);

        await Clients.Caller.SendAsync("Status", "Reading Barcode page " + pages.Count);

        // Detect barcodes on this page as it is scanned.
        _logger.LogInformation("[Page {Page}] Detecting barcodes.", pageNumber);
        var codes = await _barcodeService.ProcessImageFromDataAsync(imageBytes);
        _logger.LogInformation("[Page {Page}] Barcode detection returned {Count} raw code(s).", pageNumber, codes?.Count ?? 0);
        var detectedBarcodes = barcodeKeywords != null
            ? codes.Select(code => _documentService.MapBarcodeToBarcodeKeywordDoctype(barcodeKeywords, code))
                   .Where(b => b != null)
                   .ToList()
            : new List<BarcodeKeywordDoctype>();

        group.Add(new DetectedImageBarcodes
        {
            ImageBytes = imageBytes,
            DetectedBarcodes = detectedBarcodes
        });

        // Write a per-page activity log describing what was read from the page.
        var pageHadError = false;
        var logactivitystring = "";
        if (detectedBarcodes.Count > 0)
        {
            var firstBarcode = detectedBarcodes[0];

            logactivitystring = $"Page {pageNumber} scanned - Accession I.D: {firstBarcode.Value}.";
            await _batchService.LogActivityAsync(batchId, userId, "Info",
                logactivitystring,
                pagesScanned: 1);
        }
        else if (isMultiPage)
        {
            // In multi-page mode, a page without a barcode is a continuation page (not an error).
            logactivitystring = $"Page {pageNumber} Indexed.";
            await _batchService.LogActivityAsync(batchId, userId, "Info",
                logactivitystring,
                pagesScanned: 1);
        }
        else
        {
            // A page without a barcode is treated as an error in both single-page and multi-page
            // modes so the missing barcode is surfaced in the activity log for review.
            pageHadError = true;
            logactivitystring = $"Page {pageNumber} Indexed - Error reading barcode.";
            await _batchService.LogActivityAsync(batchId, userId, "Error",
                logactivitystring,
                pagesScanned: 1);
        }

        // Build keyword values from detected barcodes plus any request-provided values,
        // then generate the document short name for this page.
        var keywordValues = detectedBarcodes.Select(b => new keywordValue(b.KeywordId, b.Value)).ToList();
        keywordValues.AddRange(requestKeywordValues);
        var documentShortName = _documentService.GetDocumentName(batchId, documentType, keywordValues).ShortName.Replace("%", "-");

        // Compress the raw scanned image (often ~8-9MB uncompressed BMP) to a small JPEG
        // before base64-encoding. Sending the raw bytes over SignalR produces an ~11MB+
        // string that overflows the message buffer and causes an OutOfMemoryException.

        //var previewBytes = _fileStorage.CompressForPreview(imageBytes);
        //var base64Image = Convert.ToBase64String(previewBytes);
        //_logger.LogInformation("[Page {Page}] Emitting PageScanned (preview {Bytes} bytes, shortName='{ShortName}').", pageNumber, previewBytes?.Length ?? 0, documentShortName);
        await Clients.Caller.SendAsync("Scannerlogs", new
        {
            PageNumber = pages.Count,
            Message = logactivitystring,
        });

        _logger.LogInformation("[Page {Page}] Done (hadError={HadError}).", pageNumber, pageHadError);
        return pageHadError;
    }

    private static string BatchGroup(int batchId) => $"batch-{batchId}";

    /// <summary>
    /// Reads all images from the test folder location when test mode is enabled.
    /// Returns a list of image byte arrays.
    /// </summary>

    public async Task<List<byte[]>> GetTestImages()
    {
        _logger.LogInformation("GetTestImages called");

        if (!_testSettings.Mode)
        {
            _logger.LogWarning("Test mode is not enabled");
            await Clients.Caller.SendAsync("Error", "Test mode is not enabled");
            return new List<byte[]>();
        }

        if (string.IsNullOrWhiteSpace(_testSettings.ImageFolderLocation))
        {
            _logger.LogWarning("Test image folder location is not configured");
            await Clients.Caller.SendAsync("Error", "Test image folder location is not configured");
            return new List<byte[]>();
        }

        if (!Directory.Exists(_testSettings.ImageFolderLocation))
        {
            _logger.LogWarning("Test image folder does not exist: {Path}", _testSettings.ImageFolderLocation);
            await Clients.Caller.SendAsync("Error", $"Test image folder does not exist: {_testSettings.ImageFolderLocation}");
            return new List<byte[]>();
        }

        try
        {
            var imageExtensions = new[] { ".jpg", ".jpeg", ".png", ".bmp", ".tiff", ".tif", ".gif" };
            var imageFiles = Directory.GetFiles(_testSettings.ImageFolderLocation)
                .Where(file => imageExtensions.Contains(Path.GetExtension(file).ToLowerInvariant()))
                .OrderBy(file => file)
                .ToList();

            if (!imageFiles.Any())
            {
                _logger.LogWarning("No image files found in test folder: {Path}", _testSettings.ImageFolderLocation);
                await Clients.Caller.SendAsync("Error", "No image files found in test folder");
                return new List<byte[]>();
            }

            _logger.LogInformation("Found {Count} image files in test folder", imageFiles.Count);

            var imageBytesList = new List<byte[]>();

            foreach (var imageFile in imageFiles)
            {
                try
                {
                    var imageBytes = await File.ReadAllBytesAsync(imageFile);
                    imageBytesList.Add(imageBytes);
                    _logger.LogDebug("Read image file: {FileName}, size: {Size} bytes", Path.GetFileName(imageFile), imageBytes.Length);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to read image file: {FileName}", imageFile);
                    // Continue processing other files
                }
            }

            _logger.LogInformation("Successfully read {Count} image files from test folder", imageBytesList.Count);
            return imageBytesList;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error reading images from test folder");
            await Clients.Caller.SendAsync("Error", $"Error reading images from test folder: {ex.Message}");
            return new List<byte[]>();
        }
    }
}
