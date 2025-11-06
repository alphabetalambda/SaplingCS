using System.Collections.Concurrent;
using SaplingFS.Models;

namespace SaplingFS.Services;

/// <summary>
/// Handles terrain generation for SaplingFS, mapping files to Minecraft blocks.
/// Implements breadth-first search algorithm with organic shape generation.
/// </summary>
public class TerrainGenerator
{
    private readonly WorldParser _worldParser;
    private readonly ConcurrentDictionary<string, BlockMapping> _mapping = new();
    private Vector _terrainBoundsMin = new(0, 0, 0);
    private Vector _terrainBoundsMax = new(0, 0, 0);

    // Absolute world boundaries - blocks won't generate past these
    private static readonly Vector WorldBoundsMin = new(-16 * 20, -64, -16 * 20);
    private static readonly Vector WorldBoundsMax = new(16 * 20, 320, 16 * 20);

    // Debug palette for colorful terrain groups
    private static readonly string[] DebugPalette =
    {
        "red_wool", "orange_wool", "yellow_wool", "lime_wool",
        "cyan_wool", "light_blue_wool", "magenta_wool", "pink_wool"
    };

    public TerrainGenerator(WorldParser worldParser)
    {
        _worldParser = worldParser;
    }

    /// <summary>
    /// Gets the current block-to-file mapping.
    /// </summary>
    public IReadOnlyDictionary<string, BlockMapping> Mapping => _mapping;

    /// <summary>
    /// Gets the current terrain boundaries.
    /// </summary>
    public (Vector Min, Vector Max) TerrainBounds => (_terrainBoundsMin, _terrainBoundsMax);

    /// <summary>
    /// Removes a mapping entry from the dictionary.
    /// </summary>
    /// <param name="position">Position of the block to remove</param>
    public void RemoveMapping(Vector position)
    {
        string key = position.ToString();
        _mapping.TryRemove(key, out _);
    }

    /// <summary>
    /// Adds a mapping entry to the dictionary.
    /// </summary>
    /// <param name="mapping">The mapping to add</param>
    public void AddMapping(BlockMapping mapping)
    {
        string key = mapping.Position.ToString();
        _mapping[key] = mapping;
    }

    /// <summary>
    /// Updates the terrain bounds to include a new position.
    /// </summary>
    /// <param name="position">Position to include in bounds</param>
    public void UpdateTerrainBounds(Vector position)
    {
        if (position.X < _terrainBoundsMin.X) _terrainBoundsMin = _terrainBoundsMin with { X = position.X };
        else if (position.X > _terrainBoundsMax.X) _terrainBoundsMax = _terrainBoundsMax with { X = position.X };

        if (position.Y < _terrainBoundsMin.Y) _terrainBoundsMin = _terrainBoundsMin with { Y = position.Y };
        else if (position.Y > _terrainBoundsMax.Y) _terrainBoundsMax = _terrainBoundsMax with { Y = position.Y };

        if (position.Z < _terrainBoundsMin.Z) _terrainBoundsMin = _terrainBoundsMin with { Z = position.Z };
        else if (position.Z > _terrainBoundsMax.Z) _terrainBoundsMax = _terrainBoundsMax with { Z = position.Z };
    }

    /// <summary>
    /// Checks if a block is natural ground terrain.
    /// </summary>
    private static bool IsGroundBlock(string? block) =>
        block is "dirt" or "grass_block" or "stone";

    /// <summary>
    /// Checks if a block is heavy enough to convert grass to dirt beneath it.
    /// </summary>
    private static bool IsHeavyBlock(string? block) =>
        IsGroundBlock(block) || block is "oak_log" or "water";

    /// <summary>
    /// Checks if a block is air or unallocated.
    /// </summary>
    private static bool IsAir(string? block) =>
        string.IsNullOrEmpty(block) || block == "air";

    /// <summary>
    /// Counts adjacent blocks around a position in the mapping.
    /// </summary>
    private int CountAdjacent(Vector pos)
    {
        int count = 0;
        for (int i = 0; i < 6; i++)
        {
            if (_mapping.ContainsKey(pos.Shifted(i).ToString()))
                count++;
        }
        return count;
    }

