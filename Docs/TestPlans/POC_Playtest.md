# Single-racer POC playtest

This is the approved primitive-only POC spanning milestones 1, 2, 3, and 5. It includes
walker handling and recovery, boost and slipper heat, burnout replacement, a waypoint-driven
greybox loop, ordered checkpoints, three laps, countdown, wrong-way warning, finish, pause,
and restart. Final art, authored audio, combat, and AI remain deferred by decision D-03.

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
- R: reset to the latest valid checkpoint
- Escape: pause
- Controller: right trigger accelerate, left trigger brake/reverse, left stick steer,
  A boost, Y reset, Menu pause

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
9. Try skipping checkpoints; lap progress must not advance.
10. Complete three laps, verify the finish time, and restart without relaunching.
11. Reverse around the course long enough to confirm the warning is sustained rather than noisy.
12. Record promising values from `WalkerHandling_POC` and `SlipperHeat_POC` before further scope.

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

- Authored audio, smoke/VFX, final UI, and final art are deferred to milestone 8. The HUD and
  burnout foot shuffle are the current placeholder feedback.
- Position is correctly 1/1 for this single-racer POC. Fractional spline position sorting is
  deferred until AI racers are admitted after the revision gate.
- Input actions are remappable in code, but the settings/rebinding UI is not yet authored.
- Camera comfort, fixed-timestep feel, humour, visual clarity, and fun require this human pass.
- Collision layers require the manual setup above.
