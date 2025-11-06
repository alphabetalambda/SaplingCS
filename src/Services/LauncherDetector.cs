using SaplingFS.Models;

namespace SaplingFS.Services;

/// <summary>
/// Detects and locates Minecraft launchers and their instances across different platforms.
/// </summary>
public class LauncherDetector
{
    /// <summary>
    /// Gets all launcher root directories for the current platform.
    /// </summary>
    public Dictionary<LauncherType, List<string>> GetLauncherPaths()
    {
        var paths = new Dictionary<LauncherType, List<string>>();

        if (OperatingSystem.IsWindows())
        {
            paths[LauncherType.Official] = new List<string>
            {
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), ".minecraft")
            };

            paths[LauncherType.PrismLauncher] = new List<string>
            {
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "PrismLauncher"),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "scoop", "persist", "prismlauncher")
            };

            paths[LauncherType.MultiMC] = new List<string>
            {
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "MultiMC"),
                "C:\\MultiMC", // Common manual install location
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "MultiMC")
            };

            paths[LauncherType.CurseForge] = new List<string>
            {
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "Curseforge", "Minecraft"),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "curseforge", "minecraft")
            };

            paths[LauncherType.ATLauncher] = new List<string>
            {
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "ATLauncher"),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "ATLauncher")
            };

            paths[LauncherType.Modrinth] = new List<string>
            {
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "com.modrinth.theseus"),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "ModrinthApp")
            };

            paths[LauncherType.GDLauncher] = new List<string>
            {
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "gdlauncher_next"),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "gdlauncher")
            };
        }
        else if (OperatingSystem.IsMacOS())
        {
            var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            var appSupport = Path.Combine(home, "Library", "Application Support");

            paths[LauncherType.Official] = new List<string>
            {
                Path.Combine(appSupport, "minecraft")
            };

            paths[LauncherType.PrismLauncher] = new List<string>
            {
                Path.Combine(appSupport, "PrismLauncher")
            };

            paths[LauncherType.MultiMC] = new List<string>
            {
                Path.Combine(appSupport, "MultiMC"),
                Path.Combine(home, "MultiMC") // Portable install
            };

            paths[LauncherType.CurseForge] = new List<string>
            {
                Path.Combine(home, "Documents", "Curseforge", "Minecraft"),
                Path.Combine(appSupport, "curseforge", "minecraft")
            };

            paths[LauncherType.ATLauncher] = new List<string>
            {
                Path.Combine(appSupport, "ATLauncher"),
                Path.Combine(home, "ATLauncher")
            };

            paths[LauncherType.Modrinth] = new List<string>
            {
                Path.Combine(appSupport, "com.modrinth.theseus")
            };

            paths[LauncherType.GDLauncher] = new List<string>
            {
                Path.Combine(appSupport, "gdlauncher_next"),
                Path.Combine(appSupport, "gdlauncher")
            };
        }
        else // Linux
        {
            var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            var localShare = Path.Combine(home, ".local", "share");

            paths[LauncherType.Official] = new List<string>
            {
                Path.Combine(home, ".minecraft")
            };

            paths[LauncherType.PrismLauncher] = new List<string>
            {
                Path.Combine(localShare, "PrismLauncher"),
                Path.Combine(home, ".var", "app", "org.prismlauncher.PrismLauncher", "data", "PrismLauncher") // Flatpak
            };

            paths[LauncherType.MultiMC] = new List<string>
            {
                Path.Combine(localShare, "multimc"),
                Path.Combine(home, ".multimc"),
                Path.Combine(home, "MultiMC")
            };

            paths[LauncherType.CurseForge] = new List<string>
            {
                Path.Combine(home, ".local", "share", "curseforge", "minecraft"),
                Path.Combine(home, "curseforge", "minecraft")
            };

            paths[LauncherType.ATLauncher] = new List<string>
            {
                Path.Combine(localShare, "ATLauncher"),
                Path.Combine(home, ".var", "app", "com.atlauncher.ATLauncher", "data", "ATLauncher"), // Flatpak
                Path.Combine(home, "ATLauncher")
            };

            paths[LauncherType.Modrinth] = new List<string>
            {
                Path.Combine(localShare, "com.modrinth.theseus"),
                Path.Combine(home, ".var", "app", "com.modrinth.ModrinthApp", "data", "ModrinthApp") // Flatpak
            };

            paths[LauncherType.GDLauncher] = new List<string>
            {
                Path.Combine(localShare, "gdlauncher_next"),
                Path.Combine(localShare, "gdlauncher")
            };
        }

        return paths;
    }

    /// <summary>
    /// Detects all Minecraft instances across all installed launchers.
    /// </summary>
    public List<MinecraftInstance> DetectAllInstances()
    {
        var instances = new List<MinecraftInstance>();
        var launcherPaths = GetLauncherPaths();

        foreach (var (launcherType, paths) in launcherPaths)
        {
            foreach (var launcherPath in paths)
            {
                if (!Directory.Exists(launcherPath))
                    continue;

                instances.AddRange(DetectInstancesForLauncher(launcherType, launcherPath));
            }
        }

        return instances;
    }

    /// <summary>
    /// Detects instances for a specific launcher type and path.
    /// </summary>
    private List<MinecraftInstance> DetectInstancesForLauncher(LauncherType launcherType, string launcherPath)
    {
        var instances = new List<MinecraftInstance>();

        try
        {
            switch (launcherType)
            {
                case LauncherType.Official:
                    // Official launcher has no instances, just direct saves folder
                    if (Directory.Exists(Path.Combine(launcherPath, "saves")))
                    {
                        instances.Add(new MinecraftInstance
                        {
                            Name = "Default",
                            Path = launcherPath,
                            Launcher = LauncherType.Official
                        });
                    }
                    break;

                case LauncherType.PrismLauncher:
                case LauncherType.MultiMC:
                    // These launchers use an "instances" folder
                    var instancesPath = Path.Combine(launcherPath, "instances");
                    if (Directory.Exists(instancesPath))
                    {
                        foreach (var instanceDir in Directory.GetDirectories(instancesPath))
                        {
                            var minecraftPath = Path.Combine(instanceDir, ".minecraft");
                            if (Directory.Exists(minecraftPath))
                            {
                                instances.Add(new MinecraftInstance
                                {
                                    Name = Path.GetFileName(instanceDir),
                                    Path = minecraftPath,
                                    Launcher = launcherType
                                });
                            }
                        }
                    }
                    break;

                case LauncherType.CurseForge:
                    // CurseForge uses "Instances" folder (capital I)
                    var curseInstancesPath = Path.Combine(launcherPath, "Instances");
                    if (Directory.Exists(curseInstancesPath))
                    {
                        foreach (var instanceDir in Directory.GetDirectories(curseInstancesPath))
                        {
                            if (Directory.Exists(Path.Combine(instanceDir, "saves")))
                            {
                                instances.Add(new MinecraftInstance
                                {
                                    Name = Path.GetFileName(instanceDir),
                                    Path = instanceDir,
                                    Launcher = LauncherType.CurseForge
                                });
                            }
                        }
                    }
                    break;

                case LauncherType.ATLauncher:
                    // ATLauncher uses "instances" folder
                    var atInstancesPath = Path.Combine(launcherPath, "instances");
                    if (Directory.Exists(atInstancesPath))
                    {
                        foreach (var instanceDir in Directory.GetDirectories(atInstancesPath))
                        {
                            if (Directory.Exists(Path.Combine(instanceDir, "saves")))
                            {
                                instances.Add(new MinecraftInstance
                                {
                                    Name = Path.GetFileName(instanceDir),
                                    Path = instanceDir,
                                    Launcher = LauncherType.ATLauncher
                                });
                            }
                        }
                    }
                    break;

                case LauncherType.Modrinth:
                    // Modrinth uses "profiles" folder
                    var modrinthProfilesPath = Path.Combine(launcherPath, "profiles");
                    if (Directory.Exists(modrinthProfilesPath))
                    {
                        foreach (var profileDir in Directory.GetDirectories(modrinthProfilesPath))
                        {
                            if (Directory.Exists(Path.Combine(profileDir, "saves")))
                            {
                                instances.Add(new MinecraftInstance
                                {
                                    Name = Path.GetFileName(profileDir),
                                    Path = profileDir,
                                    Launcher = LauncherType.Modrinth
                                });
                            }
                        }
                    }
                    break;

                case LauncherType.GDLauncher:
                    // GDLauncher uses "instances" folder
                    var gdInstancesPath = Path.Combine(launcherPath, "instances");
                    if (Directory.Exists(gdInstancesPath))
                    {
                        foreach (var instanceDir in Directory.GetDirectories(gdInstancesPath))
                        {
                            if (Directory.Exists(Path.Combine(instanceDir, "saves")))
                            {
                                instances.Add(new MinecraftInstance
                                {
                                    Name = Path.GetFileName(instanceDir),
                                    Path = instanceDir,
                                    Launcher = LauncherType.GDLauncher
                                });
                            }
                        }
                    }
                    break;
            }
        }
        catch (UnauthorizedAccessException)
        {
            // Silently skip directories we can't access
        }
        catch (Exception)
        {
            // Silently skip other errors during detection
        }

        return instances;
    }

    /// <summary>
    /// Finds instances matching a given name across all launchers.
    /// </summary>
    public List<MinecraftInstance> FindInstancesByName(string name)
    {
        var allInstances = DetectAllInstances();
        return allInstances
            .Where(i => i.Name.Equals(name, StringComparison.OrdinalIgnoreCase))
            .ToList();
    }

    /// <summary>
    /// Gets all instances for a specific launcher type.
    /// </summary>
    public List<MinecraftInstance> GetInstancesForLauncher(LauncherType launcherType)
    {
        var allInstances = DetectAllInstances();
        return allInstances
            .Where(i => i.Launcher == launcherType)
            .ToList();
    }
}
