using System.Collections.Concurrent;
using System.IO.Compression;
using System.Security.Cryptography;
using fNbt;
using SaplingFS.Models;

namespace SaplingFS.Services;

/// <summary>
/// Handles reading and writing Minecraft region files (.mca format).
/// Implements NBT parsing, chunk extraction, and block manipulation.
/// </summary>
public class WorldParser
{
    private readonly ConcurrentDictionary<string, RegionFileCache> _regionFileCache = new();

    /// <summary>
    /// Extracts block arrays from a region (.mca) file.
    /// </summary>
    /// <param name="regionBytes">Region file byte buffer</param>
    /// <param name="blocks">3D array to write blocks to [x][y][z]</param>
    /// <param name="rx">Region file X coordinate</param>
    /// <param name="rz">Region file Z coordinate</param>
    /// <param name="bounds">Relative boundaries of blocks array [min, max]</param>
    /// <param name="expectHash">Expected hash for first chunk. If matches, returns null early.</param>
    /// <returns>Hash of first chunk on success, null if unchanged or error</returns>
    public async Task<ulong?> RegionToBlocksAsync(
        byte[] regionBytes,
        string[][][] blocks,
        int rx,
        int rz,
        (Vector Min, Vector Max) bounds,
        ulong? expectHash = null)
    {
        ulong? firstChunkHash = null;

        var (xMin, yMin, zMin) = (bounds.Min.X, bounds.Min.Y, bounds.Min.Z);
        var (xMax, yMax, zMax) = (bounds.Max.X, bounds.Max.Y, bounds.Max.Z);

        // Region files contain 32x32 chunks
        for (int i = 0; i < 1024; i++)
        {
            int chunkX = rx * 32 + (i % 32);
            int chunkZ = rz * 32 + (i / 32);

            // Skip chunks outside bounds
            if (chunkX < xMin / 16) continue;
            if (chunkX >= xMax / 16) continue;
            if (chunkZ < zMin / 16) continue;
            if (chunkZ >= zMax / 16) continue;

            // Read chunk location from header (first 4096 bytes)
            int offset = (regionBytes[i * 4] << 16) + (regionBytes[i * 4 + 1] << 8) + regionBytes[i * 4 + 2];
            int sectors = regionBytes[i * 4 + 3];

            if (offset == 0 || sectors == 0)
                continue; // Chunk doesn't exist

            // Read chunk data length and compression type
            int dataOffset = offset * 4096;
            int length = (regionBytes[dataOffset] << 24) + (regionBytes[dataOffset + 1] << 16) +
                        (regionBytes[dataOffset + 2] << 8) + regionBytes[dataOffset + 3];
            int compression = regionBytes[dataOffset + 4];

            // Extract compressed chunk data
            byte[] compressedData = new byte[length - 1]; // -1 because length includes compression byte
            Array.Copy(regionBytes, dataOffset + 5, compressedData, 0, length - 1);

            // Check first chunk hash for early exit
            if (firstChunkHash == null)
            {
                firstChunkHash = ComputeHash(compressedData);
                if (expectHash.HasValue && firstChunkHash == expectHash.Value)
                    return null;
            }

            // Decompress chunk data (compression type 2 = zlib)
            byte[] decompressedData;
            try
            {
                decompressedData = await DecompressZlibAsync(compressedData);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Warning: Chunk ({chunkX} {chunkZ}) in r.{rx}.{rz} has likely been corrupted");
                Console.WriteLine(ex.Message);
                return null;
            }

            // Parse NBT data
            var nbtFile = new NbtFile();
            using (var stream = new MemoryStream(decompressedData))
            {
                nbtFile.LoadFromStream(stream, NbtCompression.None);
            }

            var root = nbtFile.RootTag;
            var sectionsTag = root["sections"] as NbtList;
            if (sectionsTag == null)
                continue;

            // Process each 16x16x16 section in the chunk
            foreach (NbtCompound section in sectionsTag)
            {
                if (!section.Contains("block_states"))
                    continue;

                var yTag = section["Y"];
                if (yTag == null) continue;

                int sectionY = yTag.ByteValue;
                var blockStates = section["block_states"] as NbtCompound;
                var paletteTag = blockStates?["palette"] as NbtList;
                if (paletteTag == null || blockStates == null)
                    continue;

                // Extract palette (block names)
                var palette = new List<string>();
                foreach (NbtCompound paletteEntry in paletteTag)
                {
                    var nameTag = paletteEntry["Name"];
                    if (nameTag == null) continue;

                    string name = nameTag.StringValue;
                    // Remove "minecraft:" prefix
                    palette.Add(name.StartsWith("minecraft:") ? name.Substring(10) : name);
                }

                // If no data array, all blocks use palette[0]
                if (!blockStates.Contains("data"))
                {
                    for (int blockY = 0; blockY < 16; blockY++)
                    {
                        for (int blockZ = 0; blockZ < 16; blockZ++)
                        {
                            for (int blockX = 15; blockX >= 0; blockX--)
                            {
                                int x = chunkX * 16 + blockX;
                                int y = sectionY * 16 + blockY;
                                int z = chunkZ * 16 + blockZ;

                                if (x < xMin || x >= xMax || y < yMin || y >= yMax || z < zMin || z >= zMax)
                                    continue;

                                blocks[x - xMin][y - yMin][z - zMin] = palette[0];
                            }
                        }
                    }
                    continue;
                }

                // Extract block data from packed long array
                var dataTag = blockStates["data"] as NbtLongArray;
                if (dataTag == null)
                    continue;

                long[] longs = dataTag.Value;

                if (sectionY < yMin / 16) continue;
                if (sectionY >= yMax / 16) continue;

                // Decode 4-bit palette indices from packed longs
                for (int j = 0; j < longs.Length; j++)
                {
                    for (int k = 0; k < 16; k++)
                    {
                        // Extract 4-bit nibble
                        int shift = k < 8 ? (28 - k * 4) : (28 - (k - 8) * 4);
                        long longValue = k < 8 ? (longs[j] >> 32) : longs[j];
                        int paletteId = (int)((longValue >> shift) & 0b1111);

                        int x = chunkX * 16 + 15 - k;
                        int y = sectionY * 16 + (j / 16);
                        int z = chunkZ * 16 + (j % 16);

                        if (x < xMin || x >= xMax || y < yMin || y >= yMax || z < zMin || z >= zMax)
                            continue;

                        blocks[x - xMin][y - yMin][z - zMin] = palette[paletteId];
                    }
                }
            }
        }

        return firstChunkHash;
    }

