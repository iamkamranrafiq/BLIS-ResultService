using Bioreference.ScanningService.Application.Common.Interfaces;

namespace Bioreference.ScanningService.Infrastructure.Storage;

/// <summary>
/// Converts paths to Windows format (backslash separators, preserves UNC paths).
/// </summary>
public class WindowsPathConverter : IPathConverter
{
    public string ConvertPath(string path)
    {
        return path.Replace('/', '\\');
    }

    public string CombinePath(params string[] paths)
    {
        return Path.Combine(paths);
    }

    public string ConvertOnbasePath(string path, string networkPath, string mountedPath)
    {
        return path.Replace('/', '\\');
    }
}
