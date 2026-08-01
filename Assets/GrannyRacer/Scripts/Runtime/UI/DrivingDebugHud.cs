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
        }

        private void OnGUI()
        {
            if (walker == null) return;
            GUI.Box(new Rect(16f, 16f, 430f, 190f), string.Empty);
            GUI.Label(new Rect(30f, 28f, 360f, 30f), $"Speed: {walker.Speed * 3.6f:0} km/h", style);
            GUI.Label(new Rect(30f, 56f, 360f, 30f), walker.IsGrounded ? "Grounded" : "AIRBORNE", style);
            GUI.Label(new Rect(30f, 84f, 380f, 30f), $"Slippers: {walker.HeatStage}  {walker.Heat * 100f:0}%", style);
            GUI.HorizontalScrollbar(new Rect(30f, 116f, 380f, 22f), 0f, walker.Heat, 0f, 1f);

            if (race != null)
            {
                GUI.Label(new Rect(30f, 144f, 390f, 30f),
                    $"Lap {race.CurrentLap}/{race.LapTarget}    Position {race.Position}/{race.RacerCount}", style);
                if (race.IsWrongWay)
                {
                    GUI.Label(new Rect(Screen.width * 0.5f - 90f, 60f, 250f, 40f), "WRONG WAY", style);
                }
                DrawRaceMessage();
            }

            GUI.Label(new Rect(16f, Screen.height - 42f, 800f, 30f),
                "WASD/arrows drive • Space boost/tap replacement • R reset • Esc pause", style);
        }

        private void DrawRaceMessage()
        {
            var message = string.Empty;
            switch (race.State)
            {
                case RaceState.Countdown:
                    message = Mathf.CeilToInt(race.CountdownRemaining).ToString();
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
    }
}
