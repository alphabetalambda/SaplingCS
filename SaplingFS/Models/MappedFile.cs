namespace SaplingFS.Models;

/// <summary>
/// Represents a file on the filesystem that will be mapped to Minecraft blocks.
/// </summary>
public class MappedFile
{
    /// <summary>
    /// Absolute path to the file on disk.
    /// </summary>
    public string Path { get; init; }

    /// <summary>
    /// Size of the file in bytes.
    /// </summary>
    public long Size { get; init; }

    /// <summary>
    /// Depth from the root scan directory (used for grouping).
    /// </summary>
    public int Depth { get; init; }

    public MappedFile(string path, long size, int depth)
    {
        Path = System.IO.Path.GetFullPath(path);
        Size = size;
        Depth = depth;
    }

    /// <summary>
    /// Gets the parent directory path up to the specified depth.
    /// Used for grouping files into terrain "islands".
    /// </summary>
    public string GetShortParent(int parentDepth)
    {
        var pathParts = Path.Split(System.IO.Path.DirectorySeparatorChar);
        var parentParts = pathParts[..^1]; // Remove filename
        var shortParts = parentParts[..(Math.Min(parentDepth + 1, parentParts.Length))];
        return string.Join(System.IO.Path.DirectorySeparatorChar, shortParts);
    }

    /// <summary>
    /// Gets an abbreviated path for display purposes.
    /// Shows start of path + "..." + filename if path is long.
    /// </summary>
    public string GetShortPath(int parentDepth)
    {
        var pathParts = Path.Split(System.IO.Path.DirectorySeparatorChar);
        var fileName = pathParts[^1];
        var directoryParts = pathParts[..^1];
        var pathStart = string.Join(
            System.IO.Path.DirectorySeparatorChar,
            directoryParts[..(Math.Min(parentDepth + 1, directoryParts.Length))]
        );
        var pathEllipsis = directoryParts.Length > (parentDepth + 2) ? "/..." : "";
        return $"{pathStart}{pathEllipsis}{System.IO.Path.DirectorySeparatorChar}{fileName}";
    }

    public override string ToString() => Path;
}
