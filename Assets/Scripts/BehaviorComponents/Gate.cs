
using UnityEngine;

[RequireComponent(typeof(TilePlacedObject))]
public class Gate : MonoBehaviour, IGate
{
    public GameObject GameController = null;

    public Vector3Int LogicalLocation => GetComponent<TilePlacedObject>().LogicalPosition;

    public void Activate()
    {
        gameObject.SetActive(true);
    }

    public void Deactivate()
    {
        gameObject.SetActive(false);
    }

    public void Start()
    {
        var controller = GameController.GetComponent<GameController>();
        controller.RegisterGate(this);
    }
}