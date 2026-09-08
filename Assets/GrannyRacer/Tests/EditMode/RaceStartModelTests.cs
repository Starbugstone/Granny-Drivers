using GrannyRacer.Racing;
using NUnit.Framework;

namespace GrannyRacer.Tests.EditMode
{
    public sealed class RaceStartModelTests
    {
        private const float PerfectEndsAt = 2f;
        private const float PerfectWindow = 0.75f;
        private const float Threshold = 0.5f;

        [TestCase(2.76f, RaceStartOutcome.Skid)]
        [TestCase(2.75f, RaceStartOutcome.Turbo)]
        [TestCase(2.01f, RaceStartOutcome.Turbo)]
        [TestCase(2f, RaceStartOutcome.Boost)]
        [TestCase(0.1f, RaceStartOutcome.Boost)]
        public void FirstThrottlePressSelectsTheExpectedStart(float remaining,
            RaceStartOutcome expected)
        {
            var start = new RaceStartModel();

            start.Tick(remaining, 1f, PerfectEndsAt, PerfectWindow, Threshold);

            Assert.That(start.Resolve(1f, Threshold), Is.EqualTo(expected));
        }

        [Test]
        public void ReleasingBeforeGoCancelsTheReward()
        {
            var start = new RaceStartModel();
            start.Tick(2.5f, 1f, PerfectEndsAt, PerfectWindow, Threshold);
            start.Tick(1.5f, 0f, PerfectEndsAt, PerfectWindow, Threshold);

            Assert.That(start.Resolve(0f, Threshold), Is.EqualTo(RaceStartOutcome.None));
        }

        [Test]
        public void AnEarlyRevCannotBeErasedByReleasingAndTryingAgain()
        {
            var start = new RaceStartModel();
            start.Tick(2.9f, 1f, PerfectEndsAt, PerfectWindow, Threshold);
            start.Tick(2.6f, 0f, PerfectEndsAt, PerfectWindow, Threshold);
            start.Tick(2.4f, 1f, PerfectEndsAt, PerfectWindow, Threshold);

            Assert.That(start.Resolve(1f, Threshold), Is.EqualTo(RaceStartOutcome.Skid));
        }

        [Test]
        public void NoThrottleAtGoMeansNoStartEffect()
        {
            var start = new RaceStartModel();

            Assert.That(start.Resolve(0f, Threshold), Is.EqualTo(RaceStartOutcome.None));
        }
    }
}
