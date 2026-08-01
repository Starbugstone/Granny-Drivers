using GrannyRacer.Racing;
using NUnit.Framework;
using UnityEngine;

namespace GrannyRacer.Tests.EditMode
{
    public sealed class TrackDefinitionTests
    {
        [Test]
        public void ClosedSplineWrapsAtOne()
        {
            var track = ScriptableObject.CreateInstance<TrackDefinition>();
            track.SetPrototypeData(new[]
            {
                new TrackWaypoint(new Vector3(0f, 0f, 0f), 8f, true),
                new TrackWaypoint(new Vector3(10f, 0f, 0f), 8f, true),
                new TrackWaypoint(new Vector3(10f, 0f, 10f), 8f, true),
                new TrackWaypoint(new Vector3(0f, 0f, 10f), 8f, true)
            }, 3, 0, 1, 4f);

            Assert.That(Vector3.Distance(track.EvaluateClosedSpline(0f), track.EvaluateClosedSpline(1f)),
                Is.LessThan(0.0001f));
            Object.DestroyImmediate(track);
        }
    }
}
