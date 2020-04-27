using System.Collections.Generic;
using System.Linq;
using System.Collections;
using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEngine.UI;

/// <summary>
/// Used for a couple ticking coroutines
/// </summary>
delegate void TickerFunction();

/// <summary>
/// The main game controller
/// </summary>
public class GameController : MonoBehaviour, ILevelState
{
    // Game object paramaters
    public GameObject[] Maps;
    public GameObject LevelCounter;
    public GameObject ScoreCounter;
    public GameObject WinOverlay;
    public GameObject LoseOverlay;
    public AudioClip[] ItemNoises;
    public AudioClip SirenNoise;
    public AudioClip EndLevelNoise;

    // Map data
    private LogicalCellGraph cellGraph = null;

    // Used for rendering the UI lines from the player controller
    private MeshRenderer meshRenderer;
    private MeshFilter meshFilter;

    // Pawn controllers
    private AIBrain enemyBrain = null;
    private PlayerController playerController = null;

    // Misc caching stuff...
    private Vector3Int lastMouseLogicalPosition;
    private bool mapNeedsRebuild = false; 
    private bool needsMouseUpdate = false;

    // For misc sound effects
    private AudioSource audioSource = null;

    // ILevelState properties containing most of the game's juicy details
    public IEnumerable<IItem> ActiveItems => items.Select(i => i.Value);
    public IEnumerable<IGate> ActiveGates => gates;
    public IPawn Player { get; private set; }
    public IPawn Enemy { get; private set; }
    public IMap Map { get; private set; }
    public IGameStats GameStats => stats;
    private readonly GameStats stats = new GameStats
    {
        CurrentLevel = 0,
        ExitUnlocked = false,
        GameOver = false,
        PlayerScore = 0
    };
    public IItem ExitItem { get; private set; } = null;
    private Dictionary<Vector3Int, IItem> items = new Dictionary<Vector3Int, IItem>();
    private List<IGate> gates = new List<IGate>();

    // A list of subscribers that occasionally need notifying
    private List<IGameEventListener> listeners = new List<IGameEventListener>();

    /// <summary>
    /// Called when it's time to load a new level, and we need a clean slate
    /// </summary>
    private void Restart()
    {
        Map = null;
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

    /// <summary>
    /// Register a new game event listener. Listener will be notified when anything happens.
    /// </summary>
    /// <param name="listener">The event listener</param>
    public void RegisterEventListener(IGameEventListener listener)
    {
        listeners.Add(listener);
    }

    /// <summary>
    /// Register the player pawn
    /// </summary>
    /// <param name="player">The player pawn</param>
    public void RegisterPlayer(Pawn player)
    {
        Player = player;
        playerController = new PlayerController(player);
    }

    /// <summary>
    /// Register the enemy pawn
    /// </summary>
    /// <param name="enemy">The enemy pawn</param>
    public void RegisterEnemy(Pawn enemy)
    {
        Enemy = enemy;
        enemyBrain = new AIBrain(enemy);
    }

    /// <summary>
    /// Register an item
    /// </summary>
    /// <param name="item">The item</param>
    public void RegisterItem(IItem item)
    {
        items[item.LogicalLocation] = item;
        if (item.EndsLevel)
        {
            ExitItem = item;
        }
    }

    /// <summary>
    /// Register a gate (locked door/gate that gets unlocked with special items)
    /// </summary>
    /// <param name="gate">The gate</param>
    public void RegisterGate(IGate gate)
    {
        gates.Add(gate);
        mapNeedsRebuild = true;
    }

    /// <summary>
    /// Gets the gate's logical locations
    /// </summary>
    /// <returns></returns>
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
        Map = new Map(Maps[stats.CurrentLevel].GetComponent<Tilemap>());
        Map.Activate();

        mapNeedsRebuild = true;

        meshFilter = gameObject.AddComponent<MeshFilter>();
        meshRenderer = gameObject.AddComponent<MeshRenderer>();
        meshRenderer.sharedMaterial = new Material(Shader.Find("SimpleColorLine"));

        audioSource = gameObject.GetComponent<AudioSource>();

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
            cellGraph = LogicalCellGraph.BuildCellGraph(Map, GetGateLogicalPositions());
            playerController?.RebuildGraph(cellGraph);
            needsMouseUpdate = true;
            enemyBrain?.RebuildGraph(cellGraph);
            mapNeedsRebuild = false;
        }