    /// <summary>
    /// Applies the given block array to a region file.
    /// </summary>
    /// <param name="blocks">3D array of block name strings [x][y][z]</param>
    /// <param name="regionBytes">Region file byte buffer to modify</param>
    /// <param name="rx">Region file X coordinate</param>
    /// <param name="rz">Region file Z coordinate</param>
    /// <param name="bounds">Relative boundaries of blocks array [min, max]</param>
    /// <returns>Modified region file bytes</returns>
    public async Task<byte[]> BlocksToRegionAsync(
        string[][][] blocks,
        byte[] regionBytes,
        int rx,
        int rz,
        (Vector Min, Vector Max) bounds)
    {
        var (xMin, yMin, zMin) = (bounds.Min.X, bounds.Min.Y, bounds.Min.Z);
        var (xMax, yMax, zMax) = (bounds.Max.X, bounds.Max.Y, bounds.Max.Z);

        // Create a copy to modify
        byte[] result = new byte[regionBytes.Length];
        Array.Copy(regionBytes, result, regionBytes.Length);

        for (int i = 0; i < 1024; i++)
        {
            int chunkX = rx * 32 + (i % 32);
            int chunkZ = rz * 32 + (i / 32);

            if (chunkX < xMin / 16) continue;
            if (chunkX >= xMax / 16) continue;
            if (chunkZ < zMin / 16) continue;
            if (chunkZ >= zMax / 16) continue;

            int offset = (result[i * 4] << 16) + (result[i * 4 + 1] << 8) + result[i * 4 + 2];
            int sectors = result[i * 4 + 3];

            if (offset == 0 || sectors == 0)
                continue;

            int dataOffset = offset * 4096;
            int length = (result[dataOffset] << 24) + (result[dataOffset + 1] << 16) +
                        (result[dataOffset + 2] << 8) + result[dataOffset + 3];

            byte[] compressedData = new byte[length - 1];
            Array.Copy(result, dataOffset + 5, compressedData, 0, length - 1);

            // Decompress and parse NBT
            byte[] decompressedData;
            try
            {
                decompressedData = await DecompressZlibAsync(compressedData);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Warning: Chunk ({chunkX} {chunkZ}) in r.{rx}.{rz} has likely been corrupted");
                Console.WriteLine(ex.Message);
                continue;
            }

            var nbtFile = new NbtFile();
            using (var stream = new MemoryStream(decompressedData))
            {
                nbtFile.LoadFromStream(stream, NbtCompression.None);
            }

            var root = nbtFile.RootTag;
            var sectionsTag = root["sections"] as NbtList;
            if (sectionsTag == null)
                continue;

            // Rebuild each section with new block data
            foreach (NbtCompound section in sectionsTag)
            {
                var yTag = section["Y"];
                if (yTag == null) continue;

                int sectionY = yTag.ByteValue;

                var ids = new List<int>();
                var palette = new List<string>();

                // Gather all blocks in this section
                for (int y = sectionY * 16; y < sectionY * 16 + 16; y++)
                {
                    for (int z = chunkZ * 16; z < chunkZ * 16 + 16; z++)
                    {
                        for (int x = chunkX * 16; x < chunkX * 16 + 16; x++)
                        {
                            string block = "air";

                            if (x >= xMin && x < xMax && y >= yMin && y < yMax && z >= zMin && z < zMax)
                            {
                                block = blocks[x - xMin][y - yMin][z - zMin];
                            }

                            if (!palette.Contains(block))
                                palette.Add(block);

                            ids.Add(palette.IndexOf(block));
                        }
                    }
                }

                // Pack IDs into long array (4 bits per block)
                var longs = new List<long>();
                for (int j = 0; j < ids.Count; j += 16)
                {
                    long high = 0;
                    long low = 0;

                    // Pack 8 blocks into each 32-bit half
                    for (int k = 0; k < 8; k++)
                    {
                        high |= (long)ids[j + 15 - k] << (28 - k * 4);
                        low |= (long)ids[j + 7 - k] << (28 - k * 4);
                    }

                    longs.Add((high << 32) | (low & 0xFFFFFFFF));
                }

                // Build new block_states NBT
                var paletteTag = new NbtList("palette", NbtTagType.Compound);
                foreach (var blockName in palette)
                {
                    var entry = new NbtCompound
                    {
                        new NbtString("Name", "minecraft:" + blockName)
                    };
                    paletteTag.Add(entry);
                }

                var blockStates = new NbtCompound("block_states")
                {
                    paletteTag
                };

                if (palette.Count > 1)
                {
                    blockStates.Add(new NbtLongArray("data", longs.ToArray()));
                }

                // Replace old block_states
                section.Remove("block_states");
                section.Add(blockStates);

                // Update SkyLight
                section.Remove("SkyLight");
                section.Add(new NbtByteArray("SkyLight", Enumerable.Repeat((byte)255, 2048).ToArray()));
            }

            // Write modified NBT back
            byte[] uncompressed;
            using (var stream = new MemoryStream())
            {
                nbtFile.SaveToStream(stream, NbtCompression.None);
                uncompressed = stream.ToArray();
            }

            byte[] compressed = await CompressZlibAsync(uncompressed);

            int newLength = compressed.Length;
            if (newLength + 5 > 4096)
            {
                Console.WriteLine($"Warning: Chunk ({chunkX} {chunkZ}) exceeds available space. Expect a missing chunk.");
                continue;
            }

            // Write compressed data back to region file
            Array.Copy(compressed, 0, result, dataOffset + 5, compressed.Length);

            // Update chunk length header
            result[dataOffset + 3] = (byte)(newLength & 0xFF);
            result[dataOffset + 2] = (byte)((newLength >> 8) & 0xFF);
            result[dataOffset + 1] = (byte)((newLength >> 16) & 0xFF);
            result[dataOffset + 0] = (byte)((newLength >> 24) & 0xFF);
            result[dataOffset + 4] = 2; // zlib compression
        }

        return result;
    }

