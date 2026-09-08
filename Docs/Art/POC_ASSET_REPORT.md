# POC asset refresh — technical review

Owner direction: chunky cartoon comedy, expressive Granny, colourful clothing, oversized
slippers and a homemade rocket walker. Actual artistic acceptance remains pending.

Review renders: [Granny and walker](Previews/quarter.png), [back](Previews/back.png),
[boost pose](Previews/pose_Boost.png), [neighbourhood kit](Previews/prop_kit.png),
[three slippers](Previews/slippers.png).

Actual Windows player: [driving](Previews/unity_02_driving.png),
[boost](Previews/unity_03_boost.png), [pause](Previews/unity_04_pause.png),
[handling lab](Previews/unity_05_handling.png),
[character in Unity](Previews/unity_06_hero_in_unity.png).

## Mesh inventory

Dimensions below are Blender X/Y/Z in metres (Unity is Y-up). The export converts axes;
one Blender unit equals one Unity metre. Counts exclude duplicate slipper instances.

| Mesh | Triangles | Dimensions (m) | Materials |
| --- | ---: | --- | ---: |
| CHR_Granny | 25,452 | 0.635 × 0.447 × 1.481 | 1 |
| VEH_Walker | 7,728 | 0.872 × 0.827 × 0.972 | 1 |
| PRP_Slipper_Classic | 1,128 | 0.122 × 0.255 × 0.109 | 1 |
| PRP_Slipper_Bunny | 3,088 | 0.122 × 0.256 × 0.196 | 1 |
| PRP_Slipper_Rocket | 1,228 | 0.132 × 0.255 × 0.109 | 1 |
| ENV_House | 1,756 | 5.587 × 4.500 × 5.210 | 1 |
| PRP_Fence | 2,456 | 2.400 × 0.206 × 1.180 | 1 |
| PRP_Bench | 2,304 | 1.700 × 0.520 × 1.120 | 1 |
| PRP_Bin | 1,912 | 0.620 × 0.540 × 0.792 | 1 |
| PRP_Cone | 580 | 0.450 × 0.450 × 0.600 | 1 |
| ENV_Tree | 1,712 | 2.800 × 1.890 × 4.262 | 1 |
| PRP_Kerb | 216 | 0.485 × 1.000 × 0.230 | 1 |
| PRP_Barrier | 864 | 2.000 × 0.650 × 0.920 | 1 |
| PRP_RoadDash | 108 | 0.120 × 2.000 × 0.016 | 1 |
| PRP_StartGate | 5,292 | 9.050 × 0.700 × 4.200 | 1 |

The 27-bone rig, six named sockets and eight action clips retain the existing import
contract. Classic, bunny and rocket slippers retain the ankle pivot and replacement workflow.
The cardigan/sleeves are joined into a continuous surface; skinning uses spatial masks and
locomotion clips have baked grip constraints. There is no runtime IK package dependency.

All meshes use a shared 256×256 palette texture and one URP/Lit material. This deliberately
small palette has point sampling and no mipmaps; it is colour-block art, not a detailed
texture atlas. UVs, normalized skin weights and original asset GUIDs are preserved.

## Technical validation and limitations

- Every staged FBX is re-imported by Blender and gets its own manifest. See
  `POC_EXPORT_VALIDATION.json` for mesh counts, rig/socket checks and animation frame ranges.
- The source validator checks units, transforms, names, UVs, materials, texture availability,
  weights and budgets. Its machine-readable result is checked in alongside this report.
- Fence (2,456 triangles) and bench (2,304) exceed the suggested 2,000-triangle small-prop
  budget. These are warnings; profile scene density before choosing simplification or LODs.
- Unity importer settings are set explicitly: metre scale, no imported lights/cameras or
  colliders, normals imported, tangents calculated, no mesh compression, and palette remapping.
  Static props/slippers have no imported animation. Granny uses the existing Generic avatar.
- Scene placement preserves the FBX root's axis conversion inside a separate transform.
  A PlayMode regression checks house height and road-mark thickness in Unity world space.
  The scene sun casts soft shadows and the camera uses URP FXAA.
- Actual Blender front/back/quarter and Drive/Boost/TurnLeft/HitReact frames are rendered and
  inspected for geometry faults. Player captures exercise the actual Unity materials and HUD.
  Neither source renders nor automated checks establish human art or gameplay acceptance.
- The scenery uses non-colliding decoration. Existing road, kerb and barrier collision geometry
  remains authoritative. The road is still editable through the waypoint generator.
- Existing audio is retained. Music and a complete sound mix are deferred. Project licensing
  and the provenance of the existing voice clips remain unresolved; this work adds no licence.

## Reproduction and source retention

The original `Tools/Blender/Granny_Walker.blend` is preserved. The new source is
`Blender/Source/Characters/CHR_Granny_RocketClub.blend`; its palette image is packed.
`Blender/Scripts/rebuild_poc_assets.py` authors the set using the original rig helpers,
`validate_asset.py` validates the source, `validate_poc_exports.py` checks FBX round trips,
and `render_poc_slippers.py` creates the slipper comparison. Exports stage in
`Blender/Exports/POC_Refresh/` before Unity import.

On the original workstation:

```powershell
& "<Blender executable>" --background --python-exit-code 1 `
  --python Blender/Scripts/rebuild_poc_assets.py
# Use -- --force only to revise the new refresh source after retaining an earlier copy.
pwsh -File Tools/blender-validate.ps1 `
  -BlendFile Blender/Source/Characters/CHR_Granny_RocketClub.blend -MaxTriangles 70000
& "<Blender executable>" --background --python-exit-code 1 `
  --python Blender/Scripts/validate_poc_exports.py
```

Then choose **Granny Racer > POC > Import Blender Refresh and Build Scene** in Unity.
This uses editor APIs to copy/import meshes and rebuild the scene without manually editing
Unity YAML. Existing FBXs retain their `.meta` GUIDs.

By existing project decision, all of `Blender/` and the local wrappers are Git-ignored.
The editable source and generation scripts therefore remain local; they are not part of a
fresh clone. Imported Unity assets, this inventory and validation reports are tracked.
A clone can build and regenerate its scene from the tracked imported assets without Blender.

For reproducible player captures, the development build has an opt-in diagnostic mode:

```powershell
& .\Builds\Windows-Development\GrannyRacer.exe `
  -screen-fullscreen 0 -screen-width 1280 -screen-height 720 `
  -poc-review .\Logs\POC_Player_Review -logFile .\Logs\poc-player-review.log
```

It injects a temporary virtual keyboard, captures countdown/driving/boost/pause/tuning and
a character view, then exits. It is inactive during ordinary play and excluded from release
builds. Review the actual images and player log; file existence alone is not visual acceptance.
