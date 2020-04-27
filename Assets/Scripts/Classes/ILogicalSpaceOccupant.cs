using UnityEngine;

/// <summary>
/// Represents an object that occupies space in the logical world.
/// Generally it will be compared against something in the scene cell graph.
/// </summary>
public interface ILogicalSpaceOccupant
{
    /// <summary>
    /// The location in logical coordinates
    /// </summary>
    Vector3Int LogicalLocation { get; }
}