    /// <summary>
    /// Iterates over all 62 blocks that make up a tree.
    /// </summary>
    private static void ForTreeBlocks(Vector pos, Action<Vector, string> callback)
    {
        // Tree stump (5 blocks)
        for (int i = 0; i < 5; i++)
        {
            callback(pos.Add(0, i, 0), "oak_log");
        }

        // Bottom leaf layers (2 layers)
        for (int i = 0; i < 2; i++)
        {
            for (int j = -2; j <= 2; j++)
            {
                for (int k = -2; k <= 2; k++)
                {
                    if (j == 0 && k == 0) continue;
                    if (i == 1 && Math.Abs(j) == 2 && Math.Abs(k) == 2) continue;
                    callback(pos.Add(j, i + 2, k), "oak_leaves");
                }
            }
        }

        // Top leaf layers (2 layers)
        for (int i = 0; i < 2; i++)
        {
            for (int j = -1; j <= 1; j++)
            {
                for (int k = -1; k <= 1; k++)
                {
                    if (i == 0 && j == 0 && k == 0) continue;
                    if (i == 1 && j != 0 && k != 0) continue;
                    callback(pos.Add(j, i + 4, k), "oak_leaves");
                }
            }
        }
    }

    /// <summary>
    /// Main terrain generation using breadth-first search algorithm.
    /// Groups files by parent directories to create distinct terrain islands.
    /// </summary>
    public async Task BuildRegionDataAsync(
        List<MappedFile> fileList,
        int parentDepth,
        string worldPath,
        bool debug = false,
        Action<int, string>? progressCallback = null)
    {
        var nodes = new Queue<Vector>();
        nodes.Enqueue(new Vector(0, 32, 0));

        int terrainGroup = 0;
        string lastParent = "";
        int totalFiles = fileList.Count;
        int processedFiles = 0;

        // Track trees and ponds to place after current terrain group
        var trees = new List<(Vector Pos, List<MappedFile> Files)>();
        var ponds = new List<Vector>();

        var random = new Random();

        // Random direction suppression for organic shapes
        int suppressFor = 0;
        int suppressDirection = 0;

        // First pass - general terrain shape and features
        while (fileList.Count > 0 && nodes.Count > 0)
        {
            var pos = nodes.Dequeue();
            string key = pos.ToString();

            // Check if position is valid
            if (_mapping.ContainsKey(key) ||
                pos.X < WorldBoundsMin.X || pos.X > WorldBoundsMax.X ||
                pos.Y < WorldBoundsMin.Y || pos.Y > WorldBoundsMax.Y ||
                pos.Z < WorldBoundsMin.Z || pos.Z > WorldBoundsMax.Z)
            {
                // If we ran out of nodes, pick a random position in existing terrain
                if (nodes.Count == 0)
                {
                    Vector rand;
                    do
                    {
                        rand = new Vector(
                            random.Next(_terrainBoundsMin.X, _terrainBoundsMax.X),
                            random.Next(64),
                            random.Next(_terrainBoundsMin.Z, _terrainBoundsMax.Z)
                        );
                    } while (_mapping.ContainsKey(rand.ToString()));
                    nodes.Enqueue(rand);
                }
                continue;
            }

            // Update suppression for organic shapes
            suppressFor--;
            if (suppressFor <= 0)
            {
                suppressFor = random.Next(nodes.Count / 5);
                suppressDirection = random.Next(4);
            }

            var file = fileList[0];
            fileList.RemoveAt(0);
            string shortParent = file.GetShortParent(parentDepth);

            // When parent directory changes, finalize current terrain group
            if (!string.IsNullOrEmpty(lastParent) && lastParent != shortParent)
            {
                await FinalizeTerrainGroupAsync(trees, ponds, fileList, random);
                nodes.Clear();
                terrainGroup++;
            }

            if (lastParent != shortParent)
            {
                progressCallback?.Invoke(processedFiles, shortParent);
                lastParent = shortParent;
            }

            // Assign block type
            string blockType = debug
                ? DebugPalette[terrainGroup % DebugPalette.Length]
                : "grass_block";

            _mapping[key] = new BlockMapping(pos, file.Path, blockType);
            processedFiles++;

            // Report progress periodically
            if (progressCallback != null && processedFiles % 100 == 0)
            {
                progressCallback(processedFiles, shortParent);
            }

            // Update terrain bounds
            if (pos.X < _terrainBoundsMin.X) _terrainBoundsMin = _terrainBoundsMin with { X = pos.X };
            else if (pos.X > _terrainBoundsMax.X) _terrainBoundsMax = _terrainBoundsMax with { X = pos.X };
            if (pos.Y < _terrainBoundsMin.Y) _terrainBoundsMin = _terrainBoundsMin with { Y = pos.Y };
            else if (pos.Y > _terrainBoundsMax.Y) _terrainBoundsMax = _terrainBoundsMax with { Y = pos.Y };
            if (pos.Z < _terrainBoundsMin.Z) _terrainBoundsMin = _terrainBoundsMin with { Z = pos.Z };
            else if (pos.Z > _terrainBoundsMax.Z) _terrainBoundsMax = _terrainBoundsMax with { Z = pos.Z };

            // Count adjacent blocks
            int adjacent = CountAdjacent(pos);

            // Add neighboring positions to node queue (horizontal)
            for (int i = 0; i < 4; i++)
            {
                if (adjacent < 3 && suppressDirection == i) continue;
                nodes.Enqueue(pos.Shifted(i));
            }

            // Occasionally add vertical neighbors
            if (pos.Y < 127 && random.NextDouble() < 0.05)
                nodes.Enqueue(pos.Add(0, 1, 0));
            if (pos.Y > -64 && random.NextDouble() < 0.05)
                nodes.Enqueue(pos.Add(0, -1, 0));

            // Randomly queue water bodies
            if (random.Next(10000) == 0)
            {
                ponds.Add(pos);
            }

            // Randomly queue trees
            if (random.Next(5000) == 0)
            {
                if (fileList.Count < 62) continue;

                // Check if too close to existing trees
                bool tooClose = trees.Any(t =>
                    Math.Abs(t.Pos.X - pos.X) < 5 &&
                    Math.Abs(t.Pos.Z - pos.Z) < 5);

                if (!tooClose)
                {
                    var treeFiles = fileList.Take(62).ToList();
                    fileList.RemoveRange(0, 62);
                    trees.Add((pos, treeFiles));
                }
            }
        }

        // Second pass - smooth terrain and write to region files
        progressCallback?.Invoke(totalFiles, "Writing terrain to region files");

        await ForMappedChunksAsync(async (blocks, entries, chunkX, chunkZ, bounds) =>
        {
            // Smooth out terrain by moving lonely blocks
            int swaps;
            do
            {
                swaps = 0;
                foreach (var originalEntry in entries.ToList())
                {
                    if (!IsGroundBlock(originalEntry.Block)) continue;

                    var pos = originalEntry.Position;
                    int adjacent = CountAdjacent(pos);

                    // Temporarily remove current mapping
                    string key = pos.ToString();
                    _mapping.TryRemove(key, out _);

                    int bestAdjacent = adjacent;
                    (Vector Rel, Vector Abs)? bestPosition = null;

                    // Check all 6 directions for better clustering
                    for (int i = 0; i < 6; i++)
                    {
                        var abs = pos.Shifted(i);
                        var rel = abs.Relative(chunkX, chunkZ);

                        // Check chunk boundaries
                        if (rel.X < 0 || rel.X >= 16 ||
                            rel.Z < 0 || rel.Z >= 16 ||
                            rel.Y < 0 || rel.Y >= 384)
                            continue;

                        // Abort if next to water
                        string absKey = abs.ToString();
                        if (_mapping.TryGetValue(absKey, out var neighbor) && neighbor.Block == "water")
                        {
                            bestAdjacent = adjacent;
                            break;
                        }

                        // Skip if position occupied
                        if (_mapping.ContainsKey(absKey)) continue;

                        int newAdjacent = CountAdjacent(abs);
                        if (newAdjacent > bestAdjacent)
                        {
                            bestAdjacent = newAdjacent;
                            bestPosition = (rel, abs);
                        }
                    }

                    // Restore or move block
                    if (bestPosition == null)
                    {
                        // Convert single stubs to short grass
                        var updatedEntry = originalEntry;
                        if (adjacent == 1)
                        {
                            var belowKey = pos.Add(0, -1, 0).ToString();
                            if (_mapping.TryGetValue(belowKey, out var below) &&
                                below.Block == "grass_block")
                            {
                                updatedEntry = originalEntry with { Block = "short_grass" };
                            }
                        }
                        _mapping[key] = updatedEntry;
                    }
                    else
                    {
                        // Move block to better position
                        var movedEntry = originalEntry with { Position = bestPosition.Value.Abs };
                        _mapping[bestPosition.Value.Abs.ToString()] = movedEntry;
                        swaps++;
                    }
                }
            } while (swaps > 0);

            // Apply natural terrain rules
            foreach (var originalEntry in entries.ToList())
            {
                string key = originalEntry.Position.ToString();

                // Get current entry from mapping (may have been updated)
                if (!_mapping.TryGetValue(key, out var entry))
                    continue;

                // Convert covered grass to dirt
                var aboveKey = entry.Position.Add(0, 1, 0).ToString();
                if (entry.Block == "grass_block" &&
                    _mapping.TryGetValue(aboveKey, out var above) &&
                    IsHeavyBlock(above.Block))
                {
                    var newBlock = "dirt";

                    // Convert deeply buried blocks to stone
                    bool submerged = true;
                    for (int i = 1; i <= 3; i++)
                    {
                        var checkKey = entry.Position.Add(0, i, 0).ToString();
                        if (!_mapping.TryGetValue(checkKey, out var checkBlock) ||
                            !IsGroundBlock(checkBlock.Block))
                        {
                            submerged = false;
                            break;
                        }
                    }

                    if (submerged) newBlock = "stone";

                    entry = entry with { Block = newBlock };
                    _mapping[key] = entry;
                }

                // Convert isolated blocks in water to water
                int waterAdjacent = 0;
                for (int i = 0; i < 6; i++)
                {
                    var neighborKey = entry.Position.Shifted(i).ToString();
                    if (_mapping.TryGetValue(neighborKey, out var neighbor) &&
                        neighbor.Block == "water")
                    {
                        waterAdjacent++;
                    }
                    else if (i < 4)
                    {
                        waterAdjacent = 0;
                        break;
                    }
                }

                if (waterAdjacent >= 5)
                {
                    entry = entry with { Block = "water" };
                    _mapping[key] = entry;
                }

                // Assign block to chunk array
                var relPos = entry.Position.Relative(chunkX, chunkZ);
                blocks[relPos.X][relPos.Y][relPos.Z] = entry.Block;
            }

            // Add random ore veins
            var rand = new Random();
            for (int i = 0; i < 3; i++)
            {
                if (rand.NextDouble() < 0.1)
                {
                    int x = rand.Next(16);
                    int y = rand.Next(0, 64);
                    int z = rand.Next(16);

                    string[] ores = { "coal_ore", "iron_ore", "gold_ore" };
                    blocks[x][y][z] = ores[rand.Next(ores.Length)];
                }
            }

            // Write chunk to region file
            await WriteChunkToRegionAsync(blocks, chunkX, chunkZ, bounds, worldPath);
        });
    }

