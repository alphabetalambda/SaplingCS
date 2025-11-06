using SaplingFS.Models;

namespace SaplingFS.Services;

/// <summary>
/// Interactive selector for choosing Minecraft instances.
/// </summary>
public class InstanceSelector
{
    /// <summary>
    /// Prompts the user to select an instance from a list.
    /// </summary>
    public MinecraftInstance SelectInstance(List<MinecraftInstance> instances)
    {
        if (instances.Count == 0)
        {
            throw new InvalidOperationException("No instances provided to select from");
        }

        if (instances.Count == 1)
        {
            return instances[0];
        }

        Console.WriteLine("Multiple Minecraft instances detected:");
        Console.WriteLine();

        // Group by launcher for better organization
        var groupedInstances = instances
            .GroupBy(i => i.Launcher)
            .OrderBy(g => g.Key.ToString());

        int index = 1;
        var indexedInstances = new Dictionary<int, MinecraftInstance>();

        foreach (var group in groupedInstances)
        {
            Console.WriteLine($"  {group.Key}:");
            foreach (var instance in group.OrderBy(i => i.Name))
            {
                Console.WriteLine($"    [{index}] {instance.Name}");
                indexedInstances[index] = instance;
                index++;
            }
            Console.WriteLine();
        }

        // Prompt for selection
        while (true)
        {
            Console.Write($"Select an instance (1-{indexedInstances.Count}): ");
            var input = Console.ReadLine();

            if (int.TryParse(input, out var selection) && indexedInstances.ContainsKey(selection))
            {
                return indexedInstances[selection];
            }

            Console.WriteLine("Invalid selection. Please try again.");
        }
    }

    /// <summary>
    /// Auto-selects or prompts for instance selection based on launcher and instance name.
    /// </summary>
    public MinecraftInstance? AutoSelectOrPrompt(
        List<MinecraftInstance> instances,
        LauncherType? preferredLauncher,
        string? preferredInstanceName)
    {
        if (instances.Count == 0)
        {
            return null;
        }

        // Filter by launcher if specified
        if (preferredLauncher != null)
        {
            instances = instances.Where(i => i.Launcher == preferredLauncher).ToList();
        }

        // Filter by instance name if specified
        if (!string.IsNullOrEmpty(preferredInstanceName))
        {
            instances = instances.Where(i =>
                i.Name.Equals(preferredInstanceName, StringComparison.OrdinalIgnoreCase)).ToList();
        }

        if (instances.Count == 0)
        {
            return null;
        }

        if (instances.Count == 1)
        {
            return instances[0];
        }

        // Multiple matches - prompt user
        return SelectInstance(instances);
    }
}
