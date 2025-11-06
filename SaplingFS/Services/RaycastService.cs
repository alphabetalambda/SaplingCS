using SaplingFS.Models;

namespace SaplingFS.Services;

/// <summary>
/// 3D DDA (Digital Differential Analyzer) raycast service for finding
/// the first mapped block along a ray direction.
/// </summary>
public class RaycastService
{
    /// <summary>
    /// Casts a ray from a starting position in a given direction to find
    /// the first mapped block within range.
    /// </summary>
    /// <param name="mapping">Block-to-file mapping dictionary</param>
    /// <param name="pos">Starting position of the ray</param>
    /// <param name="fvec">Ray direction (must be normalized)</param>
    /// <param name="range">Maximum distance to search (default: 50)</param>
    /// <returns>BlockMapping if hit, null otherwise</returns>
    public BlockMapping? Raycast(
        IReadOnlyDictionary<string, BlockMapping> mapping,
        Vector pos,
        Vector fvec,
        int range = 50)
    {
        // Set the starting voxel
        int voxelX = (int)Math.Floor((double)pos.X);
        int voxelY = (int)Math.Floor((double)pos.Y);
        int voxelZ = (int)Math.Floor((double)pos.Z);

        // Calculate step direction for each axis
        int stepX = fvec.X > 0 ? 1 : fvec.X < 0 ? -1 : 0;
        int stepY = fvec.Y > 0 ? 1 : fvec.Y < 0 ? -1 : 0;
        int stepZ = fvec.Z > 0 ? 1 : fvec.Z < 0 ? -1 : 0;

        // Distance along the ray to cross one voxel in each axis
        double tDeltaX = fvec.X != 0 ? Math.Abs(1.0 / fvec.X) : double.PositiveInfinity;
        double tDeltaY = fvec.Y != 0 ? Math.Abs(1.0 / fvec.Y) : double.PositiveInfinity;
        double tDeltaZ = fvec.Z != 0 ? Math.Abs(1.0 / fvec.Z) : double.PositiveInfinity;

        // Distance from ray start to first voxel boundary on each axis
        double nextBoundaryX = stepX > 0 ? voxelX + 1 : voxelX;
        double nextBoundaryY = stepY > 0 ? voxelY + 1 : voxelY;
        double nextBoundaryZ = stepZ > 0 ? voxelZ + 1 : voxelZ;
        double tMaxX = fvec.X != 0 ? (nextBoundaryX - pos.X) / fvec.X : double.PositiveInfinity;
        double tMaxY = fvec.Y != 0 ? (nextBoundaryY - pos.Y) / fvec.Y : double.PositiveInfinity;
        double tMaxZ = fvec.Z != 0 ? (nextBoundaryZ - pos.Z) / fvec.Z : double.PositiveInfinity;

        // Iterate until we hit a block or exceed range
        double distance = 0;
        while (distance <= range)
        {
            // Check for intersection with a mapped block
            string key = $"{voxelX},{voxelY},{voxelZ}";
            if (mapping.TryGetValue(key, out var block))
            {
                return block;
            }

            // Choose the smallest tMax to step to the next voxel
            if (tMaxX < tMaxY)
            {
                if (tMaxX < tMaxZ)
                {
                    voxelX += stepX;
                    distance = tMaxX;
                    tMaxX += tDeltaX;
                }
                else
                {
                    voxelZ += stepZ;
                    distance = tMaxZ;
                    tMaxZ += tDeltaZ;
                }
            }
            else
            {
                if (tMaxY < tMaxZ)
                {
                    voxelY += stepY;
                    distance = tMaxY;
                    tMaxY += tDeltaY;
                }
                else
                {
                    voxelZ += stepZ;
                    distance = tMaxZ;
                    tMaxZ += tDeltaZ;
                }
            }
        }

        return null;
    }
}