        var mouseSceenSpace = Input.mousePosition;
        var mouseWorldSpace = Camera.main.ScreenToWorldPoint(mouseSceenSpace);
        var mouseGridSpace = Map.WorldToCell(mouseWorldSpace);
        var mouseLogicalSpace = GridSpaceConversion.GetLogicalSpaceFromGridSpace(mouseGridSpace, Map);

        if (playerController != null)
        {
            bool onGrid = mouseLogicalSpace.x >= 0 && mouseLogicalSpace.y >= 0 && mouseLogicalSpace.x < cellGraph.SizeX && mouseLogicalSpace.y < cellGraph.SizeY;

            if (mouseLogicalSpace != lastMouseLogicalPosition || needsMouseUpdate)
            {
                needsMouseUpdate = false;
                lastMouseLogicalPosition = mouseLogicalSpace;
                playerController.OnMouseMove(mouseLogicalSpace, !onGrid, cellGraph, Map);
            }

            if (Input.GetMouseButtonDown(0))
            {
                playerController.OnMouseClick();
                playerController.RebuildGraph(cellGraph);
                needsMouseUpdate = true;
            }
            var mesh = playerController.GenerateLineMesh(Map);
            meshFilter.mesh = mesh;
        }
    }

    /// <summary>
    /// Called when the game ends, win or lose
    /// </summary>
    /// <param name="victory">True if it was a win, false if lose</param>
    void OnGameOver(bool victory)
    {
        GameStats.GameOver = true;
        meshRenderer.enabled = false;
        Map.Deactivate();
        if (victory)
        {
            WinOverlay.SetActive(true);
        }
        else
        {
            LoseOverlay.SetActive(true);
        }
    }

    /// <summary>
    /// Called when the next level needs to be activated
    /// </summary>
    void RevealNextLevel()
    {
        Map.Deactivate();

        if (GameStats.CurrentLevel == Maps.Length - 1)
        {
            OnGameOver(true);
        }
        else
        {
            audioSource.PlayOneShot(EndLevelNoise);
            ++GameStats.CurrentLevel;
            LevelCounter.GetComponent<Text>().text = GameStats.CurrentLevel.ToString("D2");

            Restart();

            Map = new Map(Maps[GameStats.CurrentLevel].GetComponent<Tilemap>());
            Map.Activate();
            cellGraph = LogicalCellGraph.BuildCellGraph(Map, GetGateLogicalPositions());
        }
    }

    /// <summary>
    /// Coroutine function for the AI brain (on slower timer)
    /// </summary>
    void OnAITick()
    {
        enemyBrain?.Tick(cellGraph, this);
    }

    /// <summary>
    /// Called when an item is picked up
    /// </summary>
    /// <param name="item">The item</param>
    /// <param name="itemPos">The position of the item</param>
    /// <param name="pickedUpByPlayer">True if the item was picked up by a player</param>
    void OnItemPickup(IItem item, Vector3Int itemPos, bool pickedUpByPlayer)
    {
        items.Remove(itemPos);

        if (pickedUpByPlayer)
        {
            GameStats.PlayerScore += item.PointValue;
            var score = ScoreCounter.GetComponent<Text>();
            score.text = GameStats.PlayerScore.ToString("D6");
            audioSource.PlayOneShot(ItemNoises[Random.Range(0, ItemNoises.Length)]);
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
            audioSource.PlayOneShot(SirenNoise);
            GameStats.ExitUnlocked = true;
            foreach (var i in listeners)
            {
                i.ExitUnlocked();
            }
        }
    }

    /// <summary>
    /// Detect item<->player collisions
    /// </summary>
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

    /// <summary>
    /// Called after the regular logical tick has run its coarse.
    /// </summary>
    void PostLogicalTick()
    {
        playerController?.Tick();
        CollisionDetection();
    }

    /// <summary>
    /// The coroutine for the logical bits of the game
    /// </summary>
    private void OnLogicalTick()
    {
        foreach (IGameEventListener eventListener in listeners)
        {
            eventListener.LogicalTick();
        }
        PostLogicalTick();
    }

    /// <summary>
    /// Waits and ticks until the game is over
    /// </summary>
    /// <param name="tickTime">The time in seconds, between ticks</param>
    /// <param name="function">The function to call each tick</param>
    /// <returns></returns>
    private IEnumerator WaitAndTick(float tickTime, TickerFunction function)
    {
        while (!GameStats.GameOver)
        {
            function.Invoke();
            
            yield return new WaitForSeconds(tickTime);
        }
    }
}
