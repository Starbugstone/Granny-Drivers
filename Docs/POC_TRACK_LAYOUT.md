# POC track layout — Quiet Sunday

How the greybox circuit is authored, what it contains, and how to change it.

Decision context: [D-04](DECISIONS.md#d-04--track-is-generated-from-waypoints-in-editor).
Route requirements come from playbook §17.3.

---

## The two-stage pipeline

The track is generated twice over, and the stages are separate commands on purpose.

```text
QuietSundayLayout.ControlPoints()   36 authored corner and straight markers
        │
        │  TrackLayoutBuilder.Resample()   centripetal Catmull-Rom, curvature-aware
        ▼
Track_QuietSunday_POC.asset         80 waypoints  ← the file you can drag in the inspector
        │
        │  GreyboxTrackGenerator.Build()
        ▼
POC_QuietSunday.unity               road mesh, kerbs, barriers, checkpoints, shortcut
```

**Stage 1 exists because the waypoint list is the geometry.** The generator draws straight
road quads between consecutive waypoints, so a hairpin described by three waypoints comes
out as a triangle. Rather than hand-authoring eighty points, the layout is authored as three
dozen markers and the builder fills the corners in, spending samples only where curvature
needs them — currently 18 m apart on the straights, down to 2 m through the hairpin.

**Stage 2 is separate so inspector drags survive.** D-04 makes dragging waypoints the fast
iteration loop for the revision pass. Rebuilding the scene does *not* regenerate the
waypoints, so a layout you tuned by hand is not silently thrown away.

---

## Commands

Both are Unity menu items, and both are runnable headlessly.

| Goal | Menu | Headless |
| --- | --- | --- |
| Regenerate the waypoints from code | `Granny Racer/POC/Rebuild Quiet Sunday Track Layout` | `pwsh -File Tools/unity-run.ps1 -Method GrannyRacer.Editor.PocSceneBuilder.RebuildTrackLayout` |
| Regenerate the whole scene | `Granny Racer/POC/Create Complete Single-Racer POC` | `pwsh -File Tools/unity-run.ps1 -Method GrannyRacer.Editor.PocSceneBuilder.CreateCompletePoc -Graphics` |

Changing `QuietSundayLayout.ControlPoints()` requires **both**, in that order. Changing the
waypoints by hand in the inspector requires only the second.

The rebuild command reports its numbers:

```text
[POC] Rebuilt Quiet Sunday: 80 waypoints, 800 m lap, 8 checkpoints,
      tightest corner radius 9.0 m.
```

---

## What is on the lap

800 m over a 234 x 219 m footprint — roughly a minute a lap at the POC's 15 m/s cap, and
about four times the first prototype loop. The size is the point: the prototype was small
enough that every corner ran into the next one's recovery, so a handling change could not be
attributed to anything in particular.

In lap order from the start line:

| # | Feature | Notes |
| --- | --- | --- |
| 1 | Wide overtaking straight | 16 m wide, 136 m before the first braking point |
| 2 | Climbing right-hander | flat out, rising to a 12.5 m crest |
| 3 | Downhill booster section | wide and straight, 12 m of drop over ~95 m — the fastest part of the lap |
| 4 | Hairpin | 7 m wide, 9 m radius, narrowing on entry |
| 5 | Chute and sweeper | fast, opens back out to 10 m |
| 6 | The crescent | fast sweeper bulging 27 m north |
| 7 | Risky driveway shortcut | 5 m wide, straight across the crescent's base, ~20 m shorter |
| 8 | Bottleneck | 6 m wide, ~75 m long, no room to pass |
| 9 | Descending right-left | a change of direction to commit to while losing height |
| 10 | Final corner | 15 m radius left back onto the start straight |

Eight checkpoints, spaced 65–147 m apart, so a reset never costs more than about ten seconds
of redriving. Three laps.

Feature 4 is the one the handling is meant to be judged on. `WalkerHandling_POC` steers
125 deg/s scaled to 0.42 at the speed cap, which is 0.92 rad/s, so the tightest arc the walker
can hold flat out is about 16 m. A 9 m hairpin therefore has to be braked for — which is what
makes slipper heat a decision on the approach rather than a number on the HUD.

---

## Authoring rules learned the hard way

**Describe a corner with several evenly spaced points on its arc, not with one apex marker.**
A lone apex between two distant markers makes the spline pinch. The first pass authored the
hairpin as three points expecting a 16 m radius and got 5.3 m — tight enough that the inner
barrier became a 1 m circle of overlapping boxes.

**Radius and width are not independent.** The inner barrier sits half a road width plus 0.8 m
inside the centreline, so a 15 m wide road on a 10 m radius corner rings it with a 2 m circle.
Keep `radius - (width / 2 + 0.8)` comfortably positive. The rebuild command prints the
tightest radius; the widths are in the control point list next to it.

**The shortcut needs a detour to cut.** A chord across a smooth corner saves almost nothing —
several candidates saved under 5 m. The crescent exists to give the driveway something worth
bypassing.

---

## Guard rails

`Assets/GrannyRacer/Tests/EditMode/` holds the checks that keep the above honest:

- `TrackLayoutBuilderTests` — control points survive resampling at their reported indices,
  lap order is preserved, no gap or facet exceeds its budget, generated points never become
  checkpoints, widths stay in range, degenerate input does not produce NaN.
- `QuietSundayLayoutTests` — the lap is long enough, checkpoints are spread, the route offers
  a wide section, a bottleneck, real elevation, and a corner too tight to take flat out, and
  the shortcut is shorter than the road it bypasses without skipping a checkpoint or removing
  more than half the section.

None of these say the track is *good* to drive. That is a human playtest question —
`Docs/TestPlans/POC_Playtest.md`.
