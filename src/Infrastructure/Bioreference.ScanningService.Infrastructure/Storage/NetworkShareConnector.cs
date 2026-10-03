using System.ComponentModel;
using System.Runtime.InteropServices;
using Bioreference.ScanningService.Application.Common.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Bioreference.ScanningService.Infrastructure.Storage;

/// <summary>
/// Establishes an authenticated connection to a Windows network share using
/// the Win32 WNetAddConnection2 API. Credentials are read from <see cref="StorageSettings"/>.
/// On non-Windows platforms (e.g. Linux containers) this is a no-op, since the SMB share
/// is expected to be mounted into the filesystem (CIFS) and BasePath points at that mount.
/// </summary>
public sealed class NetworkShareConnector : INetworkShareConnector
{
    private readonly StorageSettings _settings;
    private readonly ILogger<NetworkShareConnector> _logger;
    private readonly string? _shareRoot;
    private readonly object _sync = new();

    // net.exe use is a process spawn + network auth round-trip (can take seconds). Once a
    // session to the share is established it stays valid for the life of the service, so we
    // cache it here instead of reconnecting/disconnecting on every single file save.
    private volatile bool _connected;

    public NetworkShareConnector(IOptions<StorageSettings> settings, ILogger<NetworkShareConnector> logger)
    {
        _settings = settings.Value;
        _logger = logger;
        _shareRoot = GetConnectTarget(_settings.BasePath);
    }

    public IDisposable Connect()
    {
        // Credential-based share authentication via WNetAddConnection2 is a Windows-only
        // (mpr.dll) concept. On Linux/containers the SMB share is mounted into the
        // filesystem (e.g. via a CIFS Docker volume) and BasePath points at that mount,
        // so no in-process authentication is required.
        if (!OperatingSystem.IsWindows())
        {
            return NoOpScope.Instance;
        }

        // No credentials configured, or path is local: nothing to authenticate.
        if (string.IsNullOrWhiteSpace(_settings.Username) ||
            string.IsNullOrWhiteSpace(_shareRoot))
        {
            return NoOpScope.Instance;
        }

        // Session already established by a previous call — reuse it instead of paying the
        // net use connect/disconnect cost again.
        if (_connected)
        {
            return NoOpScope.Instance;
        }

        lock (_sync)
        {
            if (_connected)
            {
                return NoOpScope.Instance;
            }

            var userName = BuildUserName(_settings.Domain, _settings.Username);

            // NOTE: The raw WNetAddConnection2 P/Invoke fails with ERROR_BAD_NET_NAME (67)
            // in this environment (verified against \\server\share, the full path, and even
            // IPC$), while the OS `net use` command succeeds with the exact same credentials
            // and path. `net use` drives the full multi-provider network router (resolution +
            // retry) that the direct MPR call does not replicate here. We therefore establish
            // the authenticated session via `net use`.
            var (exitCode, output) = RunNetUse($"\"{_shareRoot}\" /user:\"{userName}\" \"{_settings.Password}\"");

            switch (exitCode)
            {
                case 0:
                    _logger.LogDebug("Connected to network share {Share} as {User}.", _shareRoot, userName);
                    _connected = true;
                    return NoOpScope.Instance;

                case 2:    // ERROR_FILE_NOT_FOUND surfaced by net when already connected in some cases
                case 85:   // ERROR_ALREADY_ASSIGNED - device/name already in use
                case 1219: // ERROR_SESSION_CREDENTIAL_CONFLICT - an existing session (often an
                           // app-pool/machine-account session with no rights) is blocking our
                           // explicit-credential logon. Drop every existing session to this server
                           // and retry so OUR credentials win instead of silently reusing the
                           // powerless session (which would make File.Exists return false -> 404).
                    _logger.LogWarning(
                        "Existing session to {Share} conflicts (exit {Code}); dropping it and retrying with explicit credentials.",
                        _shareRoot, exitCode);

                    RunNetUse($"\"{_shareRoot}\" /delete /y");
                    RunNetUse("* /delete /y");

                    var (retryCode, retryOutput) = RunNetUse($"\"{_shareRoot}\" /user:\"{userName}\" \"{_settings.Password}\"");
                    if (retryCode == 0)
                    {
                        _logger.LogDebug("Connected to network share {Share} as {User} after clearing conflict.", _shareRoot, userName);
                        _connected = true;
                        return NoOpScope.Instance;
                    }

                    _logger.LogWarning(
                        "Could not open an explicit-credential session to {Share} after clearing conflict (exit {Code}: {Output}); " +
                        "falling back to the current process identity for file access.",
                        _shareRoot, retryCode, retryOutput?.Trim());
                    return NoOpScope.Instance;

                default:
                    _logger.LogWarning(
                        "Could not open an explicit-credential session to {Share} via net use (exit {Code}: {Output}); " +
                        "falling back to the current process identity for file access.",
                        _shareRoot, exitCode, output?.Trim());
                    return NoOpScope.Instance;
            }
        }
    }

