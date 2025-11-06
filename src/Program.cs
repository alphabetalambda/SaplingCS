using System.IO.Compression;
using System.Text;
using System.Text.Json;
using SaplingFS.Configuration;
using SaplingFS.Models;
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

        // Find Minecraft world path
        var worldPathResolver = new WorldPathResolver();
        string worldPath;
        try
        {
            worldPath = worldPathResolver.ResolveWorldPath(options.WorldName, options);
        }
        catch (InvalidOperationException ex)
        {
            Console.Error.WriteLine($"Error: {ex.Message}");
            return;
        }

        if (!Directory.Exists(worldPath))
        {
            Console.Error.WriteLine($"World not found: \"{worldPath}\"");
            return;
        }

        // Back up world data
        string backupWorldPath = Path.GetFullPath(worldPath) + "_SaplingFS_backup";
        if (!Directory.Exists(backupWorldPath))
        {
            Console.WriteLine($"Creating backup of world \"{options.WorldName}\"...\n");
            CopyDirectory(worldPath, backupWorldPath);
        }

        // Warn user of dangers if --allow-delete is enabled
        if (options.AllowDelete)
        {
            Console.Error.WriteLine("WARNING: --allow-delete is enabled.");
            Console.Error.WriteLine("Real files on your computer are at risk.\n");
            Console.WriteLine("You have 10 seconds to press Ctrl+C and stop the program:");

            for (int countdown = 10; countdown > 0; countdown--)
            {
                Console.WriteLine($"{countdown}...");
                await Task.Delay(1000);
            }
            Console.WriteLine();
        }

        // Initialize services
        var worldParser = new WorldParser();
        var terrainGenerator = new TerrainGenerator(worldParser);
        var fileScanner = new FileScanner();
        var processManager = new ProcessManager();
        var raycastService = new RaycastService();
        var clipboardMonitor = new ClipboardMonitor(raycastService, options.ParentDepth);
        var blockChangeMonitor = new BlockChangeMonitor(
            worldParser, terrainGenerator, processManager, worldPath, options.AllowDelete);
        var statusDisplay = new StatusDisplay();

        // Set up mapping persistence
        string mappingDir = Path.Combine(Directory.GetCurrentDirectory(), "mapping");
        Directory.CreateDirectory(mappingDir);
        string mappingJsonPath = Path.Combine(mappingDir, $"{options.WorldName}.json.zlib");

        // Load or generate terrain
        if (!options.NoProgress && File.Exists(mappingJsonPath))
        {
            Console.WriteLine("Restoring block-file mapping from file...");
            await LoadMappingFromDiskAsync(mappingJsonPath, terrainGenerator);
            Console.WriteLine($"Done, loaded {terrainGenerator.Mapping.Count} blocks.\n");
        }
        else
        {
            // Start status display
            statusDisplay.Start();
            statusDisplay.SetPhase("Scanning filesystem");

            var fileList = fileScanner.BuildFileList(
                options.RootPath,
                options.Blacklist,
                progressCallback: count => statusDisplay.UpdateFileScanning(count));

            statusDisplay.Stop();
            Console.WriteLine($"Found {fileList.Count} files.\n");

            // Start status display for terrain generation
            statusDisplay.Start();
            statusDisplay.SetPhase("Generating terrain");
            statusDisplay.SetTotalFiles(fileList.Count);

            await terrainGenerator.BuildRegionDataAsync(
                fileList,
                options.ParentDepth,
                worldPath,
                options.Debug,
                progressCallback: (processed, detail) =>
                {
                    statusDisplay.UpdateTerrainGeneration(processed);
                    statusDisplay.SetDetail(detail);
                });

            statusDisplay.Stop();
            Console.WriteLine("Terrain generation complete!\n");

            if (!options.NoProgress)
            {
                await WriteMappingToDiskAsync(mappingJsonPath, terrainGenerator);
            }
        }

        // Start clipboard monitoring
        clipboardMonitor.Start(
            () => terrainGenerator.Mapping,
            entry =>
            {
                Console.WriteLine(clipboardMonitor.FormatMappingString(entry));
            },
            () =>
            {
                Console.WriteLine("No file associated with this block.");
            });

        // Initialize region file cache
        await worldParser.FillRegionFileCacheAsync(worldPath);

        // Start block change monitoring
        blockChangeMonitor.Start((entry, newBlock) =>
        {
            Console.WriteLine($"Removed {clipboardMonitor.FormatMappingString(entry)}");
            Console.WriteLine($" ^ Replaced by \"{newBlock}\"");
        });

        // Auto-save mapping every 5 minutes
        if (!options.NoProgress)
        {
            var saveTimer = new System.Timers.Timer(1000 * 60 * 5);
            saveTimer.Elapsed += async (sender, e) =>
            {
                await WriteMappingToDiskAsync(mappingJsonPath, terrainGenerator);
            };
            saveTimer.Start();
        }

        // Wait for Ctrl+C
        Console.WriteLine("\nPress Ctrl+C to exit...");
        var cts = new CancellationTokenSource();
        Console.CancelKeyPress += (sender, e) =>
        {
            e.Cancel = true;
            cts.Cancel();
        };

        try
        {
            await Task.Delay(Timeout.Infinite, cts.Token);
        }
        catch (TaskCanceledException)
        {
            Console.WriteLine("\nShutting down...");
            clipboardMonitor.Stop();
            blockChangeMonitor.Stop();
        }
    }


    /// <summary>
    /// Recursively copies a directory.
    /// </summary>
    private static void CopyDirectory(string sourceDir, string destDir)
    {
        Directory.CreateDirectory(destDir);

        foreach (var file in Directory.GetFiles(sourceDir))
        {
            string destFile = Path.Combine(destDir, Path.GetFileName(file));
            File.Copy(file, destFile, true);
        }

        foreach (var dir in Directory.GetDirectories(sourceDir))
        {
            string destSubDir = Path.Combine(destDir, Path.GetFileName(dir));
            CopyDirectory(dir, destSubDir);
        }
    }

    /// <summary>
    /// Saves the block-file mapping to disk as compressed JSON.
    /// </summary>
    private static async Task WriteMappingToDiskAsync(string path, TerrainGenerator terrainGenerator)
    {
        var compactMapping = new Dictionary<string, MappingEntry>();

        foreach (var kvp in terrainGenerator.Mapping)
        {
            var pos = kvp.Value.Position;
            var file = kvp.Value.FilePath;

            compactMapping[kvp.Key] = new MappingEntry
            {
                pos = new[] { pos.X, pos.Y, pos.Z },
                file = new object[] { file, 0, 0 }, // Size and depth not needed for restore
                block = kvp.Value.Block
            };
        }

        string json = JsonSerializer.Serialize(compactMapping, MappingJsonContext.Default.DictionaryStringMappingEntry);
        byte[] jsonBytes = Encoding.UTF8.GetBytes(json);

        using var outputStream = File.Create(path);
        using var zlibStream = new ZLibStream(outputStream, CompressionMode.Compress);
        await zlibStream.WriteAsync(jsonBytes);
    }

    /// <summary>
    /// Loads the block-file mapping from disk.
    /// </summary>
    private static async Task LoadMappingFromDiskAsync(string path, TerrainGenerator terrainGenerator)
    {
        using var inputStream = File.OpenRead(path);
        using var zlibStream = new ZLibStream(inputStream, CompressionMode.Decompress);
        using var memoryStream = new MemoryStream();
        await zlibStream.CopyToAsync(memoryStream);

        string json = Encoding.UTF8.GetString(memoryStream.ToArray());
        using var document = JsonDocument.Parse(json);

        foreach (var property in document.RootElement.EnumerateObject())
        {
            var posArray = property.Value.GetProperty("pos");
            var pos = new Vector(
                posArray[0].GetInt32(),
                posArray[1].GetInt32(),
                posArray[2].GetInt32());

            var fileArray = property.Value.GetProperty("file");
            string filePath = fileArray[0].GetString() ?? "";

            var block = property.Value.GetProperty("block").GetString() ?? "stone";

            var mapping = new BlockMapping(pos, filePath, block);

            // Add mapping and update bounds
            terrainGenerator.AddMapping(mapping);
            terrainGenerator.UpdateTerrainBounds(pos);
        }
    }
}
