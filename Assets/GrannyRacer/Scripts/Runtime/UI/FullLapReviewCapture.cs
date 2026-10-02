#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Collections;
using System.IO;
using GrannyRacer.Racing;
using GrannyRacer.Walker;
using Unity.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

namespace GrannyRacer.UI
{
    /// <summary>Opt-in capture of a physical lap using the normal gamepad input path.</summary>
    [DefaultExecutionOrder(900)]
    public sealed class FullLapReviewCapture : MonoBehaviour
    {
        private const int Fps = 30;
        private const int MaximumFrames = Fps * 190;
        private string output;
        private Gamepad pad;
        private RaceController race;
        private ArcadeWalkerController walker;
        private Vector3[] points;
        private CircuitReviewDriver driver;
        private Rigidbody body;
        private float maximumHeat, boostSeconds, coolingSeconds, speedIntegral, drivingSeconds;
        private int burnoutCount;
        private bool wasBurnedOut;
        private float previousHeat;
        private SkinnedMeshRenderer face;
        private UnityEngine.Camera portrait;
        private Transform head;
        private bool ready;
        private float maximumRoadDistance;
        private float maximumSpeed;
        private float lapTime;
        private float lapCompletedAt = -1;
        private int frameCount;
        private int previousGate = -1;
        private int gateCount;
        private readonly int[] gateSequence = new int[32];
        private readonly float[] gateTimes = new float[32];
        private readonly float[] maximumFaceWeights = new float[10];
        private string[] framePaths;
        private Texture2D readback;
        private Rect captureRect;
        private bool recordingAudio;
        private int audioChannels, audioSampleRate;
        private NativeArray<float> audioBuffer;
        private float[] audioSamples;
        private byte[] audioBytes;
        private FileStream audioFile;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void LaunchIfRequested()
        {
            if (Application.isEditor) return;
            var args = Environment.GetCommandLineArgs();
            for (var i = 0; i < args.Length - 1; i++)
                if (args[i] == "-granny-lap-review")
                {
                    var capture = new GameObject("Full lap review capture").AddComponent<FullLapReviewCapture>();
                    capture.output = args[i + 1];
                    return;
                }
        }

        private IEnumerator Start()
        {
            Directory.CreateDirectory(output);
            if (File.Exists(Path.Combine(output, "complete.json"))) throw new IOException("Use a fresh capture directory.");
            Application.runInBackground = true;
            // The virtual pad must keep working if the user switches away from this opt-in
            // recorder. The normal game retains the project's focus/input policy.
            InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
            Time.timeScale = 1; Time.captureFramerate = Fps;
            QualitySettings.vSyncCount = 0; Application.targetFrameRate = Fps;
            race = FindAnyObjectByType<RaceController>();
            walker = FindAnyObjectByType<ArcadeWalkerController>();
            var road = GameObject.Find("Generated Road").GetComponent<MeshFilter>();
            var vertices = road.sharedMesh.vertices;
            points = new Vector3[vertices.Length / 2];
            for (var i = 0; i < points.Length; i++) points[i] = road.transform.TransformPoint((vertices[i * 2] + vertices[i * 2 + 1]) * .5f);
            foreach (var renderer in walker.GetComponentsInChildren<SkinnedMeshRenderer>())
                if (renderer.sharedMesh.blendShapeCount == 10) face = renderer;
            if (face == null) throw new InvalidOperationException("Expected the new granny's ten-control face mesh.");
            foreach (var bone in face.bones) if (bone != null && bone.name == "Head") head = bone;
            if (head == null) throw new InvalidOperationException("Face has no Head bone.");
            driver = new CircuitReviewDriver(points);
            body = walker.GetComponent<Rigidbody>();
            pad = InputSystem.AddDevice<Gamepad>("Lap Review Gamepad");
            portrait = new GameObject("Live granny portrait camera").AddComponent<UnityEngine.Camera>();
            portrait.CopyFrom(UnityEngine.Camera.main);
            portrait.depth = UnityEngine.Camera.main.depth + 1;
            portrait.rect = new Rect(.77f, .04f, .21f, .34f);
            portrait.fieldOfView = 34;
            portrait.nearClipPlane = .03f;
            framePaths = new string[MaximumFrames];
            for (var i = 0; i < framePaths.Length; i++) framePaths[i] = Path.Combine(output, i.ToString("D5") + ".jpg");
            readback = new Texture2D(Screen.width, Screen.height, TextureFormat.RGB24, false);
            captureRect = new Rect(0, 0, Screen.width, Screen.height);
            audioChannels = AudioSettings.speakerMode switch
            {
                AudioSpeakerMode.Mono => 1,
                AudioSpeakerMode.Quad => 4,
                AudioSpeakerMode.Surround => 5,
                AudioSpeakerMode.Mode5point1 => 6,
                AudioSpeakerMode.Mode7point1 => 8,
                _ => 2
            };
            audioSampleRate = AudioSettings.outputSampleRate;
            recordingAudio = AudioRenderer.Start();
            if (!recordingAudio) throw new InvalidOperationException("Could not start Unity audio capture.");
            audioFile = new FileStream(Path.Combine(output, "audio.f32"), FileMode.CreateNew, FileAccess.Write);
            ready = true;
            var endOfFrame = new WaitForEndOfFrame();
            while (frameCount < MaximumFrames)
            {
                yield return endOfFrame;
                CaptureAudioFrame();
                readback.ReadPixels(captureRect, 0, 0, false);
                File.WriteAllBytes(framePaths[frameCount], readback.EncodeToJPG(92));
                frameCount++;
                if (race.CurrentLap >= 2 && lapCompletedAt < 0) { lapCompletedAt = Time.time; lapTime = race.ElapsedTime; }
                if (lapCompletedAt >= 0 && Time.time - lapCompletedAt >= 2) break;
                if (walker.transform.position.y < -.5f || maximumRoadDistance > 7) break;
                if (race.ElapsedTime > 5 && maximumSpeed < .5f) break;
            }
            ready = false;
            audioFile.Dispose(); audioFile = null;
            AudioRenderer.Stop(); recordingAudio = false;
            InputSystem.QueueStateEvent(pad, new GamepadState());
            var report = new CaptureReport
            {
                completedLap = race.CurrentLap >= 2, frames = frameCount, fps = Fps,
                width = Screen.width, height = Screen.height, lapSeconds = lapTime,
                maximumSpeedMetresPerSecond = maximumSpeed, maximumRoadDistanceMetres = maximumRoadDistance,
                averageSpeedMetresPerSecond = speedIntegral / Mathf.Max(.001f, drivingSeconds),
                maximumHeat = maximumHeat, boostSeconds = boostSeconds, coolingSeconds = coolingSeconds, burnoutCount = burnoutCount,
                scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene().path,
                gateSequence = new int[gateCount], gateTimes = new float[gateCount],
                maximumFaceWeights = maximumFaceWeights, shapeNames = new string[face.sharedMesh.blendShapeCount],
                automatedGamepadInput = true, audioRecorded = true,
                audioChannels = audioChannels, audioSampleRate = audioSampleRate
            };
            Array.Copy(gateSequence, report.gateSequence, gateCount); Array.Copy(gateTimes, report.gateTimes, gateCount);
            for (var i = 0; i < report.shapeNames.Length; i++) report.shapeNames[i] = face.sharedMesh.GetBlendShapeName(i);
            File.WriteAllText(Path.Combine(output, report.completedLap ? "complete.json" : "failed.json"), JsonUtility.ToJson(report, true));
            Debug.Log("[LAP REVIEW] " + JsonUtility.ToJson(report));
            Time.captureFramerate = 0;
            Application.Quit(report.completedLap ? 0 : 2);
        }

