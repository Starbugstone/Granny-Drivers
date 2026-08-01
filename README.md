# Granny Racer

A humorous, cartoon-styled 3D arcade racing game in which grannies race modified walking
frames fitted with boosters.

The project is currently a **proof of concept**: one racer, one greybox track, and the
handling model. It exists to answer a single question — *is this fun to drive?* — before any
art, audio, AI racers, or combat get built. Nothing in this repository is claimed to look or
feel finished.

> The repository folder is `Granny Drivers`; the game, namespace, and asset root are all
> `GrannyRacer`. Same project.

---

## Requirements

| | |
| --- | --- |
| Unity | `6000.4.4f1` (exact — see `ProjectSettings/ProjectVersion.txt`) |
| Render pipeline | Universal RP 17.4.0 |
| Platform | Windows x86-64 |
| Input | Keyboard, or an Xbox-compatible controller |

Packages are declared in `Packages/manifest.json` and restored by Unity on first open. The
notable ones are Input System 1.19.0 and Test Framework 1.6.0. **Do not add packages without
discussing it first** — netcode, Addressables, and anything multiplayer are deliberately out
of scope until the offline race is accepted.

## Getting started

1. Clone the repository.
2. Open the root folder as a Unity project in `6000.4.4f1`. The first import takes a few
   minutes.
3. Open `Assets/GrannyRacer/Scenes/Tracks/POC_QuietSunday.unity`.
4. Press Play.

## Controls

| Action | Keyboard | Gamepad |
| --- | --- | --- |
| Throttle | `W` / `↑` | Right trigger |
| Brake / reverse | `S` / `↓` | Left trigger |
| Steer | `A` `D` / `←` `→` | Left stick |
| Boost | `Space` | A |
| Reset to last checkpoint | `R` | Y |
| Pause | `Esc` | Start |

Boost heats the slippers. Let them reach 100% and they burn out, capping your speed until
they are replaced — **tap boost repeatedly during a burnout** to swap them faster. `Q` and `E`
are bound to left/right attack but are reserved; combat is not implemented.

A debug HUD shows speed, grounded state, slipper heat, lap, and position.

## Repository layout

Only the Unity project and the design docs are in version control:

```text
Assets/GrannyRacer/
├── Scripts/Runtime/{Input,Walker,Racing,Camera,UI}   ← gameplay code
├── Scripts/Editor/                                   ← scene generator, build entry points
├── Tests/{EditMode,PlayMode}
├── Scenes/Tracks/POC_QuietSunday.unity                ← the POC scene (generated)
├── Settings/                                          ← tuning ScriptableObjects
└── Art/Materials/
Docs/                                                  ← plan, decisions, design, test plans
```

The toolchain, agent instructions, and Blender sources (`Tools/`, `.claude/`, `.project/`,
`AGENTS.md`, `CLAUDE.md`, `Blender/Source/`) are **intentionally git-ignored** and stay on the
original workstation. A fresh clone will not have them, which is why the commands below are
written as raw Unity CLI invocations rather than script calls.

## Tuning

All tuning lives in ScriptableObjects so handling can be revised without recompiling:

| Asset | Controls |
| --- | --- |
| `Settings/WalkerHandling_POC.asset` | acceleration, top speed, steering, grip, downforce, rolling resistance, lean/wobble |
| `Settings/SlipperHeat_POC.asset` | heat rate, cooling, burnout thresholds, replacement timing |
| `Settings/Track_QuietSunday_POC.asset` | waypoints, widths, lap count, shortcut |

### Regenerating the track and scene

`POC_QuietSunday.unity` is **generated**, not hand-authored. The road mesh and spawn layout
are baked from the track asset. After editing `Track_QuietSunday_POC.asset`, rebuild it:

> **Granny Racer → POC → Create Complete Single-Racer POC**

This overwrites the scene, the road mesh, and the walker's physics material.

## Building

From the editor: **Granny Racer → Build → Windows (Development)** or **(Release)**.

From the command line:

```powershell
& "<path-to>\Unity.exe" -batchmode -quit -nographics `
    -projectPath . `
    -executeMethod GrannyRacer.Editor.BuildCommands.BuildWindowsDevelopment `
    -logFile .\build.log
```

Output lands in `Builds/Windows-Development/GrannyRacer.exe`. Pass `-buildOutput <dir>` to
redirect it. If no scenes are enabled in the build profile the build falls back to every
scene under `Assets/` and logs a warning — that is not a configured build.

## Tests

EditMode covers the pure gameplay maths (steering curves, slipper heat and cooling, race
position sorting, track definitions, generated road geometry). PlayMode drives the real scene
with a virtual keyboard and checks the walker accelerates, coasts down, and sits on the road.

```powershell
& "<path-to>\Unity.exe" -batchmode -nographics -projectPath . `
    -runTests -testPlatform EditMode `
    -testResults .\editmode-results.xml -logFile .\editmode.log
```

Swap `EditMode` for `PlayMode` (and drop `-nographics`) for the scene tests. Exit code `0`
means pass, `2` means tests failed, `3` usually means a compile error — read the log, since
Unity writes almost nothing useful to stdout in batch mode.

**Unity holds a lock on the project.** Batch mode cannot run while the editor has it open;
close the editor first.

## Conventions worth knowing

- Gameplay maths lives in plain C# classes with no `MonoBehaviour` dependency, so it can be
  tested without a scene. Unity glue goes in a thin component that calls it.
- Physics in `FixedUpdate`, input sampling and presentation in `Update`.
- No allocations in `Update` / `FixedUpdate`, and no `Find` / `GetComponent` in hot paths —
  cache in `Awake`.
- Never rename a serialized field without `[FormerlySerializedAs]`.
- Race systems are written for N racers even though only one exists.

### The walker collider must stay frictionless

`ArcadeWalkerController` models its own drive, braking, lateral grip, and rolling resistance.
PhysX friction on top of that double-counts, and because `downforce` raises the contact normal
impulse, default 0.6 friction cancels the drive force entirely — the walker steers but will
not move. `PM_Walker_Frictionless.physicsMaterial` (0 static, 0 dynamic, combine `Minimum`)
is what prevents this, and `WalkerDriveTests` guards it. Do not "fix" it by giving the walker
grippy material.

Related: the generated road mesh must be wound so its normals point **up**. A `MeshCollider`
on an inside-out mesh is only solid from below, and the walker falls through the track.
`GreyboxTrackGeometryTests` guards that.

## Documentation

| File | Contents |
| --- | --- |
| [`Docs/POC_PLAN.md`](Docs/POC_PLAN.md) | POC scope, phases, and definition of done |
| [`Docs/DECISIONS.md`](Docs/DECISIONS.md) | dated decision log |
| [`Docs/TECHNICAL_DESIGN.md`](Docs/TECHNICAL_DESIGN.md) | system design |
| [`Docs/FULL_GAME_BACKLOG.md`](Docs/FULL_GAME_BACKLOG.md) | deferred full-game ideas — not implemented |
| [`Docs/TestPlans/POC_Playtest.md`](Docs/TestPlans/POC_Playtest.md) | structured playtest brief |

## Status and scope

In scope for the POC: arcade walker physics, boost, slipper heat and burnout, a greybox
track, the three-lap race loop, checkpoints and reset, and a debug HUD.

Deferred: combat, AI racers, terrain and environment art, characters, audio, items and
pickups, and multiplayer.

The next step after the POC builds is **not** the next milestone — it is a playtest pass
using the test plan above, then handling revision. If the driving does not feel good, the
controller gets revised rather than papered over with art.
