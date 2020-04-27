using System.Collections.Generic;
using System.Linq;
using UnityEngine;

class PlayerController
{
    private DijkstraWeightGraph playerWeightGraph = null;
    private readonly IPawn playerPawn = null;

    private Vector3Int pendingTarget;
    private bool pendingTargetActive = false;
    private LogicalPath pendingPath = null;
    private Vector3Int lastCommittedCell;
    private bool hasCommitedCell = false;
    private Queue<Vector3Int> commandCheckpoints = new Queue<Vector3Int>();
    private Vector3Int lastMouseLocation;
    private bool lastMouseLocationOffGrid = false;

    public PlayerController(IPawn playerPawn)
    {
        this.playerPawn = playerPawn;
    }

    public void OnMouseClick(LogicalCellGraph cellGraph)
    {
        if (pendingTargetActive && pendingPath != null && pendingTarget != lastCommittedCell)
        {
            lastCommittedCell = pendingTarget;
            hasCommitedCell = true;
            commandCheckpoints.Enqueue(pendingTarget);
            playerPawn.PushMotionPath(pendingPath);
        }
    }

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

    IEnumerable<SegmentProperties> GenerateCommandPathLines(IMap tilemap)
    {
        List<SegmentProperties> segments = new List<SegmentProperties>();
        if (pendingTargetActive && pendingPath != null)
        {
            segments.AddRange(LineElements.SegmentsFromPath(pendingPath, tilemap));
        }

        return segments;
    }

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

    public Mesh GenerateLineMesh(IMap map)
    {
        List<SegmentProperties> allSegments = new List<SegmentProperties>();
        allSegments.AddRange(GenerateCommandPathLines(map));
        allSegments.AddRange(GenerateCheckpointLines(map));
        allSegments.AddRange(GenerateSelectionBoxLines(map));
        return LineMesh.GenerateLineMesh(allSegments);
    }

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