    private static (int ExitCode, string Output) RunNetUse(string arguments)
    {
        var psi = new System.Diagnostics.ProcessStartInfo
        {
            FileName = "net.exe",
            Arguments = "use " + arguments,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using var process = System.Diagnostics.Process.Start(psi);
        if (process is null)
        {
            return (-1, "Failed to start net.exe");
        }

        var stdout = process.StandardOutput.ReadToEnd();
        var stderr = process.StandardError.ReadToEnd();
        process.WaitForExit();

        var output = string.IsNullOrWhiteSpace(stderr) ? stdout : stderr;
        return (process.ExitCode, output);
    }

    private static string BuildUserName(string? domain, string username)
        => string.IsNullOrWhiteSpace(domain) ? username : $"{domain}\\{username}";

    /// <summary>
    /// Returns the UNC share root (\\server\share) to authenticate against.
    /// WNetAddConnection2 expects a connectable resource at the share level; passing a
    /// deeper subfolder (e.g. \\server\share\sub\dir) fails with ERROR_BAD_NET_NAME (67).
    /// Authenticating the share root establishes a session that covers all subfolders.
    /// Verified: `net use \\server\share /user:...` succeeds while the full path returns 67.
    /// Returns null for non-UNC (local) paths.
    /// </summary>
    private static string? GetConnectTarget(string basePath)
    {
        if (string.IsNullOrWhiteSpace(basePath) || !basePath.StartsWith(@"\\"))
        {
            return null;
        }

        var trimmed = basePath.TrimEnd('\\');
        var parts = trimmed[2..].Split('\\', StringSplitOptions.RemoveEmptyEntries);

        // Need at least \\server\share to be a valid UNC resource.
        return parts.Length < 2 ? null : $@"\\{parts[0]}\{parts[1]}";
    }

    private sealed class NoOpScope : IDisposable
    {
        public static readonly NoOpScope Instance = new();
        public void Dispose() { }
    }

    #region P/Invoke

    private const int NO_ERROR = 0;
    private const int ERROR_ALREADY_ASSIGNED = 85;
    private const int ERROR_BAD_NET_NAME = 67;
    private const int ERROR_NO_NET_OR_BAD_PATH = 1203;
    private const int ERROR_NOT_CONNECTED = 2250;
    private const int ERROR_SESSION_CREDENTIAL_CONFLICT = 1219;

    private enum ResourceType
    {
        Any = 0,
        Disk = 1,
        Print = 2
    }

    [StructLayout(LayoutKind.Sequential)]
    private sealed class NetResource
    {
        public int dwScope;
        public ResourceType dwType;
        public int dwDisplayType;
        public int dwUsage;
        public string? lpLocalName;
        public string? lpRemoteName;
        public string? lpComment;
        public string? lpProvider;
    }

    [DllImport("mpr.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern int WNetAddConnection2(
        NetResource netResource,
        string? password,
        string? username,
        int flags);

    [DllImport("mpr.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern int WNetCancelConnection2(
        string name,
        int flags,
        bool force);

    #endregion
}
