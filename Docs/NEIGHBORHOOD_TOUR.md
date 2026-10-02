# Neighborhood tour

Three playable circuits share the original granny, charged drift, timed launch, heat,
boost panels, recovery and ordered forward-only checkpoints. The new routes use square
US suburban blocks with rounded street corners; they do not copy any Nintendo course.

| Track | Route | Landmarks |
| --- | --- | --- |
| Sunday Block Party | Original broad block and narrow driveway cut | Existing ranch/colonial homes |
| Maple Gardens | 828 m L-shaped neighborhood loop, six sectors | Porch homes, parked station wagons, community garden, picnic tables, wind spinners |
| School Run | 1,071 m loop with two inward turns, eight sectors | Brick elementary school, playground slide and animated seesaw |

Open `Assets/GrannyRacer/Scenes/Tracks/Circuit_SuburbSquare.unity` and press Play.
Pause with Esc/controller Menu, select **Choose another track** (T / controller X),
then click the course or use arrows/D-pad and Enter/controller A.
The same selector is available after finishing. Loading resets pause and starts a fresh
countdown. Track selection supports pointer, keyboard and Xbox-compatible controller input.

## Build and edit

Use **Granny Racer → Circuit Builder → Build Neighborhood Tour** to import the staged
Blender kit and build all three scenes. It preserves existing recipe edits and other build
scenes. To edit one route, select its `Circuit_*.asset` in `Assets/GrannyRacer/Settings`,
open the Circuit Builder, edit/validate, and build. `Neighborhood Details` adds private
paths, parked cars and background residential rows. Those rows also respect the race
corridor and authored scenery bounds. Scenery has no collision inside the race corridor.

Blender generation uses `Blender/Scripts/build_neighborhood_expansion.py`; the source is
`Blender/Source/Environment/ENV_NeighborhoodExpansion_v1.blend`. It refuses to overwrite
that source: use a new versioned filename for later regeneration. Stage is
`Blender/Exports/Neighborhood`; actual source previews use
`Blender/Scripts/render_neighborhood_review.py`. These paths are local-only by D-09;
back up the Blender working directory separately. Unity FBX, textures, animations, scenes,
recipes and audit reports are tracked under Assets/ and Docs/.

Generate the original sounds with `python Docs/Audio/generate_arcade_audio.py`, then run
Build Neighborhood Tour to apply deliberate mono PCM/preloaded import settings. Synthesis
is deterministic and uses no external recordings. The pause menu has master/effects
sliders; rolling and ambience gains are serialized on ArcadeAudioPresentation. Event cues
briefly duck continuous loops. Pausing suspends the new sound sources and Blender loops.
Legacy voice files are unchanged. Final sound quality requires listening review.

