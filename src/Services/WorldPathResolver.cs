namespace SaplingFS.Services;

/// <summary>
/// Service for resolving Minecraft world paths across different operating systems.
/// </summary>
public class WorldPathResolver
{
    /// <summary>
    /// Resolves the world name to a full path.
    /// </summary>
    /// <param name="worldName">The name of the world or a directory path.</param>
    /// <returns>The full path to the world directory.</returns>
    public string ResolveWorldPath(string worldName)
    {
        // Check if it's already a directory path
        if (Directory.Exists(worldName))
        {
            return Path.GetFullPath(worldName);
        }

        // Otherwise, look in .minecraft/saves
        string minecraftPath;
        if (OperatingSystem.IsWindows())
        {
            minecraftPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                ".minecraft", "saves", worldName);
        }
        else if (OperatingSystem.IsMacOS())
        {
            minecraftPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                "Library", "Application Support", "minecraft", "saves", worldName);
        }
        else
        {
            minecraftPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                ".minecraft", "saves", worldName);
        }

        return minecraftPath;
    }
}
