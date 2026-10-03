using Bioreference.ScanningService.Application.Common.Interfaces;
using Bioreference.ScanningService.Application.DTOs.Requests;

namespace Bioreference.ScanningService.Infrastructure.Storage;

/// <summary>
/// Converts paths to Linux format (forward slash separators, strips UNC prefix).
/// </summary>
public class LinuxPathConverter : IPathConverter
{
    public string ConvertPath(string path)
    {
        // Strip UNC prefix (\\server\share -> /server/share)
        if (path.StartsWith(@"\\"))
        {
            path = path[1..]; // remove one leading backslash, replace the rest below
        }

        return path.Replace('\\', '/');
    }

    public string ConvertOnbasePath(string path, string networkPath, string mountedPath)
    {
        // Normalize separators to forward slashes on BOTH the incoming path and the configured
        // network root before replacing. The stored path and the configured OnbaseRootPath can
        // use different separator styles (e.g. path has "/" while networkPath has "\\"), which
        // caused the root replacement to silently miss and leave "//nj1obfsp1/OB1/..." intact.
        var normalizedPath = path.Replace('\\', '/');
        var normalizedNetworkPath = networkPath.Replace('\\', '/');
        var normalizedMountedPath = mountedPath.Replace('\\', '/');

        return normalizedPath.Replace(normalizedNetworkPath, normalizedMountedPath, StringComparison.OrdinalIgnoreCase);
    }
    public string CombinePath(params string[] paths)
    {
        return Path.Combine(paths).Replace('\\', '/');
    }
}
