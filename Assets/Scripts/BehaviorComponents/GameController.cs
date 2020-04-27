using System.Collections.Generic;
using System.Linq;
using System.Collections;
using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEngine.UI;

delegate void TickerFunction();

public class GameController : MonoBehaviour
{
    public GameObject[] Maps;
    public GameObject LevelCounter;
    public GameObject ScoreCounter;
    public GameObject WinOverlay;
    public GameObject LoseOverlay;

    private Tilemap tilemap = null;
    private LogicalCellGraph cellGraph = null;
    private DijkstraWeightGraph playerWeightGraph = null;
    private DijkstraWeightGraph enemyWeightGraph = null;

    private MeshRenderer meshRenderer;
    private MeshFilter meshFilter;

    private Vector3Int lastMouseLogicalPosition;
    private Vector3Int pendingTarget;
    private bool pendingTargetActive = false;
    private LogicalPath pendingPath = null;
    private Vector3Int lastCommittedCell;
    private bool hasCommitedCell = false;
    private Queue<Vector3Int> commandCheckpoints = new Queue<Vector3Int>();
    private IEnumerable<SegmentProperties> pendingCommandPathLineSegments = new List<SegmentProperties>();
    private IEnumerable<SegmentProperties> pendingSelectionLineSegments = new List<SegmentProperties>();

    private int currentlevel = 0;
    private int playerScore = 0;
    private bool exitUnlocked => items.Count == 1;
    private bool gameOver = false;

    private List<IGameEventListener> listeners = new List<IGameEventListener>();
    private Pawn player = null;
    private Pawn enemy = null;
    private Dictionary<Vector3Int, Item> items = new Dictionary<Vector3Int, Item>();
    private List<Gate> gates = new List<Gate>();
    private Item exit = null;

    private bool mapNeedsRebuild = false;
    private bool needsMouseUpdate = false;

    private void Restart()
    {
        tilemap = null;
        cellGraph = null;
        playerWeightGraph = null;
        enemyWeightGraph = null;
        pendingTargetActive = false;
        pendingPath = null;
        hasCommitedCell = false;
        commandCheckpoints = new Queue<Vector3Int>();
        pendingCommandPathLineSegments = new List<SegmentProperties>();
        pendingSelectionLineSegments = new List<SegmentProperties>();
        listeners = new List<IGameEventListener>();
        player = null;
        enemy = null;
        items = new Dictionary<Vector3Int, Item>();
        gates = new List<Gate>();
        exit = null;
        mapNeedsRebuild = true;
    }

    public void RegisterEventListener(IGameEventListener listener)
    {
        listeners.Add(listener);
    }

    public void RegisterPlayer(Pawn player)
    {
        this.player = player;
    }

    public void RegisterEnemy(Pawn enemy)
    {
        this.enemy = enemy;
    }

    public void RegisterItem(Item item)
    {
        items[item.GetComponent<TilePlacedObject>().LogicalPosition] = item;
        if (item.TriggerNextLevel)
        {
            exit = item;
            SetExitColorIndicator();
        }
    }

    public void RegisterGate(Gate gate)
    {
        gates.Add(gate);
        mapNeedsRebuild = true;
    }

    IEnumerable<Vector3Int> GetGateLogicalPositions()
    {
        if (gates == null)
        {
            return new List<Vector3Int>();
        }

        return gates.Select(g => g.GetComponent<TilePlacedObject>().LogicalPosition);
    }

    void Start()
    {
        tilemap = Maps[currentlevel].GetComponent<Tilemap>();
        tilemap.gameObject.SetActive(true);

        mapNeedsRebuild = true;

        meshFilter = gameObject.AddComponent<MeshFilter>();
        meshRenderer = gameObject.AddComponent<MeshRenderer>();
        meshRenderer.sharedMaterial = new Material(Shader.Find("SimpleColorLine"));

        var logicalTickCoroutine = WaitAndTick(0.2f, OnLogicalTick);
        StartCoroutine(logicalTickCoroutine);
        var aiTickCoroutine = WaitAndTick(2.0f, OnAITick);
        StartCoroutine(aiTickCoroutine);
    }

    void SetExitColorIndicator()
    {
        if (exit != null)
        {
            exit.GetComponent<SpriteRenderer>().color = exitUnlocked ? Color.white : new Color(0.2f, 0.2f, 0.2f, 1.0f);
        }
    }

