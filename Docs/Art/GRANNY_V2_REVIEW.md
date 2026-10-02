# Granny v2 — model, rig and animation review

Owner request: replace the rejected granny model, skeleton and animations using Blender,
with images and a video of the result. Selected personality: **mischievous and determined**.

Status: replacement authored and staged; Blender technical validation passed. **Human
artistic acceptance and Unity integration are pending.** These are actual source-mesh
renders, not concept illustrations or game captures.

## Review media

- [Three-quarter view](Previews/Granny_v2/01_hero.png)
- [Front](Previews/Granny_v2/02_front.png)
- [Back](Previews/Granny_v2/03_back.png)
- [Face close-up](Previews/Granny_v2/04_face.png)
- [Side](Previews/Granny_v2/05_side.png)
- [Turntable and all eight animations — MP4](Previews/Granny_v2/granny_v2_review.mp4)

The stills use Cycles studio lighting. The silent video uses Eevee studio lighting at
960 × 720, 24 fps. Neither establishes how the character will look under Unity lighting.
The walker and swappable slipper meshes are retained; this replacement targets the granny.

## Scope review

1. Improves the demo's central racer and the readability of her movement.
2. The POC milestone and prior character art were not accepted by a human.
3. The owner explicitly rejected the previous visual placeholder and requested a rebuild.
4. Maintenance is bounded to one authoring script, one renderer, one validation script,
   the versioned source and the existing model/animation import contract.
5. This is existing-demo presentation work. No new combat mechanics, racers or features
   are implemented. Existing elbow clips are replaced for compatibility only.

## Replacement

The body, cardigan, face, hair, glasses, hands, stockings and skirt are newly authored.
The face and garment volumes are fused and relaxed before budgeting. Front/back inspection
led to corrections to the shoulder silhouette, cheek integration and scalp coverage.
Side-view inspection also led to projecting the lip and crease placement onto the actual
sculpted facial surface so these details do not float in front of it.

The new 43-bone skeleton has rebuilt rest positions and weights, articulated eyes, brows,
eyelids, jaw, fingers, thumbs, bun and four skirt controls. Existing public bone names and
all six hand/slipper/exhaust sockets remain present. `GrannyRig` remains the rig object name.
The armature's bone collections separate body, face, cloth and walker controls for editing.

All eight actions are authored anew. Two-segment limb solving keeps locomotion wrists on
the grips, alternates planted and recovering feet, and lifts both feet during boost. Motion
is baked to deformation bones at 30 fps; no new runtime IK package is required. Root motion
is zero. Loop endpoints are checked on evaluated bone matrices, not only keyframe counts.

| Clip | Frames at 30 fps | Loop |
| --- | ---: | --- |
| Idle | 0–60 | Yes |
| Drive | 0–40 | Yes |
| TurnLeft / TurnRight | 0–30 | Yes |
| Boost | 0–45 | Yes |
| HitReact | 0–26 | No |
| AttackLeft / AttackRight | 0–36 | No |

## Measured Blender costs

| Mesh | Triangles | Dimensions X/Y/Z, metres | Materials |
| --- | ---: | --- | ---: |
| Granny | 42,000 | 0.6833 × 0.5503 × 1.5670 | 1 |
| Retained walker | 7,728 | 0.8720 × 0.8269 × 0.9720 | 1 |

The displayed model with two existing bunny slippers is 55,904 triangles across four mesh
instances and two material identities. This is a source/FBX count, **not** a Unity performance
measurement. Final decimation produces a maximum of five bone influences on some granny
vertices; Unity must retain these rather than silently truncating them to four.

The granny uses a separate packed 1024 × 1024 colour palette, `MAT_GrannyV2_Palette`.
The existing walker palette is preserved. Every joined part uses the same `UVMap` name.
Source textures are packed and their paths relative. Scale and rotation are applied.

## Verification

- Source validator: no errors or budget warnings.
- All nine staged FBXs round-trip through Blender with the 43-bone hierarchy and six
  sockets intact; mesh UVs, material counts and normalized skin weights pass.
- All five looping clips close within the 0.0002 matrix tolerance. Every clip has zero
  root displacement and finite evaluated bone matrices.
- Authored hand/foot target residuals pass the 3 mm threshold in all eight clips.
- The finished H.264 MP4 decodes all 384 frames without an error: 960 × 720, 24 fps,
  16 seconds, yuv420p. See [video verification](GRANNY_V2_VIDEO_VALIDATION.json).
- Evidence: [source validator](GRANNY_V2_SOURCE_VALIDATION.json),
  [FBX and animation validator](GRANNY_V2_EXPORT_VALIDATION.json),
  [source/export manifest](GRANNY_V2_MANIFEST.json).

Initial checks caught and corrected an over-budget mesh and unreachable elbow-action targets:

```text
AttackLeft: hand contact exceeds 3 mm: 0.02180267883086767
AttackRight: hand contact exceeds 3 mm: 0.02180267883086767
```

