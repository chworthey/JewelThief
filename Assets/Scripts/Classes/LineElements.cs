
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Tilemaps;

static class LineElements
{
    const float zPos = -0.1f;

    public static IEnumerable<SegmentProperties> XSegments(Vector3 worldPos, Color color)
    {
        const float extent = 0.2f;
        const float width = 0.08f;
        Vector3 dir = Vector3.back;
        
        var corners = new Vector3[] {
                new Vector3(-extent,  extent, zPos) + worldPos,
                new Vector3( extent,  extent, zPos) + worldPos,
                new Vector3( extent, -extent, zPos) + worldPos,
                new Vector3(-extent, -extent, zPos) + worldPos,
        };

        return new SegmentProperties[] {
            new SegmentProperties { Start = corners[0], End = corners[2], FacingDirection = dir, Style = SegmentStyle.Solid, Width = width, Color = color },
            new SegmentProperties { Start = corners[1], End = corners[3], FacingDirection = dir, Style = SegmentStyle.Solid, Width = width, Color = color }
        };
    }

    public static IEnumerable<SegmentProperties> SquareSelectionSegments(Vector3 tilePosWorld, Color color)
    {
        const float squareScale = 0.8f;

        var corners = new Vector3[] {
                new Vector3(-squareScale,  squareScale, zPos) + tilePosWorld,
                new Vector3( squareScale,  squareScale, zPos) + tilePosWorld,
                new Vector3( squareScale, -squareScale, zPos) + tilePosWorld,
                new Vector3(-squareScale, -squareScale, zPos) + tilePosWorld,
        };

        Vector3 dir = Vector3.back;
        float width = 0.08f;

        return new SegmentProperties[] {
            new SegmentProperties { Start = corners[0], End = corners[1], FacingDirection = dir, Style = SegmentStyle.Solid, Width = width, Color = color },
            new SegmentProperties { Start = corners[1], End = corners[2], FacingDirection = dir, Style = SegmentStyle.Solid, Width = width, Color = color },
            new SegmentProperties { Start = corners[2], End = corners[3], FacingDirection = dir, Style = SegmentStyle.Solid, Width = width, Color = color },
            new SegmentProperties { Start = corners[3], End = corners[0], FacingDirection = dir, Style = SegmentStyle.Solid, Width = width, Color = color }
        };
    }

    public static IEnumerable<SegmentProperties> SegmentsFromPath(LogicalPath path, IMap tilemap)
    {
        var points = new List<Vector3>();
        foreach (var cell in path.Path)
        {
            var world =  GridSpaceConversion.GetWorldSpaceFromLogical(cell, tilemap);
            points.Add(world);
        }

        if (points.Count <= 1)
        {
            return new List<SegmentProperties>();
        }

        Vector3 facingDir = Vector3.back;
        const float width = 0.08f;
        Color color = Color.white;

        var segments = new SegmentProperties[points.Count - 1];
        Vector3 lastPoint = points.First();
        int segIndex = 0;
        for (int n = 1; n < points.Count; ++n)
        {
            var point = points[n];
            segments[segIndex++] = new SegmentProperties {
                Start = lastPoint, 
                End = point, 
                FacingDirection = facingDir, 
                Style = SegmentStyle.Dashed,
                Width = width,
                Color = color };
            lastPoint = point;
        }

        return segments;
    }

    #region DebugStuff
    public static IEnumerable<SegmentProperties> DebugGraphSegments(LogicalCellGraph graph, Tilemap tilemap)
    {
        List<SegmentProperties> props = new List<SegmentProperties>();
        Color color = Color.red;

        var minX = tilemap.cellBounds.xMin;
        var minY = tilemap.cellBounds.yMin;
        var offset = new Vector3(minX + 1, minY + 1, 0);

        foreach (var cell in graph.Cells)
        {
            if (cell.LeftNeighbor == null)
            {
                props.AddRange(XSegments(new Vector3(cell.X * 2 - 0.3f, cell.Y * 2, -0.1f) + offset, color));
            }
            if (cell.RightNeighbor == null)
            {
                props.AddRange(XSegments(new Vector3(cell.X * 2 + 0.3f, cell.Y * 2, -0.1f) + offset, color));
            }
            if (cell.TopNeighbor == null)
            {
                props.AddRange(XSegments(new Vector3(cell.X * 2, cell.Y * 2 + 0.3f, -0.1f) + offset, color));
            }
            if (cell.BottomNeighbor == null)
            {
                props.AddRange(XSegments(new Vector3(cell.X * 2, cell.Y * 2 - 0.3f, -0.1f) + offset, color));
            }
        }

        return props;
    }
    #endregion
}