    void OnMouseMove(Vector3Int mouseLogicalSpace, bool offGrid)
    {
        pendingCommandPathLineSegments = new List<SegmentProperties>();
        pendingSelectionLineSegments = new List<SegmentProperties>();

        pendingTargetActive = false;
        pendingPath = null;

        if (playerWeightGraph == null)
        {
            RebuildPlayerGraph();
        }

        if (playerWeightGraph == null || offGrid)
        {
            return;
        }

        var targetCell = cellGraph.LookupCell(mouseLogicalSpace.x, mouseLogicalSpace.y);
        var (accessible, path) = playerWeightGraph.LookupShortestPath(targetCell);

        var selectedCellWorldSpace = GridSpaceConversion.GetWorldSpaceFromLogical(mouseLogicalSpace, tilemap);
        Color boxColor = Color.white;
        if (accessible)
        {
            pendingCommandPathLineSegments = pendingCommandPathLineSegments.Concat(LineElements.SegmentsFromPath(path, tilemap));
            pendingTarget = targetCell.Loc;
            pendingTargetActive = true;
            pendingPath = path;
        }
        else
        {
            boxColor = Color.red;
            pendingSelectionLineSegments = pendingSelectionLineSegments.Concat(LineElements.XSegments(selectedCellWorldSpace, boxColor));
        }
        pendingSelectionLineSegments = pendingSelectionLineSegments.Concat(LineElements.SquareSelectionSegments(selectedCellWorldSpace, boxColor));
    }

    void RebuildEnemyGraph()
    {
        if (enemy == null)
        {
            return;
        }

        var enemyPos = enemy.CurrentLogicalPosition;
        var enemyCell = cellGraph.LookupCell(enemyPos.x, enemyPos.y);

        enemyWeightGraph = DijkstraWeightGraph.BuildDijkstraWeightGraph(
            cellGraph,
            enemyCell,
            maxDistance: 0,
            allowNeighborTeleportation: true);
    }

    void RebuildPlayerGraph()
    {
        if (player == null)
        {
            return;
        }

        LogicalCell commandPosition = null;
        if (hasCommitedCell)
        {
            commandPosition = cellGraph.LookupCell(lastCommittedCell.x, lastCommittedCell.y);
        }
        else
        {
            var playerPos = player.CurrentLogicalPosition;
            var playerCell = cellGraph.LookupCell(playerPos.x, playerPos.y);
            commandPosition = playerCell;
        }
        playerWeightGraph = DijkstraWeightGraph.BuildDijkstraWeightGraph(
                cellGraph, 
                commandPosition, 
                maxDistance: 6, 
                allowNeighborTeleportation: true);

        needsMouseUpdate = true;
    }

    void Update()
    {
        if (gameOver)
        {
            return;
        }

        if (mapNeedsRebuild)
        {
            cellGraph = LogicalCellGraph.BuildCellGraph(tilemap, GetGateLogicalPositions());
            RebuildPlayerGraph();
            RebuildEnemyGraph();
            mapNeedsRebuild = false;
        }

        IEnumerable<SegmentProperties> pendingLineSegments = new List<SegmentProperties>();
        var mouseSceenSpace = Input.mousePosition;
        var mouseWorldSpace = Camera.main.ScreenToWorldPoint(mouseSceenSpace);
        var mouseGridSpace = tilemap.WorldToCell(mouseWorldSpace);
        var mouseLogicalSpace = GridSpaceConversion.GetLogicalSpaceFromGridSpace(mouseGridSpace, tilemap);

        bool onGrid = mouseLogicalSpace.x >= 0 && mouseLogicalSpace.y >= 0 && mouseLogicalSpace.x < cellGraph.SizeX && mouseLogicalSpace.y < cellGraph.SizeY;

        if (mouseLogicalSpace != lastMouseLogicalPosition || needsMouseUpdate)
        {
            lastMouseLogicalPosition = mouseLogicalSpace;
            OnMouseMove(mouseLogicalSpace, !onGrid);
        }

        if (Input.GetMouseButtonDown(0) && pendingTargetActive && pendingPath != null && pendingTarget != lastCommittedCell)
        {
            lastCommittedCell = pendingTarget;
            hasCommitedCell = true;
            commandCheckpoints.Enqueue(pendingTarget);
            player.PlayerAddMotionPath(pendingPath);
            pendingCommandPathLineSegments = new List<SegmentProperties>();

            RebuildPlayerGraph();
        }

        foreach (var c in commandCheckpoints)
        {
            var worldSpace = GridSpaceConversion.GetWorldSpaceFromLogical(c, tilemap);
            var square = LineElements.SquareSelectionSegments(worldSpace, Color.grey);
            pendingLineSegments = pendingLineSegments.Concat(square);
        }

        pendingLineSegments = pendingLineSegments.Concat(pendingCommandPathLineSegments).Concat(pendingSelectionLineSegments);

        meshFilter.mesh = LineMesh.GenerateLineMesh(pendingLineSegments);
    }

