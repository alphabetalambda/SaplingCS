namespace SaplingFS.Models;

/// <summary>
/// Represents the type of Minecraft launcher.
/// </summary>
public enum LauncherType
{
    /// <summary>
    /// Official Minecraft Launcher
    /// </summary>
    Official,

    /// <summary>
    /// Prism Launcher (fork of MultiMC)
    /// </summary>
    PrismLauncher,

    /// <summary>
    /// MultiMC Launcher
    /// </summary>
    MultiMC,

    /// <summary>
    /// CurseForge Launcher
    /// </summary>
    CurseForge,

    /// <summary>
    /// ATLauncher
    /// </summary>
    ATLauncher,

    /// <summary>
    /// Modrinth Launcher
    /// </summary>
    Modrinth,

    /// <summary>
    /// GDLauncher
    /// </summary>
    GDLauncher
}
