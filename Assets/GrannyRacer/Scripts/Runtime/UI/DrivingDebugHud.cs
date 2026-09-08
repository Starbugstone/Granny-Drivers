using GrannyRacer.Walker;
using GrannyRacer.Racing;
using UnityEngine;
using UnityEngine.InputSystem;

namespace GrannyRacer.UI
{
    // A lightweight POC HUD. All text formatting and immediate GUI work stays in OnGUI.
    public sealed class DrivingDebugHud : MonoBehaviour
    {
        [SerializeField] private ArcadeWalkerController walker;
        [SerializeField] private RaceController race;
        private GUIStyle heading;
        private GUIStyle body;
        private GUIStyle small;
        private GUIStyle huge;
        private GUIStyle button;
        private bool showTuning;
        private bool showControls;
        private readonly Color ink = new Color(0.055f, 0.105f, 0.13f);
        private readonly Color paper = new Color(0.97f, 0.92f, 0.79f);
        private readonly Color teal = new Color(0.10f, 0.65f, 0.55f);

        public void Configure(ArcadeWalkerController controller, RaceController raceController)
        {
            walker = controller;
            race = raceController;
        }

        private void Update()
        {
            if (Keyboard.current != null && Keyboard.current.f1Key.wasPressedThisFrame)
                showTuning = !showTuning;
        }

        private void EnsureStyles()
        {
            if (heading != null) return;
            heading = new GUIStyle(GUI.skin.label) { fontSize = 25, fontStyle = FontStyle.Bold };
            body = new GUIStyle(GUI.skin.label) { fontSize = 18, wordWrap = true };
            small = new GUIStyle(body) { fontSize = 14 };
            huge = new GUIStyle(heading) { fontSize = 54, alignment = TextAnchor.MiddleCenter };
            button = new GUIStyle(GUI.skin.button) { fontSize = 18, fontStyle = FontStyle.Bold };
            heading.normal.textColor = paper;
            body.normal.textColor = paper;
            small.normal.textColor = paper;
            huge.normal.textColor = paper;
        }

        private void OnGUI()
        {
            if (walker == null || race == null) return;
            EnsureStyles();
            var oldMatrix = GUI.matrix;
            var scale = Mathf.Min(Screen.width / 1280f, Screen.height / 720f);
            GUI.matrix = Matrix4x4.TRS(new Vector3((Screen.width - 1280f * scale) * 0.5f,
                (Screen.height - 720f * scale) * 0.5f, 0f), Quaternion.identity, Vector3.one * scale);
            Panel(new Rect(24, 24, 310, 84));
            GUI.Label(new Rect(42, 32, 285, 32), "GRANNY RACER", heading);
            GUI.Label(new Rect(42, 67, 285, 28), "QUIET SUNDAY / ROCKET CLUB", small);
            Panel(new Rect(962, 24, 294, 84));
            GUI.Label(new Rect(980, 32, 255, 32), $"LAP {race.CurrentLap} / {race.LapTarget}", heading);
            GUI.Label(new Rect(980, 68, 180, 28), $"{race.ElapsedTime:0.0}s   •   SOLO RUN", small);
            if (GUI.Button(new Rect(1167, 67, 74, 27), "Pause")) race.TogglePause();
            DrawTelemetry();
            if (race.IsWrongWay) Banner("WRONG WAY — TURN AROUND", new Color(.78f,.20f,.13f), 139);
            if (race.State == RaceState.Countdown) DrawCountdown();
            else if (race.State == RaceState.Racing && race.StartFeedbackRemaining > 0f)
            {
                var text = race.StartOutcome == RaceStartOutcome.Turbo ? "PERFECT TURBO!"
                    : race.StartOutcome == RaceStartOutcome.Boost ? "BOOST START!"
                    : race.StartOutcome == RaceStartOutcome.Skid ? "TOO EARLY! WHEELSPIN" : "GO!";
                Banner(text, teal, 174);
            }
            else if (race.State == RaceState.Paused || race.State == RaceState.Finished) DrawMenu();
            if (showTuning && race.State != RaceState.Finished) DrawTuning();
            GUI.matrix = oldMatrix;
        }

        private void DrawTelemetry()
        {
            Panel(new Rect(24, 549, 366, 147));
            GUI.Label(new Rect(42, 556, 155, 36), $"{walker.Speed * 3.6f:0} km/h", heading);
            GUI.Label(new Rect(213, 566, 165, 24), walker.IsGrounded ? "SLIPPERS DOWN" : "FEET UP!", small);
            var burned = walker.HeatStage == SlipperHeatStage.BurnedOut;
            GUI.Label(new Rect(42, 597, 326, 28), burned
                ? $"NEW SLIPPERS IN {walker.ReplacementRemaining:0.0}s — tap boost"
                : $"SLIPPER HEAT   {walker.Heat * 100f:0}%", small);
            Fill(new Rect(42, 630, 326, 14), new Color(.22f,.26f,.26f));
            Fill(new Rect(42, 630, 326 * walker.Heat, 14), burned ? Color.red : Color.Lerp(teal,new Color(1,.35f,.12f),walker.Heat));
            var driftText = walker.IsDrifting ? $"DRIFT {walker.DriftStage} / HOLD TO CHARGE"
                : walker.DriftBoostActive ? $"{walker.DriftBoostStage} DRIFT BOOST!"
                : walker.IsDriftArmed ? "HOP — STEER INTO THE LANDING" : "HOP + HOLD TO DRIFT";
            GUI.Label(new Rect(42, 656, 326, 28), driftText, small);
            Panel(new Rect(411, 647, 845, 49));
            GUI.Label(new Rect(430, 660, 810, 30), "WASD drive  •  Space boost  •  Shift hop/drift  •  R recover  •  Esc pause  •  F1 tune", small);
        }

