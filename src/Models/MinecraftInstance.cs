namespace SaplingFS.Models;

/// <summary>
/// Represents a Minecraft instance (world container) from a launcher.
/// </summary>
public class MinecraftInstance
{
    /// <summary>
    /// Name of the instance
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Full path to the instance directory
    /// </summary>
    public string Path { get; set; } = string.Empty;

    /// <summary>
    /// Launcher that owns this instance
    /// </summary>
    public LauncherType Launcher { get; set; }

    /// <summary>
    /// Path to the saves folder within this instance
    /// </summary>
    public string SavesPath => System.IO.Path.Combine(Path, "saves");

    public override string ToString()
    {
        return $"{Name} ({Launcher})";
    }
}
