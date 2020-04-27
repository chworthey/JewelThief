using System;
using UnityEngine;
using UnityEngine.Tilemaps;

/// <summary>
/// A wrapper implementation for tilemap
/// </summary>
class Map : IMap
{
    public BoundsInt CellBounds => map.cellBounds;

    private readonly Tilemap map;

    public Map(Tilemap map)
    {
        this.map = map;
    }

    public void Activate()
    {
        map.gameObject.SetActive(true);
    }

    public void Deactivate()
    {
        map.gameObject.SetActive(false);
    }

    public Color GetColor(Vector3Int location)
    {
        return map.GetColor(location);
    }

    public Vector3Int WorldToCell(Vector3 worldPosition)
    {
        return map.WorldToCell(worldPosition);
    }
}
