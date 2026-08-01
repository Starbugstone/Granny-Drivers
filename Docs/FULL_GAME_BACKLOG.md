# Full-game backlog

Deferred work. **Nothing here is approved.** Recording an idea here is how it gets kept
without getting built — playbook §31.6 and §33.8.

Before adding, apply the five questions from playbook §31.6:

1. Does this improve the demo's core loop?
2. Is the current milestone already accepted?
3. Can this be represented by a placeholder?
4. Does this create maintenance cost?
5. Is it demo work or full-game backlog?

The playbook already contains a large idea catalogue in §27–§29. This file does not duplicate
it. It records what was **deferred by an actual decision**, so the reasoning survives.

---

## Deferred out of the POC

Deferred by [D-03](DECISIONS.md#d-03--poc-scope-milestones-0-1-2-3-5). These are demo scope,
not full-game scope — they return as soon as the POC playtest is passed.

| Item | Playbook | Why deferred |
|---|---|---|
| Combat — directional elbow/shove | §14, milestone 4 | Needs handling to be settled first. An attack tuned against physics that later change is wasted work. Input is reserved in the action map. |
| AI racers | §16, milestone 6 | Needs a finished racing line and a stable controller. `IRacerInputSource` ([D-05](DECISIONS.md#d-05--two-forward-compatibility-constraints-are-honoured-from-the-start)) is the seam it plugs into. |
| Real terrain and environment | §17, milestone 7 | The greybox answers the layout question. Art on an unproven layout is throwaway work. |
| Characters, animation, final audio | §18–§22, milestone 8 | Playbook §31.1 — no full art until movement is promising. |
| Items and pickups | §29.3 | Not needed to judge the core loop. |
| Menus, settings, pause | §21.5, milestone 5 | Partially in POC scope as a results/restart flow only. Full menu tree deferred. |
| Three additional racers | §10.1 | Race systems are already written for N racers. |

---

## Deferred indefinitely

Full-game scope. Not part of the demo at all.

| Item | Playbook | Note |
|---|---|---|
| Online multiplayer | §28 | Playbook §31.3 — no networking until the offline race is accepted. Keep gameplay logic separable from presentation so this stays possible. |
| Story campaign | §27.4 | |
| Progression and unlocks | §27.3 | Sidegrades only, never stat upgrades that disadvantage new players. |
| Additional tracks | §29.2 | The waypoint generator ([D-04](DECISIONS.md#d-04--track-is-generated-from-waypoints-in-editor)) makes these much cheaper than they would have been. |
| Additional slipper types | §13.5 | The heat model already takes a slipper-type modifier, so this is data, not code. |
| Battle modes | §29.6 | |
| Split-screen | §27.1 | Camera and input are per-racer, which keeps this open. |
| Console ports | §3.3 | Only after controller UX and performance are proven. |

---

## Observations to revisit

Not decisions — things noticed that may matter later.

### Blender pipeline built ahead of need

**Noted:** 2026-07-31

`Tools/blender-*.ps1`, `Blender/Scripts/*.py`, and `PRP_Bin_Wheelie.blend` exist. Per
[D-04](DECISIONS.md#d-04--track-is-generated-from-waypoints-in-editor) the Blender pipeline
is not needed until milestone 7 — the POC greybox is generated in-editor.

No action needed. The work is done and costs nothing to keep. Flagged only because it is the
pattern playbook §31.6 warns about, and because the wheelie bin should not be treated as
approved demo content — it has not had the human art review that playbook §19.5 requires.

### Attack input scheme undecided

**Noted:** 2026-07-31

Playbook §14.1 offers two schemes: separate left/right attack buttons, or one attack button
plus steering direction. Tracked in [DECISIONS.md](DECISIONS.md) outstanding decisions.

The action map reserves the inputs either way, so this does not block the POC.
