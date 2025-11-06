using System.Text.Json.Serialization;

namespace SaplingFS.Models;

/// <summary>
/// JSON serialization context for Native AOT compatibility.
/// Provides source-generated serialization for mapping data.
/// </summary>
[JsonSerializable(typeof(Dictionary<string, MappingEntry>))]
[JsonSerializable(typeof(MappingEntry))]
public partial class MappingJsonContext : JsonSerializerContext
{
}

/// <summary>
/// Represents a single mapping entry for JSON serialization.
/// </summary>
public class MappingEntry
{
    public int[] pos { get; set; } = new int[3];
    public object[] file { get; set; } = Array.Empty<object>();
    public string block { get; set; } = "stone";
}
