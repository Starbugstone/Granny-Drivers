# Decision log

Append-only. Newest last. Do not rewrite history — supersede an entry with a new one
and mark the old entry `Superseded by D-nn`.

Each decision that contradicts the playbook is also summarised in playbook section 0.
Where the two disagree, this file is authoritative and the playbook should be corrected.

---

## D-01 — Unity project lives at the repository root

**Date:** 2026-07-31
**Status:** Accepted
**Playbook:** contradicts §6, §5.2, §5.4, §7.1, §9.1 — all amended, see §0.2

`Assets/`, `Packages/`, and `ProjectSettings/` sit at the repository root. There is no
`UnityProject/` subfolder. `Docs/`, `Tools/`, and `Blender/` are siblings of `Assets/`.

**Why:** the Unity project was created at the root before the playbook was applied to this
repository. Migrating would require reopening the project, re-resolving IDE and `.csproj`
paths, and rewriting the pushed history, for no gameplay benefit. The nesting was
organisational preference, not a technical requirement.

**Cost:** every playbook path example needs mentally stripping of its `UnityProject/`
prefix. Mitigated by amending the affected sections in place.

---

## D-02 — Naming split between product and code

**Date:** 2026-07-31
**Status:** Accepted
**Playbook:** §1.1

| Thing | Value |
|---|---|
| Repository | `Granny-Drivers` |
| Product / build name | ~~`Granny Drivers`~~ → `GrannyRacer` — **Superseded by D-10** |
| C# root namespace | `GrannyRacer` |
| Asset root folder | `Assets/GrannyRacer/` |
| Assembly prefix | `GrannyRacer.` |

**Why:** the playbook uses `GrannyRacer` throughout for namespaces, assembly definitions,
and `-executeMethod` paths. Keeping the code identifier aligned avoids translating every
example. The product name follows the repository, which is what was chosen.

**Note:** the working title is still open per playbook §1.1. If a final title is selected,
only the display name changes. The namespace stays, to avoid a repository-wide rename.

---

## D-03 — POC scope: milestones 0, 1, 2, 3, 5

**Date:** 2026-07-31
**Status:** Accepted
**Playbook:** §11, amended — see §0.4

The first deliverable is a **playable single-racer race**, not the full demo.

In scope: arcade walker physics, camera, lean/wobble/kerb/recovery, booster, slipper heat
including burnout and replacement, greybox track, checkpoints, laps, position, countdown,
results, restart, debug HUD, Windows build.

Deferred: milestone 4 (combat), milestone 6 (AI racers), milestone 7 (real terrain and
environment), milestone 8 (art, animation, audio), items, pickups, multiplayer.

**Why:** playbook §31.1 identifies handling feel as the project's largest risk. Everything
deferred is expensive to build and cheap to add later; handling is the opposite.

Boost and slipper heat are included despite being milestone 3, because they are what makes
the handling worth tuning. Boost changes the speed envelope the physics must stay stable
across, and heat is what makes boost a decision rather than a button you hold. The core
loop cannot be judged fun without them.

**Gate:** a human playtest pass revises handling before any deferred work starts.
Automated checks cannot substitute — see playbook §24.1, "human playtests".

---

## D-04 — Track is generated from waypoints in-editor

**Date:** 2026-07-31
**Status:** Accepted
**Playbook:** §17, §19

The greybox track comes from a `TrackDefinition` ScriptableObject holding ordered waypoints,
plus a custom editor tool that generates road mesh, kerbs, barriers, ordered checkpoints,
lap trigger, spawn grid, and reset transforms.

Rejected: hand-placed primitive prefabs (tedious to reshape, and scene-file merge conflicts
per playbook §31.7); modelling the greybox in Blender (slow iteration, and a full
export/import round trip for every layout change).

**Why:** the layout must be cheap to reshape during the D-03 revision pass. Dragging
waypoints is the fastest loop. The spline also becomes the AI racing line that playbook
§16.1 requires, at no extra cost.

**Consequence:** the Blender pipeline in playbook §19 does not apply to the demo track until
milestone 7 replaces the greybox with real art.

---

## D-05 — Two forward-compatibility constraints are honoured from the start

