using GrannyRacer.Editor;
using GrannyRacer.Racing;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace GrannyRacer.Tests.EditMode
{
    public sealed class DrivewayGeometryTests
    {
        [Test] public void GardenPathTrianglesNeverCoverTheAsphalt()
        {
            var recipe = AssetDatabase.LoadAssetAtPath<CircuitRecipe>(CircuitSceneBuilder.DefaultRecipePath);
            var track = AssetDatabase.LoadAssetAtPath<TrackDefinition>(CircuitSceneBuilder.OutputFolder(recipe) + "/Track.asset");
            var mesh = DrivewayGeometry.Build(track);
            try
            {
                GreyboxTrackGenerator.BuildRoadGeometry(track.Waypoints, out var road, out var indices);
                var vertices = mesh.vertices; var triangles = mesh.triangles;
                Assert.That(triangles.Length, Is.GreaterThan(0), "Keep the usable garden path.");
                for (var i = 0; i < triangles.Length; i += 3)
                {
                    var a = vertices[triangles[i]]; var b = vertices[triangles[i+1]]; var c = vertices[triangles[i+2]];
                    Assert.That(Vector3.Cross(b-a,c-a).y, Is.GreaterThan(0), "Path surface must face up.");
                    for (var j = 0; j < indices.Length; j += 3)
                        Assert.That(Overlap(a,b,c,road[indices[j]],road[indices[j+1]],road[indices[j+2]]),
                            Is.False, $"Driveway triangle {i/3} covers asphalt triangle {j/3}.");
                }
            }
            finally { Object.DestroyImmediate(mesh); }
        }

        private static bool Overlap(Vector3 a, Vector3 b, Vector3 c, Vector3 d, Vector3 e, Vector3 f)
        {
            return !Separates(b-a,a,b,c,d,e,f) && !Separates(c-b,a,b,c,d,e,f) && !Separates(a-c,a,b,c,d,e,f)
                && !Separates(e-d,a,b,c,d,e,f) && !Separates(f-e,a,b,c,d,e,f) && !Separates(d-f,a,b,c,d,e,f);
        }

        private static bool Separates(Vector3 edge, Vector3 a, Vector3 b, Vector3 c, Vector3 d, Vector3 e, Vector3 f)
        {
            var axis = new Vector3(-edge.z,0,edge.x).normalized;
            if (axis.sqrMagnitude < .5f) return false;
            var amin = Mathf.Min(Vector3.Dot(a,axis),Vector3.Dot(b,axis),Vector3.Dot(c,axis));
            var amax = Mathf.Max(Vector3.Dot(a,axis),Vector3.Dot(b,axis),Vector3.Dot(c,axis));
            var bmin = Mathf.Min(Vector3.Dot(d,axis),Vector3.Dot(e,axis),Vector3.Dot(f,axis));
            var bmax = Mathf.Max(Vector3.Dot(d,axis),Vector3.Dot(e,axis),Vector3.Dot(f,axis));
            return amax <= bmin + .0002f || bmax <= amin + .0002f;
        }
    }
}
