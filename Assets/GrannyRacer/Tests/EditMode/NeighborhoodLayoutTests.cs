using GrannyRacer.Editor;
using GrannyRacer.Racing;
using NUnit.Framework;
using UnityEngine;

namespace GrannyRacer.Tests.EditMode
{
    public sealed class NeighborhoodLayoutTests
    {
        [TestCase(false,6)] [TestCase(true,8)]
        public void DoglegRoutesHaveContinuousValidatedRoadsAndOrderedSectors(bool school,int checkpoints)
        {
            var recipe=ScriptableObject.CreateInstance<CircuitRecipe>();
            try
            {
                recipe.controlPoints=CircuitLayout.Neighborhood(school);
                var points=CircuitLayout.Bake(recipe);
                var count=0;foreach(var point in points)if(point.checkpoint)count++;
                Assert.That(count,Is.EqualTo(checkpoints));
                Assert.That(TrackLayoutBuilder.MeasureLapLength(points),Is.InRange(700,1300));
                Assert.That(TrackLayoutBuilder.MeasureTightestCornerRadius(points),Is.GreaterThan(10));
            }
            finally { Object.DestroyImmediate(recipe); }
        }
    }
}
