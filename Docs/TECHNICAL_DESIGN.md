# Technical design

Runtime architecture for the POC. Design authority is
`.project/GRANNY_RACER_COMPLETE_DEVELOPMENT_PLAYBOOK.md` §12–§17; this file records how it
is actually built.

Status: **single-racer POC implemented; human handling review pending.** Combat, AI, final
environment art, authored audio, and polish remain deferred by D-03. Future-facing details
below are explicitly marked as planned.

---

## Assemblies

| Assembly | Path | Platforms |
|---|---|---|
| `GrannyRacer.Runtime` | `Assets/GrannyRacer/Scripts/Runtime` | all |
| `GrannyRacer.Editor` | `Assets/GrannyRacer/Scripts/Editor` | Editor only |
| `GrannyRacer.Tests.EditMode` | `Assets/GrannyRacer/Tests/EditMode` | Editor only |
| `GrannyRacer.Tests.PlayMode` | `Assets/GrannyRacer/Tests/PlayMode` | all |

Assembly definitions exist so that editor code cannot leak into builds and so that a change
to a tool does not force a full recompile of gameplay code.

---

## Layering

Runtime code is organised so that gameplay maths does not depend on Unity types where it can
be avoided.

```text
Scripts/Runtime/
├── Core/      shared types, no dependencies on other runtime folders
├── Input/     IRacerInputSource, RacerInputState, player implementation
├── Walker/    physics controller, handling settings, heat model, presentation
├── Racing/    track definition, checkpoints, laps, position, race state
├── Camera/    follow camera behaviour
└── UI/        HUD, tuning overlay
```

The dependency direction is one-way: `UI` and `Camera` may read from `Walker` and `Racing`;
neither `Walker` nor `Racing` may reference `UI`.

---

## Two structural constraints

