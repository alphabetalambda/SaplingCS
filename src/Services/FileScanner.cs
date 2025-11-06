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
    /// <returns>List of mapped files</returns>
    public List<MappedFile> BuildFileList(string startPath, List<string> blacklist)
    {
        var list = new List<MappedFile>();
        BuildFileListRecursive(startPath, blacklist, list, depth: 0);
        return list;
    }

    private void BuildFileListRecursive(
        string startPath,
        List<string> blacklist,
        List<MappedFile> list,
        int depth)
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

                BuildFileListRecursive(directory, blacklist, list, depth + 1);
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
            }
        }
        catch (UnauthorizedAccessException)
        {
            Console.WriteLine($"Warning: Access denied to directory: {startPath}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Warning: Failed to read directory: {startPath}");
            Console.WriteLine($"  Error: {ex.Message}");
        }
    }
}
