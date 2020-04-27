using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Holds a constructed path in logical-space of something important in the game
/// </summary>
public class LogicalPath
{
    private List<Vector3Int> path = new List<Vector3Int>();

    /// <summary>
    /// The path, in logical space
    /// </summary>
    public IEnumerable<Vector3Int> Path => path;

    /// <summary>
    /// Add an element to the beginning of the path
    /// </summary>
    /// <param name="cell">The cell to add</param>
    public void PrependPath(Vector3Int cell)
    {
        path.Insert(0, cell);
    }

    /// <summary>
    /// Add an element to the end of the path
    /// </summary>
    /// <param name="cell">The cell to add</param>
    public void AppendPath(Vector3Int cell)
    {
        path.Add(cell);
    }
}