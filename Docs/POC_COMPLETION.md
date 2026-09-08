# POC completion and art refresh

Requested by the project owner: finish the POC and redo its 3D assets using Blender and Unity.

**Current state:** implemented and technically validated; ready for the owner's playtest.
Final checks: 76 EditMode + 33 PlayMode tests passed, all 22 FBX exports validated, Windows
development build succeeded, and six actual-player captures inspected. Human milestone
acceptance remains pending.

This report covers the tested POC refresh revision. Concurrent circuit-generation edits
appeared in the local workspace after validation (2026-09-08, 22:51 local time), including
`CircuitRecipe`, `GreyboxTrackGenerator` and further changes to `PocSceneBuilder`. Those edits
were preserved locally and excluded from the refresh commits and validation claims.

Scope review: this improves the single-racer core loop and its readability. Human handling
acceptance is still pending. Simple presentation can remain lightweight; the new asset
generator and Unity authoring tools must remain reproducible. This is POC work, not the
full-game backlog. AI, combat, multiplayer and additional tracks remain deferred.

## Deliverables

- Rebuilt Granny, rocket walker and three slipper variants, retaining rig/socket contracts.
- Refreshed locomotion clips and a compact neighbourhood prop set around Quiet Sunday.
- Readable racing HUD, pause/restart/controls, and a live handling-tuning panel.
- Ordered three-lap finish/restart, paused-input and recovery regression coverage.
- Blender source/export validation, actual mesh preview renders, Unity import audit,
  EditMode/PlayMode tests and a Windows development build.

The owner selected **chunky cartoon comedy**: expressive granny, colourful clothing,
oversized slippers and a homemade rocket walker. This is the approved direction;
acceptance of the actual models, animation, audio and handling remains with the owner.

Sources are written to `Blender/Source/` under new names. Original source files are retained.
Exports stage in `Blender/Exports/POC_Refresh/`; existing Unity FBXs keep their `.meta` GUIDs.
The editable road/checkpoint layout remains authored by the established track generator.

## Acceptance

Implementation and automated verification are recorded here when performed. Human visual,
audio and handling approval must be recorded separately; automated tests do not establish fun.

## Implementation delivered

- Granny, homemade rocket walker, three slippers and ten neighbourhood/track props authored
  in Blender; eight rig animation exports retain the established socket contract.
- Shared palette material, imported art and a rebuilt Quiet Sunday scene. New scenery is
  decorative; Blender kerb and barrier visuals fit the established collision geometry.
- Racing HUD, lap timer, countdown feedback, pause/resume, restart, controls and master volume.
- F1 handling lab edits a temporary settings copy and can restore the original values.
- Fall/overturn recovery returns to the last accepted checkpoint. Reset, steering and braking
  inputs respect the race's driving gate.
- Five additional PlayMode checks cover imported scenery orientation, ordered three-lap
  finish/restart, paused countdown, fall recovery, and isolation/restoration of live tuning.

## Verification record — 2026-09-08

- **Final EditMode:** 76/76 passed (`unity-editmode-20260908-224607.log`).
- **Final PlayMode:** 33/33 passed (`unity-playmode-20260908-224701.log`), including the
  corrected scenery orientation check.
- **Final build:** Windows development, succeeded, 0 errors / 1 warning, 165.39 MB output
  (`unity-build-development-20260908-224913.log`).
- **Final player:** normal exit, six 1280×720 captures, no runtime exceptions
  (`poc-player-final.log`). Scenery orientation, road-mark placement, ground shadows,
  character materials, HUD, boost, pause and handling panel inspected in the actual player.
  Images are tracked in `Art/Previews/unity_*.png`.
- Blender source validation: passed; no unit, transform, UV, texture or weight errors.
  Two suggested-budget warnings: fence 2,456 and bench 2,304 triangles versus 2,000.
- FBX round-trip validation: all 22 exports passed; per-export manifests and combined report.
- Unity import completed and wrote `Art/POC_IMPORT_AUDIT.json`; existing mesh GUIDs unchanged.
  The WSL direct launcher returned 143 despite Unity logging `Application will terminate with
  return code 0`. Subsequent test runs loaded the saved scene successfully.
- EditMode after import: 76/76 passed (`unity-editmode-20260908-223203.log`).
- PlayMode after import: 32/32 passed (`unity-playmode-20260908-223242.log`).
- Initial Windows development build succeeded with zero errors and one Unity Cloud symbol
  upload warning (`unity-build-development-20260908-223710.log`).
- Initial standalone player capture produced six 1280×720 frames and no runtime exceptions.
  Inspection found static FBX placement overwrote the import's axis rotation. Placement now
  preserves that transform inside a wrapper; a scene regression checks upright houses and
  flat road markings. Soft sun shadows and camera FXAA were also enabled through URP.

The build warning is unrelated to compilation or gameplay:

> Access token is empty. Native symbols will not be uploaded for this build. Please make sure you are signed in to the Unity Cloud.

A legacy authoring warning remains in the existing physics-material creation path:

> CreateAsset() should not be used to create a file of type 'physicsMaterial' - consider using AssetDatabase.ImportAsset() to import an existing 'physicsMaterial' file instead or change the file type to '*.asset'. This error will in a future release be changed to an exception.

The current Unity version continues and the driving tests pass. Existing test files also
emit obsolete object-search API warnings. These are not hidden as a clean warning-free run.

## Human review still required

Run `Builds/Windows-Development/GrannyRacer.exe`, or open the POC scene in Unity and press
Play. Use `Docs/TestPlans/POC_Playtest.md` for the five-minute driving, controller, art, camera,
audio and humour review. No manual scene wiring is outstanding. Music and a complete mix
remain deferred; licensing/provenance of existing audio remains unresolved.

The accepted art direction is recorded in D-18. Human acceptance of the delivered assets
and the POC milestone itself has **not** been marked complete.
