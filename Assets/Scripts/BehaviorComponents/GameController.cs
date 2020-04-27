using System.Collections.Generic;
using System.Linq;
using System.Collections;
using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEngine.UI;

delegate void TickerFunction();

public class GameController : MonoBehaviour, ILevelState
{
    public GameObject[] Maps;
    public GameObject LevelCounter;
    public GameObject ScoreCounter;
    public GameObject WinOverlay;
    public GameObject LoseOverlay;

    private IMap tilemap = null;
    private LogicalCellGraph cellGraph = null;
    private AIBrain enemyBrain = null;

    private MeshRenderer meshRenderer;
    private MeshFilter meshFilter;

    private PlayerController playerController = null;

    private Vector3Int lastMouseLogicalPosition;

    public IEnumerable<IItem> ActiveItems => items.Select(i => i.Value);
    public IEnumerable<IGate> ActiveGates => gates;
    public IPawn Player { get; private set; }
    public IPawn Enemy { get; private set; }
    public IMap Map => throw new System.NotImplementedException();
    public IGameStats GameStats => stats;
    private readonly GameStats stats = new GameStats
    {
        CurrentLevel = 0,
        ExitUnlocked = false,
        GameOver = false,
        PlayerScore = 0
    };

    public IItem ExitItem { get; private set; } = null;

    private List<IGameEventListener> listeners = new List<IGameEventListener>();
    private Dictionary<Vector3Int, IItem> items = new Dictionary<Vector3Int, IItem>();
    private List<IGate> gates = new List<IGate>();

    private bool mapNeedsRebuild = false;
    private bool needsMouseUpdate = false;

    private void Restart()
    {
        tilemap = null;
        cellGraph = null;
        playerController = null;
        listeners = new List<IGameEventListener>();
        Player = null;
        Enemy = null;
        items = new Dictionary<Vector3Int, IItem>();
        gates = new List<IGate>();
        mapNeedsRebuild = true;
        ExitItem = null;
        GameStats.ExitUnlocked = false;
    }

    public void RegisterEventListener(IGameEventListener listener)
    {
        listeners.Add(listener);
    }

    public void RegisterPlayer(Pawn player)
    {
        Player = player;
        playerController = new PlayerController(player);
    }

    public void RegisterEnemy(Pawn enemy)
    {
        Enemy = enemy;
        enemyBrain = new AIBrain(enemy);
    }

    public void RegisterItem(IItem item)
    {
        items[item.LogicalLocation] = item;
        if (item.EndsLevel)
        {
            ExitItem = item;
        }
    }

    public void RegisterGate(IGate gate)
    {
        gates.Add(gate);
        mapNeedsRebuild = true;
    }

    private IEnumerable<Vector3Int> GetGateLogicalPositions()
    {
        if (gates == null)
        {
            return new List<Vector3Int>();
        }

        return gates.Select(g => g.LogicalLocation);
    }

    void Start()
    {
        tilemap = new Map(Maps[stats.CurrentLevel].GetComponent<Tilemap>());
        tilemap.Activate();

        mapNeedsRebuild = true;

        meshFilter = gameObject.AddComponent<MeshFilter>();
        meshRenderer = gameObject.AddComponent<MeshRenderer>();
        meshRenderer.sharedMaterial = new Material(Shader.Find("SimpleColorLine"));

        var logicalTickCoroutine = WaitAndTick(0.2f, OnLogicalTick);
        StartCoroutine(logicalTickCoroutine);
        var aiTickCoroutine = WaitAndTick(2.0f, OnAITick);
        StartCoroutine(aiTickCoroutine);
    }

