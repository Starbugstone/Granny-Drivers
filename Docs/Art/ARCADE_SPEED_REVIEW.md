# Arcade speed and slipper heat review

The owner's feedback was that the demo looked too slow and the brown path covered the
road. The previous automated recording used a roughly 8 m/s throttle target and 9 m/s
braking threshold. This pass changes the actual shared gameplay tuning as well as the
review driver; the old video remains available for comparison.

## Changes

| Setting | Previous | New |
| --- | ---: | ---: |
| Base cruise speed | 15 m/s (54 km/h) | 24 m/s (86.4 km/h) |
| Base rocket speed | 22 m/s (79.2 km/h) | 35 m/s (126 km/h) |
| Acceleration / boosted acceleration | 24 / 38 m/s² | 36 / 52 m/s² |
| Steering rate / high-speed fraction | 125°/s / 0.42 | 155°/s / 0.70 |
| Lateral grip | 7 | 12 |
| Heat gain | 31%/s | 24%/s |
| Cooling delay / rate | 0.55s / 20%/s | 0.20s / 32%/s |
| Automatic slipper replacement | 3.4s | 2.4s |
| Burnout speed fraction | 0.38, acceleration ceiling only | 0.55, active deceleration |

Existing granny profile multipliers still apply. Fixed-step force integration and external
impulses can briefly exceed the nominal ceiling. Releasing boost eases excess forward speed
back toward cruise at 8 m/s². Burnout applies up to 18 m/s² deceleration, and can no longer be
cancelled by a pad or drift impulse. Critical heat applies an additional 0.88 grip multiplier.
Existing hop/drift controls, charge tiers, slipper swapping and animated facial controls remain.

The camera opens from its resting FOV to 74° at cruise and 82° on boost, lowers slightly,
looks ahead and compensates follow lag so Granny remains in view at speed. The HUD exposes
boost/cooling/burnout states, 60%/80% heat markers and a pulsing critical warning. Rolling and
rocket audio pitch now scales against the configured boosted speed.

`DrivewayGeometry` subtracts the road triangle footprint from the garden path before
triangulation. The surface meets the road edge and ramps to the raised garden/sidewalk.
`ArcadeDemoTuning.Apply` updates the existing shared settings and generated driveway mesh
through Unity APIs without reauthoring scenes, changing asset GUIDs or replacing character art.

## Reproduction

Close Unity before batch commands:

```powershell
& ./Tools/unity-run.ps1 -Method GrannyRacer.Editor.ArcadeDemoTuning.Apply
& ./Tools/unity-test.ps1 -Mode EditMode
& ./Tools/unity-test.ps1 -Mode PlayMode
& ./Tools/unity-build.ps1
& ./Tools/capture-granny-lap.ps1
```

Then, with Python and FFmpeg on PATH:

```text
python3 Tools/assemble-granny-lap.py Builds/GrannyLap-<timestamp> Docs/Art/Previews/Granny_Arcade_Lap Docs/Art/GRANNY_ARCADE_LAP_CAPTURE.json
```

The development capture supplies virtual gamepad input only. It does not alter the racer
position, velocity, heat or checkpoint counters. A curvature preview determines braking,
straights use sustained rocket input, and after a first burnout the driver releases at 88%
and resumes below 25%. Capture time advances at 30 simulation frames per second; this is
not a performance benchmark. A live camera inset shows the actual face and eye animation.
Unity's [AudioRenderer](https://docs.unity3d.com/6000.0/Documentation/ScriptReference/AudioRenderer.html)
records the game's mixed output alongside those simulation frames. The assembler checks
ordered gate completion, all source frames, audio duration and full MP4 decoding.

## Validation

- Mesh application completed: one existing garden driveway repaired in place.
- EditMode: 96/96 passed, including the path/asphalt intersection regression.
  `Logs/test-results-editmode-20261002-223646.xml`.
- Initial targeted PlayMode: 2/2 passed. Fast physical lap 29.47s, peak 36.06 m/s,
  maximum sampled centreline distance 2.25m, maximum heat 100%.
  `Logs/test-results-playmode-20261002-223431.xml`.
- The separate heat test accelerates on an isolated long runway, reaches burnout, measures
  the actual speed reduction while still moving, rejects boost-pad bypass, then verifies
  fresh slippers restore boost speed. It never injects a test velocity.

Full regression results, build and final recording measurements follow below.
Human judgement of fun, handling, visual quality and audio quality remains pending.


### Full regression and correction

The first full PlayMode run passed **47/48**:
`Logs/test-results-playmode-20261002-223833.xml`. The earlier decorative-prop pause and
skid-ribbon release failures both passed in this isolated run. One assertion used the old
fixed cruise speed instead of the configured tuning:

```text
CircuitDrivingTests.LawnSlowsTheRacerAndABoostCanCarryItAcross
Expected: less than 10
But was: 10.819994f
```

The test now starts at the configured cruising speed and requires the lawn to shed at least
35% of it, then requires a boost pad to add more than 3 m/s. The gameplay lawn multiplier
remains 0.55. This preserves the test's slowdown/boost contract across handling presets.
Queued pad impulses are also discarded if burnout begins before the next physics step.


