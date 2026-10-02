using System;
using GrannyRacer.Editor;
using GrannyRacer.Racing;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace GrannyRacer.Tests.EditMode
{
    public sealed class CircuitBuilderTests
    {
        private CircuitRecipe recipe;
        [SetUp] public void Setup() { recipe = ScriptableObject.CreateInstance<CircuitRecipe>(); recipe.controlPoints = CircuitLayout.Square(); }
        [TearDown] public void Cleanup() => UnityEngine.Object.DestroyImmediate(recipe);

        [Test] public void SquarePresetProducesAnOrderedClosedRoadWithSpaceToBrake()
        {
            var points = CircuitLayout.Bake(recipe);
            Assert.That(TrackLayoutBuilder.MeasureLapLength(points), Is.InRange(800, 900));
            Assert.That(TrackLayoutBuilder.MeasureTightestCornerRadius(points), Is.GreaterThan(10));
            var checkpoints = 0;
            foreach (var point in points) if (point.checkpoint) checkpoints++;
            Assert.That(checkpoints, Is.EqualTo(8));
            GreyboxTrackGenerator.BuildRoadGeometry(points, out var vertices, out var triangles);
            Assert.That(triangles[triangles.Length - 5], Is.Zero, "Closing quad must reuse the first edge.");
            Assert.That(triangles[triangles.Length - 4], Is.EqualTo(1));
            for (var i = 0; i < triangles.Length; i += 3)
                Assert.That(Vector3.Cross(vertices[triangles[i+1]]-vertices[triangles[i]], vertices[triangles[i+2]]-vertices[triangles[i]]).y, Is.GreaterThan(0));
        }

        [Test] public void ShortcutCannotSkipARequiredGate()
        {
            recipe.drivewayShortcut = true;
            Assert.Throws<ArgumentException>(() => CircuitLayout.Bake(recipe));
            recipe.controlPoints[8].checkpoint = false;
            Assert.DoesNotThrow(() => CircuitLayout.Bake(recipe));
            Assert.That(CircuitLayout.ClearsShortcut(new Vector3(60,0,50), 2, recipe), Is.False);
        }

        [Test] public void RejectsDuplicateClosingPointBeforeSplineGeneration()
        {
            recipe.controlPoints[recipe.controlPoints.Length - 1] = recipe.controlPoints[0];
            Assert.Throws<ArgumentException>(() => CircuitLayout.Bake(recipe));
        }
        [TestCase(float.NaN)] [TestCase(float.PositiveInfinity)] [TestCase(2)]
        public void RejectsInvalidWidth(float width)
        {
            recipe.controlPoints[3].width = width;
            Assert.Throws<ArgumentException>(() => CircuitLayout.Bake(recipe));
        }
        [Test] public void RejectsMissingFinishCheckpoint()
        {
            recipe.controlPoints[0].checkpoint = false;
            Assert.Throws<ArgumentException>(() => CircuitLayout.Bake(recipe));
        }
        [Test] public void RejectsCrossingRoute()
        {
            recipe.controlPoints = new[] { new TrackWaypoint(new Vector3(-50,0,-50),10,true),
                new TrackWaypoint(new Vector3(50,0,50),10,true), new TrackWaypoint(new Vector3(-50,0,50),10,true), new TrackWaypoint(new Vector3(50,0,-50),10,true) };
            Assert.Throws<ArgumentException>(() => CircuitLayout.Bake(recipe));
        }
        [Test] public void RejectsUnsupportedElevationInsteadOfLeavingFloatingLots()
        {
            recipe.controlPoints[4].position.y = 5;
            Assert.Throws<ArgumentException>(() => CircuitLayout.Bake(recipe));
        }
        [Test] public void ClearanceAccountsForTheWholeAssetAndTheOppositeRoad()
        {
            var points = CircuitLayout.Bake(recipe);
            Assert.That(CircuitLayout.ClearsRoad(new Vector3(0,0,-90), 5, points), Is.False);
            Assert.That(CircuitLayout.ClearsRoad(new Vector3(0,0,0), 5, points), Is.True);
            Assert.That(CircuitLayout.ClearsRoad(new Vector3(0,0,0), 110, points), Is.False);
        }
        [Test] public void RecipeBuildsUseTheirOwnPersistentMeshFolder()
        {
            var saved = AssetDatabase.LoadAssetAtPath<CircuitRecipe>(CircuitSceneBuilder.DefaultRecipePath);
            if (saved == null) Assert.Ignore("Build the default circuit before the asset integration checks.");
            var path = CircuitSceneBuilder.OutputFolder(saved);
            Assert.That(path, Does.Not.Contain("QuietSunday"));
            var road = AssetDatabase.LoadAssetAtPath<Mesh>(path + "/Road.asset");
            var legacy = AssetDatabase.LoadAssetAtPath<Mesh>("Assets/GrannyRacer/Settings/Generated/QuietSunday_Road.asset");
            Assert.That(road, Is.Not.Null); Assert.That(road, Is.Not.SameAs(legacy));
            foreach (var side in new[] { -1, 1 })
            {
                var curb = AssetDatabase.LoadAssetAtPath<Mesh>(path + "/CurbRamp" + side + ".asset");
                for (var i = 0; i < road.vertexCount / 2; i++)
                {
                    Assert.That(curb.vertices[2*i], Is.EqualTo(road.vertices[2*i + (side > 0 ? 1 : 0)]));
                    Assert.That(curb.normals[2*i].sqrMagnitude, Is.GreaterThan(.99f));
                }
            }
        }
    }
}
