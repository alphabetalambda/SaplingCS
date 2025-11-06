namespace SaplingFS.Models;

/// <summary>
/// Represents a cached Minecraft region file with its byte data and checksum.
/// </summary>
public record RegionFileCache
{
    /// <summary>
    /// The raw byte data of the region file.
    /// </summary>
    public byte[] Bytes { get; init; }

    /// <summary>
    /// The checksum of the region file for change detection.
    /// </summary>
    public ulong Checksum { get; init; }

    public RegionFileCache(byte[] bytes, ulong checksum)
    {
        Bytes = bytes;
        Checksum = checksum;
    }
}
