# Drift and skid marks

The charged hop-drift is a core mechanic. This records how it now behaves, why the awkward
parts are the way they are, and what a human still has to judge.

## The three phases

| Phase | Trigger | What the player sees |
| --- | --- | --- |
| Armed | Jump pressed above `driftMinimumSpeed` | An ordinary hop. Nothing slides, smokes or marks. |
| Drifting | Landing while steering past `driftMinimumSteer` | Slide, tinted slipper smoke, skid marks, charge climbing |
| Boosting | Jump released | Rocket flames, raised speed cap for `driftBoostDuration` |

**The hop no longer commits the drift.** It only arms it. Direction is read from the stick at
the moment the slippers touch down, so a player can hop, change their mind in the air, and
still drift the other way. `driftEngageWindow` (0.6 s) keeps the hop live after touchdown, so
turning in slightly late still catches the drift instead of silently wasting it.

### Why `driftMinimumAirTime` exists

The walker's ground probe (`groundProbeDistance`, 0.8 m) reaches roughly 0.45 m below the
resting slipper, and a hop clears that in about five physics steps. Without a minimum air
time, the probe reports "grounded" a step or two after take-off and the drift engages
instantly — which is exactly the behaviour this change was meant to remove. The drift model
therefore ignores ground contact until the hop has been airborne for `driftMinimumAirTime`
(0.08 s). Contact before that is treated as the tail of the take-off.

`DriftModelTests.GroundContactDuringTakeOffIsNotALanding` guards this.

## Steering feel

`WalkerHandlingMath.DriftSteer` is deliberately asymmetric:

- The drift holds its own line at `driftSteerBias` (0.7).
- Steering **into** it adds `driftInwardSteerControl` (0.38) → full lock. Combined with
  `driftSteeringScale` (1.25× yaw), a drifted corner genuinely out-turns a gripped one, which
  is the whole reason to drift.
- **Counter-steering** subtracts `driftCounterSteerControl` (0.62), bottoming out at
  `driftMinimumHold` (0.08) — nearly straight, so a drift can be held down a straight to keep
  charging, but never resolves to neutral or flips to the other lock.

`driftEngageRamp` (0.15 s) blends from the player's raw steering onto the drift's line, so
landing does not snap the walker sideways.

Charge still banks faster inside (`driftInsideChargeRate`) than countering
(`driftOutsideChargeRate`), so holding a straight with counter-steer trades charge rate for
line — the intended risk/reward.

## Skid marks

Marks fade out within **3 seconds** (`SkidMarkFadeSeconds` in `PocSceneBuilder`). Two Unity
behaviours forced the design, and both are easy to reintroduce by accident:

1. **A TrailRenderer records points from its own movement even when `emitting` is false.**
   The ribbons were originally parented to the racer, so all ten quietly drew Granny's entire
   route around the track from the moment the scene loaded. They now live under a `Skid Marks`
   object outside the racer's hierarchy, and unused ribbons are **disabled**, not merely not
   emitting. `WalkerVfxPresentation` drives the active one to its slipper's ground contact.

2. **A TrailRenderer only ages its points while it is being rendered.** A mark left behind the
   camera stops fading and can pop back into view long after its time is up. `SkidMarkPool`
   therefore tracks release times and `WalkerVfxPresentation` clears and disables spent ribbons
   on its own clock (fade + 0.25 s margin) rather than trusting the renderer.

Each unbroken skid takes its own ribbon (five per slipper). Re-enabling a trail that still
holds points joins the two skids with a straight streak across the road, which is why
`SkidMarkPool` hands out a fresh ribbon per skid and only recycles oldest-release-first.

Marks are drawn for brake-slides as well as drifts (`ArcadeWalkerController.IsSkidding`), and
only while grounded — a drift that hops a kerb stops smoking and marking until it lands.

## Manual editor step

**The POC scene must be regenerated after any change to the ribbon layout:**

```powershell
pwsh -File Tools/unity-run.ps1 -Method GrannyRacer.Editor.PocSceneBuilder.CreateCompletePoc -Graphics
```

Or in the editor: `Granny Racer → POC → Create Complete Single-Racer POC`. This rebuilds the
scene from scratch, so any hand-tuning done in the scene is lost.

## Tuning

All values are on `Assets/GrannyRacer/Settings/WalkerHandling_POC.asset` under **Drift**.
`driftSteerControl` was renamed to `driftInwardSteerControl` with `[FormerlySerializedAs]`, so
the existing asset keeps its value (0.45) rather than the new 0.38 default. The other new
fields take their code defaults until a human tunes them.

## Not verified — needs a human

- Whether the drift *feels* right: engage window length, ramp, counter-steer authority, and
  whether landing into a drift reads as deliberate rather than sluggish.
- Whether the marks look right: width, darkness, and whether 3 seconds is the right lifetime
  at racing speed.
- No audio is wired to the drift or its charge tiers. Deferred, not done.

## Known gaps

- Charge tier changes are shown only as a smoke tint. There is no burst or pop on reaching a
  tier, and no HUD element beyond the debug overlay.
- If more than five skids per slipper overlap within the fade window, the oldest mark is cut
  short rather than fading out.
- A hop that never clears the ground probe (a very steep uphill) never registers a landing, so
  the drift does not engage. Releasing and re-hopping recovers.
