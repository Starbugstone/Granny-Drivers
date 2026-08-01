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

`RacerInputState` carries throttle, brake, steer, boost, reset, pause, and reserved attack
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
