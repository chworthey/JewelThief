using UnityEngine;

/// <summary>
/// Converts between Unity/Tilemap/Our conventions.
/// 1. WORLD SPACE - Firstly we have standard world space, self explanatory
/// 2. GRID SPACE - Then we have a tilemap object that occupies that world space with a grid.
///    The grid has integer coordinates of tiles that are similar to our preferred coordinates.
///    However, they're twice as big as our preferred "logical" coordinates.
/// 3. LOGICAL SPACE - An integer-indexed space that occupies 4 tilemap tiles. The reasoning is this:
///    The game allows you to jump between colors in logical space, but a tile can have up to 4 colors,
///    So we need some sort of method for the subdivision, and this is what made the most sense to me.
/// </summary>
static class GridSpaceConversion
{
    /// <summary>
    /// Converts a vector from logical (our) space back to tilemap grid space
    /// </summary>
    /// <param name="logicalSpace">The logical space coordinates</param>
    /// <param name="tilemap">The map in which the conversion is taking place</param>
    /// <returns>The grid space</returns>
    public static Vector3Int GetGridSpaceFromLogical(Vector3Int logicalSpace, IMap tilemap)
    {
        return logicalSpace * 2 + tilemap.CellBounds.min;
    }

    /// <summary>
    /// Converts a vector from tilemap grid space to logical (our) space
    /// </summary>
    /// <param name="gridSpace">The grid space coordinates</param>
    /// <param name="tilemap">The map in which the conversion is taking place</param>
    /// <returns>The logical space</returns>
    public static Vector3Int GetLogicalSpaceFromGridSpace(Vector3Int gridSpace, IMap tilemap)
    {
        return (gridSpace - tilemap.CellBounds.min) / 2;
    }

    /// <summary>
    /// Converts logical (our) space to world space (useful for procedural drawing/effects)
    /// </summary>
    /// <param name="logicalSpace"></param>
    /// <param name="tilemap"></param>
    /// <returns></returns>
    public static Vector3 GetWorldSpaceFromLogical(Vector3Int logicalSpace, IMap tilemap)
    {
        return GetGridSpaceFromLogical(logicalSpace, tilemap) + new Vector3(1, 1, -0.1f);
    }

    /// <summary>
    /// Extracts the logical space from a cell, likely taken from the cell graph
    /// </summary>
    /// <param name="cell">The cell to extract</param>
    /// <returns>The logical space of the cell</returns>
    public static Vector3Int GetLogicalSpaceFromCell(LogicalCell cell)
    {
        return new Vector3Int(cell.X, cell.Y, 0);
    }
}