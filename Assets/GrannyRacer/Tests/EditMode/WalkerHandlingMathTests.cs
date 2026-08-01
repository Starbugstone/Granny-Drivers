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

        // The shipped POC tuning, so these read as the feel a playtester would get.
        private const float Bias = 0.7f;
        private const float Inward = 0.38f;
        private const float Counter = 0.62f;
        private const float MinimumHold = 0.08f;

        [Test]
        public void DriftSteerHoldsItsLineAndCannotBeSteeredInsideOut()
        {
            // Steering into a right-hand drift tightens it to full lock.
            Assert.That(WalkerHandlingMath.DriftSteer(1, 1f, Bias, Inward, Counter, MinimumHold),
                Is.EqualTo(1f).Within(0.001f));
            // Hands off, the drift carves on its own.
            Assert.That(WalkerHandlingMath.DriftSteer(1, 0f, Bias, Inward, Counter, MinimumHold),
                Is.EqualTo(Bias).Within(0.001f));
            // A left drift mirrors it exactly.
            Assert.That(WalkerHandlingMath.DriftSteer(-1, -1f, Bias, Inward, Counter, MinimumHold),
                Is.EqualTo(-1f).Within(0.001f));
            Assert.That(WalkerHandlingMath.DriftSteer(-1, 0f, Bias, Inward, Counter, MinimumHold),
                Is.EqualTo(-Bias).Within(0.001f));
            // With no drift committed the player's steering passes straight through.
            Assert.That(WalkerHandlingMath.DriftSteer(0, 0.4f, Bias, Inward, Counter, MinimumHold),
                Is.EqualTo(0.4f).Within(0.001f));
        }

        /// <summary>
        /// The counter-steer is the mechanic's release valve: fighting the drift has to run the
        /// walker nearly straight, so a drift can be held down a straight to keep charging,
        /// while still never resolving to neutral or flipping to the other lock.
        /// </summary>
        [Test]
        public void CounterSteeringRunsNearlyStraightWithoutEndingTheDrift()
        {
            var countered = WalkerHandlingMath.DriftSteer(1, -1f, Bias, Inward, Counter, MinimumHold);

            Assert.That(countered, Is.LessThan(0.15f),
                $"A full counter-steer must run close to straight, but held {countered:0.000}.");
            Assert.That(countered, Is.GreaterThan(0f),
                "A drift that resolves to zero steering has stopped being a drift.");
            Assert.That(WalkerHandlingMath.DriftSteer(-1, 1f, Bias, Inward, Counter, MinimumHold),
                Is.EqualTo(-countered).Within(0.001f), "Left and right must mirror.");
        }

        [Test]
        public void SteeringIntoADriftBeatsCounteringIt()
        {
            var inward = WalkerHandlingMath.DriftSteer(1, 1f, Bias, Inward, Counter, MinimumHold);
            var neutral = WalkerHandlingMath.DriftSteer(1, 0f, Bias, Inward, Counter, MinimumHold);
            var countered = WalkerHandlingMath.DriftSteer(1, -1f, Bias, Inward, Counter, MinimumHold);

            Assert.That(inward, Is.GreaterThan(neutral).And.GreaterThan(countered));
            Assert.That(neutral, Is.GreaterThan(countered));
        }

        [Test]
        public void TheMinimumHoldFloorsAnOverpoweredCounterSteer()
        {
            // A tuning where the counter-steer outweighs the bias would otherwise send a right
            // drift into left lock.
            Assert.That(WalkerHandlingMath.DriftSteer(1, -1f, 0.3f, 0.4f, 0.9f, 0.08f),
                Is.EqualTo(0.08f).Within(0.001f),
                "A right drift must never resolve to left steering.");
            Assert.That(WalkerHandlingMath.DriftSteer(-1, 1f, 0.3f, 0.4f, 0.9f, 0.08f),
                Is.EqualTo(-0.08f).Within(0.001f));
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
