using System.Collections;
using GrannyRacer.Racing;
using GrannyRacer.Walker;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

namespace GrannyRacer.Tests.PlayMode
{
    public sealed class NeighborhoodPresentationTests
    {
        private Gamepad pad;
        [UnitySetUp] public IEnumerator Load()
        { Time.timeScale=1;yield return SceneManager.LoadSceneAsync("Circuit_SchoolRun");yield return null; }
        [UnityTearDown] public IEnumerator Cleanup()
        { Time.timeScale=1;if(pad!=null)InputSystem.RemoveDevice(pad);pad=null;yield return null; }
        [UnityTest] public IEnumerator ControllerCanOpenChooseAndLoadATrackWithoutAMouse()
        {
            var race=Object.FindAnyObjectByType<RaceController>();race.TogglePause();
            pad=InputSystem.AddDevice<Gamepad>();
            InputSystem.QueueStateEvent(pad,new GamepadState().WithButton(GamepadButton.West));yield return null;yield return null;
            Assert.That(race.ExternalMenuOpen,Is.True);
            InputSystem.QueueStateEvent(pad,new GamepadState().WithButton(GamepadButton.DpadDown));yield return null;yield return null;
            InputSystem.QueueStateEvent(pad,new GamepadState().WithButton(GamepadButton.South));yield return null;yield return null;
            InputSystem.QueueStateEvent(pad,new GamepadState());
            var deadline=Time.realtimeSinceStartup+15;
            while(SceneManager.GetActiveScene().name!="Circuit_MapleGardens" && Time.realtimeSinceStartup<deadline)yield return null;
            Assert.That(SceneManager.GetActiveScene().name,Is.EqualTo("Circuit_MapleGardens"));
            Assert.That(Time.timeScale,Is.EqualTo(1));
        }
        [UnityTest] public IEnumerator BakedBlenderPropsAnimateAndPauseWithTheRace()
        {
            Animation seesaw=null,spinner=null;
            foreach(var animation in Object.FindObjectsByType<Animation>())
            { if(animation.name.Contains("Seesaw"))seesaw=animation;if(animation.name.Contains("WindSpinner"))spinner=animation; }
            Assert.That(seesaw,Is.Not.Null);Assert.That(spinner,Is.Not.Null);
            Assert.That(seesaw.isPlaying,Is.True);Assert.That(spinner.isPlaying,Is.True);
            Transform beam=null,rotor=null;
            foreach(var t in seesaw.GetComponentsInChildren<Transform>())if(t.name.Contains("Beam"))beam=t;
            foreach(var t in spinner.GetComponentsInChildren<Transform>())if(t.name.Contains("Rotor"))rotor=t;
            Assert.That(beam,Is.Not.Null);Assert.That(rotor,Is.Not.Null);
            var initialBeam=beam.localRotation;var initialRotor=rotor.localRotation;
            yield return new WaitForSeconds(.45f);
            Assert.That(Quaternion.Angle(initialBeam,beam.localRotation),Is.GreaterThan(2));
            Assert.That(Quaternion.Angle(initialRotor,rotor.localRotation),Is.GreaterThan(15));
            var race=Object.FindAnyObjectByType<RaceController>();race.TogglePause();
            var pausedRotation=rotor.localRotation;
            yield return new WaitForSecondsRealtime(.2f);
            Assert.That(Quaternion.Angle(pausedRotation,rotor.localRotation),Is.LessThan(.1f));
            race.TogglePause();
        }
        [UnityTest] public IEnumerator RaceAudioDistinguishesLaunchGateLapFinishAndPausesLoops()
        {
            var race=Object.FindAnyObjectByType<RaceController>();
            var walker=Object.FindAnyObjectByType<ArcadeWalkerController>();
            var audio=walker.GetComponent<ArcadeAudioPresentation>();
            Assert.That(audio,Is.Not.Null);
            while(race.State==RaceState.Countdown)yield return null;
            yield return null;Assert.That(audio.LastCue,Is.EqualTo(ArcadeAudioPresentation.Cue.Go));
            var count=audio.PlayedCueCount;race.TogglePause();yield return null;
            foreach(var source in audio.GetComponentsInChildren<AudioSource>())if(source.loop)Assert.That(source.isPlaying,Is.False);
            yield return new WaitForSecondsRealtime(.1f);Assert.That(audio.PlayedCueCount,Is.EqualTo(count));
            race.TogglePause();yield return null;
            var gates=new RaceCheckpoint[race.CheckpointCount];
            foreach(var gate in Object.FindObjectsByType<RaceCheckpoint>())gates[gate.Index]=gate;
            for(var lap=1;lap<=race.LapTarget;lap++)
            {
                for(var i=1;i<gates.Length;i++)
                {
                    Assert.That(race.TryPassCheckpoint(i,gates[i].transform,walker),Is.True);
                    yield return null;Assert.That(audio.LastCue,Is.EqualTo(ArcadeAudioPresentation.Cue.Gate));
                }
                Assert.That(race.TryPassCheckpoint(0,gates[0].transform,walker),Is.True);yield return null;
                Assert.That(audio.LastCue,Is.EqualTo(lap==race.LapTarget?ArcadeAudioPresentation.Cue.Finish:
                    lap==race.LapTarget-1?ArcadeAudioPresentation.Cue.FinalLap:ArcadeAudioPresentation.Cue.Lap));
            }
        }
        [UnityTest] public IEnumerator TourLoadsSelectedTrackFromPauseAndRejectsInvalidDestinations()
        {
            var race=Object.FindAnyObjectByType<RaceController>();race.TogglePause();
            var tour=Object.FindAnyObjectByType<CircuitTour>();
            Assert.That(tour.Destinations.Length,Is.EqualTo(3));
            Assert.That(tour.Travel(-1),Is.False);Assert.That(tour.Travel(3),Is.False);
            Assert.That(tour.Travel(1),Is.True);Assert.That(tour.Travel(0),Is.False);
            var deadline=Time.realtimeSinceStartup+15;
            while(SceneManager.GetActiveScene().name!="Circuit_MapleGardens" && Time.realtimeSinceStartup<deadline)yield return null;
            Assert.That(SceneManager.GetActiveScene().name,Is.EqualTo("Circuit_MapleGardens"));
            Assert.That(Time.timeScale,Is.EqualTo(1));
            yield return null;
            Assert.That(Object.FindAnyObjectByType<RaceController>().State,Is.EqualTo(RaceState.Countdown));
        }
    }
}
