using GrannyRacer.Walker;
using NUnit.Framework;

namespace GrannyRacer.Tests.EditMode
{
    public sealed class GrannyRacerStatsTests
    {
        private static readonly GrannyRacerStats BaseStats = new GrannyRacerStats(
            24f, 12f, 38f, 15f, 5f, 22f, 7f);

        [Test]
        public void BalancedProfilePreservesBaseHandling()
        {
            var result = GrannyRacerStats.Resolve(BaseStats, GrannyStatMultipliers.Balanced);

            Assert.That(result.Acceleration, Is.EqualTo(24f));
            Assert.That(result.MaximumSpeed, Is.EqualTo(15f));
            Assert.That(result.LateralGrip, Is.EqualTo(7f));
        }

        [Test]
        public void ProfileChangesEveryRelevantPartOfTheDrivingLoop()
        {
            var result = GrannyRacerStats.Resolve(BaseStats,
                new GrannyStatMultipliers(1.25f, 0.8f, 1.1f));

            Assert.That(result.Acceleration, Is.EqualTo(30f));
            Assert.That(result.ReverseAcceleration, Is.EqualTo(15f));
            Assert.That(result.BoostAcceleration, Is.EqualTo(47.5f));
            Assert.That(result.MaximumSpeed, Is.EqualTo(16.5f));
            Assert.That(result.MaximumReverseSpeed, Is.EqualTo(5.5f));
            Assert.That(result.BoostMaximumSpeed, Is.EqualTo(24.2f).Within(0.0001f));
            Assert.That(result.LateralGrip, Is.EqualTo(5.6f).Within(0.0001f));
        }

        [Test]
        public void NegativeRuntimeMultipliersCannotInvertPhysics()
        {
            var result = GrannyRacerStats.Resolve(BaseStats,
                new GrannyStatMultipliers(-1f, -1f, -1f));

            Assert.That(result.Acceleration, Is.Zero);
            Assert.That(result.MaximumSpeed, Is.Zero);
            Assert.That(result.LateralGrip, Is.Zero);
        }
    }
}