    /// <summary>
    /// Iterates over all region files within the given bounds.
    /// </summary>
    /// <param name="worldPath">Path to Minecraft world directory</param>
    /// <param name="bounds">Absolute block boundaries</param>
    /// <param name="callback">Function to call for each region file</param>
    public async Task ForRegionAsync(
        string worldPath,
        (Vector Min, Vector Max) bounds,
        Func<RegionFileCache, int, int, Task> callback)
    {
        var (xMin, yMin, zMin) = (bounds.Min.X, bounds.Min.Y, bounds.Min.Z);
        var (xMax, yMax, zMax) = (bounds.Max.X, bounds.Max.Y, bounds.Max.Z);

        int rxMin = (int)Math.Floor(xMin / (16.0 * 32));
        int rxMax = (int)Math.Ceiling(xMax / (16.0 * 32));
        int rzMin = (int)Math.Floor(zMin / (16.0 * 32));
        int rzMax = (int)Math.Ceiling(zMax / (16.0 * 32));

        for (int rx = rxMin; rx < rxMax; rx++)
        {
            for (int rz = rzMin; rz < rzMax; rz++)
            {
                string mcaFile = $"r.{rx}.{rz}.mca";
                string path = Path.Combine(worldPath, "region", mcaFile);

                if (!File.Exists(path))
                    continue;

                byte[] bytes = await File.ReadAllBytesAsync(path);
                ulong checksum = ComputeHash(bytes);

                // Update cache if changed
                if (!_regionFileCache.TryGetValue(mcaFile, out var cached) || cached.Checksum != checksum)
                {
                    cached = new RegionFileCache(bytes, checksum);
                    _regionFileCache[mcaFile] = cached;
                }

                await callback(cached, rx, rz);
            }
        }
    }