Both come from [D-05](DECISIONS.md#d-05--two-forward-compatibility-constraints-are-honoured-from-the-start).
They are cheap now and expensive to retrofit, and should not be relaxed without a new decision.

### Input abstraction

```csharp
public interface IRacerInputSource
{
    RacerInputState Sample();
}
```

`RacerInputState` carries throttle, brake, steer, boost, jump, reset, pause, and reserved attack
inputs. `PlayerRacerInput` owns remappable Input System actions constructed once in `Awake`;
sampling is allocation-free and supports keyboard/controller switching at runtime.

The walker controller consumes `IRacerInputSource` and **never references the Input System
directly**. The player implementation wraps Input System actions; milestone 6 adds an
AI implementation against the same interface without touching physics code that has already
been tuned and playtested.

### N-racer race systems

Only one racer exists in the POC. No API may assume that — not position sorting, not results,
not the HUD. Adding three AI racers must not require reshaping these types.

---

## Walker controller

Playbook §12. A custom arcade controller on a `Rigidbody`, not Unity's `WheelCollider`.

- Ground detection by a central sphere cast for the POC. Four contact points remain a
  possible handling revision if the central probe makes kerbs unreliable.
- Custom acceleration, braking, and reverse forces.
- Speed-dependent steering falloff — strong at low speed, reduced at high (playbook §12.3).
- Lateral grip with configurable slide, plus yaw assistance.
- Downforce for grounding.
- Airborne handling when contact points lose the ground.
- A small buffered grounded jump on Left Shift / controller right shoulder.
- A tunable skid state when the player brakes and steers above the configured speed; lateral
  grip is reduced during the skid without changing the Rigidbody/collider architecture.
- A charged hop-drift sharing that skid state (see [Drift](#drift) below).

**Threading of the update loop:** input is sampled in `Update`; all force application happens
in `FixedUpdate`. Presentation reads state in `Update`/`LateUpdate` and never writes to the
Rigidbody.

**Physics and presentation are separate objects.** The physics root carries the Rigidbody and
collider; a visual child hierarchy carries the frame, wheels, granny, and boosters. Lean,
wobble, and shake are applied to the visual child only, so presentation can be exaggerated
freely without destabilising the simulation. This is what playbook §2.2 requires — the walker
must not feel like a kart wearing a walker model.

**Tuning** lives in a `WalkerHandlingSettings` ScriptableObject, not in serialized fields on
the component, so values can be swapped and compared during the revision pass.

**Testability:** the steering curve, grip solve, and speed limits are static methods taking
plain values and returning plain values. They are unit-tested without a scene.

---

## Slipper heat

Playbook §13. The heat model is a **plain C# class with no `MonoBehaviour` dependency**, so
it is fully unit-testable — playbook §13.2 requires heat values to be deterministic and
testable, and §24.1 lists heat, cooling, and burnout thresholds as EditMode test targets.

Normalised 0–1 with thresholds: safe below 0.60, warning 0.60–0.79, critical 0.80–0.99,
burnout at 1.00.

Inputs: heat per second while boosting, collision heat spike, cooling delay, cooling rate.
All in a ScriptableObject.

Burnout starts a short automatic replacement sequence at reduced speed. The racer keeps
moving — playbook §13.4 is explicit that this keeps the race going. Contextual button taps
shorten it slightly.

State is exposed for presentation but presentation does not drive it. Playbook §20.4:
gameplay-critical timing is code-driven, not animation-event-driven.

Boost flame/smoke, slipper heat smoke, and skid-mark trails are driven directly from the
controller's boost, heat-stage, grounded, and skid state. Jump and skid use small additive
bone poses in `LateUpdate` over the imported locomotion clips; they are placeholder animation
that requires human review, not new hero FBX authoring.

---

## Drift

`DriftModel` is a plain C# class, tuned by a `DriftTuning` struct that
`WalkerHandlingSettings` builds per physics step. Same rule as slipper heat: the timings and
payouts are deterministic and unit-tested without a scene.

The hop is the entry. `ArcadeWalkerController` calls `TryStart` on the frame the jump
impulse is applied; it fails quietly below `driftMinimumSpeed` or `driftMinimumSteer`, so a
straight hop stays a plain jump. The drift direction is fixed at that moment and cannot be
flipped by later steering.

While drifting:

- Charge accrues **only while grounded** — air time keeps the drift alive but banks nothing.
- Steering into the drift charges at `driftInsideChargeRate`, counter-steering at
  `driftOutsideChargeRate`, giving the player a reason to work the line.
- Steering is `WalkerHandlingMath.DriftSteer`, which blends a fixed bias toward the drift
  direction with the player's input and clamps so the result never crosses zero.
- Lateral grip is scaled by `driftGripScale`.

Charge passes through **red, yellow, then blue** tiers. Releasing the button — or dropping
below the minimum speed — ends the drift and pays the tier reached as a one-shot forward
velocity change, plus a window during which the speed cap is raised to `boostMaximumSpeed`
and rolling resistance is suspended. `ConsumeBoostImpulse` hands the impulse out exactly
once so it cannot be re-applied every physics step.

`IsSkidding` is true for a drift or a brake-slide, so marks, smoke, and the braced pose have
one trigger. The braced pose uses the committed drift direction rather than live steering,
so counter-steering does not flip Granny's hips.

---

## Walker VFX

Effects hang off an imported rig whose bones carry a 100x scale and animated orientations.
Two rules follow, and both exist because breaking them made the first pass invisible:

1. **Emitters are never scaled to compensate for a parent bone.** They keep
   `localScale = 1` with an explicit `ParticleSystemScalingMode.Local`, which ignores parent
   scale, so sizes and speeds in `PocSceneBuilder` are literal metres.
2. **Emitters are aimed in world space each frame**, not down a bone axis — rockets
   backward, smoke upward with negative gravity.

Skid marks are `TrailRenderer`s parented to the racer root and projected each frame onto the
ground beneath each slipper by a ray cast that skips the walker's own colliders. They are
aligned so the ribbon lies flat on the road, and linger 4 seconds. Drift smoke is tinted per
charge tier at runtime via `main.startColor`.

---

## Track representation

Playbook §15.1, and [D-04](DECISIONS.md#d-04--track-is-generated-from-waypoints-in-editor).

A `TrackDefinition` ScriptableObject currently holds ordered position/width/checkpoint
waypoints and a shortcut branch. Banking, surface types, and a spline evaluator remain
planned for AI/environment milestones.

The POC editor tool currently generates, into a child object rebuilt wholesale:

- road mesh
- kerb geometry
- outer barriers
- ordered checkpoint triggers
- lap trigger
- checkpoint reset poses and a single-racer spawn

Rebuilding wholesale keeps the scene diff small and avoids the merge conflicts playbook
§31.7 warns about.

The same spline becomes the AI racing line in milestone 6.

---

## Race progress

Playbook §15.2. Position is **not** computed from world distance to the finish.

POC progress is completed laps plus the last ordered checkpoint. Checkpoints must be crossed
in order, blocking direct checkpoint skips. Fractional spline progress and multi-racer
sorting remain planned for milestone 6, while the one-racer POC correctly reports 1/1.

Wrong-way detection compares the racer heading with the latest valid checkpoint direction
and only warns after 1.5 seconds of sustained opposing travel, avoiding ordinary spin noise.

Progress and position sorting are pure functions and unit-tested.

---

## Physics layers

Playbook §17.6. Defined in `ProjectSettings/TagManager.asset`:

```text
Racer  Track  SoftObstacle  HardObstacle  DynamicHazard
AttackHitbox  AttackHurtbox  Trigger  Pickup  ResetVolume  Decoration
```

`AttackHitbox` and `AttackHurtbox` are reserved for milestone 4 and unused in the POC. They
are defined now because adding layers after colliders exist means revisiting every prefab.

The collision matrix must be configured alongside the layers — decorative objects must not
block racers (playbook §17.6).

---

## Performance rules

Playbook §3.5 and §25. Enforced by review, not yet by tooling:

- No allocations in `Update`, `FixedUpdate`, or `LateUpdate`. No LINQ, no string
  concatenation, no `new` in hot paths.
- No `GameObject.Find` or `FindObjectOfType` in gameplay. Cache in `Awake`.
- Target 60 FPS at 1920×1080 on the reference PC.

Profiling comes after the POC is playable. Playbook §25: *do not optimise blindly before
measuring.*
