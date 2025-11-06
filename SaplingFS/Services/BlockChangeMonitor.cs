using System.Collections.Concurrent;
using SaplingFS.Models;

namespace SaplingFS.Services;

/// <summary>
/// Monitors Minecraft region files for block changes and triggers file deletion
/// when blocks are destroyed.
/// </summary>
public class BlockChangeMonitor
{
    private readonly WorldParser _worldParser;
    private readonly TerrainGenerator _terrainGenerator;
    private readonly ProcessManager _processManager;
    private readonly string _worldPath;
    private readonly bool _allowDelete;
    private readonly ConcurrentDictionary<string, ulong> _regionChecksums = new();
    private readonly ConcurrentDictionary<string, ulong> _chunkChecksums = new();
    private System.Timers.Timer? _timer;
    private bool _isChecking;

    public BlockChangeMonitor(
        WorldParser worldParser,
        TerrainGenerator terrainGenerator,
        ProcessManager processManager,
        string worldPath,
        bool allowDelete)
    {
        _worldParser = worldParser;
        _terrainGenerator = terrainGenerator;
        _processManager = processManager;
        _worldPath = worldPath;
        _allowDelete = allowDelete;
    }

    /// <summary>
    /// Starts monitoring for block changes every 200ms.
    /// </summary>
    /// <param name="onBlockRemoved">Callback when a block is removed</param>
    public void Start(Action<BlockMapping, string> onBlockRemoved)
    {
        _timer = new System.Timers.Timer(200);
        _timer.Elapsed += async (sender, e) =>
        {
            if (_isChecking) return; // Skip if previous check still running
            _isChecking = true;

            try
            {
                await CheckBlockChangesAsync(onBlockRemoved);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Error checking block changes: {ex.Message}");
            }
            finally
            {
                _isChecking = false;
            }
        };

        _timer.Start();
        Console.WriteLine("Listening for block changes...");
    }

    /// <summary>
    /// Stops monitoring for block changes.
    /// </summary>
    public void Stop()
    {
        _timer?.Stop();
        _timer?.Dispose();
        _timer = null;
    }

    /// <summary>
    /// Checks all region files for block changes.
    /// </summary>
    private async Task CheckBlockChangesAsync(Action<BlockMapping, string> onBlockRemoved)
    {
        var bounds = _terrainGenerator.TerrainBounds;

        await _worldParser.ForRegionAsync(_worldPath, bounds, async (region, rx, rz) =>
        {
            // Compare checksums and skip region if no changes were made
            string regionKey = $"{rx},{rz}";
            if (_regionChecksums.TryGetValue(regionKey, out var cachedChecksum)
                && cachedChecksum == region.Checksum)
            {
                return;
            }
            _regionChecksums[regionKey] = region.Checksum;

            // Iterate over all mapped chunks within this region
            await _terrainGenerator.ForMappedChunksAsync(async (blocks, entries, chunkX, chunkZ, chunkBounds) =>
            {
                // Small delay to allow other tasks to run
                await Task.Delay(10);

                // Check for changes in chunk hash and load data into block array
                string chunkKey = $"{chunkX},{chunkZ}";
                ulong? expectHash = _chunkChecksums.TryGetValue(chunkKey, out var hash) ? hash : null;

                var returnHash = await _worldParser.RegionToBlocksAsync(
                    region.Bytes, blocks, rx, rz, chunkBounds, expectHash);

                if (returnHash == null) return; // No changes
                _chunkChecksums[chunkKey] = returnHash.Value;

                // Look for blocks that don't match the expected mapping
                var tasks = new List<Task>();
                foreach (var entry in entries)
                {
                    tasks.Add(Task.Run(async () =>
                    {
                        var relativePos = entry.Position.Relative(chunkX, chunkZ);
                        var block = blocks[relativePos.X][relativePos.Y][relativePos.Z];

                        if (block == entry.Block) return;

                        // Block has been modified or removed
                        onBlockRemoved(entry, block);

                        // If permitted, delete the associated file
                        if (_allowDelete)
                        {
                            string fullPath = entry.FilePath;
                            try
                            {
                                // First, kill any processes holding a handle to this file
                                var pids = await _processManager.GetHandleOwnersAsync(fullPath);
                                var killTasks = pids.Select(pid => Task.Run(async () =>
                                {
                                    await _processManager.KillProcessAsync(pid);
                                    Console.WriteLine($"Killing process {pid}");
                                }));
                                await Task.WhenAll(killTasks);

                                // Then, delete the file
                                try
                                {
                                    if (File.Exists(fullPath))
                                    {
                                        File.Delete(fullPath);
                                    }
                                }
                                catch (Exception ex)
                                {
                                    Console.Error.WriteLine($"Failed to delete file at \"{fullPath}\": {ex.Message}");
                                }
                            }
                            catch (Exception ex)
                            {
                                Console.Error.WriteLine($"Failed to release handles of \"{fullPath}\": {ex.Message}");
                            }
                        }

                        // Remove from mapping
                        _terrainGenerator.RemoveMapping(entry.Position);
                    }));
                }

                await Task.WhenAll(tasks);

            }, rx, rz);
        });
    }
}
