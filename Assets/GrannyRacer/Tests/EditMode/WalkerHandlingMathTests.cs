using GrannyRacer.Walker;
using NUnit.Framework;

namespace GrannyRacer.Tests.EditMode
{
    public sealed class WalkerHandlingMathTests
    {
        [Test]
        public void SteeringScale_IsFullAtRest()
        {
            Assert.That(WalkerHandlingMath.SteeringScale(0f, 15f, 0.4f), Is.EqualTo(1f));
        }

        [Test]
        public void SteeringScale_ReachesConfiguredScaleAtTopSpeed()
        {
            Assert.That(WalkerHandlingMath.SteeringScale(15f, 15f, 0.4f), Is.EqualTo(0.4f).Within(0.0001f));
        }

        [Test]
        public void SteeringScale_DoesNotFallFurtherAboveTopSpeed()
        {
            Assert.That(WalkerHandlingMath.SteeringScale(30f, 15f, 0.4f), Is.EqualTo(0.4f).Within(0.0001f));
        }

        [Test]
        public void BrakeBecomesGentlerReverseInputWhenStopped()
        {
            Assert.That(WalkerHandlingMath.DriveInput(0f, 1f, 0f), Is.EqualTo(-0.65f).Within(0.0001f));
        }
    }
}
