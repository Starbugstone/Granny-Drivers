"""Render pose sheets from the generated Granny_Walker.blend for review.

    blender --background Tools/Blender/Granny_Walker.blend \
        --python Tools/Blender/preview_anim.py -- Drive 0 10 20 30
"""

import bpy
import sys
from pathlib import Path
from mathutils import Vector

argv = sys.argv[sys.argv.index("--") + 1:] if "--" in sys.argv else []
view = "front" if "--front" in argv else "quarter"
argv = [a for a in argv if not a.startswith("--")]
clip = argv[0] if argv else "Idle"
frames = [int(x) for x in argv[1:]] or [0]

OUT = Path(__file__).resolve().parent / "preview"
OUT.mkdir(parents=True, exist_ok=True)

scene = bpy.context.scene
arm = bpy.data.objects["GrannyRig"]
act = bpy.data.actions[clip]
if arm.animation_data is None:
    arm.animation_data_create()
arm.animation_data.action = act
if hasattr(arm.animation_data, "action_slot") and len(act.slots):
    arm.animation_data.action_slot = act.slots[0]

for engine in ("BLENDER_EEVEE_NEXT", "BLENDER_EEVEE", "BLENDER_WORKBENCH"):
    try:
        scene.render.engine = engine
        break
    except TypeError:
        continue
scene.render.resolution_x = 520
scene.render.resolution_y = 640

world = bpy.data.worlds.new("W")
world.use_nodes = True
world.node_tree.nodes["Background"].inputs[0].default_value = (0.16, 0.18, 0.22, 1)
scene.world = world

for name, loc, energy in (("K", (2.6, -3.2, 3.4), 900),
                          ("F", (-3.4, -2.2, 1.8), 320),
                          ("R", (0.4, 3.6, 2.6), 500)):
    ld = bpy.data.lights.new(name, type="AREA")
    ld.energy = energy
    ld.size = 3.0
    lo = bpy.data.objects.new(name, ld)
    lo.location = loc
    lo.rotation_euler = (Vector((0, 0, 0.9)) - Vector(loc)).to_track_quat("-Z", "Y").to_euler()
    scene.collection.objects.link(lo)

cam_data = bpy.data.cameras.new("C")
cam_data.lens = 50
cam = bpy.data.objects.new("C", cam_data)
scene.collection.objects.link(cam)
scene.camera = cam
loc = Vector((0.0, -3.6, 1.30)) if view == "front" else Vector((2.4, -2.9, 1.55))
cam.location = loc
cam.rotation_euler = (Vector((0, -0.15, 0.72)) - loc).to_track_quat("-Z", "Y").to_euler()

for f in frames:
    scene.frame_set(f)
    scene.render.filepath = str(OUT / ("anim_%s_%s_%03d.png" % (clip, view, f)))
    bpy.ops.render.render(write_still=True)
    print("  ->", scene.render.filepath)
