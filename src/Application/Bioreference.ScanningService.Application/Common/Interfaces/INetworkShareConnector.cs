namespace Bioreference.ScanningService.Application.Common.Interfaces;

/// <summary>
/// Establishes an authenticated connection to a Windows network share using
/// the configured credentials (username/password/domain). Once connected, standard
/// <see cref="System.IO"/> file operations against the share path work transparently.
/// </summary>
public interface INetworkShareConnector
{
    /// <summary>
    /// Ensures an authenticated session to the configured network share exists.
    /// Returns a scope that keeps the connection open until disposed.
    /// Reuse this in a <c>using</c> block around file operations.
    /// </summary>
    IDisposable Connect();
}