    void OnGameOver(bool victory)
    {
        gameOver = true;
        meshRenderer.enabled = false;
        tilemap.gameObject.SetActive(false);
        if (victory)
        {
            WinOverlay.SetActive(true);
        }
        else
        {
            LoseOverlay.SetActive(true);
        }
    }

    void RevealNextLevel()
    {
        tilemap.gameObject.SetActive(false);

        if (currentlevel == Maps.Length - 1)
        {
            OnGameOver(true);
        }
        else
        {
            ++currentlevel;
            LevelCounter.GetComponent<Text>().text = currentlevel.ToString("D2");

            Restart();

            tilemap = Maps[currentlevel].GetComponent<Tilemap>();
            tilemap.gameObject.SetActive(true);
            cellGraph = LogicalCellGraph.BuildCellGraph(tilemap, GetGateLogicalPositions());
        }
    }

    LogicalCell FindAIGoal()
    {
        int closestItemDistance = int.MaxValue;
        LogicalCell closestItemCell = null;
        foreach (Item i in items.Values)
        {
            if ((!exitUnlocked && i.TriggerNextLevel) || (exitUnlocked && !i.TriggerNextLevel))
            {
                continue;
            }

            var logicalPosition = i.GetComponent<TilePlacedObject>().LogicalPosition;
            var cell = cellGraph.LookupCell(logicalPosition.x, logicalPosition.y);
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

    void OnAITick()
    {
        if (cellGraph == null)
        {
            return;
        }

        RebuildEnemyGraph();
        
        if (enemy == null || enemyWeightGraph == null)
        {
            return;
        }
        var goal = FindAIGoal();
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
            enemy.PlayerAddMotionPath(truncatedPath);
        }
    }

    void OnItemPickup(Item item, Vector3Int itemPos, bool pickedUpByPlayer)
    {
        items.Remove(itemPos);

        if (pickedUpByPlayer)
        {
            playerScore += item.PointValue;
            var score = ScoreCounter.GetComponent<Text>();
            score.text = playerScore.ToString("D6");
        }

        item.gameObject.SetActive(false);
        bool triggerNextLevel = item.TriggerNextLevel;

        
        if (item.Unlocks != null)
        {
            var gate = item.Unlocks.GetComponent<Gate>();
            gates.Remove(gate);
            gate.gameObject.SetActive(false);
            Destroy(gate);
            mapNeedsRebuild = true;
        }

        Destroy(item);
        if (triggerNextLevel)
        {
            if (pickedUpByPlayer)
            {
                RevealNextLevel();
            }
            else
            {
                OnGameOver(false);
            }
        }
    }


    void PostLogicalTick()
    {
        if (cellGraph == null)
        {
            return;
        }

        if (player != null)
        {
            var playerPos = player.CurrentLogicalPosition;

            if (commandCheckpoints.Any())
            {
                var firstCheckpoint = commandCheckpoints.Peek();
                if (firstCheckpoint == playerPos)
                {
                    commandCheckpoints.Dequeue();
                }
            }
            
            if (items.ContainsKey(playerPos))
            {
                var item = items[playerPos];
                if (!item.TriggerNextLevel || exitUnlocked)
                {
                    OnItemPickup(item, playerPos, true);
                }
            }
        }

        if (enemy != null)
        {
            var enemyPos = enemy.CurrentLogicalPosition;

            if (items.ContainsKey(enemyPos))
            {
                var item = items[enemyPos];

                if (!item.TriggerNextLevel || exitUnlocked)
                {
                    OnItemPickup(item, enemyPos, false);
                }
            }
        }

        SetExitColorIndicator();
    }

    private void OnLogicalTick()
    {
        foreach (IGameEventListener eventListener in listeners)
        {
            eventListener.LogicalTick();
        }
        PostLogicalTick();
    }

    private IEnumerator WaitAndTick(float tickTime, TickerFunction function)
    {
        while (!gameOver)
        {
            function.Invoke();
            
            yield return new WaitForSeconds(tickTime);
        }
    }
}
