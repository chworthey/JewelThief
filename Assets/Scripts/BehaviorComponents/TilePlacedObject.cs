
using UnityEngine;
using UnityEngine.Tilemaps;

/// <summary>
/// A general convenience class for fetching the tile the object
/// is currently on, approximately
/// </summary>
public class TilePlacedObject : MonoBehaviour
{
    public IMap Tilemap { get; private set; }

    public Vector3Int LogicalPosition => GridSpaceConversion.GetLogicalSpaceFromGridSpace(
        Tilemap?.WorldToCell(gameObject.transform.localPosition) ?? Vector3Int.zero, 
        Tilemap
    );

    public void Awake()
    {
        Tilemap = new Map(GetComponentInParent<Tilemap>());
    }
}