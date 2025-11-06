using SaplingFS.Models;
using TextCopy;

namespace SaplingFS.Services;

/// <summary>
/// Monitors clipboard for Minecraft player position commands and performs raycasting
/// to identify which block/file the player is looking at.
/// </summary>
public class ClipboardMonitor
{
    private readonly RaycastService _raycastService;
    private readonly int _parentDepth;
    private string _clipboardLast = "";
    private System.Timers.Timer? _timer;

    public ClipboardMonitor(RaycastService raycastService, int parentDepth)
    {
        _raycastService = raycastService;
        _parentDepth = parentDepth;
    }

    /// <summary>
    /// Starts monitoring the clipboard every 200ms for player position updates.
    /// </summary>
    /// <param name="mapping">Block-to-file mapping to search</param>
    /// <param name="onBlockIdentified">Callback when a block is identified</param>
    /// <param name="onNoBlock">Callback when no block is found</param>
    public void Start(
        Func<IReadOnlyDictionary<string, BlockMapping>> getMapping,
        Action<BlockMapping> onBlockIdentified,
        Action onNoBlock)
    {
        _timer = new System.Timers.Timer(200);
        _timer.Elapsed += async (sender, e) =>
        {
            try
            {
                var text = await ClipboardService.GetTextAsync();
                if (text == _clipboardLast || string.IsNullOrEmpty(text))
                    return;

                _clipboardLast = text;

                // Check if this is a Minecraft teleport command
                if (!text.StartsWith("/execute in minecraft:overworld run tp @s"))
                    return;

                // Parse position and angles from command
                var parts = text.Split("@s ")[1].Split(" ");
                if (parts.Length < 5)
                    return;

                double x = double.Parse(parts[0]);
                double y = double.Parse(parts[1]);
                double z = double.Parse(parts[2]);
                double yaw = double.Parse(parts[3]);
                double pitch = double.Parse(parts[4]);

                // Calculate eye position (1.62 blocks above feet)
                var pos = new Vector((int)x, (int)(y + 1.62), (int)z);

                // Calculate forward vector from angles
                var fvec = Vector.FromAngles(yaw, pitch);

                // Perform raycast
                var mapping = getMapping();
                var entry = _raycastService.Raycast(mapping, pos, fvec);

                if (entry == null)
                {
                    onNoBlock();
                }
                else
                {
                    onBlockIdentified(entry);
                }
            }
            catch (Exception ex)
            {
                // Silently ignore clipboard read errors
                Console.Error.WriteLine($"Clipboard error: {ex.Message}");
            }
        };

        _timer.Start();
        Console.WriteLine("Listening for clipboard changes...");
    }

    /// <summary>
    /// Stops monitoring the clipboard.
    /// </summary>
    public void Stop()
    {
        _timer?.Stop();
        _timer?.Dispose();
        _timer = null;
    }

    /// <summary>
    /// Formats a mapping entry as a human-readable string.
    /// </summary>
    public string FormatMappingString(BlockMapping entry)
    {
        var positionString = $"{entry.Position.X} {entry.Position.Y} {entry.Position.Z}";
        var shortPath = GetShortPath(entry.FilePath, _parentDepth);
        return $"\"{entry.Block}\" at ({positionString}): \"{shortPath}\"";
    }

    /// <summary>
    /// Abbreviates a file path for display purposes.
    /// </summary>
    private string GetShortPath(string fullPath, int depth)
    {
        var parts = fullPath.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        if (parts.Length <= depth + 1)
            return fullPath;

        var fileName = parts[^1];
        var parentParts = parts.Take(depth).ToArray();
        return string.Join(Path.DirectorySeparatorChar, parentParts) + "/.../" + fileName;
    }
}
