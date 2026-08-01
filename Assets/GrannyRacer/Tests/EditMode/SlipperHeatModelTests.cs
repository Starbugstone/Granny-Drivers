using GrannyRacer.Walker;
using NUnit.Framework;

namespace GrannyRacer.Tests.EditMode
{
    public sealed class SlipperHeatModelTests
    {
        [Test]
        public void BoostingAccumulatesDeterministicHeat()
        {
            var model = new SlipperHeatModel();
            model.Tick(2f, true, false, 0.25f, 0.5f, 0.2f, 3f, 0.2f, 0.2f);
            Assert.That(model.Heat, Is.EqualTo(0.5f).Within(0.0001f));
        }

        [Test]
        public void CoolingWaitsForDelayThenReducesHeat()
        {
            var model = new SlipperHeatModel();
            model.Tick(2f, true, false, 0.25f, 0.5f, 0.2f, 3f, 0.2f, 0.2f);
            model.Tick(0.5f, false, false, 0.25f, 0.5f, 0.2f, 3f, 0.2f, 0.2f);
            Assert.That(model.Heat, Is.EqualTo(0.5f).Within(0.0001f));
            model.Tick(1f, false, false, 0.25f, 0.5f, 0.2f, 3f, 0.2f, 0.2f);
            Assert.That(model.Heat, Is.EqualTo(0.3f).Within(0.0001f));
        }

        [Test]
        public void BurnoutKeepsHeatUntilReplacementCompletes()
        {
            var model = new SlipperHeatModel();
            model.Tick(4f, true, false, 0.25f, 0.5f, 0.2f, 3f, 0.2f, 0.2f);
            Assert.That(model.IsBurnedOut, Is.True);
            model.Tick(3f, false, false, 0.25f, 0.5f, 0.2f, 3f, 0.2f, 0.2f);
            Assert.That(model.IsBurnedOut, Is.False);
            Assert.That(model.Heat, Is.EqualTo(0.2f).Within(0.0001f));
        }

        [Test]
        public void ContextTapsShortenReplacement()
        {
            var model = new SlipperHeatModel();
            model.Tick(4f, true, false, 0.25f, 0.5f, 0.2f, 3f, 0.5f, 0.2f);
            model.Tick(0.1f, false, true, 0.25f, 0.5f, 0.2f, 3f, 0.5f, 0.2f);
            Assert.That(model.ReplacementRemaining, Is.EqualTo(2.4f).Within(0.0001f));
        }
    }
}