        private void DrawCountdown()
        {
            Panel(new Rect(463, 129, 354, 204));
            GUI.Label(new Rect(480, 142, 320, 36), "THE TEA CAN WAIT", heading);
            GUI.Label(new Rect(480, 175, 320, 90), Mathf.CeilToInt(race.CountdownRemaining).ToString(), huge);
            var active = race.CountdownRemaining > 2f ? 0 : 1;
            for (var i = 0; i < 3; i++)
                Fill(new Rect(537 + i * 76, 276, 54, 18), i == active
                    ? (i == 0 ? new Color(.96f,.28f,.15f) : new Color(1,.73f,.17f)) : new Color(.24f,.27f,.27f));
            GUI.Label(new Rect(491, 303, 302, 24), "Time your first rev. Hold through GO.", small);
        }

        private void DrawMenu()
        {
            var finished = race.State == RaceState.Finished;
            Panel(new Rect(389, 126, 502, 494));
            GUI.Label(new Rect(419, 148, 444, 40), finished ? "BACK BEFORE BINGO!" : "TEA BREAK", heading);
            GUI.Label(new Rect(419, 191, 444, 36), finished
                ? $"Three laps in {race.FinishTime:0.0} seconds." : "Paused. Your slippers can catch their breath.", body);
            if (!finished && GUI.Button(new Rect(419, 239, 442, 42), "Resume  /  Esc or controller Menu", button)) race.TogglePause();
            if (GUI.Button(new Rect(419, 291, 442, 42), "Race again", button)) race.RestartScene();
            if (GUI.Button(new Rect(419, 343, 216, 36), "Controls", button)) showControls = !showControls;
            if (GUI.Button(new Rect(645, 343, 216, 36), "Handling lab", button)) showTuning = !showTuning;
            if (showControls)
                GUI.Label(new Rect(419, 390, 442, 153), "Keyboard: WASD / arrows drive, Space boost, Shift hop and hold to drift, R recover.\nController: triggers drive/brake, stick steer, A boost, RB hop/drift, Y recover, Menu pause.\nCool slippers between boosts. Tap boost during burnout to replace them faster.", small);
            else
            {
                GUI.Label(new Rect(419, 402, 190, 28), "Master volume", body);
                AudioListener.volume = GUI.HorizontalSlider(new Rect(614, 413, 237, 20), AudioListener.volume, 0f, 1f);
                GUI.Label(new Rect(419, 454, 442, 70), "One granny. Three laps. Questionable engineering.\nCool your slippers between boosts and keep an eye on the heat.", small);
            }
            if (GUI.Button(new Rect(419, 556, 442, 36), "Quit to desktop", button)) Application.Quit();
        }

        private void DrawTuning()
        {
            walker.BeginSessionTuning();
            var settings = walker.Handling;
            Panel(new Rect(899, 127, 357, 490));
            GUI.Label(new Rect(917, 141, 300, 35), "HANDLING LAB", heading);
            GUI.Label(new Rect(917, 179, 320, 45), "Changes last for this run. Original tuning stays safe.", small);
            GUI.changed = false;
            settings.acceleration = Slider("Acceleration", settings.acceleration, 10, 40, 233);
            settings.maximumSpeed = Slider("Top speed (m/s)", settings.maximumSpeed, 8, 22, 291);
            settings.lateralGrip = Slider("Grip", settings.lateralGrip, 2, 12, 349);
            settings.steeringDegreesPerSecond = Slider("Steering", settings.steeringDegreesPerSecond, 60, 180, 407);
            if (GUI.changed) walker.RefreshHandling();
            GUI.Label(new Rect(917, 471, 320, 55), $"Accel {settings.acceleration:0.0} / speed {settings.maximumSpeed:0.0}\nGrip {settings.lateralGrip:0.0} / steer {settings.steeringDegreesPerSecond:0}", small);
            if (GUI.Button(new Rect(917, 539, 153, 37), "Restore defaults", button)) walker.RestoreSessionTuning();
            if (GUI.Button(new Rect(1080, 539, 153, 37), "Close", button)) showTuning = false;
        }

        private float Slider(string label, float value, float min, float max, float y)
        {
            GUI.Label(new Rect(917, y, 305, 26), $"{label}   {value:0.0}", body);
            return GUI.HorizontalSlider(new Rect(919, y + 31, 310, 20), value, min, max);
        }

        private void Banner(string text, Color color, float y)
        {
            Fill(new Rect(390, y, 500, 50), color);
            GUI.Label(new Rect(410, y + 9, 465, 35), text, heading);
        }

        private void Panel(Rect rect)
        {
            Fill(rect, ink);
            Fill(new Rect(rect.x, rect.y, 5, rect.height), teal);
        }

        private static void Fill(Rect rect, Color color)
        {
            var previous = GUI.color;
            GUI.color = color;
            GUI.DrawTexture(rect, Texture2D.whiteTexture);
            GUI.color = previous;
        }
    }
}
