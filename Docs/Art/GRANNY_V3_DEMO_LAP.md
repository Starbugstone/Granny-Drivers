# Granny v3 — demo circuit lap

The owner approved proceeding from the Blender review to a full-lap video of the new granny
on the demo circuit. This integrates the staged character and its facial animation into the
existing Unity demo. It does not rebuild the circuit or add opponents.

## Scope

This demonstrates the current demo's core driving loop. Human milestone acceptance remains
pending. A placeholder would not satisfy the request to see the new character in gameplay.
Maintenance is bounded to a targeted model importer, a model postprocessor, a facial binding
test and an opt-in development capture component. No full-game systems or packages are added.

## Integration

`GrannyV3Importer.ImportAndAudit` copies the ten validated FBXs and character palette through
the existing staging pipeline. Unity's import APIs configure Generic rig/Avatar reuse,
metre scale, imported normals, calculated tangents and blendshape normals, four skin
influences, no compression, no generated colliders, explicit palette materials, and clip
loop flags. Existing FBX GUIDs are preserved. The original walker palette remains mapped.

The existing scenes place their Animator on `GrannyRig`. The new body and face meshes
initially imported as siblings of that object, so facial curves could not reach them.
`GrannyV3ModelPostprocessor` reparents the base model's skinned renderers under `GrannyRig`
while preserving their world transforms. Mesh-bearing FBX clips also retain an extra
`GrannyRig/` prefix on bone curves compared with the previous skeleton-only clips. The
postprocessor removes that prefix and discards constant object-placement tracks, leaving
bone and facial curves relative to the existing Animator. Bones and sockets retain their names; scene geometry and layout are unchanged.

`GrannyFacialAnimationTests` exercises the actual demo scene and checks eye rotation,
complete idle blink, determined driving and delighted boost on the facial renderer.
`GRANNY_V3_UNITY_IMPORT.txt` records imported meshes and facial curve paths.

## Recording method

The development player accepts `-granny-lap-review <fresh-output-directory>`. The opt-in
`FullLapReviewCapture` component follows the generated road with virtual gamepad throttle,
brake and steering, using the normal racer input and Rigidbody controller. Brief boost
inputs are allowed on straights. It never teleports the racer or advances checkpoints.

The chase camera and actual HUD remain visible. A second live camera shows Granny's face
in a corner inset. The capture records 1280 × 720 frames at 30 simulation frames per second,
including countdown, the complete ordered lap and two seconds after crossing the line.
It is an automated driving demonstration, not a human playtest or a real-time performance
benchmark. The video has no audio track.

Completion is read from the race's lap counter, and the capture records gate order, gate
times, maximum speed, road distance and maximum facial weights. The encoder requires a
successful lap and all source frames, then decodes the complete MP4 to verify its duration,
frame count, resolution and H.264 encoding.

## Reproduction

With Unity closed:

```powershell
& ./Tools/unity-run.ps1 -Method GrannyRacer.Editor.GrannyV3Importer.ImportAndAudit -Graphics
& ./Tools/unity-test.ps1 -Mode EditMode
& ./Tools/unity-test.ps1 -Mode PlayMode
& ./Tools/unity-build.ps1
& ./Tools/capture-granny-lap.ps1
# Run Python with ffmpeg on PATH, using the capture directory printed above:
python Tools/assemble-granny-lap.py Builds/GrannyLap-<timestamp>
```

Do not run the older **Import Blender Refresh and Build Scene** menu command: it copies
older character exports over the new assets and rebuilds the scene.

## Review artifacts

- [Full lap MP4](Previews/Granny_v3_Lap/granny_v3_demo_full_lap.mp4)
- [Start](Previews/Granny_v3_Lap/01_start.jpg)
- [Driving](Previews/Granny_v3_Lap/02_racing.jpg)
- [Completed lap](Previews/Granny_v3_Lap/03_finish.jpg)
- [Capture and full-video verification](GRANNY_V3_LAP_CAPTURE.json)

Human review of appearance, handling, humour and in-editor gameplay remains required.

## Initial validation failures

The first full PlayMode run passed 43/46 tests. These failures identified the extra
armature prefix on the bone animation curves; it was corrected in the postprocessor:

```text
Eye gaze curve must animate the rig.
Expected: greater than 3
But was: 0.0f

Drive must visibly animate the legs; Foot_L moved only 0,000 degrees.
Expected: greater than 0.25f
But was: 0.0f

Idle must visibly animate the rig; Head moved only 0,000 degrees.
Expected: greater than 0.25f
But was: 0.0f
```

Initial run: `Logs/test-results-playmode-20261002-213243.xml`.

The corrected animation fixtures passed **11/11** in
`Logs/test-results-playmode-20261002-214133.xml`. The final EditMode run passed **95/95**
in `Logs/test-results-editmode-20261002-214244.xml`.

Windows development build succeeded with **0 errors and 1 warning**, 226 MB:
`Logs/unity-build-development-20261002-214421.log`. The warning concerns optional
Unity Cloud native-symbol upload:

```text
Access token is empty. Native symbols will not be uploaded for this build. Please make sure you are signed in to the Unity Cloud.
```

## Captured lap

The standalone player exited with code 0 after a **101.865 s** physical lap. Its checkpoint
sequence was `1 → 2 → 3 → 4 → 5 → 6 → finish → 1` (the final 1 is the next lap's first gate).
Maximum sampled speed was **13.213 m/s**; maximum distance from sampled road centreline
points was **1.868 m**. No reset or position teleport is used by the recorder.

The recording contains **3,207 frames** at **1280 × 720, 30 fps**, or **106.9 seconds**
including countdown and two seconds after the finish crossing. Recorded facial weights
confirm the actual player reached determined driving (80%), delighted boost (85%),
surprise (100%) and closed blinks (approximately 100%).

Player log: `Logs/granny-lap-20261002-214814.log`. Raw frames and capture completion report:
`Builds/GrannyLap-20261002-214814/`. These intermediate files are git-ignored.

The final full PlayMode run passed **44/46**. The granny animation tests and physical lap
tests passed, but two existing tests failed while capture/encoding work was also running:

```text
NeighborhoodPresentationTests.BakedBlenderPropsAnimateAndPauseWithTheRace
Expected: less than 0.100000001f
But was: 0.978779137f

WalkerDriveTests.DriftingPaintsSkidRibbonsThatFadeAwayAfterTheDrift
Expected: False
But was: True
```

Run: `Logs/test-results-playmode-20261002-214948.xml`. The cause is not established by this
run; a separate targeted rerun is recorded below. No unrelated gameplay code was changed
to make these assertions pass.

The first video encode stopped after 1,738 frames despite reporting process success:
`Error during demuxing: Cannot allocate memory`. The full-decode frame-count assertion
rejected that incomplete video. The encoder now limits its decoder/encoder thread count
and uses `-xerror`, in addition to requiring the full expected frame count.

The separate rerun passed **1/2**: the drift-release test passed, while the prop-pause
test still failed with:

```text
Expected: less than 0.100000001f
But was: 0.353877425f
```

Rerun: `Logs/test-results-playmode-20261002-215810.xml`. This scenery-animation pause
issue remains unresolved; it does not invalidate the captured racing lap. The full
suite is **not** claimed to pass. See the [consolidated validation record](GRANNY_V3_DEMO_VALIDATION.json).

A second image2 encode, with reduced threads and fail-on-error enabled, reported the same
`Error during demuxing: Cannot allocate memory` and exited unsuccessfully. The final
assembly path instead feeds the exact ordered JPEG bytes through a bounded MJPEG pipe,
avoiding the image2 reader's repeated file opens on the Windows mount. The full-frame
validation remains mandatory; incomplete outputs are never accepted as the lap video.

The first piped attempt still used Linux reads of the Windows files and failed with
`OSError: [Errno 12] Cannot allocate memory`. The final feeder,
`Tools/stream-granny-frames.ps1`, reads each JPEG using a bounded Windows-native FileStream
and writes its exact bytes to the encoder pipe. No captured frames are replaced or synthesized.

Final video verification **passed**: all **3,207 frames** decoded, H.264/yuv420p at
1280 × 720 and 30 fps, 106.9 seconds. The MP4 is 43,171,646 bytes. Its final encoded frame
was inspected with the HUD showing lap two. See `Logs/granny-v3-lap-encode-native.log`
and the linked capture report for the video SHA-256 and full-decode result.