    /// <summary>
    /// Finalizes a terrain group by placing trees and water bodies.
    /// </summary>
    private async Task FinalizeTerrainGroupAsync(
        List<(Vector Pos, List<MappedFile> Files)> trees,
        List<Vector> ponds,
        List<MappedFile> fileList,
        Random random)
    {
        // Place water bodies
        foreach (var pond in ponds)
        {
            // Find surface level at pond position
            var candidates = _mapping.Values
                .Where(c => c.Position.X == pond.X && c.Position.Z == pond.Z)
                .OrderByDescending(c => c.Position.Y)
                .ToList();

            if (!candidates.Any()) continue;

            var surfacePos = candidates[0].Position;
            var fillNodes = new Queue<Vector>();
            fillNodes.Enqueue(surfacePos);

            while (fillNodes.Count > 0 && random.Next(2000) != 0)
            {
                var curr = fillNodes.Dequeue();
                string key = curr.ToString();

                if (!_mapping.ContainsKey(key)) continue;

                // Check if too close to trees
                bool nearTree = trees.Any(t =>
                    Math.Abs(t.Pos.X - curr.X) < 3 &&
                    Math.Abs(t.Pos.Z - curr.Z) < 3);

                if (nearTree) continue;

                // Check if ground blocks above
                var aboveKey = curr.Add(0, 1, 0).ToString();
                if (_mapping.TryGetValue(aboveKey, out var above) && IsGroundBlock(above.Block))
                    continue;

                // Check neighbors
                int neighbors = 0;
                bool skip = false;
                for (int i = 0; i < 6; i++)
                {
                    var neighborKey = curr.Shifted(i).ToString();
                    if (_mapping.TryGetValue(neighborKey, out var neighbor))
                    {
                        if (i < 4 && IsAir(neighbor.Block))
                        {
                            skip = true;
                            break;
                        }
                        if (neighbor.Block == "water") neighbors++;
                    }
                }

                if (skip) continue;

                // Convert to water
                _mapping[key] = _mapping[key] with { Block = "water" };

                // Expand water
                for (int i = 0; i < 4; i++)
                {
                    if (neighbors < 3 && random.NextDouble() < 0.1) continue;
                    fillNodes.Enqueue(curr.Shifted(i));
                }

                if (curr.Y < 127 && random.NextDouble() < 0.05)
                    fillNodes.Enqueue(curr.Add(0, 1, 0));
                if (curr.Y > -64 && random.NextDouble() < 0.05)
                    fillNodes.Enqueue(curr.Add(0, -1, 0));
            }
        }

        ponds.Clear();

        // Place trees
        foreach (var tree in trees)
        {
            // Find surface level at tree position
            var candidates = _mapping.Values
                .Where(c => c.Position.X == tree.Pos.X && c.Position.Z == tree.Pos.Z)
                .OrderByDescending(c => c.Position.Y)
                .ToList();

            if (!candidates.Any()) continue;

            var treeBase = candidates[0].Position.Add(0, 1, 0);

            ForTreeBlocks(treeBase, (pos, block) =>
            {
                if (tree.Files.Count == 0) return;

                string key = pos.ToString();
                var file = tree.Files[^1];
                tree.Files.RemoveAt(tree.Files.Count - 1);

                if (_mapping.ContainsKey(key))
                {
                    fileList.Add(file);
                }
                else
                {
                    _mapping[key] = new BlockMapping(pos, file.Path, block);
                }
            });
        }

        trees.Clear();
        await Task.CompletedTask;
    }

