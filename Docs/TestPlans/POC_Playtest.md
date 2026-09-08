# Single-racer POC playtest

This is the approved single-racer POC spanning milestones 1, 2, 3, and 5. It includes
walker handling and recovery, boost and slipper heat, burnout replacement, a waypoint-driven
greybox loop, ordered checkpoints, three laps, countdown, wrong-way warning, finish, pause,
and restart. D-11 and D-18 add the rebuilt Granny/walker, locomotion animations, slipper
variants, reaction voice clips, boost/smoke/skid effects, neighbourhood art and the racing
HUD/pause menu. Combat, AI, multiplayer, music and full-game progression remain deferred.

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
- F1: handling lab; adjust live acceleration, top speed, grip and steering for this session
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
23. Pause during the countdown and while driving; resume and confirm the timer and car position
    remain stable. Try R while paused; it must not recover the racer until normal play resumes.
24. Use the handling lab, restore defaults, and restart. Confirm the underlying tuning asset
    remains unchanged; record preferred values separately.
25. Review the new Granny from the front/back and while boosting: eyes, shoulder continuity,
    hand contact, slipper clearance and rocket exhaust placement. Review all three slippers.
26. Drive past the houses, trees, fences and gate. Check sight lines and whether the scenery
    makes the course easier to read. Record actual frame rate on the intended hardware.

Answer the playbook questions explicitly: did the walker feel promising within one minute,
was boost/heat understandable, was burnout funny or annoying, did reset feel fair, and did
the camera cause discomfort?

## Collision layers

The refresh importer configures the eleven playbook layers through Unity editor APIs.
Racer/Track/Trigger/Decoration are assigned in the generated scene. Decoration and reserved
attack layers collide with nothing; triggers, pickups and reset volumes interact only with
Racer. The checked-in project settings retain this setup on fresh clones. See D-18.

## Known prototype gaps and deferrals

- The art direction is approved; the delivered meshes, animation and handling still need
  human acceptance. Music, a complete sound mix and dormant combat/item clips remain deferred.
- Position is correctly 1/1 for this single-racer POC. Fractional spline position sorting is
  deferred until AI racers are admitted after the revision gate.
- Input actions are remappable in code, but the settings/rebinding UI is not yet authored.
- Camera comfort, fixed-timestep feel, humour, visual clarity, and fun require this human pass.
- Existing voice asset provenance and a project licence remain unresolved; no new licence
  is implied by this art refresh.
