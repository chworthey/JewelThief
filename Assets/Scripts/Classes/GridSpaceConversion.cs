using UnityEngine;
using UnityEngine.Tilemaps;

static class GridSpaceConversion
{
    public static Vector3Int GetGridSpaceFromLogical(Vector3Int logicalSpace, Tilemap tilemap)
    {
        return logicalSpace * 2 + tilemap.cellBounds.min;
    }

    public static Vector3Int GetLogicalSpaceFromGridSpace(Vector3Int gridSpace, Tilemap tilemap)
    {
        return (gridSpace - tilemap.cellBounds.min) / 2;
    }

    public static Vector3 GetWorldSpaceFromLogical(Vector3Int logicalSpace, Tilemap tilemap)
    {
        return GetGridSpaceFromLogical(logicalSpace, tilemap) + new Vector3(1, 1, -0.1f);
    }

    public static Vector3Int GetLogicalSpaceFromCell(LogicalCell cell)
    {
        return new Vector3Int(cell.X, cell.Y, 0);
    }
}