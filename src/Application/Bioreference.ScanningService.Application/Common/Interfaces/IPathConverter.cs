namespace Bioreference.ScanningService.Application.Common.Interfaces;

/// <summary>
/// Converts file/folder paths to the format expected by the current OS environment.
/// </summary>
public interface IPathConverter
{
    /// <summary>
    /// Converts the given path to the platform-appropriate format.
    /// </summary>
    string ConvertPath(string path);

    /// <summary>
    /// Combines path segments using the platform-appropriate separator.
    /// </summary>
    string CombinePath(params string[] paths);
    /// <summary>
    /// Replace the onbase path with mounted path.
    /// </summary>
    string ConvertOnbasePath(string path, string networkPath, string mountedPath);
}