**Date:** 2026-07-31
**Status:** Accepted

1. **Input is abstracted.** The walker controller consumes `IRacerInputSource` and never
   references the Input System directly.
2. **Race systems are written for N racers.** Only one racer exists in the POC, but no API
   assumes that.

**Why:** both are nearly free now and expensive later. Without (1), adding AI in milestone 6
means editing physics code that was tuned and playtested — the highest-risk file in the
project. Without (2), position sorting, results, and the HUD all need reworking.

These are the *only* two speculative allowances. Everything else follows playbook §31.6:
if it is not POC scope, it goes to the backlog.

---

## D-06 — PNG files are tracked in Git LFS

**Date:** 2026-07-31
**Status:** Accepted
**Playbook:** §7.2 explicitly left this open

PNG is tracked in LFS, along with the other binary formats.

**Why:** the project will accumulate texture atlases and UI art. Deciding late means
rewriting history. The repository already uses the `gitattributes/gitattributes` Unity
template, which defines an `[attr]lfs` macro and already covers `.png`, `.blend`, `.fbx`,
`.psd`, `.wav`, `.mp3`, and `.ogg`.

**Outstanding:** `.glb`, `.gltf`, `.kra`, and `.flac` from the playbook §7.2 list are not yet
in `.gitattributes`. Low priority — none are in use.

---

## D-07 — Package set

**Date:** 2026-07-31
**Status:** Accepted
**Playbook:** §3.1, amended — see §0.9

Editor is Unity **6000.4.4f1**.

Add: **Cinemachine** — required by §3.1 and missing from the manifest.

Remove as unused: `com.unity.visualscripting`, `com.unity.multiplayer.center`,
`com.unity.collab-proxy`.

Retain despite not being needed yet: `com.unity.ai.navigation` (milestone 6),
`com.unity.timeline` (milestone 8).

**Why:** §3.1 — "Do not add packages merely because they might be useful later." Visual
Scripting in particular adds generated files that need gitignore entries and slows domain
reload for a project that will not use it.

---

## D-08 — Multiple agents work this repository in parallel, by task

**Date:** 2026-07-31
**Status:** Accepted
**Playbook:** contradicts §4.3, "one writer per worktree"

Agents work concurrently in the single working tree, separated by task assignment rather
than by worktree.

**Why:** the user's chosen workflow. Task boundaries are expected to keep file sets disjoint.

**Risk accepted:** playbook §4.3 exists because concurrent writers clobber each other. This
was observed on 2026-07-31 — a second agent created `AGENTS.md`, `CLAUDE.md`, `Tools/*.ps1`,
and `Blender/Scripts/*.py` while this session was editing the playbook. No work was lost,
because a read-before-write guard blocked an overwrite of `AGENTS.md`.

**Mitigation:** before writing, check whether a file already exists and read it first. Stay
inside the assigned task's file set. Do not "tidy" adjacent files.

**Reconsider if:** work is lost, or two agents produce contradictory versions of the same
file. The fallback is `git worktree` per playbook §4.3.

---

## D-09 — Agent scaffolding stays git-ignored (confirms the open question below)

**Date:** 2026-07-31
**Status:** Accepted — user confirmed when asked directly
**Playbook:** contradicts §6, §7.1, and §11 milestone 0

`.project/`, `.claude/`, `AGENTS.md`, `CLAUDE.md`, `Tools/`, and all of `Blender/` remain
excluded from version control. `Assets/`, `Packages/`, `ProjectSettings/`, and `Docs/` are
tracked.

**Why:** the user's choice. Agent scaffolding is treated as local workspace, not shared
project history.

**Costs accepted**, restated from the open question this supersedes:

- Playbook §11 milestone 0's "a clean clone can be configured using documented local
  settings" cannot be met — a clone has no playbook, rules, or build/test scripts.
- `Blender/Source/` never reaches Git LFS, so **the working copy is the only copy of every
  `.blend`**. There is no backup through Git.
- `.project/GRANNY_RACER_COMPLETE_DEVELOPMENT_PLAYBOOK.md` — the design source of truth —
  exists only on this machine.

