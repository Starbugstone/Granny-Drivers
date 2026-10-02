# Granny v3 — facial expressions and gaze

Requested revision: add facial expressions and eye movement, with a slightly less cartoony
face. The v2 body, clothing, silver bun, oversized slippers and mischievous/determined
personality remain the reference. This is a versioned facial revision of that source.

Original facial-review status: source and FBX assets were staged for review. The owner
subsequently requested Unity integration and a full demo lap; see the
[Unity integration and lap recording](GRANNY_V3_DEMO_LAP.md) for the current status.
The images and facial video below render the actual Blender meshes.

## Review media

- [Neutral face](Previews/Granny_v3/01_mischief.png)
- [Determined](Previews/Granny_v3/02_determined.png)
- [Delighted](Previews/Granny_v3/03_delighted.png)
- [Surprised](Previews/Granny_v3/04_surprised.png)
- [Annoyed](Previews/Granny_v3/05_annoyed.png)
- [Closed eyelids](Previews/Granny_v3/06_blink.png)
- [Left wink](Previews/Granny_v3/07_wink_left.png) / [right wink](Previews/Granny_v3/08_wink_right.png)
- [Gaze left](Previews/Granny_v3/09_gaze_left.png) / [gaze right](Previews/Granny_v3/10_gaze_right.png)
- [Angled face](Previews/Granny_v3/11_face_angle.png)
- [Full character](Previews/Granny_v3/12_full_character.png)
- [Imported FBX expression](Previews/Granny_v3/13_fbx_delighted.png)
- [Facial animation video](Previews/Granny_v3/granny_v3_face_review.mp4)

Stills: 800 × 800. Silent video: 640 × 640, 24 fps, eight seconds. Eevee studio lighting;
these are not Unity captures. The earlier [body animation video](Previews/Granny_v2/granny_v2_review.mp4)
remains available; its facial treatment predates this revision.

## Changes

Smaller eyes and irises, reduced nose/cheek projection, and thinner lips and brows soften
the exaggeration. The eyes now rotate around their sphere centres with coordinated yaw,
pitch and slight convergence. Opaque upper and lower eyelids close over the eyes; each eye
has independent blink, squint and widening controls. A moving lid crease makes closure
visible. Brows follow the forehead surface and sit above the glasses.

Four expression blendshapes coordinate brow, mouth and facial skin: Determined, Delighted,
Surprised and Annoyed. The neutral pose retains an asymmetric smile. The mouth is a shallow
surface treatment with a tooth strip, not a complete oral cavity or dialogue/lip-sync rig.

Facial curves are baked into all eight existing actions: focused driving and turning,
a delighted boost, startled hit reaction, and annoyed side glances during elbow clips.
Idle includes gaze movement and a blink. `FacialReview` is a separate eight-second demo of
gaze, the four expressions, a blink and independent winks. This is baked gaze animation;
there is no new runtime target-tracking component.

The public 43-bone hierarchy and six sockets remain intact. The legacy Jaw/Brow/Lid bones
remain for compatibility; shape keys drive the new visible facial deformation. Eye bones
drive the eyeballs. A separate `CHR_Granny_Face` skinned mesh carries ten shape controls;
the retained body carries the four matching expression controls. Maximum vertex influences
are reduced to four normalized weights.

## Scope review

1. Facial reactions improve readability of the demo's central racer.
2. The current milestone has not received human acceptance.
3. The owner requested this upgrade to the reviewed placeholder presentation.
4. Maintenance is limited to the versioned facial source, scripts, exports and review media.
5. This is demo presentation work; no dialogue, combat mechanics or full-game systems added.

## Measured costs and validation

| Mesh | Triangles | Dimensions X/Y/Z, metres | Materials |
| --- | ---: | --- | ---: |
| Body, hair, clothes and glasses | 36,936 | 0.6833 × 0.5339 × 1.5670 | 1 |
| Facial features | 7,800 | 0.2451 × 0.1191 × 0.2139 | 1 |
| Retained walker | 7,728 | 0.8720 × 0.8269 × 0.9720 | 1 |

Combined character: **44,736 triangles**, below the 45,000 character budget. The two
character meshes share one palette material but remain separate renderers. Including the
walker and two retained bunny slippers, the source preview contains 58,640 triangles across
five displayed mesh instances and two material identities. This is not a runtime performance measurement.

