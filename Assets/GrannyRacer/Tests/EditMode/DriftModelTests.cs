using GrannyRacer.Walker;
using NUnit.Framework;

namespace GrannyRacer.Tests.EditMode
{
    public sealed class DriftModelTests
    {
        private static DriftTuning Tuning()
        {
            return new DriftTuning(
                minimumSpeed: 5f,
                minimumSteer: 0.3f,
                insideChargeRate: 1.5f,
                outsideChargeRate: 0.5f,
                redSeconds: 0.7f,
                yellowSeconds: 1.6f,
                blueSeconds: 2.6f,
                redBoostSpeed: 2f,
                yellowBoostSpeed: 4f,
                blueBoostSpeed: 6f,
                boostDuration: 0.9f);
        }

        /// <summary>Holds the drift for the given time at a fixed 50 Hz step.</summary>
        private static void Hold(DriftModel drift, float seconds, float steer, bool grounded = true)
        {
            var tuning = Tuning();
            var steps = (int)System.Math.Round(seconds / 0.02f);
            for (var i = 0; i < steps; i++)
            {
                drift.Tick(tuning, 0.02f, true, grounded, 12f, steer);
            }
        }

        [Test]
        public void HoppingStraightOrTooSlowDoesNotStartADrift()
        {
            var drift = new DriftModel();

            Assert.That(drift.TryStart(Tuning(), 12f, 0.05f), Is.False,
                "A hop without steering must stay an ordinary jump.");
            Assert.That(drift.TryStart(Tuning(), 2f, 1f), Is.False,
                "Below the minimum speed there is nothing to drift.");
            Assert.That(drift.IsDrifting, Is.False);
        }

        [Test]
        public void HoppingWhileSteeringLocksTheDriftDirection()
        {
            var drift = new DriftModel();

            Assert.That(drift.TryStart(Tuning(), 12f, -0.8f), Is.True);
            Assert.That(drift.IsDrifting, Is.True);
            Assert.That(drift.Direction, Is.EqualTo(-1), "Steering left must drift left.");

            // Steering the other way mid-drift must not flip the committed direction.
            Hold(drift, 0.4f, 1f);
            Assert.That(drift.Direction, Is.EqualTo(-1));
        }

        [Test]
        public void ChargeClimbsThroughRedYellowThenBlue()
        {
            var drift = new DriftModel();
            var tuning = Tuning();
            drift.TryStart(tuning, 12f, 1f);

            Assert.That(drift.GetStage(tuning), Is.EqualTo(DriftChargeStage.None));

            // Steering into the drift banks charge at 1.5x, so 0.6 s of holding passes 0.7 s.
            Hold(drift, 0.6f, 1f);
            Assert.That(drift.GetStage(tuning), Is.EqualTo(DriftChargeStage.Red));

            Hold(drift, 0.6f, 1f);
            Assert.That(drift.GetStage(tuning), Is.EqualTo(DriftChargeStage.Yellow));

            Hold(drift, 1f, 1f);
            Assert.That(drift.GetStage(tuning), Is.EqualTo(DriftChargeStage.Blue));
        }

        [Test]
        public void CounterSteeringChargesSlowerThanSteeringIntoTheDrift()
        {
            var inside = new DriftModel();
            var outside = new DriftModel();
            inside.TryStart(Tuning(), 12f, 1f);
            outside.TryStart(Tuning(), 12f, 1f);

            Hold(inside, 1f, 1f);
            Hold(outside, 1f, -1f);

            Assert.That(inside.Charge, Is.GreaterThan(outside.Charge),
                $"Inside charge {inside.Charge:0.00}s must beat outside {outside.Charge:0.00}s.");
        }

        [Test]
        public void AirborneTimeKeepsTheDriftButBanksNoCharge()
        {
            var drift = new DriftModel();
            drift.TryStart(Tuning(), 12f, 1f);

            Hold(drift, 0.5f, 1f, grounded: false);

            Assert.That(drift.IsDrifting, Is.True, "A hop must not cancel the drift.");
            Assert.That(drift.Charge, Is.EqualTo(0f).Within(0.0001f),
                "Charge is banked by slipper on tarmac, not by air time.");
        }

        [Test]
        public void ReleasingBeforeRedGivesNoBoost()
        {
            var drift = new DriftModel();
            var tuning = Tuning();
            drift.TryStart(tuning, 12f, 1f);
            Hold(drift, 0.2f, 1f);

            drift.Tick(tuning, 0.02f, false, true, 12f, 1f);

            Assert.That(drift.IsDrifting, Is.False);
            Assert.That(drift.ConsumeBoostImpulse(), Is.EqualTo(0f));
            Assert.That(drift.BoostRemaining, Is.EqualTo(0f));
        }

        [Test]
        public void EachTierReleasesAStrongerBoost()
        {
            Assert.That(BoostFor(0.6f), Is.EqualTo(2f).Within(0.001f), "Red tier boost.");
            Assert.That(BoostFor(1.2f), Is.EqualTo(4f).Within(0.001f), "Yellow tier boost.");
            Assert.That(BoostFor(2f), Is.EqualTo(6f).Within(0.001f), "Blue tier boost.");
        }

        [Test]
        public void TheBoostIsHandedOutOnceAndThenExpires()
        {
            var drift = new DriftModel();
            var tuning = Tuning();
            drift.TryStart(tuning, 12f, 1f);
            Hold(drift, 0.6f, 1f);
            drift.Tick(tuning, 0.02f, false, true, 12f, 1f);

            Assert.That(drift.ConsumeBoostImpulse(), Is.GreaterThan(0f));
            Assert.That(drift.ConsumeBoostImpulse(), Is.EqualTo(0f),
                "A one-shot impulse must not be applied on every physics step.");
            Assert.That(drift.BoostStage, Is.EqualTo(DriftChargeStage.Red));

            for (var i = 0; i < 60; i++)
            {
                drift.Tick(tuning, 0.02f, false, true, 12f, 0f);
            }

            Assert.That(drift.BoostRemaining, Is.EqualTo(0f));
            Assert.That(drift.BoostStage, Is.EqualTo(DriftChargeStage.None));
        }

        [Test]
        public void DroppingBelowTheMinimumSpeedEndsTheDrift()
        {
            var drift = new DriftModel();
            var tuning = Tuning();
            drift.TryStart(tuning, 12f, 1f);
            Hold(drift, 0.6f, 1f);

            drift.Tick(tuning, 0.02f, true, true, 1f, 1f);

            Assert.That(drift.IsDrifting, Is.False);
            Assert.That(drift.ConsumeBoostImpulse(), Is.GreaterThan(0f),
                "Charge already banked is still paid out when the drift dies naturally.");
        }

        [Test]
        public void ResetClearsDriftAndBoost()
        {
            var drift = new DriftModel();
            var tuning = Tuning();
            drift.TryStart(tuning, 12f, 1f);
            Hold(drift, 1f, 1f);

            drift.Reset();

            Assert.That(drift.IsDrifting, Is.False);
            Assert.That(drift.Charge, Is.EqualTo(0f));
            Assert.That(drift.BoostRemaining, Is.EqualTo(0f));
            Assert.That(drift.ConsumeBoostImpulse(), Is.EqualTo(0f));
        }

        private static float BoostFor(float heldSeconds)
        {
            var drift = new DriftModel();
            var tuning = Tuning();
            drift.TryStart(tuning, 12f, 1f);
            Hold(drift, heldSeconds, 1f);
            drift.Tick(tuning, 0.02f, false, true, 12f, 1f);
            return drift.ConsumeBoostImpulse();
        }
    }
}
