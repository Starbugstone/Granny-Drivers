using GrannyRacer.Racing;
using NUnit.Framework;

namespace GrannyRacer.Tests.EditMode
{
    public sealed class CheckpointCrossingTests
    {
        [TestCase(-.3f,.2f,8,true)]
        [TestCase(.3f,-.2f,-8,false)]
        [TestCase(.3f,.8f,8,false)]
        [TestCase(-.8f,-.3f,8,false)]
        [TestCase(0,.1f,0,false)]
        [TestCase(0,.1f,1,true)]
        public void OnlyForwardPlaneCrossingsCount(float previous,float current,float speed,bool expected)
            => Assert.That(CheckpointCrossing.Forward(previous,current,speed),Is.EqualTo(expected));
    }
}
