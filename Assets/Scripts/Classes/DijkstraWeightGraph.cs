using System;
using System.Collections.Generic;
using System.Linq;

class DijkstraWeightGraph
{
    private Dictionary<LogicalCell, int> distancesByCell = new Dictionary<LogicalCell, int>();
    private Dictionary<LogicalCell, LogicalCell> paths = new Dictionary<LogicalCell, LogicalCell>();
    private LogicalCell startingLocation;

    public (bool accessible, LogicalPath path) LookupShortestPath(LogicalCell target)
    {
        LogicalPath path = new LogicalPath();
        var p = new List<LogicalCell>();
        bool accessible = false;

        if (distancesByCell[target] != int.MaxValue || target == startingLocation)
        {
            LogicalCell currentCell = target;
            while (currentCell != null)
            {
                accessible = true;
                path.PrependPath(currentCell.Loc);
                currentCell = paths[currentCell];
            }
        }

        return (accessible: accessible, path: path);
    }

    public (bool accessible, int distance) LookupDistance(LogicalCell target)
    {
        bool accessible = false;
        int distance = 0;
        if (distancesByCell.ContainsKey(target))
        {
            int d = distancesByCell[target];
            if (d != int.MaxValue)
            {
                accessible = true;
                distance = d;
            }
        }

        return (accessible: accessible, distance: distance);
    }

    public static DijkstraWeightGraph BuildDijkstraWeightGraph(LogicalCellGraph graph, LogicalCell startingLocation, int maxDistance = 0, bool allowNeighborTeleportation = false)
    {
        DijkstraWeightGraph g = new DijkstraWeightGraph();
        g.startingLocation = startingLocation;

        var distances = new Dictionary<LogicalCell, int>();
        var path = new Dictionary<LogicalCell, LogicalCell>();
        var remainingCells = new HashSet<LogicalCell>();

        foreach (LogicalCell cell in graph.Cells)
        {
            distances[cell] = int.MaxValue;
            path[cell] = null;
            remainingCells.Add(cell);
        }
        distances[startingLocation] = 0;

        while (remainingCells.Any())
        {

            LogicalCell currentCell = remainingCells.First();
            int currentCellDistance = int.MaxValue;
            foreach (LogicalCell cell in remainingCells)
            {
                var d = distances[cell];
                if (d < currentCellDistance)
                {
                    currentCellDistance = d;
                    currentCell = cell;
                }
            }
            remainingCells.Remove(currentCell);

            if (currentCellDistance != int.MaxValue && (maxDistance == 0 || currentCellDistance + 1 <= maxDistance)) {
                foreach (var neighbor in currentCell.Neighbors)
                {
                    if (neighbor == null)
                    {
                        continue;
                    }

                    if (!allowNeighborTeleportation)
                    {
                        bool adjacent = (Math.Abs(neighbor.X - currentCell.X) <= 1 && neighbor.Y == currentCell.Y) || 
                            (Math.Abs(neighbor.Y - currentCell.Y) <= 1 && neighbor.X == currentCell.X);
                        if (!adjacent)
                        {
                            continue; // Skip
                        }
                    }

                    if (!remainingCells.Contains(neighbor))
                    {
                        continue; // Skip
                    }

                    int newDistance = currentCellDistance + 1;

                    if (newDistance < distances[neighbor])
                    {
                        distances[neighbor] = newDistance;
                        path[neighbor] = currentCell;
                    }
                }
            }
        }

        g.distancesByCell = distances;
        g.paths = path;

        return g;
    }
}