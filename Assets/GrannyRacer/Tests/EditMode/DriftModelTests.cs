using GrannyRacer.Walker;
using NUnit.Framework;

namespace GrannyRacer.Tests.EditMode
{
    public sealed class DriftModelTests
    {
        private const float Step = 0.02f;

        private static DriftTuning Tuning()
        {
            return new DriftTuning(
                minimumSpeed: 5f,
                minimumSteer: 0.3f,
                minimumAirTime: 0.08f,
                engageWindow: 0.6f,
                engageRamp: 0.15f,
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

        /// <summary>Ticks at a fixed 50 Hz with the jump held.</summary>
        private static void Hold(DriftModel drift, float seconds, float steer, bool grounded = true,
            float speed = 12f)
        {
            var tuning = Tuning();
            var steps = (int)System.Math.Round(seconds / Step);
            for (var i = 0; i < steps; i++)
            {
                drift.Tick(tuning, Step, true, grounded, speed, steer);
            }
        }

        /// <summary>
        /// The whole hop: arm, a hop's worth of air time, then touchdown while steering. This is
        /// the only way into a drift, so every test that needs one goes through it.
        /// </summary>
        private static DriftModel Drifting(float landingSteer = 1f)
        {
            var drift = new DriftModel();
            drift.TryArm(Tuning(), 12f);
            Hold(drift, 0.4f, landingSteer, grounded: false);
            Hold(drift, Step, landingSteer);
            return drift;
        }

        [Test]
        public void HoppingTooSlowDoesNotArmADrift()
        {
            var drift = new DriftModel();

            Assert.That(drift.TryArm(Tuning(), 2f), Is.False,
                "Below the minimum speed there is nothing to drift.");
            Assert.That(drift.IsArmed, Is.False);
            Assert.That(drift.IsDrifting, Is.False);
        }

        [Test]
        public void TheHopArmsTheDriftButDoesNotStartIt()
        {
            var drift = new DriftModel();

            Assert.That(drift.TryArm(Tuning(), 12f), Is.True);
            Assert.That(drift.IsArmed, Is.True);
            Assert.That(drift.IsDrifting, Is.False,
                "Nothing may slide, smoke or mark the road while Granny is in the air.");

            Hold(drift, 0.6f, 1f, grounded: false);

            Assert.That(drift.IsDrifting, Is.False,
                "Holding a full steering lock through the whole hop must still not start the "
                + "drift before the landing.");
            Assert.That(drift.Charge, Is.EqualTo(0f).Within(0.0001f));
        }

        [Test]
        public void TheDriftEngagesOnLandingInTheDirectionSteeredThen()
        {
            var drift = new DriftModel();
            drift.TryArm(Tuning(), 12f);

            // Steering right through the air, then landing steering left. The landing decides.
            Hold(drift, 0.4f, 1f, grounded: false);
            Hold(drift, Step, -0.8f);

            Assert.That(drift.IsDrifting, Is.True, "The landing must start the drift.");
            Assert.That(drift.IsArmed, Is.False);
            Assert.That(drift.Direction, Is.EqualTo(-1),
                "Direction is read from the stick at touchdown, not from the hop.");
        }

        /// <summary>
        /// The regression this guards is subtle and was the whole point of the change: the
        /// walker's ground probe reaches further than the first few centimetres of the hop, so
        /// contact reported immediately after take-off is the take-off, not a landing.
        /// </summary>
        [Test]
        public void GroundContactDuringTakeOffIsNotALanding()
        {
            var drift = new DriftModel();
            drift.TryArm(Tuning(), 12f);

            // One airborne step, then the probe finds the road again — the walker has barely
            // left it. That is not enough air time to count.
            Hold(drift, Step, 1f, grounded: false);
            Hold(drift, Step, 1f);

            Assert.That(drift.IsDrifting, Is.False,
                "A drift that engages one step after the hop starts is a drift that engages on "
                + "the hop.");
            Assert.That(drift.IsArmed, Is.True, "The hop is still live and can still land.");

            Hold(drift, 0.4f, 1f, grounded: false);
            Hold(drift, Step, 1f);

            Assert.That(drift.IsDrifting, Is.True, "A real landing after real air time engages.");
        }

        [Test]
        public void LandingStraightStillEngagesIfTheStickTurnsInWithinTheWindow()
        {
            var drift = new DriftModel();
            drift.TryArm(Tuning(), 12f);
            Hold(drift, 0.4f, 0f, grounded: false);

            Hold(drift, 0.3f, 0f);
            Assert.That(drift.IsDrifting, Is.False, "Landing straight does not drift on its own.");
            Assert.That(drift.IsArmed, Is.True, "The grace window is still open.");

            Hold(drift, Step, 1f);
            Assert.That(drift.IsDrifting, Is.True,
                "Turning in during the grace window must still be rewarded with the drift.");
        }

        [Test]
        public void LandingStraightAndStayingStraightGivesUpOnTheDrift()
        {
            var drift = new DriftModel();
            drift.TryArm(Tuning(), 12f);
            Hold(drift, 0.4f, 0f, grounded: false);

            Hold(drift, 0.8f, 0f);

            Assert.That(drift.IsArmed, Is.False, "The grace window must expire.");
            Assert.That(drift.IsDrifting, Is.False);

            Hold(drift, Step, 1f);
            Assert.That(drift.IsDrifting, Is.False,
                "Once the window has closed the hop is spent; steering must not resurrect it.");
        }

        [Test]
        public void ReleasingTheButtonInTheAirCancelsTheArmedHop()
        {
            var drift = new DriftModel();
            var tuning = Tuning();
            drift.TryArm(tuning, 12f);
            Hold(drift, 0.4f, 1f, grounded: false);

            drift.Tick(tuning, Step, false, false, 12f, 1f);

            Assert.That(drift.IsArmed, Is.False);
            Hold(drift, 0.2f, 1f);
            Assert.That(drift.IsDrifting, Is.False,
                "Letting go mid-hop must land as an ordinary jump.");
        }

        [Test]
        public void SteeringTheOtherWayMidDriftDoesNotFlipTheCommittedDirection()
        {
            var drift = Drifting(-0.8f);
            Assert.That(drift.Direction, Is.EqualTo(-1));

            Hold(drift, 0.4f, 1f);

            Assert.That(drift.Direction, Is.EqualTo(-1));
            Assert.That(drift.IsDrifting, Is.True);
        }

        [Test]
        public void TheDriftLineRampsInRatherThanSnapping()
        {
            var drift = Drifting();

            Assert.That(drift.EngageWeight, Is.LessThan(0.25f),
                "The frame the drift engages must still be steering mostly by the stick.");

            Hold(drift, 0.2f, 1f);

            Assert.That(drift.EngageWeight, Is.EqualTo(1f).Within(0.0001f),
                "Past the ramp the drift holds its own line completely.");
        }

        [Test]
        public void ChargeClimbsThroughRedYellowThenBlue()
        {
            var drift = Drifting();
            var tuning = Tuning();

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
            var inside = Drifting();
            var outside = Drifting();

            Hold(inside, 1f, 1f);
            Hold(outside, 1f, -1f);

            Assert.That(inside.Charge, Is.GreaterThan(outside.Charge),
                $"Inside charge {inside.Charge:0.00}s must beat outside {outside.Charge:0.00}s.");
            Assert.That(outside.IsDrifting, Is.True,
                "Counter-steering opens the drift out; it must not end it.");
        }

        [Test]
        public void AirborneTimeKeepsAnEngagedDriftButBanksNoCharge()
        {
            var drift = Drifting();
            var chargeOnLanding = drift.Charge;

            Hold(drift, 0.5f, 1f, grounded: false);

            Assert.That(drift.IsDrifting, Is.True, "A bump mid-drift must not cancel it.");
            Assert.That(drift.Charge, Is.EqualTo(chargeOnLanding).Within(0.0001f),
                "Charge is banked by slipper on tarmac, not by air time.");
        }

        [Test]
        public void ReleasingBeforeRedGivesNoBoost()
        {
            var drift = Drifting();
            var tuning = Tuning();
            Hold(drift, 0.2f, 1f);

            drift.Tick(tuning, Step, false, true, 12f, 1f);

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
            var drift = Drifting();
            var tuning = Tuning();
            Hold(drift, 0.6f, 1f);
            drift.Tick(tuning, Step, false, true, 12f, 1f);

            Assert.That(drift.ConsumeBoostImpulse(), Is.GreaterThan(0f));
            Assert.That(drift.ConsumeBoostImpulse(), Is.EqualTo(0f),
                "A one-shot impulse must not be applied on every physics step.");
            Assert.That(drift.BoostStage, Is.EqualTo(DriftChargeStage.Red));

            for (var i = 0; i < 60; i++)
            {
                drift.Tick(tuning, Step, false, true, 12f, 0f);
            }

            Assert.That(drift.BoostRemaining, Is.EqualTo(0f));
            Assert.That(drift.BoostStage, Is.EqualTo(DriftChargeStage.None));
        }

        [Test]
        public void DroppingBelowTheMinimumSpeedEndsTheDrift()
        {
            var drift = Drifting();
            var tuning = Tuning();
            Hold(drift, 0.6f, 1f);

            drift.Tick(tuning, Step, true, true, 1f, 1f);

            Assert.That(drift.IsDrifting, Is.False);
            Assert.That(drift.ConsumeBoostImpulse(), Is.GreaterThan(0f),
                "Charge already banked is still paid out when the drift dies naturally.");
        }

        [Test]
        public void ResetClearsArmedHopsDriftsAndBoosts()
        {
            var drift = Drifting();
            Hold(drift, 1f, 1f);

            drift.Reset();

            Assert.That(drift.IsArmed, Is.False);
            Assert.That(drift.IsDrifting, Is.False);
            Assert.That(drift.Charge, Is.EqualTo(0f));
            Assert.That(drift.EngageWeight, Is.EqualTo(0f));
            Assert.That(drift.BoostRemaining, Is.EqualTo(0f));
            Assert.That(drift.ConsumeBoostImpulse(), Is.EqualTo(0f));
        }

        private static float BoostFor(float heldSeconds)
        {
            var drift = Drifting();
            var tuning = Tuning();
            Hold(drift, heldSeconds, 1f);
            drift.Tick(tuning, Step, false, true, 12f, 1f);
            return drift.ConsumeBoostImpulse();
        }
    }
}
