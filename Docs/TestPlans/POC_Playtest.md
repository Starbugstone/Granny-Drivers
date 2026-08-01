# Single-racer POC playtest

This is the approved single-racer POC spanning milestones 1, 2, 3, and 5. It includes
walker handling and recovery, boost and slipper heat, burnout replacement, a waypoint-driven
greybox loop, ordered checkpoints, three laps, countdown, wrong-way warning, finish, pause,
and restart. By decision D-11 it also uses the merged Granny/walker model, locomotion
animations, slipper variants, and reaction voice clips. Combat, AI, environment art, VFX,
music, and general polish remain deferred.

## Run it

The current Windows build is `Builds/Windows-Development/GrannyRacer.exe`.

To refresh the generated content in Unity 6000.4.4f1:

1. Open the project and wait for compilation.
2. Select **Granny Racer > POC > Create Complete Single-Racer POC**.
3. Open `Assets/GrannyRacer/Scenes/Tracks/POC_QuietSunday.unity`.
4. Press Play.

The generator rebuilds the scene and generated road mesh while preserving the handling,
heat, and waypoint tuning assets in `Assets/GrannyRacer/Settings/`.

## Controls

- W / Up: accelerate
- S / Down: brake and reverse
- A/D or Left/Right: steer
- Space: boost; tap repeatedly during burnout to shorten slipper replacement
- Left Shift: small jump; hop while steering at speed and hold to drift, release to boost
- R: reset to the latest valid checkpoint
- Escape: pause
- Controller: right trigger accelerate, left trigger brake/reverse, left stick steer,
  A boost, right shoulder jump, Y reset, Menu pause

Q/E and controller X/B are reserved for deferred combat and intentionally do nothing.

## Human acceptance pass

1. Drive continuously for five minutes and confirm the racer never falls through the track.
2. Check that low-speed correction is easy and top-speed steering stays controllable.
3. Confirm keyboard/controller switching works without restarting.
4. Judge camera comfort at 30, 60, and uncapped frame rates.
5. Confirm kerbs produce readable disruption without commonly ending the run.
6. Confirm ordinary impacts wobble visibly and recovery remains generous.
7. Decide whether short boost taps are useful and continuous boost creates understandable risk.
8. Trigger burnout, verify the racer keeps moving, and judge whether replacement is funny or annoying.
9. Confirm the slippers disappear during burnout and a new variant appears after replacement.
10. Confirm idle, drive, steering, boost, and collision reactions select the expected animation.
11. Trigger repeated boosts and collisions; judge voice clarity, repetition, mix, and fatigue.
12. Boost and confirm both rockets emit flame and smoke; cross the warning heat threshold and
    confirm smoke begins at both slippers.
13. Jump from flat road and judge the height, landing stability, and Granny's tucked pose.
14. Brake while steering above skid speed; confirm the braced pose and two road marks start and stop cleanly.
15. Hop with Left Shift while steering at speed, hold it, and confirm the walker commits to a
    drift on that side and that counter-steering opens the line without flipping it.
16. Hold a drift through all three tiers; confirm the slipper smoke turns red, then yellow,
    then blue, and that the debug HUD tier agrees with the smoke.
17. Release at each tier in turn and judge whether the boost is felt and whether the three
    strengths are distinguishable. Record whether the ~1 s hop hang time makes entry sluggish.
18. Watch the skid marks: they must lie flat on the road, start and stop with the drift, and
    fade out about 4 seconds later. Reset mid-drift and confirm no mark is dragged across the map.
19. Try skipping checkpoints; lap progress must not advance.
20. Complete three laps, verify the finish time, and restart without relaunching.
21. Reverse around the course long enough to confirm the warning is sustained rather than noisy.
22. Record promising values from `WalkerHandling_POC` and `SlipperHeat_POC` before further scope.

Answer the playbook questions explicitly: did the walker feel promising within one minute,
was boost/heat understandable, was burnout funny or annoying, did reset feel fair, and did
the camera cause discomfort?

## Required editor layer setup

The generated POC intentionally stays on Unity's Default layer so it is immediately runnable.
Before authoring reusable colliders or prefabs, configure the playbook §17.6 layers manually:

1. Open **Edit > Project Settings > Tags and Layers**.
2. Add these user layers in order: `Racer`, `Track`, `SoftObstacle`, `HardObstacle`,
   `DynamicHazard`, `AttackHitbox`, `AttackHurtbox`, `Trigger`, `Pickup`, `ResetVolume`,
   `Decoration`.
3. Open **Edit > Project Settings > Physics**.
4. In the Layer Collision Matrix, disable `Decoration` against `Racer`; disable `Trigger`,
   `Pickup`, and `ResetVolume` against everything except `Racer`; keep `AttackHitbox` and
   `AttackHurtbox` disabled until combat resumes.
5. Save the project and record the final matrix in `Docs/DECISIONS.md` before assigning
   layers to generated objects.

This setup is deliberately not guessed through YAML editing because layer indices and the
collision matrix are project-wide serialized references.

## Known prototype gaps and deferrals

- Only the merged Granny model, its locomotion/hit animations, slipper variants, and
  input/collision voice banks are integrated under D-11. Smoke/VFX, music, mixing, final UI,
  final art direction, and the dormant combat/item clips remain deferred.
- Position is correctly 1/1 for this single-racer POC. Fractional spline position sorting is
  deferred until AI racers are admitted after the revision gate.
- Input actions are remappable in code, but the settings/rebinding UI is not yet authored.
- Camera comfort, fixed-timestep feel, humour, visual clarity, and fun require this human pass.
- Collision layers require the manual setup above.
