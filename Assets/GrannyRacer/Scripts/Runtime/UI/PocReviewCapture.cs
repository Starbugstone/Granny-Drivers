#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Collections;
using System.IO;
using GrannyRacer.Camera;
using GrannyRacer.Racing;
using GrannyRacer.Walker;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

namespace GrannyRacer.UI
{
    // Opt-in development capture. Runs in the actual player, including the HUD and URP.
    public sealed class PocReviewCapture : MonoBehaviour
    {
        private string output;
        private Keyboard keyboard;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void LaunchIfRequested()
        {
            if (Application.isEditor) return;
            var args = Environment.GetCommandLineArgs();
            for (var i = 0; i < args.Length - 1; i++)
                if (args[i] == "-poc-review")
                {
                    var capture = new GameObject("POC review capture").AddComponent<PocReviewCapture>();
                    DontDestroyOnLoad(capture.gameObject);
                    capture.output = args[i + 1];
                    return;
                }
        }

        private IEnumerator Start()
        {
            Directory.CreateDirectory(output);
            Application.runInBackground = true;
            keyboard = InputSystem.AddDevice<Keyboard>("POC Review Keyboard");
            var race = FindAnyObjectByType<RaceController>();
            var walker = FindAnyObjectByType<ArcadeWalkerController>();
            yield return new WaitForSeconds(.6f);
            yield return Capture("01_countdown");
            while (race.State == RaceState.Countdown) yield return null;
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.W));
            yield return new WaitForSeconds(.7f);
            yield return Capture("02_driving");
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.W, Key.Space));
            yield return new WaitForSeconds(.35f);
            yield return Capture("03_boost");
            InputSystem.QueueStateEvent(keyboard, new KeyboardState());
            race.TogglePause();
            yield return null;
            yield return Capture("04_pause");
            var hud=FindAnyObjectByType<DrivingDebugHud>();
            if(FindAnyObjectByType<CircuitTour>()!=null)
            {
                hud.ToggleTrackSelection();yield return Capture("04b_track_selection");hud.ToggleTrackSelection();
            }
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.F1));
            yield return null;
            yield return null;
            InputSystem.QueueStateEvent(keyboard, new KeyboardState());
            yield return Capture("05_handling");
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.F1));
            yield return null;
            yield return null;
            InputSystem.QueueStateEvent(keyboard, new KeyboardState());
            race.TogglePause();
            var camera = UnityEngine.Camera.main;
            camera.GetComponent<WalkerFollowCamera>().enabled = false;
            camera.transform.position = walker.transform.TransformPoint(new Vector3(2.1f, 1.8f, 3.3f));
            camera.transform.LookAt(walker.transform.position + Vector3.up * .6f);
            yield return Capture("06_hero_in_unity");
            Debug.Log($"[POC REVIEW] Captured player at {Screen.width}x{Screen.height}; state={race.State}; speed={walker.Speed:0.0}; grounded={walker.IsGrounded}.");
            for(var destination=1;destination<3;destination++)
            {
                var tour=FindAnyObjectByType<CircuitTour>();
                if(tour==null)break;
                var previousScene=UnityEngine.SceneManagement.SceneManager.GetActiveScene().path;
                if(!tour.Travel(destination))break;
                while(UnityEngine.SceneManagement.SceneManager.GetActiveScene().path==previousScene)yield return null;
                yield return null;
                race=FindAnyObjectByType<RaceController>();walker=FindAnyObjectByType<ArcadeWalkerController>();
                yield return new WaitForSeconds(.5f);yield return Capture("tour_"+destination+"_countdown");
                while(race.State==RaceState.Countdown)yield return null;
                InputSystem.QueueStateEvent(keyboard,new KeyboardState(Key.W));yield return new WaitForSeconds(1.2f);
                yield return Capture("tour_"+destination+"_driving");
                InputSystem.QueueStateEvent(keyboard,new KeyboardState());
                Debug.Log($"[POC REVIEW] Tour {destination}: {race.CircuitName}; speed={walker.Speed:0.0}; grounded={walker.IsGrounded}.");
            }
            yield return new WaitForSecondsRealtime(.5f);
            InputSystem.RemoveDevice(keyboard);
            keyboard = null;
            Application.Quit();
        }

        private IEnumerator Capture(string name)
        {
            yield return new WaitForEndOfFrame();
            ScreenCapture.CaptureScreenshot(Path.Combine(output, name + ".png"));
            yield return new WaitForSecondsRealtime(.2f);
        }

        private void OnDestroy()
        {
            if (keyboard != null && keyboard.added) InputSystem.RemoveDevice(keyboard);
        }
    }
}
#endif
