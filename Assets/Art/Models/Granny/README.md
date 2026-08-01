# Granny + Rocket Walker

Source of truth is the generator script, **not** the FBX files:

```
Tools/Blender/granny_walker_gen.py     # builds meshes, rig, clips, exports FBX
Tools/Blender/preview_anim.py          # renders pose sheets from a clip
Tools/Blender/verify_export.py         # round-trips the FBX and reports contents
```

Regenerate everything (Blender 5.2, headless):

```
blender --background --python Tools/Blender/granny_walker_gen.py
blender --background --python Tools/Blender/granny_walker_gen.py -- --preview
```

Anything hand-edited in the FBX will be lost on the next run. Edit the script.

## Files

| File | Contents |
| --- | --- |
| `Granny_Walker.fbx` | `SK_Granny` (7.7k tris) + `SK_Walker` (4.8k tris), 27-bone rig, sockets |
| `Granny_Walker@<Clip>.fbx` | Animation-only, same skeleton. 8 clips, 30 fps |
| `Slippers/Slipper_*.fbx` | Static, symmetric slipper meshes authored on the ankle socket |

Scale is metric, 1 unit = 1 m. She stands ~1.45 m at the head, hunched.

## Clips

| Clip | Frames | Loop | Notes |
| --- | --- | --- | --- |
| `Idle` | 0-60 | yes | breathing, sway, glance |
| `Drive` | 0-40 | yes | forward lean, scooting shuffle, 2 wheel revolutions |
| `TurnLeft` / `TurnRight` | 0-30 | hold from f14 | lean + steer; ease in over 14 frames, then holds |
| `AttackLeft` / `AttackRight` | 0-30 | no | wind-up f8, contact f15, recovery to grip by f30 |
| `HitReact` | 0-26 | no | knocked back at f3, wobble, settled by f26 |
| `Boost` | 0-45 | yes | deep lean, rattle, 4 wheel revolutions |

Turn clips ease into the pose by frame 14 and hold to the end, so they work
either as one-shots or as the extremes of a steering blend tree.

## Rig

`Root` -> `Hips` -> `Spine` -> `Chest` -> `Neck` -> `Head`, with
`Shoulder/UpperArm/LowerArm/Hand` and `Thigh/Shin/Foot` per side.

The walker hangs off `Root` (not off the hands) as
`Walker_Root` -> `Caster_L/R` -> `Wheel_L/R`, plus `Rocket_L/R`. That means
gameplay code can drive steering and wheel spin procedurally on top of any clip:

* `Caster_L/R` — steering yaw, rotate about its own local Y
* `Wheel_L/R` — rolling spin, rotate about its own local Y (the bone points
  sideways along the axle, so the spin axis is roll-independent)
* `Rocket_L/R` — gimbal / recoil shake

Sides are anatomical: `_L` bones are on her left, which is Unity -X when she
faces +Z.

## Sockets

Empty transforms parented to bones, all with identity rotation so an attachment
mounts with an identity local transform:

| Socket | Parent bone | Use |
| --- | --- | --- |
| `Slipper_Socket_L` / `_R` | `Foot_L` / `_R` | slippers (see below) |
| `Hand_Socket_L` / `_R` | `Hand_L` / `_R` | handbag, rolling pin, weapons |
| `FX_Exhaust_L` / `_R` | `Rocket_L` / `_R` | thruster VFX; +Z points down the plume |

## Interchangeable slippers

Slipper meshes are left/right symmetric and authored around the ankle socket
point with the sole exactly on the floor plane, so **one prefab fits either
foot** with no mirroring and no offset. `Assets/Scripts/Characters/SlipperSwapper.cs`
does the mounting:

```csharp
swapper.Equip(slipperPrefab);   // both feet
swapper.EquipNext();            // cycle the configured list
swapper.Unequip();              // barefoot (she has stocking feet underneath)
```

Her sock feet are modelled deliberately slim so any slipper shell swallows them.
To add a variant, add a builder to `SLIPPER_VARIANTS` in the generator; keep the
silhouette within roughly x +/-0.06, y -0.14..+0.13, z 0..0.15 of the socket.

Shipped variants: `Slipper_Classic` (fluffy), `Slipper_Bunny`, `Slipper_Rocket`.

## Unity import settings

Set these on `Granny_Walker.fbx` after the first import:

* **Model** — Scale Factor 1, Convert Units on, Import Cameras/Lights off
* **Rig** — Animation Type **Generic**, Avatar Definition *Create From This Model*,
  Root node `Root`. Humanoid is the wrong fit: the walker bones are part of the
  same skeleton and would be dropped from the avatar.
* **Materials** — Material Creation Mode *Standard*, Location *Use External
  Materials (Legacy)* to extract the 20 materials, then reassign them to URP/Lit.
  `M_Rocket_Glow` and `M_Slipper_Rocket_Glow` are authored as emissive.

On each `Granny_Walker@<Clip>.fbx`, set **Rig -> Avatar Definition** to *Copy From
Other Avatar* and point it at the avatar on `Granny_Walker.fbx`. Mark `Idle`,
`Drive` and `Boost` as Loop Time; the turn clips loop cleanly too because they
hold their final pose.

Two `SkinnedMeshRenderer`s come in, `SK_Granny` and `SK_Walker`, sharing one
`Animator` — keep them split so the walker can be reskinned or hidden
independently.
