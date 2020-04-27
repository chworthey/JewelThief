using System.Collections.Generic;
using UnityEngine;

public class LogicalCell
{
    public int X { get; set; } = 0;
    public int Y { get; set; } = 0;
    public LogicalCell TopNeighbor => neighborReferences[0];
    public LogicalCell LeftNeighbor => neighborReferences[1];
    public LogicalCell RightNeighbor => neighborReferences[2];
    public LogicalCell BottomNeighbor => neighborReferences[3];

    public IEnumerable<LogicalCell> Neighbors => neighborReferences;

    private LogicalCell[] neighborReferences = new LogicalCell[4];

    public void SetNeighbors(LogicalCell top, LogicalCell left, LogicalCell right, LogicalCell bottom)
    {
        neighborReferences[0] = top;
        neighborReferences[1] = left;
        neighborReferences[2] = right;
        neighborReferences[3] = bottom;
    }

    public Vector3Int Loc => new Vector3Int(X, Y, 0);
}
