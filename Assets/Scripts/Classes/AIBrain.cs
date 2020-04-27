using System.Linq;

/// <summary>
/// The controller for the enemy pawn. Includes the AI
/// </summary>
public class AIBrain
{
    /// <summary>
    /// The closest distances to everything else
    /// </summary>
    private DijkstraWeightGraph enemyWeightGraph = null;

    /// <summary>
    /// The controlee
    /// </summary>
    private readonly IPawn enemyPawn = null;

    /// <summary>
    /// Constructs an AI controller with the given pawn
    /// </summary>
    /// <param name="pawn"></param>
    public AIBrain(IPawn pawn)
    {
        enemyPawn = pawn;
    }

    /// <summary>
    /// Should be called when the scene's graph changes
    /// (Ie. a locked gate has been lifted)
    /// </summary>
    /// <param name="cellGraph">The scene graph</param>
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

    /// <summary>
    /// Ticks the AI's brain. It will move one square each tick currently.
    /// </summary>
    /// <param name="graph">The scene graph</param>
    /// <param name="state">The state of the level</param>
    public void Tick(LogicalCellGraph graph, ILevelState state)
    {
        // So on a bigger game I probably wouldn't do this,
        // But it seems Djikstra's is performant enough to
        // Recompute each tick!! (keep in mind, the AI's tick
        // is only approximately once every second). If this
        // weren't the case, I would probably go with A*.
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

    /// <summary>
    /// Finds something for the AI to seek towards (we'll go with closest)
    /// </summary>
    /// <param name="graph">The scene graph</param>
    /// <param name="state">The state of the level</param>
    /// <returns></returns>
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
