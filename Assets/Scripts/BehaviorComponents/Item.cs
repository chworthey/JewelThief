using UnityEngine;

/// <summary>
/// Represents the physical item pickup
/// </summary>
[RequireComponent(typeof(TilePlacedObject))]
public class Item : MonoBehaviour, IItem, IGameEventListener
{
    public GameObject GameController = null;
    public int PointValue = 12;
    public GameObject Unlocks = null;
    public bool TriggerNextLevel = false;

    public IGate OpensGate => Unlocks == null ? null : Unlocks.GetComponent<Gate>();

    public bool EndsLevel => TriggerNextLevel;

    public Vector3Int LogicalLocation => GetComponent<TilePlacedObject>().LogicalPosition;

    int IItem.PointValue => PointValue;

    public void Activate()
    {
        gameObject.SetActive(true);
    }

    public void Deactivate()
    {
        gameObject.SetActive(false);
    }

    public void ExitUnlocked()
    {
        if (EndsLevel)
        {
            GetComponent<SpriteRenderer>().color = Color.white;
        }
    }

    public void LogicalTick()
    {
    }

    public void Start()
    {
        var controller = GameController.GetComponent<GameController>();
        controller.RegisterItem(this);
        controller.RegisterEventListener(this);

        if (EndsLevel)
        {
            GetComponent<SpriteRenderer>().color = new Color(0.2f, 0.2f, 0.2f, 1.0f);
        }
    }
}