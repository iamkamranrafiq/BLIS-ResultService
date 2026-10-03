using Bioreference.ScanningService.Application.Common.Interfaces;

namespace Bioreference.ScanningService.Jobs.IngestDocs
{
    /// <summary>
    /// Provides the user name used for auditing when documents are imported by the
    /// background job host (no HTTP request context is available).
    /// </summary>
    public sealed class JobRequestUserContext : IRequestUserContext
    {
        private string? _userName = "ingestdocs";

        public string? GetUserName() => _userName;

        public void SetUserName(string? userName) => _userName = userName;
    }
}
