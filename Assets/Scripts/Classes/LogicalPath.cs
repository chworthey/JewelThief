using System.Collections.Generic;
using UnityEngine;

public class LogicalPath
{
    private List<Vector3Int> path = new List<Vector3Int>();
    public IEnumerable<Vector3Int> Path => path;

    public void PrependPath(Vector3Int cell)
    {
        path.Insert(0, cell);
    }

    public void AppendPath(Vector3Int cell)
    {
        path.Add(cell);
    }
}