- Required source validation passes without errors or warnings.
- All ten FBXs round-trip through Blender: base mesh plus nine animation clips.
- Bone hierarchy, sockets, materials, UVs, normalized weights and shape key sets pass.
- Each exported facial curve is sampled at every authored frame and compared to its source.
- Facial geometry is compared to the source at eight expression/blink/wink poses.
- Loop endpoints are checked on bone matrices and facial weights; root motion remains zero.
- Frontal rays confirm opaque skin covers each pupil at the closed blink and wink poses.
  This is a targeted closure check, not an exhaustive intersection proof.
- Both palette images are present in staging and resolve after FBX import.
- Video verification decodes all 192 frames and checks resolution, frame rate and codec.

Evidence: [source validation](GRANNY_V3_SOURCE_VALIDATION.json),
[FBX and facial animation validation](GRANNY_V3_EXPORT_VALIDATION.json),
[manifest](GRANNY_V3_MANIFEST.json), [video validation](GRANNY_V3_VIDEO_VALIDATION.json).

An initial authoring run failed with `TypeError: 'BMDeformVert' object is not iterable`;
the script now iterates the deform layer's keys. An initial FBX preview reported
`ERROR Failed to create GPU texture from Blender image` because the retained walker palette
was absent beside the exports. The generator now stages both palettes and uses relative
references. These issues were corrected before the final validation. The renderer emits
Blender's `World.use_nodes` deprecation warning; it remains supported in the installed version.

No Unity assets, C#, packages, scenes or existing metadata were changed. Unity import,
animation bindings, in-game appearance, EditMode/PlayMode tests and the Windows build were
not run for this staged revision. Human visual and gameplay acceptance remain outstanding.

## Editable source and reproduction

- Source: `Blender/Source/Characters/CHR_Granny_Expression_v3.blend`
- Authoring: `Blender/Scripts/animate_granny_face_v3.py`
- Rendering: `Blender/Scripts/render_granny_face_v3.py`
- Validation: `Blender/Scripts/validate_granny_face_v3.py`
- Movie assembly: `Blender/Scripts/assemble_granny_face_v3_video.py`
- Staging: `Blender/Exports/Granny_v3/`

The v2 source remains unchanged. The generator depends on that source and its existing
geometry/export helper scripts. It refuses to replace v3 without `--force`. Blender source,
scripts and exports are git-ignored by repository policy; this working copy is their only copy.

```powershell
& ./Tools/blender-run.ps1 -Script Blender/Scripts/animate_granny_face_v3.py
& ./Tools/blender-validate.ps1 -BlendFile Blender/Source/Characters/CHR_Granny_Expression_v3.blend -MaxTriangles 65000
& ./Tools/blender-run.ps1 -Script Blender/Scripts/validate_granny_face_v3.py
& ./Tools/blender-run.ps1 -Script Blender/Scripts/render_granny_face_v3.py -BlendFile Blender/Source/Characters/CHR_Granny_Expression_v3.blend
# Repeat rendering with -ScriptArgs @('--angles'), @('--fbx') and @('--video').
# Assemble the completed frames using Python with ffmpeg on PATH:
python Blender/Scripts/assemble_granny_face_v3_video.py
```

## Unity handoff after visual review

The repository skill `.claude/skills/blender-assets/SKILL.md` specifies:
“preview render → human review → **then** copy into `Assets/GrannyRacer/Art/<Category>/`.”
This revised face is therefore staged, pending the owner's review.

1. Follow the [v2 importer steps](GRANNY_V2_REVIEW.md#unity-import-handoff-after-visual-review),
   using the v3 export folder. Preserve existing `.meta` files. Keep the existing walker
   material mapping; the staged `POC_Palette.png` is included for portable FBX inspection.
2. On the base FBX and every clip FBX, enable **Import BlendShapes** and animation import.
   Use Generic rig, base-model Avatar for clip imports, imported normals, calculated
   tangents, no mesh or animation compression during initial review, and four bone
   influences. Keep Optimize Game Objects off for socket access.
3. The animation FBXs intentionally include the meshes because their blendshape curves
   require those bindings. Preserve both `CHR_Granny` and `CHR_Granny_Face` names and relative
   hierarchy paths on the runtime model. Remap both to `MAT_GrannyV2_Palette`.
4. Scrub imported clips in Unity and inspect both SkinnedMeshRenderer blendshape weights.
   Confirm Idle blinks, turning gaze, Boost smile and HitReact surprise on the actual
   prefab/controller instance. Check blendshape normals, skinned bounds and transitions;
   Unity binding correctness has not been established by the Blender round trip.
5. Keep `FacialReview` as a review clip; it does not need a gameplay controller state. Do not
   run the old refresh/scene rebuild menu item, which copies older assets over these files.
6. Run the required Unity tests and Windows build, then have the owner review the in-game
   character under the race camera and lighting before declaring the feature accepted.
