using SaplingFS.Configuration;
using SaplingFS.Models;

namespace SaplingFS.Services;

/// <summary>
/// Service for resolving Minecraft world paths across different operating systems and launchers.
/// </summary>
public class WorldPathResolver
{
    private readonly LauncherDetector _launcherDetector;
    private readonly InstanceSelector _instanceSelector;

    public WorldPathResolver()
    {
        _launcherDetector = new LauncherDetector();
        _instanceSelector = new InstanceSelector();
    }

    /// <summary>
    /// Resolves the world name to a full path, with optional launcher and instance support.
    /// </summary>
    /// <param name="worldName">The name of the world or a directory path.</param>
    /// <param name="options">Command-line options containing launcher and instance preferences.</param>
    /// <returns>The full path to the world directory.</returns>
    public string ResolveWorldPath(string worldName, CommandLineOptions? options = null)
    {
        // Check if it's already a directory path
        if (Directory.Exists(worldName))
        {
            return Path.GetFullPath(worldName);
        }

        // If launcher is specified, use launcher-specific resolution
        if (options?.Launcher != null)
        {
            return ResolveWorldPathForLauncher(worldName, options.Launcher.Value, options.InstanceName);
        }

        // If instance name is specified without launcher, search all launchers
        if (!string.IsNullOrEmpty(options?.InstanceName))
        {
            var instances = _launcherDetector.FindInstancesByName(options.InstanceName);
            if (instances.Count > 0)
            {
                return Path.Combine(instances[0].SavesPath, worldName);
            }
        }

        // Default: look in official launcher location
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

    /// <summary>
    /// Resolves world path for a specific launcher and optional instance.
    /// </summary>
    private string ResolveWorldPathForLauncher(string worldName, LauncherType launcherType, string? instanceName)
    {
        var instances = _launcherDetector.GetInstancesForLauncher(launcherType);

        if (instances.Count == 0)
        {
            throw new InvalidOperationException($"No instances found for launcher: {launcherType}");
        }

        // If instance name specified, find it
        if (!string.IsNullOrEmpty(instanceName))
        {
            var matchingInstance = instances.FirstOrDefault(i =>
                i.Name.Equals(instanceName, StringComparison.OrdinalIgnoreCase));

            if (matchingInstance == null)
            {
                throw new InvalidOperationException(
                    $"Instance '{instanceName}' not found for launcher {launcherType}");
            }

            return Path.Combine(matchingInstance.SavesPath, worldName);
        }

        // If only one instance, use it
        if (instances.Count == 1)
        {
            return Path.Combine(instances[0].SavesPath, worldName);
        }

        // Multiple instances found - prompt user to select
        Console.WriteLine($"Multiple instances found for {launcherType}.");
        var selectedInstance = _instanceSelector.SelectInstance(instances);
        return Path.Combine(selectedInstance.SavesPath, worldName);
    }

    /// <summary>
    /// Lists all detected instances across all launchers.
    /// </summary>
    public List<MinecraftInstance> ListAllInstances()
    {
        return _launcherDetector.DetectAllInstances();
    }
}