    /// <summary>
    /// Iterates over all chunks with mapped blocks.
    /// </summary>
    public async Task ForMappedChunksAsync(
        Func<string[][][], List<BlockMapping>, int, int, (Vector Min, Vector Max), Task> callback)
    {
        var processedChunks = new HashSet<(int, int)>();

        foreach (var entry in _mapping.Values)
        {
            int chunkX = (int)Math.Floor(entry.Position.X / 16.0);
            int chunkZ = (int)Math.Floor(entry.Position.Z / 16.0);

            if (processedChunks.Contains((chunkX, chunkZ)))
                continue;

            processedChunks.Add((chunkX, chunkZ));

            // Create empty block array
            var blocks = new string[16][][];
            for (int x = 0; x < 16; x++)
            {
                blocks[x] = new string[384][];
                for (int y = 0; y < 384; y++)
                {
                    blocks[x][y] = new string[16];
                    for (int z = 0; z < 16; z++)
                    {
                        blocks[x][y][z] = "air";
                    }
                }
            }

            // Get all entries in this chunk
            var entries = _mapping.Values
                .Where(e =>
                {
                    int ex = (int)Math.Floor(e.Position.X / 16.0);
                    int ez = (int)Math.Floor(e.Position.Z / 16.0);
                    return ex == chunkX && ez == chunkZ;
                })
                .ToList();

            var bounds = (
                new Vector(chunkX * 16, -64, chunkZ * 16),
                new Vector(chunkX * 16 + 16, 320, chunkZ * 16 + 16)
            );

            await callback(blocks, entries, chunkX, chunkZ, bounds);
        }
    }

