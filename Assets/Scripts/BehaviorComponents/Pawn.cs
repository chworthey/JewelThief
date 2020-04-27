using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// Represents a pawn that can be moved along a specified path by a controller
/// </summary>
[RequireComponent(typeof(TilePlacedObject))]
public class Pawn : MonoBehaviour, IGameEventListener, IPawn
{
    public GameObject GameController;
    private GameController controller;
    private Animator animator;

    private Queue<Vector3Int> currentMotionPath = new Queue<Vector3Int>();

    public Vector3Int LogicalLocation => logicalLocation;
    private Vector3Int logicalLocation;

    public bool IsPlayer = false;

    private TilePlacedObject tilePlacedComponent;

    void Start()
    {
        tilePlacedComponent = GetComponent<TilePlacedObject>();
        logicalLocation = GetComponent<TilePlacedObject>().LogicalPosition;
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

            if (nextPosLogical == LogicalLocation)
            {
                continue;
            }
            else
            {
                var nextPosWorld = GridSpaceConversion.GetWorldSpaceFromLogical(
                  nextPosLogical, 
                  tilePlacedComponent.Tilemap);

                float angle = 180.0f;
                if (nextPosLogical.x > LogicalLocation.x)
                {
                    angle = 90.0f;
                }
                else if (nextPosLogical.x < LogicalLocation.x)
                {
                    angle = -90.0f;
                }
                else if (nextPosLogical.y > LogicalLocation.y)
                {
                    angle = 0.0f;
                }

                logicalLocation = nextPosLogical;
                gameObject.transform.localPosition = nextPosWorld;
                gameObject.transform.localRotation = Quaternion.AngleAxis(angle, Vector3.back) * Quaternion.Euler(-90.0f, 0.0f, 0.0f);
                break;
            }
        }
    }

    public void PushMotionPath(LogicalPath path)
    {
        foreach (var cell in path.Path)
        {
            currentMotionPath.Enqueue(new Vector3Int(cell.x, cell.y, 0));
        }
    }

    public void ExitUnlocked()
    {
    }

    public void Activate()
    {
        gameObject.SetActive(true);
    }

    public void Deactivate()
    {
        gameObject.SetActive(false);
    }
}