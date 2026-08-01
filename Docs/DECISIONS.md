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
