
using UnityEngine;

[RequireComponent(typeof(TilePlacedObject))]
public class Gate : MonoBehaviour
{
    public GameObject GameController = null;

    public void Start()
    {
        var controller = GameController.GetComponent<GameController>();
        controller.RegisterGate(this);
    }
}