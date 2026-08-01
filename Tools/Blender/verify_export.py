"""Round-trip the exported FBX files and report what Unity will see.

    blender --background --python Tools/Blender/verify_export.py
"""

import bpy
import sys
from pathlib import Path
from mathutils import Vector

ROOT = Path(__file__).resolve().parents[2]
MODEL_DIR = ROOT / "Assets" / "Art" / "Models" / "Granny"
PREVIEW_DIR = Path(__file__).resolve().parent / "preview"

bpy.ops.wm.read_factory_settings(use_empty=True)

# ---- base model ------------------------------------------------------------
bpy.ops.import_scene.fbx(filepath=str(MODEL_DIR / "Granny_Walker.fbx"))
print("\n=== Granny_Walker.fbx ===")
arm = None
for o in bpy.context.scene.objects:
    if o.type == "ARMATURE":
        arm = o
        print("  ARMATURE %-14s bones=%d" % (o.name, len(o.data.bones)))
    elif o.type == "MESH":
        vgs = len(o.vertex_groups)
        mats = [m.name for m in o.data.materials]
        skinned = any(m.type == "ARMATURE" for m in o.modifiers)
        print("  MESH     %-14s verts=%5d tris=%5d vgroups=%2d skinned=%s mats=%d"
              % (o.name, len(o.data.vertices),
                 sum(len(p.vertices) - 2 for p in o.data.polygons), vgs, skinned,
                 len(mats)))
        print("           materials: %s" % ", ".join(sorted(mats)))
    elif o.type == "EMPTY":
        w = o.matrix_world.translation
        print("  SOCKET   %-18s parent=%-10s world=(%.3f, %.3f, %.3f)"
              % (o.name, o.parent_bone or (o.parent.name if o.parent else "-"),
                 w.x, w.y, w.z))

if arm:
    names = sorted(b.name for b in arm.data.bones)
    print("\n  bones (%d): %s" % (len(names), ", ".join(names)))
    missing = [n for n in ("Root", "Hips", "Spine", "Chest", "Head", "Walker_Root",
                           "Wheel_L", "Wheel_R", "Rocket_L", "Rocket_R",
                           "Hand_L", "Hand_R", "Foot_L", "Foot_R")
               if n not in names]
    print("  MISSING EXPECTED BONES: %s" % (missing or "none"))
    # which side is which, measured from the mesh
    for bone in ("Hand_L", "Hand_R", "Foot_L", "Foot_R"):
        if bone in arm.data.bones:
            hx = (arm.matrix_world @ arm.data.bones[bone].head_local).x
            print("  %-8s blender x=%+.3f" % (bone, hx))

# ---- animation clips -------------------------------------------------------
print("\n=== animation clips ===")
for fbx in sorted(MODEL_DIR.glob("Granny_Walker@*.fbx")):
    before = set(bpy.data.actions.keys())
    bpy.ops.import_scene.fbx(filepath=str(fbx))
    new = [a for a in bpy.data.actions if a.name not in before]
    for act in new:
        rng = act.frame_range
        try:
            chans = sum(len(cb.fcurves)
                        for layer in act.layers
                        for strip in layer.strips
                        for cb in strip.channelbags)
        except Exception:
            chans = len(getattr(act, "fcurves", []))
        print("  %-34s action=%-28s frames %.0f-%.0f  fcurves=%d"
              % (fbx.name, act.name, rng[0], rng[1], chans))
    if not new:
        print("  %-34s !! NO ACTION IMPORTED" % fbx.name)

# ---- slippers vs socket ----------------------------------------------------
print("\n=== slippers ===")
socket = bpy.data.objects.get("Slipper_Socket_L")
for fbx in sorted((MODEL_DIR / "Slippers").glob("*.fbx")):
    before = set(bpy.context.scene.objects)
    bpy.ops.import_scene.fbx(filepath=str(fbx))
    for o in set(bpy.context.scene.objects) - before:
        if o.type != "MESH":
            continue
        cos = [o.matrix_world @ Vector(c) for c in o.bound_box]
        lo = Vector((min(c.x for c in cos), min(c.y for c in cos), min(c.z for c in cos)))
        hi = Vector((max(c.x for c in cos), max(c.y for c in cos), max(c.z for c in cos)))
        print("  %-22s verts=%4d bounds x[%+.3f %+.3f] y[%+.3f %+.3f] z[%+.3f %+.3f]"
              % (o.name, len(o.data.vertices), lo.x, hi.x, lo.y, hi.y, lo.z, hi.z))
        if socket:
            # attach to the left ankle socket with an identity local transform
            o.parent = socket
            o.matrix_parent_inverse = socket.matrix_world.inverted()
            o.location = (0, 0, 0)
print("\nsocket world:", socket.matrix_world.translation if socket else "MISSING")
