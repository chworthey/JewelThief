using UnityEngine;

[RequireComponent(typeof(TilePlacedObject))]
public class Item : MonoBehaviour
{
    public GameObject GameController = null;
    public int PointValue = 12;
    public GameObject Unlocks = null;
    public bool TriggerNextLevel = false;

    public void Start()
    {
        var controller = GameController.GetComponent<GameController>();
        controller.RegisterItem(this);
    }
}