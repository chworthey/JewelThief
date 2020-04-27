using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using System.Linq;

namespace Tests
{
    public class DijkstraTests
    {
        const int logicalCellSize = 2;

        class TestMap : IMap
        {
            public BoundsInt CellBounds => new BoundsInt(0, 0, 0, 4 * logicalCellSize, 4 * logicalCellSize, 0);

            public void Activate()
            {
            }

            public void Deactivate()
            {
            }

            // SGGG  (G = green tile, R = red tile)
            // GRXR  (S = start location, on green tile)
            // GRTG  (X = Locked gate we will place later, on green tile. Will have to walk around gate as well)
            // GGGG  (T = Target, on green tile)
            Color[,] colors = new Color[,]
            {
                //         S
                { Color.green, Color.green,    Color.green, Color.green,      Color.green, Color.green,    Color.green, Color.green },
                { Color.green, Color.green,    Color.green, Color.green,      Color.green, Color.green,    Color.green, Color.green },

                //                                                                 Gate (Locked)
                { Color.green, Color.green,    Color.red,     Color.red,      Color.green, Color.green,    Color.red, Color.red },
                { Color.green, Color.green,    Color.red,     Color.red,      Color.green, Color.green,    Color.red, Color.red },

                //                                                                 Goal
                { Color.green, Color.green,    Color.red,     Color.red,      Color.green, Color.green,    Color.green, Color.green },
                { Color.green, Color.green,    Color.red,     Color.red,      Color.green, Color.green,    Color.green, Color.green },


                { Color.green, Color.green,    Color.green, Color.green,      Color.green, Color.green,    Color.green, Color.green },
                { Color.green, Color.green,    Color.green, Color.green,      Color.green, Color.green,    Color.green, Color.green },
            };

            public Color GetColor(Vector3Int location)
            {
                return colors[location.y, location.x];
            }

            public Vector3Int WorldToCell(Vector3 worldPosition)
            {
                return Vector3Int.FloorToInt(worldPosition);
            }
        }

        private readonly Vector3Int[] testGateLocations = new[] { new Vector3Int(2, 1, 0) };

        private LogicalCellGraph generateTestGraph()
        {
            return LogicalCellGraph.BuildCellGraph(new TestMap(), testGateLocations);
        }

        private bool PathsMatch(IEnumerable<Vector3Int> path1, IEnumerable<Vector3Int> path2)
        {
            var l1 = path2.Except(path1);
            var l2 = path1.Except(path2);
            return !l1.Any() && !l2.Any();
        }

        [Test]
        public void GraphWithoutTeleportRule()
        {
            var cellGraph = generateTestGraph();
            var startingCell = cellGraph.LookupCell(0, 0);
            var targetCell = cellGraph.LookupCell(2, 2);

            var graph = DijkstraWeightGraph.BuildDijkstraWeightGraph(cellGraph, startingCell, maxDistance: 0, allowNeighborTeleportation: false);

            var expectedPath = new Vector3Int[]
            {
                new Vector3Int(0, 0, 0),
                new Vector3Int(0, 1, 0),
                new Vector3Int(0, 2, 0),
                new Vector3Int(0, 3, 0),
                new Vector3Int(1, 3, 0),
                new Vector3Int(2, 3, 0),
                new Vector3Int(2, 2, 0),
            };
            var expectedDistance = expectedPath.Length - 1;

            var (accessible, shortestPath) = graph.LookupShortestPath(targetCell);
            Assert.True(accessible);
            Assert.True(PathsMatch(shortestPath.Path, expectedPath));

            var (accessible2, distance) = graph.LookupDistance(targetCell);
            Assert.True(accessible2);
            Assert.True(distance == expectedDistance);
        }

        [Test]
        public void GraphWithTeleportRule()
        {
            var cellGraph = generateTestGraph();
            var startingCell = cellGraph.LookupCell(0, 0);
            var targetCell = cellGraph.LookupCell(2, 2);

            var graph = DijkstraWeightGraph.BuildDijkstraWeightGraph(cellGraph, startingCell, maxDistance: 0, allowNeighborTeleportation: true);

            // Either of these is acceptable...
            var expectedPathOne = new Vector3Int[]
            {
                new Vector3Int(0, 0, 0),
                new Vector3Int(0, 1, 0),
                new Vector3Int(0, 2, 0),
                new Vector3Int(2, 2, 0),
            };
            var expectedPathTwo = new Vector3Int[] // Hop the gate allowed, contrary to actual rules in original
            {                                      // I might adjust that later, but for now it's expected
                new Vector3Int(0, 0, 0),
                new Vector3Int(1, 0, 0),
                new Vector3Int(2, 0, 0),
                new Vector3Int(2, 2, 0),
            };
            var expectedDistance = expectedPathOne.Length - 1;

            var (accessible, shortestPath) = graph.LookupShortestPath(targetCell);
            Assert.True(accessible);
            Assert.True(PathsMatch(shortestPath.Path, expectedPathOne) ||
                PathsMatch(shortestPath.Path, expectedPathTwo));

            var (accessible2, distance) = graph.LookupDistance(targetCell);
            Assert.True(accessible2);
            Assert.True(distance == expectedDistance);
        }

        [Test]
        public void GraphWithLimitedDistance()
        {
            var cellGraph = generateTestGraph();
            var startingCell = cellGraph.LookupCell(0, 0);
            var targetCell = cellGraph.LookupCell(2, 2);

            var graph = DijkstraWeightGraph.BuildDijkstraWeightGraph(cellGraph, startingCell, maxDistance: 2, allowNeighborTeleportation: true);

            var (accessible, _) = graph.LookupShortestPath(targetCell);
            Assert.False(accessible);

            var (accessible2, _) = graph.LookupDistance(targetCell);
            Assert.False(accessible2);
        }
    }
}