### Final code and gameplay checks

- **EditMode 96/96 passed**: `Logs/test-results-editmode-20261002-224613.xml`.
- **PlayMode 48/48 passed**: `Logs/test-results-playmode-20261002-224653.xml`.
- Final fast-lap test: 29.43s, 36.06 m/s peak, 100% maximum heat, 2.25m maximum
  distance from sampled road-centre points. The controller crossed all ordered gates.
- The updated lawn test, burnout recovery, camera framing contract, facial/eye animations,
  drift charging/release/skid fade, shortcut route and all three circuit lap tests passed.


The first standalone recording completed a 30.10s lap and exposed a clipped critical-heat
label. Its rectangle was widened from 380 to 530 reference pixels before the final capture.
This UI-only correction passed EditMode **96/96** (`Logs/test-results-editmode-20261002-225913.xml`)
and the focused camera scene check **1/1** (`Logs/test-results-playmode-20261002-230104.xml`).

Unity audio mixing ends on DSP block boundaries. The assembler permits at most one video
frame of residual audio length and pads/trims only the tail to the exact video duration;
it records that adjustment in the capture report and fully decodes both streams.


A second capture (`Builds/GrannyLap-20261002-230423`) remained stationary after the player
lost foreground focus; its virtual gamepad was disabled by the default Input System
background policy. The task-owned capture player was stopped (wrapper reported
`Player exit code: -1`); those incomplete frames are not a deliverable. Recording mode now
sets `InputSettings.BackgroundBehavior.IgnoreFocus` in player memory only and aborts if
no motion is observed within five racing seconds. The normal game uses its existing focus
policy. This capture-only fix passed EditMode **96/96**
(`Logs/test-results-editmode-20261002-230642.xml`); actual standalone capture supplies its
end-to-end verification because the recorder deliberately does not start inside the editor.


## Delivered recording and build

- [Full uninterrupted lap with game sound](Previews/Granny_Arcade_Lap/granny_v3_demo_full_lap.mp4)
- [Boost at 129 km/h](Previews/Granny_Arcade_Lap/04_boost.jpg)
- [Repaired path junction and readable critical-heat warning](Previews/Granny_Arcade_Lap/05_repaired_junction.jpg)
- [Slipper burnout and replacement](Previews/Granny_Arcade_Lap/06_burnout.jpg)
- [Completed lap](Previews/Granny_Arcade_Lap/03_finish.jpg)
- [Capture telemetry, video hash and complete media validation](GRANNY_ARCADE_LAP_CAPTURE.json)
- [Test/build validation manifest](GRANNY_ARCADE_VALIDATION.json)

The final standalone capture completed **one 30.100s lap** with ordered gate sequence
`1 → 2 → 3 → 4 → 5 → 6 → finish → 1`. Peak speed was **36.051 m/s (129.8 km/h)**,
mean sampled driving speed **28.267 m/s (101.8 km/h)** and maximum distance from sampled
road-centre points **2.183m**. The prior low-throttle recording took 101.865s; the new result
reflects both the gameplay tuning and the removal of the review driver's artificial cap.

The lap includes **17.467s boosting**, **8.333s decreasing heat**, and **one burnout**. Peak
heat reaches 100%. Granny's actual animation renderer reaches determined driving (80%),
delighted boosting (85%), surprise (100%) and full blinks. These are recorded runtime values,
not authored still-image substitutes or a claim of human approval.

The MP4 contains **1,054 frames**, **1280×720 at 30 fps**, lasting **35.133s** including the
countdown and two seconds beyond the finish. Every video frame and the AAC stereo audio
stream decoded successfully. The source audio tail needed 18.667ms of silence to align the
DSP block boundary with the video end. Decoded audio duration is 35.136s; measured encoded
peak is -9.37 dBFS and RMS -27.10 dBFS, with nonzero signal and no digital clipping.
Visual inspection confirmed the warning text fits, the path stays outside the asphalt,
and the lap counter advances to 2/3. Human quality judgement remains pending.

Raw capture: `Builds/GrannyLap-20261002-230842/` (ignored).
Player log: `Logs/granny-lap-20261002-230842.log`, exit 0.
Encoding log: `Logs/arcade-encode.log`.

The final Windows development build succeeded with **0 errors and 1 warning**, 226.53MB:
`Logs/unity-build-development-20261002-230730.log`. Playable output:
`Builds/Windows-Development/GrannyRacer.exe` (ignored).

The warning is about optional Unity Cloud native-symbol upload:

```text
Access token is empty. Native symbols will not be uploaded for this build. Please make sure you are signed in to the Unity Cloud.
```

No manual scene/import steps are needed for this working copy. Run the updated Windows
player or open the existing `Circuit_SuburbSquare` scene for the owner's handling review.
Keyboard: WASD drive, Space boost / tap during replacement, Shift hop and hold to drift.
Xbox-compatible controller: RT accelerate, LT brake, left stick steer, A boost, RB hop/drift.
A human playtest and human judgement of appearance, sound and fun are still outstanding;
the milestone is not marked accepted.
