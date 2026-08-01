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

        [Test]
        public void DriftSteerHoldsItsLineAndCannotBeSteeredInsideOut()
        {
            // Steering into a right-hand drift tightens it.
            Assert.That(WalkerHandlingMath.DriftSteer(1, 1f, 0.7f, 0.45f),
                Is.EqualTo(1f).Within(0.001f));
            // Counter-steering opens it out but never crosses back through zero.
            Assert.That(WalkerHandlingMath.DriftSteer(1, -1f, 0.7f, 0.45f),
                Is.EqualTo(0.25f).Within(0.001f));
            Assert.That(WalkerHandlingMath.DriftSteer(1, -1f, 0.3f, 0.9f),
                Is.EqualTo(0f).Within(0.001f),
                "A right drift must never resolve to left steering.");
            // A left drift mirrors it.
            Assert.That(WalkerHandlingMath.DriftSteer(-1, -1f, 0.7f, 0.45f),
                Is.EqualTo(-1f).Within(0.001f));
            Assert.That(WalkerHandlingMath.DriftSteer(-1, 1f, 0.7f, 0.45f),
                Is.EqualTo(-0.25f).Within(0.001f));
            // With no drift committed the player's steering passes straight through.
            Assert.That(WalkerHandlingMath.DriftSteer(0, 0.4f, 0.7f, 0.45f),
                Is.EqualTo(0.4f).Within(0.001f));
        }

        [Test]
        public void SkidRequiresSpeedBrakeAndSteeringTogether()
        {
            Assert.That(WalkerHandlingMath.ShouldSkid(10f, 0.8f, -0.7f, 7f, 0.35f, 0.35f),
                Is.True);
            Assert.That(WalkerHandlingMath.ShouldSkid(4f, 0.8f, -0.7f, 7f, 0.35f, 0.35f),
                Is.False);
            Assert.That(WalkerHandlingMath.ShouldSkid(10f, 0.1f, -0.7f, 7f, 0.35f, 0.35f),
                Is.False);
            Assert.That(WalkerHandlingMath.ShouldSkid(10f, 0.8f, 0.1f, 7f, 0.35f, 0.35f),
                Is.False);
        }
    }
}
