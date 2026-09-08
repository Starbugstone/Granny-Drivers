using GrannyRacer.Walker;
using GrannyRacer.Racing;
using UnityEngine;

namespace GrannyRacer.UI
{
    public sealed class DrivingDebugHud : MonoBehaviour
    {
        [SerializeField] private ArcadeWalkerController walker;
        [SerializeField] private RaceController race;
        private GUIStyle style;
        private GUIStyle lightLabelStyle;
        private Texture2D lampTexture;

        public void Configure(ArcadeWalkerController controller, RaceController raceController)
        {
            walker = controller;
            race = raceController;
        }

        private void Awake()
        {
            style = new GUIStyle
            {
                fontSize = 22,
                normal = { textColor = Color.white }
            };
            lightLabelStyle = new GUIStyle(style)
            {
                fontSize = 14,
                alignment = TextAnchor.MiddleCenter
            };
            lampTexture = CreateLampTexture(48);
        }

        private void OnDestroy()
        {
            if (lampTexture != null) Destroy(lampTexture);
        }

        private void OnGUI()
        {
            if (walker == null) return;
            GUI.Box(new Rect(16f, 16f, 430f, 220f), string.Empty);
            GUI.Label(new Rect(30f, 28f, 360f, 30f), $"Speed: {walker.Speed * 3.6f:0} km/h", style);
            GUI.Label(new Rect(30f, 56f, 360f, 30f), walker.IsGrounded ? "Grounded" : "AIRBORNE", style);
            GUI.Label(new Rect(30f, 84f, 380f, 30f), $"Slippers: {walker.HeatStage}  {walker.Heat * 100f:0}%", style);
            GUI.HorizontalScrollbar(new Rect(30f, 116f, 380f, 22f), 0f, walker.Heat, 0f, 1f);

            DrawDriftState();

            if (race != null)
            {
                GUI.Label(new Rect(30f, 174f, 390f, 30f),
                    $"Lap {race.CurrentLap}/{race.LapTarget}    Position {race.Position}/{race.RacerCount}", style);
                if (race.IsWrongWay)
                {
                    GUI.Label(new Rect(Screen.width * 0.5f - 90f, 60f, 250f, 40f), "WRONG WAY", style);
                }
                DrawRaceMessage();
                DrawTrafficLight();
            }

            GUI.Label(new Rect(16f, Screen.height - 42f, 950f, 30f),
                "WASD/arrows drive • Space boost • Hold Left Shift to hop, steer as you land to "
                + "drift • R reset", style);
        }

        private void DrawDriftState()
        {
            var text = "Drift: —";
            var tint = Color.white;
            if (walker.IsDriftArmed)
            {
                // Worth showing separately: "armed" is the window between the hop and the drift,
                // and when a hop-drift fails to catch, this is where it went wrong.
                tint = new Color(0.75f, 0.75f, 0.78f);
                text = walker.IsGrounded
                    ? "Drift armed: steer to engage"
                    : "Drift armed: landing…";
            }
            else if (walker.IsDrifting)
            {
                var stage = walker.DriftStage;
                tint = DriftColor(stage);
                text = $"Drift {(walker.DriftDirection < 0 ? "left" : "right")}: "
                    + $"{stage}  {walker.DriftCharge:0.0}s";
            }
            else if (walker.DriftBoostActive)
            {
                tint = DriftColor(walker.DriftBoostStage);
                text = $"Drift boost: {walker.DriftBoostStage}";
            }

            var previous = style.normal.textColor;
            style.normal.textColor = tint;
            GUI.Label(new Rect(30f, 146f, 390f, 30f), text, style);
            style.normal.textColor = previous;
        }

        private static Color DriftColor(DriftChargeStage stage)
        {
            switch (stage)
            {
                case DriftChargeStage.Red: return new Color(1f, 0.35f, 0.25f);
                case DriftChargeStage.Yellow: return new Color(1f, 0.88f, 0.3f);
                case DriftChargeStage.Blue: return new Color(0.45f, 0.7f, 1f);
                default: return Color.white;
            }
        }

        private void DrawRaceMessage()
        {
            var message = string.Empty;
            switch (race.State)
            {
                case RaceState.Countdown:
                    message = Mathf.CeilToInt(race.CountdownRemaining).ToString();
                    break;
                case RaceState.Racing:
                    if (race.StartFeedbackRemaining > 0f)
                    {
                        switch (race.StartOutcome)
                        {
                            case RaceStartOutcome.Skid: message = "WHEELSPIN!"; break;
                            case RaceStartOutcome.Boost: message = "BOOST START!"; break;
                            case RaceStartOutcome.Turbo: message = "PERFECT TURBO!"; break;
                            default: message = "GO!"; break;
                        }
                    }
                    break;
                case RaceState.Finished:
                    message = $"FINISHED in {race.FinishTime:0.0}s — Press Space or R to race again";
                    break;
                case RaceState.Paused:
                    message = "PAUSED";
                    break;
            }

            if (message.Length > 0)
            {
                GUI.Label(new Rect(Screen.width * 0.5f - 220f, Screen.height * 0.25f, 520f, 50f), message, style);
            }
        }

        private void DrawTrafficLight()
        {
            var showingGo = race.State == RaceState.Racing && race.StartFeedbackRemaining > 0f;
            if (race.State != RaceState.Countdown && !showingGo) return;

            var x = Screen.width * 0.5f - 46f;
            var y = 28f;
            GUI.Box(new Rect(x, y, 92f, 196f), string.Empty);

            var red = race.State == RaceState.Countdown && race.CountdownRemaining > 2f;
            var amber = race.State == RaceState.Countdown && race.CountdownRemaining <= 2f;
            DrawLamp(new Rect(x + 22f, y + 10f, 48f, 48f), Color.red, red, "WAIT");
            DrawLamp(new Rect(x + 22f, y + 68f, 48f, 48f), new Color(1f, 0.72f, 0.05f), amber, "READY");
            DrawLamp(new Rect(x + 22f, y + 126f, 48f, 48f), Color.green, showingGo, "GO");
        }

        private void DrawLamp(Rect rect, Color color, bool active, string label)
        {
            var previous = GUI.color;
            GUI.color = active ? color : color * 0.18f;
            GUI.DrawTexture(rect, lampTexture);
            GUI.color = Color.white;
            GUI.Label(rect, label, lightLabelStyle);
            GUI.color = previous;
        }

        private static Texture2D CreateLampTexture(int size)
        {
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                name = "Runtime Traffic Light Lamp",
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp
            };
            var pixels = new Color32[size * size];
            var radius = size * 0.48f;
            var centre = (size - 1) * 0.5f;
            for (var y = 0; y < size; y++)
            {
                for (var x = 0; x < size; x++)
                {
                    var dx = x - centre;
                    var dy = y - centre;
                    pixels[y * size + x] = dx * dx + dy * dy <= radius * radius
                        ? new Color32(255, 255, 255, 255)
                        : new Color32(0, 0, 0, 0);
                }
            }

            texture.SetPixels32(pixels);
            texture.Apply(false, true);
            return texture;
        }
    }
}
