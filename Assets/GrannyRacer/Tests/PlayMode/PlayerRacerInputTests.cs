using System.Collections;
using GrannyRacer.Input;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.TestTools;

namespace GrannyRacer.Tests.PlayMode
{
    public sealed class PlayerRacerInputTests : InputTestFixture
    {
        private Keyboard keyboard;
        private PlayerRacerInput input;

        public override void Setup()
        {
            base.Setup();
            keyboard = InputSystem.AddDevice<Keyboard>();
            input = new GameObject("Input Probe").AddComponent<PlayerRacerInput>();
        }

        public override void TearDown()
        {
            if (input != null) Object.DestroyImmediate(input.gameObject);
            base.TearDown();
        }

        [UnityTest]
        public IEnumerator HoldingWProducesThrottle()
        {
            yield return null;
            Press(keyboard.wKey);
            yield return null;

            var state = input.Sample();
            Assert.That(state.Throttle, Is.EqualTo(1f).Within(0.01f),
                $"W must drive throttle. Read {state.Throttle}.");
        }

        [UnityTest]
        public IEnumerator HoldingUpArrowProducesThrottle()
        {
            yield return null;
            Press(keyboard.upArrowKey);
            yield return null;

            var state = input.Sample();
            Assert.That(state.Throttle, Is.EqualTo(1f).Within(0.01f),
                $"Up arrow must drive throttle. Read {state.Throttle}.");
        }

        [UnityTest]
        public IEnumerator HoldingSProducesBrake()
        {
            yield return null;
            Press(keyboard.sKey);
            yield return null;

            var state = input.Sample();
            Assert.That(state.Brake, Is.EqualTo(1f).Within(0.01f),
                $"S must drive brake. Read {state.Brake}.");
        }

        [UnityTest]
        public IEnumerator HoldingDProducesPositiveSteer()
        {
            yield return null;
            Press(keyboard.dKey);
            yield return null;

            var state = input.Sample();
            Assert.That(state.Steer, Is.EqualTo(1f).Within(0.01f),
                $"D must steer right. Read {state.Steer}.");
        }

        [UnityTest]
        public IEnumerator ThrottleAndSteerReadTogether()
        {
            yield return null;
            Press(keyboard.wKey);
            Press(keyboard.dKey);
            yield return null;

            var state = input.Sample();
            Assert.That(state.Steer, Is.EqualTo(1f).Within(0.01f), $"Steer read {state.Steer}.");
            Assert.That(state.Throttle, Is.EqualTo(1f).Within(0.01f), $"Throttle read {state.Throttle}.");
        }
    }
}
