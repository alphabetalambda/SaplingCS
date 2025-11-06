namespace SaplingFS.Models;

/// <summary>
/// Represents the mapping between a world position, a file path, and a Minecraft block.
/// </summary>
public record BlockMapping
{
    /// <summary>
    /// Position of the block in the Minecraft world.
    /// </summary>
    public Vector Position { get; init; }

    /// <summary>
    /// Path of the file this block represents.
    /// </summary>
    public string FilePath { get; init; }

    /// <summary>
    /// The Minecraft block type (e.g., "grass_block", "stone", "oak_log").
    /// </summary>
    public string Block { get; init; }

    public BlockMapping(Vector position, string filePath, string block)
    {
        Position = position;
        FilePath = filePath;
        Block = block;
    }

    public override string ToString() =>
        $"\"{Block}\" at ({Position}): \"{FilePath}\"";
}
