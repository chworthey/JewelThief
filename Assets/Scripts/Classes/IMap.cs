using UnityEngine;

public interface IMap
{
    BoundsInt CellBounds { get; }

    Color GetColor(Vector3Int location);
    Vector3Int WorldToCell(Vector3 worldPosition);

    void Activate();
    void Deactivate();
}
