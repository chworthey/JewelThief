
using UnityEngine;
using UnityEngine.Tilemaps;

public class TilePlacedObject : MonoBehaviour
{
    public IMap Tilemap => tilemap;
    private IMap tilemap;

    public Vector3Int LogicalPosition => GridSpaceConversion.GetLogicalSpaceFromGridSpace(
        tilemap?.WorldToCell(gameObject.transform.localPosition) ?? Vector3Int.zero, 
        tilemap
    );

    public void Awake()
    {
        tilemap = new Map(GetComponentInParent<Tilemap>());
    }
}