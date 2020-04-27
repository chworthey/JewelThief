
using UnityEngine;
using UnityEngine.Tilemaps;

public class TilePlacedObject : MonoBehaviour
{
    public Tilemap Tilemap => tilemap;
    private Tilemap tilemap;

    public Vector3Int LogicalPosition => GridSpaceConversion.GetLogicalSpaceFromGridSpace(
        tilemap?.WorldToCell(gameObject.transform.localPosition) ?? Vector3Int.zero, 
        tilemap
    );

    public void Awake()
    {
        tilemap = GetComponentInParent<Tilemap>();
    }
}