        private void Update()
        {
            if (!ready) return;
            if (previousGate != race.NextCheckpoint && gateCount < gateSequence.Length)
            {
                previousGate = race.NextCheckpoint; gateSequence[gateCount] = previousGate;
                gateTimes[gateCount++] = race.ElapsedTime;
            }
            if (race.State != RaceState.Racing) return;
            var state = driver.Sample(walker, body.linearVelocity);
            maximumRoadDistance = Mathf.Max(maximumRoadDistance, driver.RoadDistance);
            maximumSpeed = Mathf.Max(maximumSpeed, walker.Speed);
            maximumHeat = Mathf.Max(maximumHeat, walker.Heat);
            if (lapCompletedAt < 0)
            {
                speedIntegral += walker.Speed * Time.deltaTime; drivingSeconds += Time.deltaTime;
                if (walker.IsBoosting) boostSeconds += Time.deltaTime;
                if (walker.Heat < previousHeat && walker.HeatStage != SlipperHeatStage.BurnedOut) coolingSeconds += Time.deltaTime;
                var burnedOut = walker.HeatStage == SlipperHeatStage.BurnedOut;
                if (burnedOut && !wasBurnedOut) burnoutCount++;
                wasBurnedOut = burnedOut;
            }
            previousHeat = walker.Heat;
            InputSystem.QueueStateEvent(pad, state);
        }

        private void LateUpdate()
        {
            if (!ready) return;
            var target = head.position;
            portrait.transform.position = target + walker.transform.forward * 1.1f + walker.transform.right * .35f + Vector3.up * .06f;
            portrait.transform.LookAt(target + Vector3.up * .02f);
            for (var i = 0; i < maximumFaceWeights.Length; i++)
                maximumFaceWeights[i] = Mathf.Max(maximumFaceWeights[i], face.GetBlendShapeWeight(i));
        }

        private void OnDestroy()
        {
            if (recordingAudio) AudioRenderer.Stop();
            audioFile?.Dispose();
            if (audioBuffer.IsCreated) audioBuffer.Dispose();
            if (pad != null && pad.added) InputSystem.RemoveDevice(pad);
            if (readback != null) Destroy(readback);
            Time.captureFramerate = 0;
        }

        private void CaptureAudioFrame()
        {
            var count = AudioRenderer.GetSampleCountForCaptureFrame() * audioChannels;
            if (!audioBuffer.IsCreated || audioBuffer.Length != count)
            {
                if (audioBuffer.IsCreated) audioBuffer.Dispose();
                audioBuffer = new NativeArray<float>(count, Allocator.Persistent);
                audioSamples = new float[count]; audioBytes = new byte[count * sizeof(float)];
            }
            if (!AudioRenderer.Render(audioBuffer)) throw new IOException("Unity audio frame capture failed.");
            audioBuffer.CopyTo(audioSamples);
            Buffer.BlockCopy(audioSamples, 0, audioBytes, 0, audioBytes.Length);
            audioFile.Write(audioBytes, 0, audioBytes.Length);
        }

        [Serializable] private sealed class CaptureReport
        {
            public bool completedLap, automatedGamepadInput, audioRecorded;
            public int frames, fps, width, height;
            public int audioChannels, audioSampleRate;
            public float lapSeconds, maximumSpeedMetresPerSecond, maximumRoadDistanceMetres;
            public float averageSpeedMetresPerSecond, maximumHeat, boostSeconds, coolingSeconds;
            public int burnoutCount;
            public string scene;
            public int[] gateSequence;
            public float[] gateTimes, maximumFaceWeights;
            public string[] shapeNames;
        }
    }
}
#endif
