using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// The style of the line segment to draw
/// </summary>
public enum SegmentStyle
{
    Solid = 0,
    Dashed = 1,
}

/// <summary>
/// The properties of a particular segment
/// </summary>
public struct SegmentProperties
{
    /// <summary>
    /// The segment style
    /// </summary>
    public SegmentStyle Style { get; set; }

    /// <summary>
    /// The start location (world space) of the segment
    /// </summary>
    public Vector3 Start { get; set; }

    /// <summary>
    /// The end location (world space) of the segment
    /// </summary>
    public Vector3 End { get; set; }
    
    /// <summary>
    /// The up-vector in which the faces and normals will be facing
    /// </summary>
    public Vector3 FacingDirection { get; set; }

    /// <summary>
    /// The width, in world space units, of the segment
    /// </summary>
    public float Width { get; set; }

    /// <summary>
    /// The vertex colors of the line segment.
    /// </summary>
    public Color Color { get; set; }
}

/// <summary>
/// The class for converting segment data into a usable mesh that
/// can be attached to a Unity mesh renderer
/// </summary>
public static class LineMesh
{
    /// <summary>
    /// Intermediate mesh data
    /// </summary>
    struct MeshData
    {
        /// <summary>
        /// The vertices to add
        /// </summary>
        public Vector3[] Vertices { get; set; }

        /// <summary>
        /// The triangles/indices to add
        /// </summary>
        public int[] Indices { get; set; }

        /// <summary>
        /// The normals to add
        /// </summary>
        public Vector3[] Normals { get; set; }

        /// <summary>
        /// The UVs to add
        /// </summary>
        public Vector2[] TexCoords { get; set; }

        /// <summary>
        /// The vertex colors to add
        /// </summary>
        public Color[] Colors { get; set; }
    }

    /// <summary>
    /// Generates mesh data for a specific line segment
    /// </summary>
    /// <param name="start">The start position of the line segment</param>
    /// <param name="end">The end position of the line segment</param>
    /// <param name="width">The width of the line segment</param>
    /// <param name="up">The up direction</param>
    /// <param name="color">The color of the line segment vertex colors</param>
    /// <returns></returns>
    private static MeshData generateSegmentData(Vector3 start, Vector3 end, float width, Vector3 up, Color color)
    {
        Vector3[] vertices;
        int[] indices;
        Vector3[] normals;
        Vector2[] texCoords;
        Color[] colors;

        float halfWidth = width / 2.0f;

        Vector3 segmentDirection = (end - start).normalized;
        Vector3 tangent = Vector3.Cross(segmentDirection, up).normalized;

        vertices = new Vector3[4] {
            (tangent - segmentDirection) * halfWidth + start,
            (-tangent - segmentDirection) * halfWidth + start,
            (tangent + segmentDirection) * halfWidth + end,
            (-tangent + segmentDirection) * halfWidth + end
        };

        indices = new int[6] {
            0, 2, 1,
            2, 3, 1
        };

        normals = new Vector3[4] {
            up,
            up,
            up,
            up
        };

        texCoords = new Vector2[4] {
            new Vector2(0.0f, 0.0f),
            new Vector2(1.0f, 0.0f),
            new Vector2(0.0f, 1.0f),
            new Vector2(1.0f, 1.0f),
        };

        colors = new Color[4] {
            color,
            color,
            color, 
            color
        };

        return new MeshData { Vertices = vertices, Indices = indices, Normals = normals, TexCoords = texCoords, Colors = colors };
    }

    /// <summary>
    /// Converts a list of segment data into a more usable mesh.
    /// </summary>
    /// <param name="segments">A list of line segments</param>
    /// <returns>A Unity mesh</returns>
    public static Mesh GenerateLineMesh(IEnumerable<SegmentProperties> segments)
    {
        Mesh mesh = new Mesh();

        const float dashedSpacing = 0.5f;
        const float dashLength = 0.5f;

        var vertices = new List<Vector3>();
        var indices = new List<int>();
        var normals = new List<Vector3>();
        var texCoords = new List<Vector2>();
        var colors = new List<Color>();

        var datas = new List<MeshData>();
        foreach (var segment in segments)
        {
            switch (segment.Style)
            {
                case SegmentStyle.Solid:
                    datas.Add(generateSegmentData(segment.Start, segment.End, segment.Width, segment.FacingDirection, segment.Color));
                    break;
                case SegmentStyle.Dashed:
                    var direction = segment.End - segment.Start;
                    var directionNorm = direction.normalized;
                    var totalLen = direction.magnitude;
                    var insertions = new List<Tuple<float, float>>();
                    for (float n = 0.0f; n < totalLen; n += dashedSpacing + dashLength)
                    {
                        insertions.Add(Tuple.Create(n, n + dashLength));
                    }
                    foreach (var insertion in insertions)
                    {
                        Vector3 start = directionNorm * insertion.Item1 + segment.Start;
                        Vector3 end   = directionNorm * insertion.Item2 + segment.Start;
                        datas.Add(generateSegmentData(start, end, segment.Width, segment.FacingDirection, segment.Color));
                    }
                    break;
                default:
                    break;
            }
        }

        int currentIndex = 0;
        foreach (var data in datas)
        {
            vertices.AddRange(data.Vertices);
            normals.AddRange(data.Normals);
            texCoords.AddRange(data.TexCoords);
            indices.AddRange(data.Indices.Select(i => currentIndex + i));
            colors.AddRange(data.Colors);
            currentIndex += data.Vertices.Length;
        }

        mesh.vertices  = vertices.ToArray();
        mesh.uv = texCoords.ToArray();
        mesh.normals = normals.ToArray();
        mesh.triangles = indices.ToArray();
        mesh.colors = colors.ToArray();

        return mesh;
    }
}
