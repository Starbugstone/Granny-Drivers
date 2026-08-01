# POC plan

**Goal:** a playable single-racer race — arcade walker physics, boost, slipper heat, and a
greybox track you can drive three laps of. Then stop, playtest, and revise before building
anything else.

**Scope decision:** [D-03](DECISIONS.md#d-03--poc-scope-milestones-0-1-2-3-5).
**Milestones covered:** playbook 0, 1, 2, 3, 5.
**Deferred:** combat (4), AI racers (6), terrain and environment (7), art and audio (8),
items, pickups, multiplayer.

---

## Why this cut line

Playbook §31.1 names the project's largest risk: *the physics never feels fun*. Every hour
spent on characters, AI, or environment art before that risk is retired is an hour that may
need throwing away.

So the POC builds the smallest thing that can answer "is this fun to drive?" — and answers it
with a real track, real speed, and the boost/heat decision loop that gives the handling
something to be tested against.

Boost and heat are in scope despite being milestone 3 because without them the question is
unanswerable. Boost sets the speed envelope the physics has to stay stable across; heat is
what turns boost from a held button into a decision.

---

## Phases

Tasks are tracked in the session task list. IDs below match it.

### Phase 1 — Foundation (tasks 1–4)

Scaffolding, project settings, assembly structure, and the automation wrappers.

Ends when an empty Windows build succeeds from the command line — playbook milestone 0's
acceptance bar. Nothing here is gameplay; it exists so that everything after it is verifiable.

The critical item is the **physics layers** (playbook §17.6). `TagManager.asset` currently
holds only Unity defaults. Collision response, reset volumes, and later the attack
hitbox/hurtbox split all depend on them, and adding layers after colliders exist means
revisiting every prefab.

### Phase 2 — Driving feel (tasks 5–9)

The part that decides whether the game is worth making.

Input abstraction first ([D-05](DECISIONS.md#d-05--two-forward-compatibility-constraints-are-honoured-from-the-start)),
then the Rigidbody arcade controller, the greybox walker and camera, then the walker's
personality — lean, wobble, kerb reaction, recovery — and finally booster and slipper heat.

Two rules throughout:

- **All tuning lives in ScriptableObjects.** The revision pass must not require recompiling.
- **All pure maths is unit-tested.** Steering curves, grip solve, heat accumulation, cooling,
  and burnout thresholds are plain C# with no `MonoBehaviour` dependency. Playbook §13.2
  requires heat to be deterministic and testable; §24.1 lists these explicitly.

### Phase 3 — Track and race (tasks 10–12)

The waypoint generator ([D-04](DECISIONS.md#d-04--track-is-generated-from-waypoints-in-editor)),
then the Quiet Sunday greybox loop, then the race loop.

Track layout follows playbook §17.3: one wide overtaking straight, one bottleneck, one
downhill boost section, one sharp corner where heat management matters, one risky shortcut.
Greybox primitives only — no houses, no terrain, no art.

The layout, the two commands that regenerate it, and the authoring rules are documented in
[POC_TRACK_LAYOUT.md](POC_TRACK_LAYOUT.md).

Race systems are written for N racers from the start ([D-05](DECISIONS.md#d-05--two-forward-compatibility-constraints-are-honoured-from-the-start)),
even though only one exists.

### Phase 4 — Playable (tasks 13–14)

Debug HUD with a live tuning overlay, scenes wired up, a 0.1.0 Windows build, and a
structured playtest brief built from the playbook §24.3 questions.

The tuning overlay is the main instrument for the revision pass. It is not a debug
convenience — it is the deliverable that makes phase 2 revisable.

---

## Definition of done

The POC is done when, on a Windows build:

- A three-lap race can be started, driven, and finished with keyboard **and** controller.
- The walker drives for five minutes without falling through the world (playbook milestone 1).
- Steering stays controllable at top speed.
- Boost is worth using and heat makes it a decision.
- Burnout is recoverable and does not end the race.
- Reset reliably restores the racer to the last valid checkpoint.
- Lap and position readouts are correct.
- Restart works without relaunching.
- EditMode tests pass for handling maths, heat model, and race progress.

Explicitly **not** claimed at this point: that it looks good, or that it is fun. Those need
a human playtest — playbook §24.1 and the honesty rules in `AGENTS.md`.

---

## The revision gate

After the build exists, the next step is **not** the next milestone. It is a playtest pass
using `Docs/TestPlans/POC_Playtest.md`, then handling revision.

Deferred work starts only once the driving is judged promising. Playbook §11 milestone 2
puts it plainly: human feedback must describe the movement as *fun or promising*, not merely
functional.

If the answer is "it does not feel good", the correct response is to revise the controller —
not to proceed and hope that art fixes it.

---

## Out of scope

Anything not listed above. When something new comes up, apply the five questions from
playbook §31.6 and, if it is full-game work, record it in [FULL_GAME_BACKLOG.md](FULL_GAME_BACKLOG.md)
rather than building it.
