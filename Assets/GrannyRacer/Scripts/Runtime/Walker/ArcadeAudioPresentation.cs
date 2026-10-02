using GrannyRacer.Racing;
using UnityEngine;

namespace GrannyRacer.Walker
{
    // All sources and clips are created/wired by the editor builder, never in a frame loop.
    public sealed class ArcadeAudioPresentation : MonoBehaviour
    {
        public enum Cue { Countdown, Go, Gate, Lap, FinalLap, Finish, BoostBurst, Burnout, Impact }
        [SerializeField] private ArcadeWalkerController walker;
        [SerializeField] private RaceController race;
        [SerializeField] private AudioSource cues, roll, skid, rocket, garden;
        [SerializeField] private AudioClip[] clips;
        [SerializeField, Range(0, 1)] private float effectsVolume = .65f;
        [SerializeField, Range(0, 1)] private float rollingVolume = .13f;
        [SerializeField, Range(0, 1)] private float ambienceVolume = .035f;
        private RaceState previousState;
        private int previousCount = -1, previousGate, previousLap;
        private bool previousBoost, previousBurnout, paused;
        private float duckUntil, impactAfter;
        private static float sessionEffectsVolume = -1;
        public Cue LastCue { get; private set; }
        public int PlayedCueCount { get; private set; }
        public float EffectsVolume { get => effectsVolume; set => effectsVolume = sessionEffectsVolume = Mathf.Clamp01(value); }
        public void Configure(ArcadeWalkerController controller, RaceController raceController,
            AudioSource events, AudioSource wheels, AudioSource drifting, AudioSource boosting, AudioSource ambience, AudioClip[] bank)
        { walker=controller;race=raceController;cues=events;roll=wheels;skid=drifting;rocket=boosting;garden=ambience;clips=bank; }
        private void Start()
        {
            if(sessionEffectsVolume>=0)effectsVolume=sessionEffectsVolume;
            previousState=race.State;previousGate=race.NextCheckpoint;previousLap=race.CurrentLap;
            roll.Play();skid.Play();rocket.Play();garden.Play();
        }
        private void Update()
        {
            var shouldPause=race.State==RaceState.Paused;
            if(shouldPause!=paused)
            {
                paused=shouldPause;
                PauseSource(cues);PauseSource(roll);PauseSource(skid);PauseSource(rocket);PauseSource(garden);
            }
            if(paused)return;
            if(race.State==RaceState.Countdown)
            {
                var count=Mathf.CeilToInt(race.CountdownRemaining);
                if(count>0 && count!=previousCount)Play(Cue.Countdown,true);
                previousCount=count;
            }
            else if(race.State==RaceState.Racing)
            {
                if(previousState==RaceState.Countdown)Play(Cue.Go,true);
                else if(race.CurrentLap!=previousLap)Play(race.CurrentLap==race.LapTarget?Cue.FinalLap:Cue.Lap,true);
                else if(race.NextCheckpoint!=previousGate)Play(Cue.Gate,true);
            }
            else if(race.State==RaceState.Finished && previousState!=RaceState.Finished)Play(Cue.Finish,true);
            var boost=walker.IsBoosting || walker.IsTrackBoostActive || walker.DriftBoostActive || walker.IsStartBoostActive;
            var burnout=walker.HeatStage==SlipperHeatStage.BurnedOut;
            if(boost && !previousBoost)Play(Cue.BoostBurst,false);
            if(burnout && !previousBurnout)Play(Cue.Burnout,false);
            var driving=race.State==RaceState.Racing;
            var duck=Time.unscaledTime<duckUntil?.35f:1f;
            var speed=Mathf.Clamp01(walker.Speed/Mathf.Max(1,walker.Stats.BoostMaximumSpeed));
            roll.pitch=Mathf.Lerp(.7f,1.8f,speed);
            roll.volume=Mathf.MoveTowards(roll.volume,driving && walker.IsGrounded?speed*rollingVolume*effectsVolume*duck:0,Time.deltaTime);
            skid.pitch=1+walker.DriftCharge*.2f;
            skid.volume=Mathf.MoveTowards(skid.volume,driving && walker.IsDrifting?.12f*effectsVolume*duck:0,Time.deltaTime*2);
            rocket.pitch=Mathf.Lerp(.85f,1.25f,speed);
            rocket.volume=Mathf.MoveTowards(rocket.volume,driving && boost?.23f*effectsVolume*duck:0,Time.deltaTime*3);
            garden.volume=ambienceVolume*effectsVolume*duck;
            previousState=race.State;previousGate=race.NextCheckpoint;previousLap=race.CurrentLap;
            previousBoost=boost;previousBurnout=burnout;
        }
        private void PauseSource(AudioSource source) { if(paused)source.Pause();else source.UnPause(); }
        private void Play(Cue cue,bool priority)
        {
            if(!priority && Time.unscaledTime<duckUntil)return;
            var clip=clips[(int)cue];if(clip==null)return;
            if(priority) { cues.Stop();duckUntil=Time.unscaledTime+clip.length; }
            cues.PlayOneShot(clip,effectsVolume);LastCue=cue;PlayedCueCount++;
        }
        private void OnCollisionEnter(Collision collision)
        {
            if(race==null || race.State!=RaceState.Racing || collision.relativeVelocity.magnitude<3 || Time.time<impactAfter)return;
            impactAfter=Time.time+.35f;Play(Cue.Impact,false);
        }
    }
}
