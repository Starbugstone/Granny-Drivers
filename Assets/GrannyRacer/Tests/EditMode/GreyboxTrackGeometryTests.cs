using GrannyRacer.Editor;
using GrannyRacer.Racing;
using NUnit.Framework;
using UnityEngine;

namespace GrannyRacer.Tests.EditMode
{
    public sealed class GreyboxTrackGeometryTests
    {
        private static TrackWaypoint[] SquareLoop()
        {
            return new[]
            {
                new TrackWaypoint(new Vector3(0f, 0f, -20f), 10f, true),
                new TrackWaypoint(new Vector3(20f, 0f, 0f), 10f, false),
                new TrackWaypoint(new Vector3(0f, 0f, 20f), 10f, true),
                new TrackWaypoint(new Vector3(-20f, 0f, 0f), 10f, false)
            };
        }

        [Test]
        public void RoadTrianglesFaceUpwards()
        {
            GreyboxTrackGenerator.BuildRoadGeometry(SquareLoop(), out var vertices, out var triangles);

            for (var i = 0; i < triangles.Length; i += 3)
            {
                var a = vertices[triangles[i]];
                var b = vertices[triangles[i + 1]];
                var c = vertices[triangles[i + 2]];
                var normal = Vector3.Cross(b - a, c - a);

                Assert.That(normal.y, Is.GreaterThan(0f),
                    $"Triangle {i / 3} faces downwards (normal {normal}). An inverted road "
                    + "renders inside-out and its MeshCollider is only solid from below.");
            }
        }

        [Test]
        public void RoadRibbonClosesTheLoop()
        {
            var points = SquareLoop();
            GreyboxTrackGenerator.BuildRoadGeometry(points, out var vertices, out var triangles);

            Assert.That(vertices, Has.Length.EqualTo(points.Length * 2));
            Assert.That(triangles, Has.Length.EqualTo(points.Length * 6));
            foreach (var index in triangles)
            {
                Assert.That(index, Is.InRange(0, vertices.Length - 1));
            }
        }

        [Test]
        public void RoadEdgesStraddleTheWaypointByHalfItsWidth()
        {
            var points = SquareLoop();
            GreyboxTrackGenerator.BuildRoadGeometry(points, out var vertices, out _);

            for (var i = 0; i < points.Length; i++)
            {
                var left = vertices[i * 2];
                var right = vertices[i * 2 + 1];
                Assert.That(Vector3.Distance(left, right), Is.EqualTo(points[i].width).Within(0.01f));
                Assert.That(Vector3.Distance((left + right) * 0.5f, points[i].position),
                    Is.LessThan(0.01f));
            }
        }
    }
}
