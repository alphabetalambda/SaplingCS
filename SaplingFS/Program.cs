using SaplingFS.Configuration;
using SaplingFS.Services;

namespace SaplingFS;

/// <summary>
/// SaplingFS - Voxel-based Entropy-oriented Minecraft File System
/// Maps every file on your computer to a block in a Minecraft world.
/// </summary>
class Program
{
    static async Task Main(string[] args)
    {
        // Parse command-line arguments
        var options = CommandLineOptions.Parse(args);

        // Validate and show usage if invalid
        if (!options.Validate())
        {
            CommandLineOptions.PrintUsage();
            return;
        }

        Console.WriteLine("SaplingFS - .NET Edition");
        Console.WriteLine($"World: {options.WorldName}");
        Console.WriteLine($"Root Path: {options.RootPath}");
        Console.WriteLine($"Debug Mode: {options.Debug}");
        Console.WriteLine();

        // TODO: Implement world backup
        // TODO: Implement file scanning
        // TODO: Implement terrain generation
        // TODO: Implement clipboard monitoring
        // TODO: Implement block change detection

        Console.WriteLine("Press Ctrl+C to exit...");
        await Task.Delay(Timeout.Infinite);
    }
}
