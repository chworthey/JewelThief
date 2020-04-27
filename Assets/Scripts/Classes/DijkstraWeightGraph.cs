using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Calculates and stores the shortest path to every reachable cell from the source cell.
/// </summary>
public class DijkstraWeightGraph
{
    /// <summary>
    /// Stores distance values for each cell
    /// </summary>
    private Dictionary<LogicalCell, int> distancesByCell = new Dictionary<LogicalCell, int>();

    /// <summary>
    /// Stores links from each cell to the previous cell along the shortest path
    /// </summary>
    private Dictionary<LogicalCell, LogicalCell> paths = new Dictionary<LogicalCell, LogicalCell>();

    /// <summary>
    /// The cell the graph was calculated from
    /// </summary>
    private LogicalCell startingLocation;

    /// <summary>
    /// Finds the shortest path from precalculated starting location to the given target
    /// Adjacent cells and teleports (per special game rules) count as 1 distance point.
    /// </summary>
    /// <param name="target">The cell to find the path to</param>
    /// <returns>1. True if cell is accessible, False if blocked 2. The shortest path</returns>
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

    /// <summary>
    /// Finds the shortest distance from precalculated starting location to the given target.
    /// Adjacent cells and teleports (per special game rules) count as 1 distance point.
    /// </summary>
    /// <param name="target">The cell to find the distance to</param>
    /// <returns>1. True if cell is accessible, False if blocked 2. The shortest distance</returns>
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

    /// <summary>
    /// Builds a new precalculated Dijkstra graph based on the starting location.
    /// </summary>
    /// <param name="graph">The cell graph we are working from</param>
    /// <param name="startingLocation">The location common to all futher distance calculations</param>
    /// <param name="maxDistance">The maximum distance away from the starting location the graph will store. If left as 0, it counts as infinite.</param>
    /// <param name="allowNeighborTeleportation">This game has special rules to allow traversal on non-adjacent cells. Passing false will disable this rule making it more like the assignment likely intended (but that would be less fun!)</param>
    /// <returns>The baked/calculated Dijkstra graph</returns>
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

                    // The special rule for this game allows non-adjacent traversal on matching colors.
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