using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// Controls the player, handling any of the mouse+line input stuff.
/// </summary>
class PlayerController
{
    /// <summary>
    /// Stores the distance to every other node, up to 6 traversable cells 
    /// away (including 1 point for transportations)
    /// </summary>
    private DijkstraWeightGraph playerWeightGraph = null;

    /// <summary>
    /// The player pawn to control
    /// </summary>
    private readonly IPawn playerPawn = null;

    /// <summary>
    /// The location the mouse is currently over
    /// </summary>
    private Vector3Int pendingTarget;

    /// <summary>
    /// True if the pending target holds any value given the context
    /// </summary>
    private bool pendingTargetActive = false;

    /// <summary>
    /// The path leading up to target from the player's current position
    /// </summary>
    private LogicalPath pendingPath = null;

    /// <summary>
    /// The last cell committed with a mouse click move action
    /// </summary>
    private Vector3Int lastCommittedCell;

    /// <summary>
    /// Used to determine if we have a committed move recently
    /// </summary>
    private bool hasCommitedCell = false;

    /// <summary>
    /// Used for the grey boxes that appear whenever the player clicks
    /// </summary>
    private Queue<Vector3Int> commandCheckpoints = new Queue<Vector3Int>();

    /// <summary>
    /// The last known mouse location
    /// </summary>
    private Vector3Int lastMouseLocation;

    /// <summary>
    /// True if the last known mouse location was off grid
    /// </summary>
    private bool lastMouseLocationOffGrid = false;

    /// <summary>
    /// Creates a new player controller, with a player to possess
    /// </summary>
    /// <param name="playerPawn">The player pawn</param>
    public PlayerController(IPawn playerPawn)
    {
        this.playerPawn = playerPawn;
    }

    /// <summary>
    /// Called on mouse click (in valid logical space)
    /// </summary>
    public void OnMouseClick()
    {
        if (pendingTargetActive && pendingPath != null && pendingTarget != lastCommittedCell)
        {
            lastCommittedCell = pendingTarget;
            hasCommitedCell = true;
            commandCheckpoints.Enqueue(pendingTarget);
            playerPawn.PushMotionPath(pendingPath);
        }
    }

    /// <summary>
    /// Called when the mouse moves (in logical space)
    /// </summary>
    /// <param name="mouseLogicalSpace">The mouse coordinates in logical space</param>
    /// <param name="offGrid">True if the coordinates are valid</param>
    /// <param name="cellGraph">The current scene cell graph</param>
    /// <param name="tilemap">The map</param>
    public void OnMouseMove(Vector3Int mouseLogicalSpace, bool offGrid, LogicalCellGraph cellGraph, IMap tilemap)
    {
        pendingTargetActive = false;
        pendingPath = null;

        lastMouseLocation = mouseLogicalSpace;
        lastMouseLocationOffGrid = offGrid;

        if (playerWeightGraph == null)
        {
            RebuildGraph(cellGraph);
        }

        if (playerWeightGraph == null || offGrid)
        {
            return;
        }

        var targetCell = cellGraph.LookupCell(mouseLogicalSpace.x, mouseLogicalSpace.y);
        var (accessible, path) = playerWeightGraph.LookupShortestPath(targetCell);

        if (accessible)
        {
            pendingTarget = targetCell.Loc;
            pendingTargetActive = true;
            pendingPath = path;
        }
    }

    /// <summary>
    /// Call this when something changes in the environment
    /// (Gate unlocked, for example)
    /// </summary>
    /// <param name="cellGraph">The current scene cell graph</param>
    public void RebuildGraph(LogicalCellGraph cellGraph)
    {
        if (playerPawn == null)
        {
            return;
        }

        LogicalCell commandPosition;
        if (hasCommitedCell)
        {
            commandPosition = cellGraph.LookupCell(lastCommittedCell.x, lastCommittedCell.y);
        }
        else
        {
            var playerPos = playerPawn.LogicalLocation;
            var playerCell = cellGraph.LookupCell(playerPos.x, playerPos.y);
            commandPosition = playerCell;
        }
        playerWeightGraph = DijkstraWeightGraph.BuildDijkstraWeightGraph(
                cellGraph,
                commandPosition,
                maxDistance: 6,
                allowNeighborTeleportation: true);
    }

    /// <summary>
    /// Creates a predictor line following the pending path to indicate
    /// to the player where they would go if they clicked.
    /// </summary>
    /// <param name="tilemap">The map</param>
    /// <returns>The line segments to render</returns>
    IEnumerable<SegmentProperties> GenerateCommandPathLines(IMap tilemap)
    {
        List<SegmentProperties> segments = new List<SegmentProperties>();
        if (pendingTargetActive && pendingPath != null)
        {
            segments.AddRange(LineElements.SegmentsFromPath(pendingPath, tilemap));
        }

        return segments;
    }

    /// <summary>
    /// Creates a box around where the user has their mouse targeted.
    /// The appears might vary depending on the validity of that location
    /// </summary>
    /// <param name="tilemap">The map</param>
    /// <returns>The line segments to render</returns>
    IEnumerable<SegmentProperties> GenerateSelectionBoxLines(IMap tilemap)
    {
        if (lastMouseLocationOffGrid)
        {
            return new List<SegmentProperties>();
        }

        List<SegmentProperties> segments = new List<SegmentProperties>();

        var selectedCellWorldSpace = GridSpaceConversion.GetWorldSpaceFromLogical(lastMouseLocation, tilemap);

        Color boxColor = Color.white;
        if (!pendingTargetActive)
        {
            boxColor = Color.red;
            segments.AddRange(LineElements.XSegments(selectedCellWorldSpace, boxColor));
        }

        segments.AddRange(LineElements.SquareSelectionSegments(selectedCellWorldSpace, boxColor));

        return segments;
    }

    /// <summary>
    /// Creates grey boxes at each point the player has clicked before, but
    /// the player pawn hasn't caught up to yet.
    /// </summary>
    /// <param name="tilemap">The map</param>
    /// <returns>The line segments to render</returns>
    IEnumerable<SegmentProperties> GenerateCheckpointLines(IMap tilemap)
    {
        var segments = new List<SegmentProperties>();
        foreach (var c in commandCheckpoints)
        {
            var worldSpace = GridSpaceConversion.GetWorldSpaceFromLogical(c, tilemap);
            var square = LineElements.SquareSelectionSegments(worldSpace, Color.grey);
            segments.AddRange(square);
        }

        return segments;
    }

    /// <summary>
    /// Combines all the user interface line meshes for final submission mesh
    /// rendering submission
    /// </summary>
    /// <param name="map">The map</param>
    /// <returns></returns>
    public Mesh GenerateLineMesh(IMap map)
    {
        List<SegmentProperties> allSegments = new List<SegmentProperties>();
        allSegments.AddRange(GenerateCommandPathLines(map));
        allSegments.AddRange(GenerateCheckpointLines(map));
        allSegments.AddRange(GenerateSelectionBoxLines(map));
        return LineMesh.GenerateLineMesh(allSegments);
    }

    /// <summary>
    /// Checks collision with the command checkpoints so they can be
    /// removed when the player pawn passes them.
    /// </summary>
    public void Tick()
    {
        if (commandCheckpoints.Any())
        {
            var firstCheckpoint = commandCheckpoints.Peek();
            if (firstCheckpoint == playerPawn.LogicalLocation)
            {
                commandCheckpoints.Dequeue();
            }
        }
    }
}