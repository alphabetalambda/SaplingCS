using SaplingFS.Models;

namespace SaplingFS.Services;

/// <summary>
/// Scans the filesystem and builds a list of files to map to Minecraft blocks.
/// </summary>
public class FileScanner
{
    /// <summary>
    /// Builds a list of files via depth-first search starting from the given path.
    /// </summary>
    /// <param name="startPath">Directory from which to begin recursing</param>
    /// <param name="blacklist">List of paths to omit from the scan</param>
    /// <param name="progressCallback">Optional callback invoked with file count updates</param>
    /// <returns>List of mapped files</returns>
    public List<MappedFile> BuildFileList(
        string startPath,
        List<string> blacklist,
        Action<int>? progressCallback = null)
    {
        var list = new List<MappedFile>();
        BuildFileListRecursive(startPath, blacklist, list, depth: 0, progressCallback);
        return list;
    }

    private void BuildFileListRecursive(
        string startPath,
        List<string> blacklist,
        List<MappedFile> list,
        int depth,
        Action<int>? progressCallback = null)
    {
        try
        {
            // First, recurse into subdirectories
            var directories = Directory.EnumerateDirectories(startPath);
            foreach (var directory in directories)
            {
                var dirInfo = new DirectoryInfo(directory);

                // Skip cache directories
                if (dirInfo.Name.Contains("cache", StringComparison.OrdinalIgnoreCase))
                    continue;

                // Skip blacklisted paths
                if (blacklist.Contains(directory))
                    continue;

                BuildFileListRecursive(directory, blacklist, list, depth + 1, progressCallback);
            }

            // Then, add files from current directory
            var files = Directory.EnumerateFiles(startPath);
            foreach (var file in files)
            {
                var fileInfo = new FileInfo(file);

                // Skip empty files
                if (fileInfo.Length == 0)
                    continue;

                list.Add(new MappedFile(file, fileInfo.Length, depth));

                // Report progress every 100 files
                if (progressCallback != null && list.Count % 100 == 0)
                {
                    progressCallback(list.Count);
                }
            }
        }
        catch (UnauthorizedAccessException)
        {
            // Silently skip access denied directories during scanning
        }
        catch (Exception)
        {
            // Silently skip directories that fail to read during scanning
        }
    }
}
