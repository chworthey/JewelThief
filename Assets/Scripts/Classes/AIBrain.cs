using System.Linq;

public class AIBrain
{
    private DijkstraWeightGraph enemyWeightGraph = null;

    private readonly IPawn enemyPawn = null;

    public AIBrain(IPawn pawn)
    {
        enemyPawn = pawn;
    }

    public void RebuildGraph(LogicalCellGraph cellGraph)
    {
        if (enemyPawn == null)
        {
            return;
        }

        var enemyPos = enemyPawn.LogicalLocation;
        var enemyCell = cellGraph.LookupCell(enemyPos.x, enemyPos.y);

        enemyWeightGraph = DijkstraWeightGraph.BuildDijkstraWeightGraph(
            cellGraph,
            enemyCell,
            maxDistance: 0,
            allowNeighborTeleportation: true);
    }

    public void Tick(LogicalCellGraph graph, ILevelState state)
    {
        RebuildGraph(graph);

        if (enemyPawn == null || enemyWeightGraph == null)
        {
            return;
        }
        var goal = findAIGoal(graph, state);
        if (goal == null)
        {
            return;
        }

        var (accessible, path) = enemyWeightGraph.LookupShortestPath(goal);
        if (!accessible)
        {
            return;
        }

        if (!path.Path.Any())
        {
            return;
        }

        var nextCell = path.Path.Skip(1).Take(1).FirstOrDefault();
        if (nextCell != null)
        {
            var truncatedPath = new LogicalPath();
            truncatedPath.PrependPath(nextCell);
            enemyPawn.PushMotionPath(truncatedPath);
        }
    }

    private LogicalCell findAIGoal(LogicalCellGraph graph, ILevelState state)
    {
        int closestItemDistance = int.MaxValue;
        LogicalCell closestItemCell = null;
        var stats = state.GameStats;
        foreach (var i in state.ActiveItems)
        {
            if ((!stats.ExitUnlocked && i.EndsLevel) || (stats.ExitUnlocked && !i.EndsLevel))
            {
                continue;
            }

            var logicalPosition = i.LogicalLocation;
            var cell = graph.LookupCell(logicalPosition.x, logicalPosition.y);
            var (accessible, distance) = enemyWeightGraph.LookupDistance(cell);

            if (!accessible)
            {
                continue;
            }

            if (distance < closestItemDistance)
            {
                closestItemDistance = distance;
                closestItemCell = cell;
            }
        }

        return closestItemCell;
    }
}
