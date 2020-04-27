using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Represents the main logical grid system used in the game. Each cell has
/// originates from 4 tilemap grid cells of different colors.
/// The graph is calculated with accessible neighbors, not physically spaced ones.
/// This means teleportation can happen just by traversing to a neighbor.
/// </summary>
public class LogicalCell
{
    /// <summary>
    /// The x logical coordinate
    /// </summary>
    public int X { get; set; } = 0;

    /// <summary>
    /// The y logical coordinate
    /// </summary>
    public int Y { get; set; } = 0;

    /// <summary>
    /// The first neighboring cell to the top [0,+1, 2, 3....]
    /// </summary>
    public LogicalCell TopNeighbor => neighborReferences[0];

    /// <summary>
    /// The first neighboring cell to the left [-1, -2, -3..., 0]
    /// </summary>
    public LogicalCell LeftNeighbor => neighborReferences[1];

    /// <summary>
    /// The first neighboring cell to the right
    /// </summary>
    public LogicalCell RightNeighbor => neighborReferences[2];

    /// <summary>
    /// The first neighboring cell to the bottom
    /// </summary>
    public LogicalCell BottomNeighbor => neighborReferences[3];

    /// <summary>
    /// A list of all logical neighbors in the surrounding directions
    /// </summary>
    public IEnumerable<LogicalCell> Neighbors => neighborReferences;
    private LogicalCell[] neighborReferences = new LogicalCell[4];

    /// <summary>
    /// Sets the neighbor linked cells for this cell
    /// </summary>
    /// <param name="top">The first neighboring cell to the top</param>
    /// <param name="left">The first neighboring cell to the left</param>
    /// <param name="right">The first neighboring cell to the right</param>
    /// <param name="bottom">The first neighboring cell to the bottom</param>
    public void SetNeighbors(LogicalCell top, LogicalCell left, LogicalCell right, LogicalCell bottom)
    {
        neighborReferences[0] = top;
        neighborReferences[1] = left;
        neighborReferences[2] = right;
        neighborReferences[3] = bottom;
    }

    /// <summary>
    /// The location of the cell in logical coordinates
    /// </summary>
    public Vector3Int Loc => new Vector3Int(X, Y, 0);
}
