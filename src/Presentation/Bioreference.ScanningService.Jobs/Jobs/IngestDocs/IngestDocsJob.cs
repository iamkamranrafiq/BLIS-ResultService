using Bioreference.Jobs.Abstractions;
using Bioreference.Jobs.Core;
using Bioreference.ScanningService.Application.Common.Interfaces;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace Bioreference.ScanningService.Jobs.IngestDocs
{
    public class IngestDocsJob : Job
    {
        private readonly IBLISJob job;

        public IngestDocsJob(IBLISJob job, ILoggerFactory loggerFactory) : base(loggerFactory)
        {
            this.job = job;
        }

        protected override Task<JobResult> ExecuteJob(CancellationToken cancellationToken)
        {
            var sw = Stopwatch.StartNew();

            try
            {
                Logger.LogDebug("Executing ReportsOut Job...");

                Task.Run(() => job.Execute(), cancellationToken).Wait(cancellationToken);

                sw.Stop();
                Logger.LogInformation("Entity: {Entity}; Event: {Event}; Message: {Message}; ElapsedTime: {ElapsedTime} ms", "Job", "IngestDocs", "IngestDocs Job completed successfully.", sw.ElapsedMilliseconds);

                return Task.FromResult(JobResult.Success("IngestDocs Job"));
            }
            catch (OperationCanceledException)
            {
                sw.Stop();
                Logger.LogWarning("Entity: {Entity}; Event: {Event}; Message: {Message}; ElapsedTime: {ElapsedTime} ms", "Job", "IngestDocs", "IngestDocs Job was canceled.", sw.ElapsedMilliseconds);

                return Task.FromResult(JobResult.Failure("IngestDocs Job canceled"));
            }
            catch (Exception ex)
            {
                sw.Stop();
                Logger.LogError(ex, "Entity: {Entity}; Event: {Event}; Message: {Message}; ElapsedTime: {ElapsedTime} ms", "Job", "IngestDocs", "IngestDocs Job failed.", sw.ElapsedMilliseconds);

                return Task.FromResult(JobResult.Failure("IngestDocs Job failed"));
            }
        }
    }
}