    /// <summary>
    /// Fills the region file cache with all available region files.
    /// </summary>
    /// <param name="worldPath">Path to Minecraft world directory</param>
    public async Task FillRegionFileCacheAsync(string worldPath)
    {
        string regionDir = Path.Combine(worldPath, "region");
        if (!Directory.Exists(regionDir))
            return;

        var files = Directory.GetFiles(regionDir, "r.*.mca");
        foreach (var path in files)
        {
            string fileName = Path.GetFileName(path);
            byte[] bytes = await File.ReadAllBytesAsync(path);
            ulong checksum = ComputeHash(bytes);
            _regionFileCache[fileName] = new RegionFileCache(bytes, checksum);
        }
    }

    /// <summary>
    /// Gets the current region file cache.
    /// </summary>
    public IReadOnlyDictionary<string, RegionFileCache> RegionFileCache => _regionFileCache;

    private static async Task<byte[]> DecompressZlibAsync(byte[] compressed)
    {
        using var input = new MemoryStream(compressed);
        using var zlib = new ZLibStream(input, CompressionMode.Decompress);
        using var output = new MemoryStream();
        await zlib.CopyToAsync(output);
        return output.ToArray();
    }

    private static async Task<byte[]> CompressZlibAsync(byte[] uncompressed)
    {
        using var output = new MemoryStream();
        using (var zlib = new ZLibStream(output, CompressionLevel.Optimal))
        {
            await zlib.WriteAsync(uncompressed, 0, uncompressed.Length);
        }
        return output.ToArray();
    }

    private static ulong ComputeHash(byte[] data)
    {
        // Use SHA256 and take first 8 bytes as ulong for checksum
        using var sha = SHA256.Create();
        byte[] hash = sha.ComputeHash(data);
        return BitConverter.ToUInt64(hash, 0);
    }
}
