using GrannyRacer.Racing;
using NUnit.Framework;

namespace GrannyRacer.Tests.EditMode
{
    public sealed class RaceProgressTests
    {
        [Test]
        public void CheckpointsMustBeCrossedInOrder()
        {
            var progress = new RaceProgress(4, 3);
            Assert.That(progress.TryPassCheckpoint(2), Is.False);
            Assert.That(progress.TryPassCheckpoint(1), Is.True);
            Assert.That(progress.ExpectedCheckpoint, Is.EqualTo(2));
        }

        [Test]
        public void LapCompletesOnlyAfterFullOrderedLoop()
        {
            var progress = new RaceProgress(4, 3);
            progress.TryPassCheckpoint(1);
            progress.TryPassCheckpoint(2);
            progress.TryPassCheckpoint(3);
            Assert.That(progress.CompletedLaps, Is.EqualTo(0));
            progress.TryPassCheckpoint(0);
            Assert.That(progress.CompletedLaps, Is.EqualTo(1));
        }

        [Test]
        public void RaceFinishesAtConfiguredLapTarget()
        {
            var progress = new RaceProgress(2, 3);
            for (var lap = 0; lap < 3; lap++)
            {
                progress.TryPassCheckpoint(1);
                progress.TryPassCheckpoint(0);
            }

            Assert.That(progress.IsFinished, Is.True);
            Assert.That(progress.TryPassCheckpoint(1), Is.False);
        }

        [Test]
        public void PositionMathSupportsMultipleRacers()
        {
            var leader = new RaceProgress(4, 3);
            var trailer = new RaceProgress(4, 3);
            leader.TryPassCheckpoint(1);
            leader.TryPassCheckpoint(2);
            trailer.TryPassCheckpoint(1);

            var standings = new[] { trailer, leader };
            Assert.That(RacePositionMath.GetPosition(0, standings), Is.EqualTo(2));
            Assert.That(RacePositionMath.GetPosition(1, standings), Is.EqualTo(1));
        }
    }
}
