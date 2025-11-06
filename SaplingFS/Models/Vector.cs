namespace SaplingFS.Models;

/// <summary>
/// Represents a 3D vector with integer coordinates.
/// Used for block positions and directional operations.
/// </summary>
public readonly record struct Vector(int X, int Y, int Z)
{
    /// <summary>
    /// Cardinal direction vectors (East, West, South, North, Up, Down)
    /// </summary>
    public static readonly Vector[] Directions =
    [
        new(1, 0, 0),   // East
        new(-1, 0, 0),  // West
        new(0, 0, 1),   // South
        new(0, 0, -1),  // North
        new(0, 1, 0),   // Up
        new(0, -1, 0)   // Down
    ];

    /// <summary>
    /// Adds two vectors component-wise.
    /// </summary>
    public Vector Add(Vector other) => new(X + other.X, Y + other.Y, Z + other.Z);

    /// <summary>
    /// Adds scalar components to this vector.
    /// </summary>
    public Vector Add(int x, int y, int z) => new(X + x, Y + y, Z + z);

    /// <summary>
    /// Subtracts another vector from this one.
    /// </summary>
    public Vector Sub(Vector other) => new(X - other.X, Y - other.Y, Z - other.Z);

    /// <summary>
    /// Subtracts scalar components from this vector.
    /// </summary>
    public Vector Sub(int x, int y, int z) => new(X - x, Y - y, Z - z);

    /// <summary>
    /// Calculates the length (magnitude) of this vector.
    /// </summary>
    public double Length() => Math.Sqrt(X * X + Y * Y + Z * Z);

    /// <summary>
    /// Returns a normalized version of this vector (length = 1).
    /// </summary>
    public Vector Normalize()
    {
        var length = Length();
        if (length == 0) return new Vector(0, 0, 0);
        return new((int)(X / length), (int)(Y / length), (int)(Z / length));
    }

    /// <summary>
    /// Shifts this vector by one unit in the specified direction (0-5).
    /// </summary>
    public Vector Shifted(int direction) => Add(Directions[direction]);

    /// <summary>
    /// Converts chunk-relative coordinates to absolute world coordinates.
    /// </summary>
    public Vector Absolute(int chunkX, int chunkZ) => Add(chunkX * 16, -64, chunkZ * 16);

    /// <summary>
    /// Converts absolute world coordinates to chunk-relative coordinates.
    /// </summary>
    public Vector Relative(int chunkX, int chunkZ) => Sub(chunkX * 16, -64, chunkZ * 16);

    /// <summary>
    /// Creates a forward vector from yaw and pitch angles (in degrees).
    /// Used for raycasting from player perspective.
    /// </summary>
    public static Vector FromAngles(double yaw, double pitch)
    {
        var yawRad = yaw * Math.PI / 180;
        var pitchRad = pitch * Math.PI / 180;

        var cosPitch = Math.Cos(pitchRad);
        var sinPitch = Math.Sin(pitchRad);
        var cosYaw = Math.Cos(yawRad);
        var sinYaw = Math.Sin(yawRad);

        var dx = cosPitch * -sinYaw;
        var dy = -sinPitch;
        var dz = cosPitch * cosYaw;

        return new((int)dx, (int)dy, (int)dz);
    }

    /// <summary>
    /// Converts the vector to an array [X, Y, Z].
    /// </summary>
    public int[] ToArray() => [X, Y, Z];

    /// <summary>
    /// String representation in the format "X,Y,Z".
    /// Used as dictionary keys for block mapping.
    /// </summary>
    public override string ToString() => $"{X},{Y},{Z}";
}