    void Update()
    {
        if (GameStats.GameOver)
        {
            return;
        }

        if (mapNeedsRebuild)
        {
            cellGraph = LogicalCellGraph.BuildCellGraph(tilemap, GetGateLogicalPositions());
            playerController?.RebuildGraph(cellGraph);
            needsMouseUpdate = true;
            enemyBrain?.RebuildGraph(cellGraph);
            mapNeedsRebuild = false;
        }

        var mouseSceenSpace = Input.mousePosition;
        var mouseWorldSpace = Camera.main.ScreenToWorldPoint(mouseSceenSpace);
        var mouseGridSpace = tilemap.WorldToCell(mouseWorldSpace);
        var mouseLogicalSpace = GridSpaceConversion.GetLogicalSpaceFromGridSpace(mouseGridSpace, tilemap);

        if (playerController != null)
        {
            bool onGrid = mouseLogicalSpace.x >= 0 && mouseLogicalSpace.y >= 0 && mouseLogicalSpace.x < cellGraph.SizeX && mouseLogicalSpace.y < cellGraph.SizeY;

            if (mouseLogicalSpace != lastMouseLogicalPosition || needsMouseUpdate)
            {
                needsMouseUpdate = false;
                lastMouseLogicalPosition = mouseLogicalSpace;
                playerController.OnMouseMove(mouseLogicalSpace, !onGrid, cellGraph, tilemap);
            }

            if (Input.GetMouseButtonDown(0))
            {
                playerController.OnMouseClick(cellGraph);
                playerController.RebuildGraph(cellGraph);
                needsMouseUpdate = true;
            }
            var mesh = playerController.GenerateLineMesh(tilemap);
            meshFilter.mesh = mesh;
        }
    }

    void OnGameOver(bool victory)
    {
        GameStats.GameOver = true;
        meshRenderer.enabled = false;
        tilemap.Deactivate();
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
        tilemap.Deactivate();

        if (GameStats.CurrentLevel == Maps.Length - 1)
        {
            OnGameOver(true);
        }
        else
        {
            ++GameStats.CurrentLevel;
            LevelCounter.GetComponent<Text>().text = GameStats.CurrentLevel.ToString("D2");

            Restart();

            tilemap = new Map(Maps[GameStats.CurrentLevel].GetComponent<Tilemap>());
            tilemap.Activate();
            cellGraph = LogicalCellGraph.BuildCellGraph(tilemap, GetGateLogicalPositions());
        }
    }

    void OnAITick()
    {
        enemyBrain?.Tick(cellGraph, this);
    }

    void OnItemPickup(IItem item, Vector3Int itemPos, bool pickedUpByPlayer)
    {
        items.Remove(itemPos);

        if (pickedUpByPlayer)
        {
            GameStats.PlayerScore += item.PointValue;
            var score = ScoreCounter.GetComponent<Text>();
            score.text = GameStats.PlayerScore.ToString("D6");
        }

        bool triggerNextLevel = item.EndsLevel;


        if (item.OpensGate != null)
        {
            var gate = item.OpensGate;
            gates.Remove(gate);
            gate.Deactivate();
            mapNeedsRebuild = true;
        }

        item.Deactivate();

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

        if (items.Count == 1 && !GameStats.ExitUnlocked)
        {
            GameStats.ExitUnlocked = true;
            foreach (var i in listeners)
            {
                i.ExitUnlocked();
            }
        }
    }

    void CollisionDetection()
    {
        if (cellGraph == null)
        {
            return;
        }

        if (Player != null && playerController != null)
        {
            var playerPos = Player.LogicalLocation;

            if (items.ContainsKey(playerPos))
            {
                var item = items[playerPos];
                if (!item.EndsLevel || GameStats.ExitUnlocked)
                {
                    OnItemPickup(item, playerPos, true);
                }
            }
        }

        if (Enemy != null)
        {
            var enemyPos = Enemy.LogicalLocation;

            if (items.ContainsKey(enemyPos))
            {
                var item = items[enemyPos];

                if (!item.EndsLevel || GameStats.ExitUnlocked)
                {
                    OnItemPickup(item, enemyPos, false);
                }
            }
        }
    }

    void PostLogicalTick()
    {
        playerController?.Tick();
        CollisionDetection();
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
        while (!GameStats.GameOver)
        {
            function.Invoke();
            
            yield return new WaitForSeconds(tickTime);
        }
    }
}
