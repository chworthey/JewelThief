using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class LogicalCellGraph
{
    private LogicalCell[,] indexedCells = null;

    public IEnumerable<LogicalCell> Cells 
    {
        get
        {
            foreach (var cell in indexedCells)
                yield return cell;
        }
    }

    public int SizeX => indexedCells.GetLength(0);
    public int SizeY => indexedCells.GetLength(1);

    public LogicalCell LookupCell(int x, int y)
    {
        return indexedCells[x, y];
    }

    public static LogicalCellGraph BuildCellGraph(Tilemap tilemap, IEnumerable<Vector3Int> gateLocations)
    {
        LogicalCellGraph graph = new LogicalCellGraph();
        var logicalSize = tilemap.cellBounds.size / 2;
        var sizeX = logicalSize.x;
        var sizeY = logicalSize.y;

        int[,,] colors = new int[sizeX, logicalSize.y, 4];
        LogicalCell[,] cells = new LogicalCell[sizeX, logicalSize.y];
        var gateLocationsSet = new HashSet<Vector3Int>();
        foreach (var g in gateLocations)
        {
            gateLocationsSet.Add(g);
        }

        for (int y = 0; y < sizeY; ++y)
        {
            for (int x = 0; x < sizeX; ++x)
            {
                var cell = new LogicalCell { X = x, Y = y };
                var logicalSpace = GridSpaceConversion.GetLogicalSpaceFromCell(cell);

                if (gateLocationsSet.Contains(logicalSpace))
                {
                    colors[x, y, 0] = 0; // Reserved for gates
                    colors[x, y, 1] = 0;
                    colors[x, y, 2] = 0;
                    colors[x, y, 3] = 0;
                }
                else
                {
                    var gridSpace = GridSpaceConversion.GetGridSpaceFromLogical(logicalSpace, tilemap);

                    // I wouldn't typically depend on render state for logical stuff,
                    // But this game will be all about color so I think it's OK.
                    colors[x, y, 0] = tilemap.GetColor(new Vector3Int(    gridSpace.x,     gridSpace.y, 0)).GetHashCode();
                    colors[x, y, 1] = tilemap.GetColor(new Vector3Int(gridSpace.x + 1,     gridSpace.y, 0)).GetHashCode();
                    colors[x, y, 2] = tilemap.GetColor(new Vector3Int(    gridSpace.x, gridSpace.y + 1, 0)).GetHashCode();
                    colors[x, y, 3] = tilemap.GetColor(new Vector3Int(gridSpace.x + 1, gridSpace.y + 1, 0)).GetHashCode();
                }

                cells[x, y] = cell;
            }
        }

        for (int y = 0; y < sizeY; ++y)
        {
            for (int x = 0; x < sizeX; ++x)
            {
                HashSet<int> colorsInThisCell = new HashSet<int>(new int[] {
                    colors[x, y, 0],
                    colors[x, y, 1],
                    colors[x, y, 2],
                    colors[x, y, 3],
                });

                LogicalCell thisCell = cells[x, y];

                LogicalCell upNeighbor = null;
                for (int up = y + 1; up < sizeY; ++up)
                {
                    if (colorsInThisCell.Contains(colors[x, up, 0]) ||
                        colorsInThisCell.Contains(colors[x, up, 1]) ||
                        colorsInThisCell.Contains(colors[x, up, 2]) ||
                        colorsInThisCell.Contains(colors[x, up, 3]))
                    {
                        upNeighbor = cells[x, up];
                        break;
                    }
                }

                LogicalCell downNeighbor = null;
                for (int down = y - 1; down >= 0; --down)
                {
                    if (colorsInThisCell.Contains(colors[x, down, 0]) ||
                        colorsInThisCell.Contains(colors[x, down, 1]) ||
                        colorsInThisCell.Contains(colors[x, down, 2]) ||
                        colorsInThisCell.Contains(colors[x, down, 3]))
                    {
                        downNeighbor = cells[x, down];
                        break;
                    }
                }

                LogicalCell rightNeighbor = null;
                for (int right = x + 1; right < sizeX; ++right)
                {
                    if (colorsInThisCell.Contains(colors[right, y, 0]) ||
                        colorsInThisCell.Contains(colors[right, y, 1]) ||
                        colorsInThisCell.Contains(colors[right, y, 2]) ||
                        colorsInThisCell.Contains(colors[right, y, 3]))
                    {
                        rightNeighbor = cells[right, y];
                        break;
                    }
                }

                LogicalCell leftNeighbor = null;
                for (int left = x - 1; left >= 0; --left)
                {
                    if (colorsInThisCell.Contains(colors[left, y, 0]) ||
                        colorsInThisCell.Contains(colors[left, y, 1]) ||
                        colorsInThisCell.Contains(colors[left, y, 2]) ||
                        colorsInThisCell.Contains(colors[left, y, 3]))
                    {
                        leftNeighbor = cells[left, y];
                        break;
                    }
                }

                thisCell.SetNeighbors(upNeighbor, leftNeighbor, rightNeighbor, downNeighbor);
            }
        }

        graph.indexedCells = cells;

        return graph;
    }
}
