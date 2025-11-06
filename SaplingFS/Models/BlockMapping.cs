namespace SaplingFS.Models;

/// <summary>
/// Represents the mapping between a world position, a file, and a Minecraft block.
/// </summary>
public class BlockMapping
{
    /// <summary>
    /// Position of the block in the Minecraft world.
    /// </summary>
    public Vector Position { get; set; }

    /// <summary>
    /// The file this block represents.
    /// </summary>
    public MappedFile File { get; set; }

    /// <summary>
    /// The Minecraft block type (e.g., "grass_block", "stone", "oak_log").
    /// </summary>
    public string Block { get; set; }

    /// <summary>
    /// Used during chunk iteration to track which blocks have been processed.
    /// </summary>
    public bool Valid { get; set; }

    public BlockMapping(Vector position, MappedFile file, string block)
    {
        Position = position;
        File = file;
        Block = block;
        Valid = false;
    }

    public override string ToString() =>
        $"\"{Block}\" at ({Position}): \"{File.Path}\"";
}
