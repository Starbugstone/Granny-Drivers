using GrannyRacer.Walker;
using NUnit.Framework;

namespace GrannyRacer.Tests.EditMode
{
    public sealed class SkidMarkPoolTests
    {
        [Test]
        public void OneSkidDrawsIntoOneRibbon()
        {
            var pool = new SkidMarkPool(4);

            var first = pool.Acquire(0f, out var started);
            Assert.That(started, Is.True, "The first frame of a skid takes a ribbon.");

            for (var i = 1; i < 20; i++)
            {
                Assert.That(pool.Acquire(i * 0.02f, out var again), Is.EqualTo(first),
                    "An unbroken skid must keep drawing into the same ribbon.");
                Assert.That(again, Is.False,
                    "Only the first frame reports a start; clearing on any other would erase "
                    + "the mark that is being drawn.");
            }
        }

        [Test]
        public void EachSkidGetsItsOwnRibbon()
        {
            var pool = new SkidMarkPool(4);

            var first = pool.Acquire(0f, out _);
            pool.Release(1f);
            var second = pool.Acquire(1.2f, out var started);

            Assert.That(started, Is.True);
            Assert.That(second, Is.Not.EqualTo(first),
                "Reusing the ribbon that is still fading would bridge the two skids with a "
                + "straight streak across the road.");
        }

        [Test]
        public void RibbonsAreRecycledOldestReleaseFirst()
        {
            var pool = new SkidMarkPool(3);
            var order = new int[3];
            for (var i = 0; i < 3; i++)
            {
                order[i] = pool.Acquire(i, out _);
                pool.Release(i + 0.5f);
            }

            Assert.That(pool.Acquire(10f, out _), Is.EqualTo(order[0]),
                "The fourth skid must recycle the ribbon released longest ago, which is the one "
                + "closest to having finished fading.");
        }

        [Test]
        public void ReleasingWithNothingActiveIsHarmless()
        {
            var pool = new SkidMarkPool(2);

            Assert.That(pool.Release(1f), Is.EqualTo(SkidMarkPool.NoRibbon));
            Assert.That(pool.Active, Is.EqualTo(SkidMarkPool.NoRibbon));

            var ribbon = pool.Acquire(1f, out _);
            Assert.That(pool.Release(2f), Is.EqualTo(ribbon));
            Assert.That(pool.Release(2f), Is.EqualTo(SkidMarkPool.NoRibbon),
                "A second release must not report a ribbon that is already fading.");
        }

        [Test]
        public void ARibbonIsRecycledOnceItsFadeHasElapsed()
        {
            var pool = new SkidMarkPool(2);
            var ribbon = pool.Acquire(0f, out _);
            pool.Release(1f);

            Assert.That(pool.TryRecycle(2f, 3f), Is.EqualTo(SkidMarkPool.NoRibbon),
                "A mark that is still fading must not be wiped out from under the player.");
            Assert.That(pool.TryRecycle(4.5f, 3f), Is.EqualTo(ribbon),
                "Once the fade is over the ribbon must be handed back to be emptied.");
            Assert.That(pool.TryRecycle(9f, 3f), Is.EqualTo(SkidMarkPool.NoRibbon),
                "Clearing the same ribbon on every later frame would be pure waste.");
        }

        [Test]
        public void TheRibbonBeingDrawnIntoIsNeverRecycled()
        {
            var pool = new SkidMarkPool(2);
            pool.Acquire(0f, out _);

            Assert.That(pool.TryRecycle(100f, 3f), Is.EqualTo(SkidMarkPool.NoRibbon),
                "Recycling the live ribbon would erase the skid as it is being painted.");
            Assert.That(pool.Active, Is.EqualTo(0));
        }

        [Test]
        public void ResetReturnsEveryRibbonToTheFreePool()
        {
            var pool = new SkidMarkPool(2);
            pool.Acquire(0f, out _);

            pool.Reset();

            Assert.That(pool.Active, Is.EqualTo(SkidMarkPool.NoRibbon));
            Assert.That(pool.Acquire(0f, out var started), Is.EqualTo(0));
            Assert.That(started, Is.True);
        }

        [Test]
        public void APoolAlwaysHasAtLeastOneRibbon()
        {
            Assert.That(new SkidMarkPool(0).Count, Is.EqualTo(1));
            Assert.That(new SkidMarkPool(-3).Count, Is.EqualTo(1));
        }
    }
}