    /// <summary>
    /// Iterates over all chunks with mapped blocks in a specific region.
    /// </summary>
    public async Task ForMappedChunksAsync(
        Func<string[][][], List<BlockMapping>, int, int, (Vector Min, Vector Max), Task> callback,
        int regionX,
        int regionZ)
    {
        var processedChunks = new HashSet<(int, int)>();

        // Only process chunks within this region
        int minChunkX = regionX * 32;
        int maxChunkX = regionX * 32 + 32;
        int minChunkZ = regionZ * 32;
        int maxChunkZ = regionZ * 32 + 32;

        foreach (var entry in _mapping.Values)
        {
            int chunkX = (int)Math.Floor(entry.Position.X / 16.0);
            int chunkZ = (int)Math.Floor(entry.Position.Z / 16.0);

            // Skip if not in this region
            if (chunkX < minChunkX || chunkX >= maxChunkX ||
                chunkZ < minChunkZ || chunkZ >= maxChunkZ)
                continue;

            if (processedChunks.Contains((chunkX, chunkZ)))
                continue;

            processedChunks.Add((chunkX, chunkZ));

            // Create empty block array
            var blocks = new string[16][][];
            for (int x = 0; x < 16; x++)
            {
                blocks[x] = new string[384][];
                for (int y = 0; y < 384; y++)
                {
                    blocks[x][y] = new string[16];
                    for (int z = 0; z < 16; z++)
                    {
                        blocks[x][y][z] = "air";
                    }
                }
            }

            // Get all entries in this chunk
            var entries = _mapping.Values
                .Where(e =>
                {
                    int ex = (int)Math.Floor(e.Position.X / 16.0);
                    int ez = (int)Math.Floor(e.Position.Z / 16.0);
                    return ex == chunkX && ez == chunkZ;
                })
                .ToList();

            var bounds = (
                new Vector(chunkX * 16, -64, chunkZ * 16),
                new Vector(chunkX * 16 + 16, 320, chunkZ * 16 + 16)
            );

            await callback(blocks, entries, chunkX, chunkZ, bounds);
        }
    }

    /// <summary>
    /// Writes a chunk to the appropriate region file.
    /// </summary>
    private async Task WriteChunkToRegionAsync(
        string[][][] blocks,
        int chunkX,
        int chunkZ,
        (Vector Min, Vector Max) bounds,
        string worldPath)
    {
        int regionX = (int)Math.Floor(chunkX / 32.0);
        int regionZ = (int)Math.Floor(chunkZ / 32.0);

        string regionPath = Path.Combine(worldPath, "region", $"r.{regionX}.{regionZ}.mca");

        // Read existing region file or create new one
        byte[] regionBytes;
        if (File.Exists(regionPath))
        {
            regionBytes = await File.ReadAllBytesAsync(regionPath);
        }
        else
        {
            // Create empty region file (8 MB)
            regionBytes = new byte[8 * 1024 * 1024];
        }

        // Write blocks to region
        regionBytes = await _worldParser.BlocksToRegionAsync(blocks, regionBytes, regionX, regionZ, bounds);

        // Save region file
        await File.WriteAllBytesAsync(regionPath, regionBytes);
    }
}
