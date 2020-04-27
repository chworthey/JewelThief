using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[RequireComponent(typeof(TilePlacedObject))]
public class Pawn : MonoBehaviour, IGameEventListener
{
    public GameObject GameController;
    private GameController controller;
    private Animator animator;

    private Queue<Vector3Int> currentMotionPath = new Queue<Vector3Int>();

    public Vector3Int CurrentLogicalPosition { get; private set; }
    public bool IsPlayer = false;

    private TilePlacedObject tilePlacedComponent;

    void Start()
    {
        tilePlacedComponent = GetComponent<TilePlacedObject>();
        CurrentLogicalPosition = tilePlacedComponent.LogicalPosition;
        controller = GameController.GetComponent<GameController>();
        animator = GetComponent<Animator>();
        controller.RegisterEventListener(this);
        if (IsPlayer)
        {
            controller.RegisterPlayer(this);
        }
        else
        {
            controller.RegisterEnemy(this);
        }
    }

    public void LogicalTick()
    {
        if (currentMotionPath.Any())
        {
            animator.SetBool("IsMoving", true);
        }
        else
        {
            animator.SetBool("IsMoving", false);
        }

        while (currentMotionPath.Any())
        {
            var nextPosLogical = currentMotionPath.Dequeue();

            if (nextPosLogical == CurrentLogicalPosition)
            {
                continue;
            }
            else
            {
                var nextPosWorld = GridSpaceConversion.GetWorldSpaceFromLogical(
                  nextPosLogical, 
                  tilePlacedComponent.Tilemap);

                float angle = 180.0f;
                if (nextPosLogical.x > CurrentLogicalPosition.x)
                {
                    angle = 90.0f;
                }
                else if (nextPosLogical.x < CurrentLogicalPosition.x)
                {
                    angle = -90.0f;
                }
                else if (nextPosLogical.y > CurrentLogicalPosition.y)
                {
                    angle = 0.0f;
                }

                CurrentLogicalPosition = nextPosLogical;
                gameObject.transform.localPosition = nextPosWorld;
                gameObject.transform.localRotation = Quaternion.AngleAxis(angle, Vector3.back) * Quaternion.Euler(-90.0f, 0.0f, 0.0f);
                break;
            }
        }
    }

    public void PlayerAddMotionPath(LogicalPath path)
    {
        foreach (var cell in path.Path)
        {
            currentMotionPath.Enqueue(new Vector3Int(cell.x, cell.y, 0));
        }
    }
}