Editor automation is the only writer of Unity scenes, materials and serialized settings.
Importer choices follow [Unity ModelImporter](https://docs.unity3d.com/6000.0/Documentation/ScriptReference/ModelImporter.html);
Blender exports bake transform animation using the [FBX export API](https://docs.blender.org/api/main/bpy.ops.export_scene.html).

## Content and review limits

The source kit has 14,686 triangles; Unity imports 14,258 after mesh processing. Across
seven assets (nine mesh objects), there is one shared
palette material and one UV layer per mesh. Both animated props have four-second loops.
Animated props each use two mesh slots sharing that same material. Exact source dimensions/materials are in `Art/ENV_NeighborhoodExpansion_v1.validation.json`;
Unity triangle counts, dimensions and animation counts are in `Art/NEIGHBORHOOD_IMPORT_AUDIT.json`.

The sound audit is `Audio/ARCADE_AUDIO_AUDIT.json`: 24 kHz, 16-bit mono, 13 original clips,
individual peaks approximately -3.74 dBFS, no clipped samples. These are synthesized
review sounds, not a human-approved final mix. `Audio/arcade_cues_preview.wav` provides an eleven-second cue-only listening
preview; its timestamps are in `Audio/PREVIEW_TIMELINE.json`. It is an offline asset
showcase, not a recording of the in-game mix. New music, final mastered audio, spatial
neighborhood emitters and full AudioMixer group authoring remain deferred. The built-in
master slider also controls the existing voice; the effects slider controls new sources.

No human has accepted the art, animations, sound or driving feel. No AI opponents,
multiplayer, new combat or moving traffic hazards are included in this change. Existing
track elevation constraints remain in force.

## Validation

- Final EditMode: **95/95 passed**, `Logs/unity-editmode-20260909-075848.log` and matching XML.
- Blender: source validation passed, 14,686 triangles, no asset errors or warnings.
- Import: Unity audit confirms seven assets, 14,258 triangles and two four-second animation clips.
- Scene generation: all three circuits generated through Unity editor APIs.
- Unity screenshots: `Logs/unity-run-capturetour-20260909-075640.log` exited 0. Actual imported
  meshes rendered from the driver camera and overhead for all three courses.
- Audio: 13 mono PCM files checked for expected format, duration and non-clipping sample peaks.
- Final PlayMode: **45/45 passed**, `Logs/unity-playmode-20260909-080052.log` and matching XML.
  Both new routes completed physical laps through the ordered gates. Road/sidewalk probes,
  baked prop motion, pause, race audio cues, track travel and controller-only selection passed.
  The two launch tests passed after the fixture correction. Exact cases/durations are in
  `Art/NEIGHBORHOOD_TEST_RESULTS.json`.
- Windows Development build succeeded, exit 0, **0 errors / 1 warning**, 217 MB reported
  build size; `Logs/unity-build-development-20260909-080659.log`.
  Output: `Builds/Windows-Development/GrannyRacer.exe`.
  Warning: `Access token is empty. Native symbols will not be uploaded for this build. Please make sure you are signed in to the Unity Cloud.`
  Local executable generation succeeded; cloud symbol upload was unavailable.
- Native Windows player: exit **0**, `Logs/neighborhood-player-review.log`.
  Captured all three circuits, pause/track selector and driving at 1280×720. Probe logged
  21.2 m/s on Sunday Block Party and 15.4 m/s on each new course, grounded on all three.
  No player exceptions/errors were found. Screenshots are under `Art/Previews/Neighborhood/Player`.
- `git diff --check` passed. No packages were added; no commit or push was made.

Intermediate failures resolved during implementation:

- Blender generation: `ValueError: faces.new(...): face already exists`. Duplicate reversed
  faces were replaced by thin solid geometry; the subsequent source validator passed.
- First Unity compile: `error CS0619: 'AudioImporter.preloadAudioData' is obsolete: 'Preload audio data has been moved to AudioImporter.SampleSettings as a per platform local setting'`.
  Preload was moved to per-platform sample settings.
- Two retries encountered a stale lockfile plus unrelated running Unity projects:
  `The Unity editor currently has this project open.` / `Batch mode cannot acquire the project lock. Close the Unity editor and retry.`
  No editor was terminated and no locks were removed. The wrapper incorrectly matched any Unity process. `Tools/_common.ps1` now checks the
  actual project argument and conservatively handles unknown command lines. The user's
  unrelated Unity projects and Blender GUI were left open.
- Unity scene-build batch `Logs/unity-run-buildtour-20260909-074350.log`: Bee returned
  `ExitCode: -1073741819 Duration: 22s` and its parent batch stopped progressing. Only that
  verified, task-owned batch was stopped; a single recovery restart was attempted.
- Blender's validator emitted `DeprecationWarning: 'Material.use_nodes' is expected to be removed in Blender 6.0`;
  the source validation itself passed with no asset warnings.

- The recovery build completed with exit 0 and generated all scenes, but its initialization
  logged `OutOfMemoryException: Out of memory` / `System.OutOfMemoryException: Out of memory`.
  Windows reported about 1.5 GB free virtual memory while other Unity projects were active.
  Subsequent independent test/build results, rather than this initialization log, determine validation.
- First full PlayMode run: 44/45 passed, including every new track/presentation test.
  `RaceStartPlayModeTests.HoldingThrottleTooEarlyProducesWheelspinAndSmoke` failed:
  ```text
  Expected: Skid
    But was: None
  ```
  The pre-existing launch fixture used one manual `Press` across scene/editor updates.
  The fixture now resets the race after loading and supplies held keyboard state through
  every countdown frame, with an explicit assertion that throttle reaches the action map.
  Launch gameplay maths and rewards are unchanged. Editor focus/reset behavior is a
  possible source of the intermittent lost input; it was not established as the cause.


## Human review

Use `Art/Previews/Neighborhood/kit.png` for the Blender kit, the paired spinner/seesaw
frames for authored motion, and `Circuit_*_overview.png` for route layout. The static
start-grid screenshots include the start gantry at the top of the view; actual player
captures also show driving beyond it. These renders are evidence of the meshes, not
human acceptance of their style. The background rows are decorative residential scenery,
not a fully simulated or traversable street network.

Play all three laps on each route, try recovery/backward gates, test both keyboard and
controller, and listen through repeated drift/boost/lap cycles. Review voice balance and
fatigue with the effects slider. Milestone acceptance remains pending that playtest.