**Mitigation:** anything that must survive belongs in `Docs/`, which is tracked. Agents must
not assume another machine has `AGENTS.md`.

**Reconsider if:** a second machine or contributor joins, or the working copy is lost.

---

## D-10 — Build output is `GrannyRacer.exe`

**Date:** 2026-07-31
**Status:** Accepted — user confirmed when asked directly
**Supersedes:** the build/product-name half of [D-02](#d-02--naming-split-between-product-and-code)

The Windows build produces `GrannyRacer.exe`, matching the namespace and asset root rather
than the repository folder name.

**Why:** asked directly; the user chose to follow playbook §1.1, which mandates `GrannyRacer`
as the repository, namespace, build, and documentation name until a final title is selected.

**Unchanged from D-02:** C# root namespace `GrannyRacer`, asset root `Assets/GrannyRacer/`,
assembly prefix `GrannyRacer.`. Only D-02's "product / build name = `Granny Drivers`" row is
superseded.

**Implemented in:** `Assets/GrannyRacer/Scripts/Editor/BuildCommands.cs` (`ExecutableName`),
consumed by `Tools/unity-build.ps1`.

---

## D-11 — Merged Granny model and reaction audio are admitted to the POC

**Date:** 2026-08-01
**Status:** Accepted — user explicitly requested integration after merging the asset PRs
**Supersedes:** the blanket art/audio deferral in [D-03](#d-03--poc-scope-milestones-0-1-2-3-5), only for the assets listed here

The single-racer POC uses the merged Granny/walker FBX, its idle, drive, turn, boost, and
hit-reaction clips, the three merged slipper variants, and the merged input/collision voice
banks. Slippers disappear during burnout and cycle to the next variant after replacement.

Combat and items remain deferred, so their imported clips and voice banks stay dormant.
Environment art, VFX, music, mixer work, AI, and general milestone-8 polish also remain
deferred. This is an integration of already-approved assets, not permission to expand the
POC into a polish milestone.

**Gate:** visual clarity, animation quality, voice quality, repetition, mix, humour, and
audio fatigue require human review in the POC playtest.

---

## D-12 — POC admits jump, skid, and state-driven propulsion/footwear VFX

**Date:** 2026-08-01
**Status:** Accepted — user explicitly requested the feature set
**Supersedes:** the remaining VFX/animation deferral in [D-03](#d-03--poc-scope-milestones-0-1-2-3-5), only for the items listed here

The single-racer POC includes a small grounded jump, an automatic brake-and-steer skid,
rocket flame/smoke during boost, slipper smoke from the warning heat stage onward, and
temporary road marks from both slippers while skidding. Jump uses Left Shift / controller
right shoulder so the existing Space / controller A boost mapping remains unchanged.

Jump and skid use code-driven additive poses over the existing rig as placeholders. No new
hero animation FBX is approved by this decision. VFX use generated URP materials and particle
systems wired by `PocSceneBuilder`; no new package is required. Final timing, clarity,
appearance, humour, and feel require the human POC playtest.

---

## D-13 — POC adds a charged hop-drift with a tiered exit boost

**Date:** 2026-08-01
**Status:** Accepted — user explicitly requested the feature set
**Extends:** [D-12](#d-12--poc-admits-jump-skid-and-state-driven-propulsionfootwear-vfx)

The hop is now also a drift entry, in the Mario Kart idiom. Hopping (Left Shift / right
shoulder) while steering above `driftMinimumSpeed` commits the walker to a drift in the
steered direction; holding the button keeps it alive, releasing it ends it. Charge is banked
only while grounded, faster when steering into the drift than when counter-steering, and
passes through three tiers — **red, then yellow, then blue** — each releasing a larger exit
boost. Colours follow the user's brief, not Mario Kart's blue/orange/purple.

The brake-and-steer skid from D-12 is kept as a separate, uncharged slide. Both raise
`IsSkidding`, so one set of marks, smoke, and braced poses serves both.

The drift rules live in `DriftModel`, a plain C# class with no `MonoBehaviour` dependency,
tuned by a `DriftTuning` struct built from `WalkerHandlingSettings`. This keeps the tier
timings and boost payouts EditMode-testable, per the same rule that governs slipper heat.

Consequences accepted:

- `RacerInputState` gains `JumpHeld`. The hop cannot be a drift without a held state.
- The exit boost temporarily raises the speed cap to `boostMaximumSpeed` and suspends
  rolling resistance; otherwise the normal cap claws the reward straight back.
- Tier timings, boost strengths, and whether the ~1 s hop hang time makes drift entry feel
  sluggish are tuning questions for the human playtest, not settled by this decision.

---

## D-14 — Walker VFX are aimed and scaled in world space, not in rig space

**Date:** 2026-08-01
**Status:** Accepted
**Amends:** the VFX wiring in [D-12](#d-12--poc-admits-jump-skid-and-state-driven-propulsionfootwear-vfx)

The first VFX pass was wired correctly but invisible in play. Emitters parented to the
Granny rig were given `localScale = 0.01` to cancel the rig's 100x bone scale, but a Unity
particle system defaults to **Local** scaling mode, which reads only its own transform and
ignores its parents. The compensation therefore applied without anything to compensate for:
a 0.13 m flame rendered at 1.3 mm, travelling at 4.8 cm/s.

The rule adopted here: **never scale a particle emitter to compensate for a parent bone.**
Emitters keep `localScale = 1` with an explicit `ParticleSystemScalingMode.Local`, so every
size and speed in `PocSceneBuilder` is a literal metre value.

Two related fixes follow from the same cause — presentation attached to an animated rig
cannot trust the rig's axes:

- Emitters are aimed in world space every frame (rockets backward, smoke up) instead of
  firing down a bone axis, and smoke uses negative gravity to rise. A cone aimed down a
  socket's local Z sprayed into the road on some animation frames.
- Skid marks are parented to the racer root and projected onto the ground under each
  slipper by a ray cast, rather than parented to a foot. A trail on a foot draws in mid-air
  as the leg lifts, and its `TransformZ` alignment turned the ribbon edge-on to the camera.

Marks last 4 seconds by the user's brief. A generated soft radial sprite is assigned to all
VFX materials; untextured URP particles render as hard white squares.

Whether the effects now read clearly at speed is a human playtest question.

---

## D-15 — Generated assets are seeded by the builder, then owned by the human

**Date:** 2026-08-01
**Status:** Accepted
**Amends:** the material generation in [D-12](#d-12--poc-admits-jump-skid-and-state-driven-propulsionfootwear-vfx) and [D-14](#d-14--walker-vfx-are-aimed-and-scaled-in-world-space-not-in-rig-space)

`PocSceneBuilder` writes a generated asset's tuning values **only when it creates the asset**.
On a rebuild it refreshes structure — shader, textures, wiring — and leaves the eyeballed
values alone. This covers the VFX materials and the `TrackDefinition` waypoints. Deleting the
asset is how you ask for the generated defaults back.

**Why:** the two were in conflict. Commit `9726f3a` moved the boost flame off additive
blending after human review; the next scene rebuild silently set it back, because
`LoadOrCreateVfxMaterial` reapplied `_DstBlend` unconditionally. Anything a human tunes by eye
cannot also be owned by a generator that runs on every rebuild.

The same reasoning gives the track layout two commands rather than one:
`Rebuild Quiet Sunday Track Layout` regenerates the waypoints from
`QuietSundayLayout.ControlPoints()`, and `Create Complete Single-Racer POC` does not.
[D-04](#d-04--track-is-generated-from-waypoints-in-editor) makes dragging waypoints the fast
iteration loop for the revision pass, which only works if a rebuild preserves the drags.

**Cost accepted:** a stale asset can drift from the code that generated it, and nothing
detects that. The rebuild commands are the reset button, and they are documented in
`Docs/POC_TRACK_LAYOUT.md`.

---

## D-16 — Granny identity is data layered over one racer controller

**Date:** 2026-08-01
**Status:** Accepted — user explicitly requested selectable grannies with different driving stats
**Extends:** [D-05](#d-05--two-forward-compatibility-constraints-are-honoured-from-the-start)

Every granny uses the same `ArcadeWalkerController` and base `WalkerHandlingSettings`.
A selectable `GrannyRacerProfile` supplies multipliers for acceleration, adherence (lateral
grip), and maximum speed. The multipliers also affect the equivalent reverse and boost
values so a character keeps her identity throughout the driving loop. With no profile, all
multipliers are 1.0 and the existing POC behaviour is unchanged.

The full cast in playbook §29.1 remains full-game content. The POC admits only the reusable
profile contract and placeholder profile assets needed for tuning. Roster UI, persistence,
portraits, and additional finished models stay deferred until their milestones are approved.

Profile differences require human playtesting for readability, balance, and feel.

---

## D-17 — Countdown throttle timing selects the launch

**Date:** 2026-08-01
**Status:** Accepted — user explicitly requested a Mario Kart-style starting technique
**Extends:** [D-03](#d-03--poc-scope-milestones-0-1-2-3-5)

The POC countdown now records the player's first accelerate press and requires accelerate
to remain held through GO. The perfect window is exactly 0.75 seconds, from 2.75 seconds
remaining until the display reaches 2. Pressing earlier produces a slower wheelspin launch
with an automatic alternating swerve; pressing from 2 down to GO produces a smaller boost;
releasing before GO produces no launch effect. An early press remains an early press if the
player releases and tries again during the same countdown.

The timing rules live in the scene-free `RaceStartModel`. Launch impulse, reward duration,
wheelspin duration, acceleration penalty, speed penalty, and swerve are exposed on
`WalkerHandlingSettings` for the required human tuning pass. Existing rocket VFX represent
the two rewarded starts, while the existing drift smoke, skid ribbons, and braced pose
represent wheelspin. The current traffic light is a runtime-drawn HUD placeholder, labelled
as well as coloured for readability.

Final timing legibility, vehicle feel, visual clarity, and humour require human playtesting.

---

## Outstanding decisions

Not yet decided. Listed so they are not forgotten.

| Topic | Notes |
|---|---|
| Licence | `LICENSE` is listed in playbook §6 but no licence has been chosen. Blocks any public release. |
| Final title | Playbook §1.1 lists alternatives. Only affects display name per D-02. |
| Attack input mapping | Reserved in the input map but unimplemented until milestone 4. Playbook §14.1 offers two schemes: two buttons, or one button plus steering direction. |
| `.cursor/rules/` | Playbook §8.3 prescribes five rule files. Deferred — unclear whether Cursor is in use, and duplicating `AGENTS.md` invites drift. |
| Fixed timestep | Playbook §11 milestone 1 requires consistent behaviour across frame rates. Value not yet chosen. |
| ~~Agent scaffolding excluded from version control~~ | Resolved by [D-09](#d-09--agent-scaffolding-stays-git-ignored-confirms-the-open-question-below). Confirmed intentional. |

### Agent scaffolding is currently git-ignored

**Noted:** 2026-07-31

`.gitignore` lines 109–119 exclude `.project/`, `.claude/`, `AGENTS.md`, `CLAUDE.md`,
`Tools/`, `Blender/Source/`, `Blender/Scripts/`, `Blender/Exports/`, `Blender/Renders/`, and
`Blender/Reports/` under the heading "Local project-construction and agent scaffolding".

This looks deliberate, and keeping agent scaffolding out of the shared history is a
legitimate choice. Recording it because it has consequences that should be chosen knowingly:

- It contradicts playbook §7.1, which commits `Tools`, and §6, which tracks `AGENTS.md`,
  `CLAUDE.md`, and Blender source through LFS.
- It breaks playbook §11 milestone 0's acceptance criterion — *"a clean clone can be
  configured using documented local settings"* — because a clean clone would contain no
  `AGENTS.md`, no playbook, and no build or test scripts.
- `Blender/Source/` being ignored means `.blend` files never reach Git LFS, which makes
  [D-06](#d-06--png-files-are-tracked-in-git-lfs) and playbook §7.2 moot for 3D source.
- Losing the working copy would lose the playbook and every decision recorded outside `Docs/`.

`Docs/` is not excluded, so decisions and plans are tracked either way.

**No action taken** — `.gitignore` is outside this session's assigned task. Raised for the
user to confirm or correct.
