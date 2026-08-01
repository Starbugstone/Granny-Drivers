using System;
using GrannyRacer.Input;
using GrannyRacer.Walker;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace GrannyRacer.Racing
{
    public enum RaceState
    {
        Countdown,
        Racing,
        Finished,
        Paused
    }

    [DefaultExecutionOrder(-900)]
    public sealed class RaceController : MonoBehaviour
    {
        [SerializeField] private ArcadeWalkerController[] racers = Array.Empty<ArcadeWalkerController>();
        [SerializeField] private Vector3[] startPositions = Array.Empty<Vector3>();
        [SerializeField] private Vector3[] startForwards = Array.Empty<Vector3>();
        [SerializeField] private int playerRacerIndex;
        [SerializeField] private PlayerRacerInput playerInput;
        [SerializeField, Min(2)] private int checkpointCount = 8;
        [SerializeField, Min(1)] private int lapTarget = 3;
        [SerializeField, Min(0f)] private float countdownDuration = 3f;

        private RaceProgress[] progressByRacer = Array.Empty<RaceProgress>();
        private float countdownRemaining;
        private RaceState stateBeforePause;
        private Vector3 expectedTrackDirection;
        private float wrongWayTime;
        private float raceElapsed;

        private ArcadeWalkerController PlayerRacer => racers[playerRacerIndex];
        private RaceProgress PlayerProgress => progressByRacer[playerRacerIndex];

        public RaceState State { get; private set; }
        public int CurrentLap => progressByRacer.Length == 0 ? 1 : Mathf.Min(PlayerProgress.CompletedLaps + 1, lapTarget);
        public int LapTarget => lapTarget;
        public int Position => RacePositionMath.GetPosition(playerRacerIndex, progressByRacer);
        public int RacerCount => racers.Length;
        public float CountdownRemaining => countdownRemaining;
        public bool IsWrongWay { get; private set; }
        public float FinishTime { get; private set; }
        public bool IsInitialized { get; private set; }

        public void Configure(ArcadeWalkerController[] raceRacers, Vector3[] gridPositions,
            Vector3[] gridForwards, int playerIndex, PlayerRacerInput input, int checkpoints, int laps)
        {
            racers = raceRacers;
            startPositions = gridPositions;
            startForwards = gridForwards;
            playerRacerIndex = playerIndex;
            playerInput = input;
            checkpointCount = checkpoints;
            lapTarget = laps;
        }

        private void Awake()
        {
            StartRace();
        }

        private void Update()
        {
            if (playerInput != null)
            {
                var input = playerInput.Sample();
                if (input.Pause) TogglePause();
                if (State == RaceState.Finished && (input.BoostPressed || input.Reset))
                {
                    RestartScene();
                    return;
                }
            }

            if (State == RaceState.Countdown)
            {
                countdownRemaining -= Time.deltaTime;
                if (countdownRemaining <= 0f)
                {
                    countdownRemaining = 0f;
                    State = RaceState.Racing;
                    SetAllCanDrive(true);
                }
            }
            else if (State == RaceState.Racing)
            {
                raceElapsed += Time.deltaTime;
                var headingDot = Vector3.Dot(PlayerRacer.transform.forward, expectedTrackDirection);
                wrongWayTime = headingDot < -0.35f && PlayerRacer.Speed > 2f
                    ? wrongWayTime + Time.deltaTime
                    : 0f;
                IsWrongWay = wrongWayTime >= 1.5f;
            }
        }

        public void StartRace()
        {
            ValidateConfiguration();
            Time.timeScale = 1f;
            progressByRacer = new RaceProgress[racers.Length];
            for (var i = 0; i < racers.Length; i++)
            {
                progressByRacer[i] = new RaceProgress(checkpointCount, lapTarget);
                var forward = startForwards[i].sqrMagnitude > 0.001f ? startForwards[i].normalized : Vector3.forward;
                racers[i].ResetForRace(startPositions[i], Quaternion.LookRotation(forward, Vector3.up));
            }

            countdownRemaining = countdownDuration;
            raceElapsed = 0f;
            FinishTime = 0f;
            wrongWayTime = 0f;
            IsWrongWay = false;
            State = RaceState.Countdown;
            expectedTrackDirection = startForwards[playerRacerIndex].normalized;
            SetAllCanDrive(false);
            IsInitialized = true;
        }

        public bool TryPassCheckpoint(int index, Transform checkpoint, ArcadeWalkerController racer)
        {
            if (State != RaceState.Racing) return false;
            var racerIndex = IndexOfRacer(racer);
            if (racerIndex < 0 || !progressByRacer[racerIndex].TryPassCheckpoint(index)) return false;

            racers[racerIndex].SetResetPose(checkpoint.position + Vector3.up * 0.9f, checkpoint.rotation);
            if (progressByRacer[racerIndex].IsFinished)
            {
                racers[racerIndex].SetCanDrive(false);
                if (racerIndex == playerRacerIndex) FinishTime = raceElapsed;
                if (HaveAllRacersFinished()) State = RaceState.Finished;
            }
            else if (racerIndex == playerRacerIndex)
            {
                expectedTrackDirection = checkpoint.forward;
            }

            return true;
        }

        public void TogglePause()
        {
            if (State == RaceState.Paused)
            {
                State = stateBeforePause;
                Time.timeScale = 1f;
                SetAllCanDrive(State == RaceState.Racing);
                return;
            }

            stateBeforePause = State;
            State = RaceState.Paused;
            Time.timeScale = 0f;
            SetAllCanDrive(false);
        }

        public void RestartScene()
        {
            Time.timeScale = 1f;
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }

        private int IndexOfRacer(ArcadeWalkerController racer)
        {
            for (var i = 0; i < racers.Length; i++)
            {
                if (racers[i] == racer) return i;
            }

            return -1;
        }

        private void SetAllCanDrive(bool canDrive)
        {
            for (var i = 0; i < racers.Length; i++) racers[i].SetCanDrive(canDrive);
        }

        private bool HaveAllRacersFinished()
        {
            for (var i = 0; i < progressByRacer.Length; i++)
            {
                if (!progressByRacer[i].IsFinished) return false;
            }

            return true;
        }

        private void ValidateConfiguration()
        {
            if (racers.Length == 0 || racers.Length != startPositions.Length || racers.Length != startForwards.Length)
            {
                throw new InvalidOperationException("Race requires matching racer and spawn-grid arrays.");
            }

            if (playerRacerIndex < 0 || playerRacerIndex >= racers.Length)
            {
                throw new InvalidOperationException("Player racer index is outside the configured racer grid.");
            }
        }
    }
}