An initial validator incorrectly required one Blender action per imported FBX:
`Expected exactly one exported action`. FBX imports separate actions for the animated
socket objects as well as the armature. The validator now checks the armature's assigned
action and duration. The final run passes. Blender emits future API deprecation notices
for `Material.use_nodes` and `World.use_nodes`; these do not fail validation.
The first video metadata check returned
`FileNotFoundError: [Errno 2] No such file or directory: 'ffprobe'` after successful encoding.
Verification now uses FFmpeg to decode every frame, with ffprobe optional. The final
verification command succeeds.

No C#, Unity assets, existing `.meta` files, scenes, packages or gameplay code were changed.
Unity EditMode/PlayMode tests, the Windows build, imported triangle/bone counts, scene
animation bindings, runtime jump/drift layering, lighting and gameplay feel have **not**
been verified for this replacement. They are required after import. Existing player code
layers jump/drift rotations over these bones, so Blender grip measurements do not establish
runtime grip contact during those states. Audio is unchanged; the review video is silent.

## Source and reproduction

- Source: `Blender/Source/Characters/CHR_Granny_Mischief_v2.blend`
- Generator: `Blender/Scripts/rebuild_granny_v2.py`
- Renderer: `Blender/Scripts/render_granny_v2.py`
- FBX/animation validator: `Blender/Scripts/validate_granny_v2.py`
- Video assembler: `Blender/Scripts/assemble_granny_v2_video.py` (Python + ffmpeg; ffprobe optional)
- Staging directory: `Blender/Exports/Granny_v2/`
- Intermediate renders: `Blender/Renders/Granny_v2/`

The prior RocketClub source and original `Tools/Blender/Granny_Walker.blend` remain intact.
The generator requires the prior source for the retained walker/slippers, and imports the
existing `rebuild_poc_assets.py` and `Tools/Blender/granny_walker_gen.py` geometry/FBX helpers.
Blender sources/scripts/staging remain git-ignored by repository policy; the working copy
is their only copy. Reports and review media live under tracked `Docs/Art/`.

```powershell
# The generator refuses to overwrite its own source without --force.
& ./Tools/blender-run.ps1 -Script Blender/Scripts/rebuild_granny_v2.py
& ./Tools/blender-validate.ps1 `
  -BlendFile Blender/Source/Characters/CHR_Granny_Mischief_v2.blend -MaxTriangles 65000
& ./Tools/blender-run.ps1 -Script Blender/Scripts/validate_granny_v2.py
& ./Tools/blender-run.ps1 `
  -BlendFile Blender/Source/Characters/CHR_Granny_Mischief_v2.blend `
  -Script Blender/Scripts/render_granny_v2.py
& ./Tools/blender-run.ps1 `
  -BlendFile Blender/Source/Characters/CHR_Granny_Mischief_v2.blend `
  -Script Blender/Scripts/render_granny_v2.py -ScriptArgs @('--video')
```

## Unity import handoff after visual review

The repository Blender skill requires **preview render → human review → then copy into
Assets**. Therefore the existing in-game asset is deliberately still installed.

1. Review the linked stills and motion video. Confirm the visual direction before replacement.
2. Copy the nine staged `Granny_Walker*.fbx` files over the same names in
   `Assets/GrannyRacer/Art/Imported/Models/Granny/`. Preserve every existing `.meta` file.
   Copy `GrannyV2_Palette.png` into `Assets/GrannyRacer/Art/Textures/`.
3. In Unity, select the new texture: sRGB on, Filter Mode Point, mipmaps off, Compression
   None. Click Apply. Create `MAT_GrannyV2_Palette` in `Art/Materials/` using **Universal
   Render Pipeline/Lit**, white Base Color, the new texture as Base Map, Metallic 0,
   Smoothness approximately 0.25, Opaque surface.
4. Select `Granny_Walker.fbx`. Model tab: Scale Factor 1, Use File Scale on, Bake Axis
   Conversion off, no imported cameras/lights/colliders, Normals Import, Tangents Calculate
   Mikktspace, Mesh Compression Off. Set Skin Weights Custom with Max Bones/Vertex 8 and
   a low weight cutoff. Verify the imported vertex weights retain the measured influences.
5. Rig tab: Generic, Create From This Model, Root node `Root`, Optimize Game Objects off
   so the named sockets remain accessible. Apply. Materials tab: remap
   `MAT_GrannyV2_Palette` to the new material and retain `MAT_POC_Palette` for the walker.
6. For each animation FBX, use Generic and Copy From Other Avatar, referencing the base
   model's newly imported avatar. Enable animation import, turn compression off for the
   initial review, use the clip name/range/loop flags in the table above, and Apply.
   Keep root motion disabled on the runtime Animator.
7. Open the existing race scene and inspect the model instance. Verify the new bones and
   original sockets, material assignments, animation controller clip references, slipper
   placement and skinned bounds. Do not run the old **Import Blender Refresh and Build
   Scene** command: it copies the previous `POC_Refresh` exports back over the replacement.
   Do not rebuild manually edited tracks merely to update the character.
8. In Play Mode inspect idle, push, turns, boost, hit reaction, jump and drift under the
   game camera. Check both grips, feet, skirt clearance and camera framing. Run the required
   EditMode/PlayMode suites and Windows build, then have the owner review in-game quality.

The elbow clips are review assets; the existing runtime controller currently selects the
six locomotion/reaction clips. This task does not add a combat state machine.
