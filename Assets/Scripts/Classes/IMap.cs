using UnityEngine;

/// <summary>
/// Represents the grid-space object.
/// Activate/Deactivate will affect the entire level.
/// </summary>
public interface IMap : IActivatable
{
    /// <summary>
    /// The bounds, in grid-space coordinates of the map
    /// </summary>
    BoundsInt CellBounds { get; }

    /// <summary>
    /// Looks up the color at a particular grid location
    /// </summary>
    /// <param name="location"></param>
    /// <returns></returns>
    Color GetColor(Vector3Int location);

    /// <summary>
    /// Converts a world-space coordinate to a grid-space coordinate
    /// </summary>
    /// <param name="worldPosition">The world location which will get quantized</param>
    /// <returns>The grid-space coordinate</returns>
    Vector3Int WorldToCell(Vector3 worldPosition);
}
