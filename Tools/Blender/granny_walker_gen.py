"""
Granny Drivers - procedural asset generator for the Granny + rocket walker.

Run headless:
    blender --background --python Tools/Blender/granny_walker_gen.py

Optional args after "--":
    --no-export     build the scene but skip FBX export
    --preview       render turnaround PNGs to Tools/Blender/preview/
    --out <dir>     override the Unity model output directory

Authoring conventions
---------------------
Blender is Z-up. The character is built facing -Y so that the standard Unity
FBX axis conversion (forward = -Z, up = Y) puts her facing Unity +Z with her
left hand on Unity -X. Helper `p(x, fwd, z)` takes a *forward* distance and
flips it, so all measurements below read as "how far in front of granny".

Everything is rigid- or distance-weighted procedurally; no manual skinning.
"""

import bpy
import bmesh
import math
import sys
from math import radians
from pathlib import Path
from mathutils import Vector, Matrix, Quaternion

# ---------------------------------------------------------------------------
# paths / options
# ---------------------------------------------------------------------------

SCRIPT_DIR = Path(__file__).resolve().parent
PROJECT_ROOT = SCRIPT_DIR.parents[1]
MODEL_DIR = PROJECT_ROOT / "Assets" / "Art" / "Models" / "Granny"
SLIPPER_DIR = MODEL_DIR / "Slippers"
PREVIEW_DIR = SCRIPT_DIR / "preview"
BLEND_OUT = SCRIPT_DIR / "Granny_Walker.blend"

_argv = sys.argv[sys.argv.index("--") + 1:] if "--" in sys.argv else []
DO_EXPORT = "--no-export" not in _argv
DO_PREVIEW = "--preview" in _argv
if "--out" in _argv:
    MODEL_DIR = Path(_argv[_argv.index("--out") + 1]).resolve()
    SLIPPER_DIR = MODEL_DIR / "Slippers"

FPS = 30
MODEL_NAME = "Granny_Walker"

# ---------------------------------------------------------------------------
# small math helpers
# ---------------------------------------------------------------------------

AXIS = {"X": Vector((1, 0, 0)), "Y": Vector((0, 1, 0)), "Z": Vector((0, 0, 1))}

# rockets hang outboard of, and just below, the lower side rail
ROCKET_X = 0.352
ROCKET_Z = 0.490


def p(x, fwd, z):
    """Position from (side, forward, up). Forward is -Y in Blender."""
    return Vector((x, -fwd, z))


def mirrored(v):
    return Vector((-v.x, v.y, v.z))


def seg_dist(pt, a, b):
    ab = b - a
    denom = ab.length_squared
    t = 0.0 if denom < 1e-12 else max(0.0, min(1.0, (pt - a).dot(ab) / denom))
    return (pt - (a + ab * t)).length


def track_matrix(a, b):
    """Matrix that maps a +Z aligned primitive onto the segment a -> b."""
    d = b - a
    length = d.length
    if length < 1e-9:
        d = Vector((0, 0, 1))
        length = 1e-9
    rot = d.to_track_quat("Z", "Y").to_matrix().to_4x4()
    return Matrix.Translation((a + b) * 0.5) @ rot, length


# ---------------------------------------------------------------------------
# materials
# ---------------------------------------------------------------------------


def make_material(name, color, metallic=0.0, roughness=0.65, emission=None):
    mat = bpy.data.materials.get(name)
    if mat is None:
        mat = bpy.data.materials.new(name)
    mat.use_nodes = True
    bsdf = mat.node_tree.nodes.get("Principled BSDF")
    if bsdf:
        bsdf.inputs["Base Color"].default_value = (*color, 1.0)
        bsdf.inputs["Metallic"].default_value = metallic
        bsdf.inputs["Roughness"].default_value = roughness
        if emission is not None:
            if "Emission Color" in bsdf.inputs:
                bsdf.inputs["Emission Color"].default_value = (*emission, 1.0)
            if "Emission Strength" in bsdf.inputs:
                bsdf.inputs["Emission Strength"].default_value = 3.0
    mat.diffuse_color = (*color, 1.0)
    return mat


def build_materials():
    m = {}
    # granny
    m["skin"] = make_material("M_Granny_Skin", (0.93, 0.74, 0.65), 0.0, 0.72)
    m["hair"] = make_material("M_Granny_Hair", (0.88, 0.88, 0.91), 0.0, 0.85)
    m["cardigan"] = make_material("M_Granny_Cardigan", (0.72, 0.30, 0.42), 0.0, 0.85)
    m["dress"] = make_material("M_Granny_Dress", (0.13, 0.38, 0.42), 0.0, 0.80)
    m["stocking"] = make_material("M_Granny_Stocking", (0.90, 0.87, 0.80), 0.0, 0.80)
    m["button"] = make_material("M_Granny_Trim", (0.95, 0.86, 0.35), 0.35, 0.45)
    m["frame_dark"] = make_material("M_Granny_GlassFrame", (0.12, 0.10, 0.10), 0.2, 0.45)
    m["lens"] = make_material("M_Granny_Lens", (0.72, 0.86, 0.92), 0.0, 0.12)
    m["eye"] = make_material("M_Granny_Eye", (0.09, 0.09, 0.12), 0.0, 0.35)
    m["mouth"] = make_material("M_Granny_Mouth", (0.45, 0.16, 0.20), 0.0, 0.55)
    # walker
    m["metal"] = make_material("M_Walker_Metal", (0.70, 0.72, 0.76), 1.0, 0.32)
    m["grip"] = make_material("M_Walker_Grip", (0.13, 0.13, 0.15), 0.0, 0.72)
    m["seat"] = make_material("M_Walker_Seat", (0.22, 0.16, 0.14), 0.0, 0.62)
    m["tyre"] = make_material("M_Walker_Tyre", (0.07, 0.07, 0.08), 0.0, 0.72)
    m["hub"] = make_material("M_Walker_Hub", (0.85, 0.86, 0.90), 1.0, 0.22)
    m["tennis"] = make_material("M_Walker_TennisBall", (0.78, 0.86, 0.18), 0.0, 0.85)
    # rockets
    m["rocket"] = make_material("M_Rocket_Body", (0.83, 0.20, 0.14), 0.1, 0.38)
    m["rocket_trim"] = make_material("M_Rocket_Trim", (0.95, 0.83, 0.20), 0.2, 0.40)
    m["nozzle"] = make_material("M_Rocket_Nozzle", (0.26, 0.26, 0.29), 1.0, 0.30)
    m["glow"] = make_material("M_Rocket_Glow", (1.0, 0.55, 0.12), 0.0, 0.30,
                              emission=(1.0, 0.45, 0.10))
    # slippers
    m["slip_a"] = make_material("M_Slipper_Classic_Fluff", (0.92, 0.55, 0.68), 0.0, 0.92)
    m["slip_a_sole"] = make_material("M_Slipper_Classic_Sole", (0.35, 0.24, 0.28), 0.0, 0.72)
    m["slip_b"] = make_material("M_Slipper_Bunny_Fur", (0.94, 0.92, 0.90), 0.0, 0.92)
    m["slip_b_pink"] = make_material("M_Slipper_Bunny_Pink", (0.95, 0.62, 0.70), 0.0, 0.80)
    m["slip_b_eye"] = make_material("M_Slipper_Bunny_Eye", (0.08, 0.08, 0.10), 0.0, 0.35)
    m["slip_c"] = make_material("M_Slipper_Rocket_Shell", (0.85, 0.24, 0.16), 0.15, 0.40)
    m["slip_c_metal"] = make_material("M_Slipper_Rocket_Metal", (0.72, 0.74, 0.78), 1.0, 0.30)
    m["slip_c_glow"] = make_material("M_Slipper_Rocket_Glow", (1.0, 0.60, 0.15), 0.0, 0.30,
                                     emission=(1.0, 0.50, 0.12))
    return m


# ---------------------------------------------------------------------------
# geometry part builder
# ---------------------------------------------------------------------------


class Part:
    """One mesh chunk with a single material and a skinning rule."""

    def __init__(self, name, material, bone=None, bones=None, smooth=True):
        self.name = name
        self.material = material
        self.bone = bone          # rigid bind
        self.bones = bones        # distance-blended bind
        self.smooth = smooth
        self.bm = bmesh.new()

    # -- primitives ---------------------------------------------------------

    def tube(self, a, b, r0, r1=None, seg=14, caps=True):
        r1 = r0 if r1 is None else r1
        mat, length = track_matrix(Vector(a), Vector(b))
        bmesh.ops.create_cone(self.bm, cap_ends=caps, cap_tris=False, segments=seg,
                              radius1=r0, radius2=r1, depth=length, matrix=mat)
        return self

    def sphere(self, center, radius, scale=(1, 1, 1), useg=14, vseg=8, rot=None):
        m = Matrix.Translation(Vector(center))
        if rot is not None:
            m = m @ rot
        m = m @ Matrix.Diagonal(Vector(scale)).to_4x4()
        bmesh.ops.create_uvsphere(self.bm, u_segments=useg, v_segments=vseg,
                                  radius=radius, matrix=m)
        return self

    def box(self, center, size, rot=None):
        m = Matrix.Translation(Vector(center))
        if rot is not None:
            m = m @ rot
        m = m @ Matrix.Diagonal(Vector(size)).to_4x4()
        bmesh.ops.create_cube(self.bm, size=1.0, matrix=m)
        return self

    def torus(self, center, major_r, minor_r, axis="Z", major_seg=16, minor_seg=6,
              scale=(1, 1, 1)):
        """Manual torus - bmesh has no torus op."""
        basis = {
            "Z": (Vector((1, 0, 0)), Vector((0, 1, 0)), Vector((0, 0, 1))),
            "Y": (Vector((1, 0, 0)), Vector((0, 0, 1)), Vector((0, 1, 0))),
            "X": (Vector((0, 1, 0)), Vector((0, 0, 1)), Vector((1, 0, 0))),
        }[axis]
        ex, ey, ez = basis
        center = Vector(center)
        sc = Vector(scale)
        rings = []
        for i in range(major_seg):
            a = 2 * math.pi * i / major_seg
            radial = ex * math.cos(a) + ey * math.sin(a)
            ring = []
            for j in range(minor_seg):
                b = 2 * math.pi * j / minor_seg
                off = radial * (major_r + minor_r * math.cos(b)) + ez * (minor_r * math.sin(b))
                pos = center + Vector((off.x * sc.x, off.y * sc.y, off.z * sc.z))
                ring.append(self.bm.verts.new(pos))
            rings.append(ring)
        for i in range(major_seg):
            r0 = rings[i]
            r1 = rings[(i + 1) % major_seg]
            for j in range(minor_seg):
                k = (j + 1) % minor_seg
                try:
                    self.bm.faces.new((r0[j], r0[k], r1[k], r1[j]))
                except ValueError:
                    pass
        self.bm.verts.ensure_lookup_table()
        return self

    # -- output -------------------------------------------------------------

    def to_object(self, collection):
        bmesh.ops.recalc_face_normals(self.bm, faces=self.bm.faces)
        mesh = bpy.data.meshes.new(self.name)
        self.bm.to_mesh(mesh)
        self.bm.free()
        mesh.materials.append(self.material)
        for poly in mesh.polygons:
            poly.use_smooth = self.smooth
        obj = bpy.data.objects.new(self.name, mesh)
        collection.objects.link(obj)
        return obj


def assign_weights(obj, part, bone_segments):
    """Create vertex groups on a freshly built part object."""
    mesh = obj.data
    if part.bone:
        vg = obj.vertex_groups.new(name=part.bone)
        vg.add([v.index for v in mesh.vertices], 1.0, "REPLACE")
        return

    names = part.bones or []
    groups = {n: obj.vertex_groups.new(name=n) for n in names}
    for v in mesh.vertices:
        scored = []
        for n in names:
            a, b = bone_segments[n]
            d = seg_dist(v.co, a, b)
            scored.append((1.0 / (d + 0.02) ** 4, n))
        scored.sort(reverse=True)
        scored = scored[:3]
        total = sum(s for s, _ in scored)
        for s, n in scored:
            groups[n].add([v.index], s / total, "REPLACE")


def build_mesh_object(name, parts, collection, bone_segments, armature):
    objs = []
    for part in parts:
        o = part.to_object(collection)
        assign_weights(o, part, bone_segments)
        objs.append(o)

    bpy.ops.object.select_all(action="DESELECT")
    for o in objs:
        o.select_set(True)
    bpy.context.view_layer.objects.active = objs[0]
    bpy.ops.object.join()
    merged = bpy.context.view_layer.objects.active
    merged.name = name
    merged.data.name = name + "_Mesh"

    bmesh_cleanup(merged)
    unwrap(merged)
    try:
        bpy.ops.object.shade_auto_smooth(angle=radians(38))
    except Exception as exc:  # noqa: BLE001 - older/newer API fallback
        print("  shade_auto_smooth unavailable (%s); using flat/smooth flags" % exc)

    merged.parent = armature
    mod = merged.modifiers.new("Armature", "ARMATURE")
    mod.object = armature
    return merged


def bmesh_cleanup(obj):
    bm = bmesh.new()
    bm.from_mesh(obj.data)
    bmesh.ops.remove_doubles(bm, verts=bm.verts, dist=0.0002)
    bm.to_mesh(obj.data)
    bm.free()
    obj.data.update()


def unwrap(obj):
    bpy.ops.object.select_all(action="DESELECT")
    obj.select_set(True)
    bpy.context.view_layer.objects.active = obj
    try:
        bpy.ops.object.mode_set(mode="EDIT")
        bpy.ops.mesh.select_all(action="SELECT")
        bpy.ops.uv.smart_project(angle_limit=radians(66), island_margin=0.01)
        bpy.ops.object.mode_set(mode="OBJECT")
    except Exception as exc:  # noqa: BLE001
        print("  smart_project failed (%s); adding empty UV layer" % exc)
        if bpy.context.object.mode != "OBJECT":
            bpy.ops.object.mode_set(mode="OBJECT")
        if not obj.data.uv_layers:
            obj.data.uv_layers.new(name="UVMap")


# ---------------------------------------------------------------------------
# skeleton
# ---------------------------------------------------------------------------

# head, tail, parent, deform
BONE_SPECS = [
    ("Root",        p(0, 0, 0),          p(0, 0.16, 0),        None,       False),
    ("Hips",        p(0, 0, 0.70),       p(0, 0, 0.80),        "Root",     True),
    ("Spine",       p(0, 0, 0.80),       p(0, 0.03, 0.94),     "Hips",     True),
    ("Chest",       p(0, 0.03, 0.94),    p(0, 0.06, 1.08),     "Spine",    True),
    ("Neck",        p(0, 0.06, 1.08),    p(0, 0.09, 1.16),     "Chest",    True),
    ("Head",        p(0, 0.09, 1.16),    p(0, 0.11, 1.33),     "Neck",     True),
    # walker rides under Root so gameplay code can drive it independently
    ("Walker_Root", p(0, 0.22, 0.02),    p(0, 0.22, 0.16),     "Root",     True),
]

# generated for both sides; head, tail, parent-suffix pattern
SIDE_BONE_SPECS = [
    ("Shoulder",   p(0.045, 0.03, 1.10), p(0.15, 0.00, 1.12), "Chest",       True),
    ("UpperArm",   p(0.15, 0.00, 1.12),  p(0.26, 0.06, 1.05), "Shoulder_%s", True),
    ("LowerArm",   p(0.26, 0.06, 1.05),  p(0.27, 0.14, 0.95), "UpperArm_%s", True),
    ("Hand",       p(0.27, 0.14, 0.95),  p(0.27, 0.21, 0.94), "LowerArm_%s", True),
    ("Thigh",      p(0.09, 0.00, 0.70),  p(0.10, 0.01, 0.40), "Hips",        True),
    ("Shin",       p(0.10, 0.01, 0.40),  p(0.10, 0.00, 0.09), "Thigh_%s",    True),
    ("Foot",       p(0.10, 0.00, 0.09),  p(0.10, 0.11, 0.03), "Shin_%s",     True),
    # walker steering + wheels. Caster points down (local Y = world -Z) so its
    # own Y rotation is the steering yaw. Wheel points outward along X so its
    # own Y rotation is the roll spin - both are roll-independent.
    ("Caster",     p(0.25, 0.55, 0.16),  p(0.25, 0.55, 0.055), "Walker_Root", True),
    ("Wheel",      p(0.25, 0.55, 0.055), p(0.31, 0.55, 0.055), "Caster_%s",   True),
    ("Rocket",     p(ROCKET_X, 0.60, ROCKET_Z), p(ROCKET_X, 0.16, ROCKET_Z),
     "Walker_Root", True),
]

# Blender is right-handed, so for a character facing -Y the anatomical left is
# +X (left = up x forward). Keep _L bones on that side so the naming matches the
# body. World-axis rotation signs follow from the same frame:
#   +X = lean forward, +Y = lean/roll to HER LEFT, +Z = yaw to HER LEFT.
SIDE_SIGN = {"L": 1.0, "R": -1.0}


def side_pos(v, side):
    return Vector((v.x * SIDE_SIGN[side], v.y, v.z))


def build_armature(collection):
    arm_data = bpy.data.armatures.new("GrannyRig")
    arm_obj = bpy.data.objects.new("GrannyRig", arm_data)
    collection.objects.link(arm_obj)
    bpy.context.view_layer.objects.active = arm_obj
    arm_obj.select_set(True)
    bpy.ops.object.mode_set(mode="EDIT")

    ebs = arm_data.edit_bones
    segments = {}

    def add(name, head, tail, parent, deform):
        eb = ebs.new(name)
        eb.head = head
        eb.tail = tail
        eb.use_connect = False
        eb.use_deform = deform
        if parent:
            eb.parent = ebs[parent]
        segments[name] = (Vector(head), Vector(tail))

    for name, head, tail, parent, deform in BONE_SPECS:
        add(name, head, tail, parent, deform)

    for side in ("L", "R"):
        for base, head, tail, parent, deform in SIDE_BONE_SPECS:
            parent_name = parent % side if "%s" in parent else parent
            add("%s_%s" % (base, side),
                side_pos(head, side), side_pos(tail, side),
                parent_name, deform)

    bpy.ops.object.mode_set(mode="OBJECT")
    for pb in arm_obj.pose.bones:
        pb.rotation_mode = "QUATERNION"
    return arm_obj, segments


# ---------------------------------------------------------------------------
# attachment sockets (empties parented to bones, world-aligned)
# ---------------------------------------------------------------------------


def add_socket(collection, armature, name, bone, location, yaw_deg=0.0, size=0.05,
               display="ARROWS"):
    empty = bpy.data.objects.new(name, None)
    empty.empty_display_type = display
    empty.empty_display_size = size
    collection.objects.link(empty)
    empty.parent = armature
    empty.parent_type = "BONE"
    empty.parent_bone = bone
    bpy.context.view_layer.update()
    empty.matrix_parent_inverse = Matrix.Identity(4)
    empty.matrix_world = (Matrix.Translation(Vector(location))
                          @ Matrix.Rotation(radians(yaw_deg), 4, "Z"))
    return empty


def attach_to_socket(obj, socket):
    """Mirrors what Unity does: child of the socket, identity local transform."""
    obj.parent = socket
    obj.matrix_parent_inverse = Matrix.Identity(4)
    obj.location = Vector((0, 0, 0))
    obj.rotation_euler = (0, 0, 0)
    return obj


# ---------------------------------------------------------------------------
# granny mesh
# ---------------------------------------------------------------------------


def granny_parts(M):
    parts = []
    torso_bones = ["Hips", "Spine", "Chest"]

    # ---- head -------------------------------------------------------------
    head = Part("Granny_Head", M["skin"], bone="Head")
    head.sphere(p(0, 0.10, 1.245), 0.105, scale=(1.0, 1.02, 1.10), useg=18, vseg=12)
    head.sphere(p(0, 0.150, 1.183), 0.064, scale=(1.0, 0.78, 0.74))          # jowls
    head.tube(p(0, 0.180, 1.242), p(0, 0.246, 1.216), 0.030, 0.017, seg=10)  # nose
    head.sphere(p(0, 0.249, 1.215), 0.018, useg=10, vseg=8)
    for s in (-1, 1):
        head.sphere(p(0.100 * s, 0.085, 1.248), 0.026, scale=(0.5, 1.0, 1.25))  # ears
    parts.append(head)

    neck = Part("Granny_Neck", M["skin"], bones=["Neck", "Chest", "Head"])
    neck.tube(p(0, 0.045, 1.055), p(0, 0.085, 1.165), 0.046, 0.042, seg=12)
    parts.append(neck)

    mouth = Part("Granny_Mouth", M["mouth"], bone="Head")
    mouth.sphere(p(0, 0.186, 1.176), 0.031, scale=(1.0, 0.30, 0.50), useg=12, vseg=8)
    parts.append(mouth)

    eyes = Part("Granny_Eyes", M["eye"], bone="Head")
    for s in (-1, 1):
        eyes.sphere(p(0.045 * s, 0.168, 1.258), 0.017, useg=10, vseg=8)
    parts.append(eyes)

    hair = Part("Granny_Hair", M["hair"], bone="Head")
    hair.sphere(p(0, 0.065, 1.272), 0.111, scale=(1.02, 1.02, 0.92), useg=18, vseg=12)
    hair.sphere(p(0, -0.045, 1.325), 0.062, useg=14, vseg=10)                 # bun
    for s in (-1, 1):
        hair.sphere(p(0.088 * s, 0.020, 1.310), 0.036, useg=10, vseg=8)       # curls
        hair.sphere(p(0.098 * s, 0.095, 1.268), 0.030, useg=10, vseg=8)
    parts.append(hair)

    glasses = Part("Granny_GlassFrames", M["frame_dark"], bone="Head")
    for s in (-1, 1):
        glasses.torus(p(0.046 * s, 0.184, 1.258), 0.040, 0.006, axis="Y",
                      major_seg=16, minor_seg=5)
        glasses.tube(p(0.083 * s, 0.180, 1.264), p(0.104 * s, 0.075, 1.262),
                     0.005, seg=6)
    glasses.tube(p(-0.010, 0.190, 1.258), p(0.010, 0.190, 1.258), 0.005, seg=6)
    parts.append(glasses)

    lens = Part("Granny_Lenses", M["lens"], bone="Head")
    for s in (-1, 1):
        lens.tube(p(0.046 * s, 0.180, 1.258), p(0.046 * s, 0.186, 1.258),
                  0.038, seg=16)
    parts.append(lens)

    # ---- torso ------------------------------------------------------------
    torso = Part("Granny_Cardigan", M["cardigan"], bones=torso_bones)
    torso.tube(p(0, 0.005, 0.660), p(0, 0.020, 0.900), 0.172, 0.163, seg=18)
    torso.tube(p(0, 0.020, 0.900), p(0, 0.055, 1.095), 0.163, 0.140, seg=18)
    torso.sphere(p(0, -0.085, 1.030), 0.100, scale=(1.15, 0.95, 0.85),
                 useg=14, vseg=10)                                            # hunch
    for s in (-1, 1):
        torso.sphere(p(0.140 * s, 0.030, 1.100), 0.072, useg=14, vseg=10)     # shoulders
    parts.append(torso)

    buttons = Part("Granny_Buttons", M["button"], bones=torso_bones)
    for i, z in enumerate((0.78, 0.86, 0.94, 1.02)):
        buttons.sphere(p(0, 0.150 + i * 0.008, z), 0.014, scale=(1, 0.5, 1),
                       useg=8, vseg=6)
    parts.append(buttons)

    skirt = Part("Granny_Skirt", M["dress"], bones=["Hips", "Spine"])
    skirt.tube(p(0, 0.010, 0.700), p(0, 0.010, 0.440), 0.178, 0.240, seg=20)
    skirt.torus(p(0, 0.010, 0.442), 0.238, 0.016, axis="Z", major_seg=20, minor_seg=6)
    parts.append(skirt)

    # ---- arms -------------------------------------------------------------
    for side in ("L", "R"):
        s = SIDE_SIGN[side]
        shoulder = side_pos(p(0.15, 0.00, 1.12), side)
        elbow = side_pos(p(0.26, 0.06, 1.05), side)
        wrist = side_pos(p(0.27, 0.14, 0.95), side)

        arm = Part("Granny_Arm_%s" % side, M["cardigan"])
        arm.bones = ["UpperArm_%s" % side, "LowerArm_%s" % side, "Shoulder_%s" % side]
        arm.tube(shoulder, elbow, 0.058, 0.050, seg=12)
        arm.sphere(elbow, 0.051, useg=12, vseg=8)
        arm.tube(elbow, wrist, 0.050, 0.040, seg=12)
        cuff_dir = (wrist - elbow).normalized()
        arm.tube(wrist - cuff_dir * 0.034, wrist + cuff_dir * 0.004,
                 0.046, 0.044, seg=12)                                        # cuff
        parts.append(arm)

        hand = Part("Granny_Hand_%s" % side, M["skin"], bone="Hand_%s" % side)
        hand.sphere(side_pos(p(0.27, 0.175, 0.945), side), 0.046,
                    scale=(0.85, 1.35, 0.95), useg=12, vseg=8)
        hand.sphere(side_pos(p(0.242, 0.150, 0.930), side), 0.020,
                    scale=(1.0, 1.4, 0.9), useg=8, vseg=6)                    # thumb
        parts.append(hand)

    # ---- legs -------------------------------------------------------------
    for side in ("L", "R"):
        hip = side_pos(p(0.09, 0.00, 0.560), side)
        knee = side_pos(p(0.10, 0.01, 0.400), side)
        ankle = side_pos(p(0.10, 0.00, 0.095), side)

        leg = Part("Granny_Leg_%s" % side, M["stocking"])
        leg.bones = ["Thigh_%s" % side, "Shin_%s" % side]
        leg.tube(hip, knee, 0.077, 0.062, seg=12)
        leg.sphere(knee, 0.063, useg=12, vseg=8)
        leg.tube(knee, ankle, 0.058, 0.046, seg=12)
        leg.sphere(ankle, 0.047, useg=12, vseg=8)
        parts.append(leg)

        # kept deliberately slim so any slipper variant swallows it cleanly
        foot = Part("Granny_Foot_%s" % side, M["stocking"], bone="Foot_%s" % side)
        foot.sphere(side_pos(p(0.10, 0.030, 0.048), side), 0.050,
                    scale=(0.70, 1.75, 0.78), useg=16, vseg=10)
        foot.sphere(side_pos(p(0.10, -0.012, 0.070), side), 0.042,
                    scale=(0.82, 0.88, 0.85), useg=12, vseg=8)   # heel/ankle blend
        parts.append(foot)

    return parts


# ---------------------------------------------------------------------------
# walker mesh
# ---------------------------------------------------------------------------

FRAME_R = 0.015


def walker_parts(M):
    parts = []
    frame = Part("Walker_Frame", M["metal"], bone="Walker_Root")
    seat = Part("Walker_Seat", M["seat"], bone="Walker_Root")
    grips = Part("Walker_Grips", M["grip"], bone="Walker_Root")
    tennis = Part("Walker_TennisBalls", M["tennis"], bone="Walker_Root")

    for side in ("L", "R"):
        rear_bottom = side_pos(p(0.27, 0.20, 0.045), side)
        rear_top = side_pos(p(0.27, 0.19, 0.900), side)
        front_bottom = side_pos(p(0.25, 0.55, 0.160), side)
        front_top = side_pos(p(0.26, 0.52, 0.860), side)

        # uprights
        frame.tube(rear_bottom, rear_top, FRAME_R, seg=10)
        frame.tube(front_bottom, front_top, FRAME_R, seg=10)
        # side rails (upper + lower); the lower one carries the rocket clamps
        frame.tube(rear_top, front_top, FRAME_R, seg=10)
        frame.tube(side_pos(p(0.27, 0.20, 0.560), side),
                   side_pos(p(0.255, 0.545, 0.560), side), FRAME_R, seg=10)
        # handle: short riser then a backwards grip bar
        frame.tube(rear_top, side_pos(p(0.27, 0.175, 0.945), side), FRAME_R, seg=10)
        frame.tube(side_pos(p(0.27, 0.175, 0.945), side),
                   side_pos(p(0.27, 0.100, 0.942), side), FRAME_R, seg=10)
        grips.tube(side_pos(p(0.27, 0.230, 0.947), side),
                   side_pos(p(0.27, 0.108, 0.943), side), 0.024, seg=12)
        grips.sphere(side_pos(p(0.27, 0.236, 0.947), side), 0.024, useg=12, vseg=8)

        # rear glide - the classic tennis balls
        tennis.sphere(side_pos(p(0.27, 0.20, 0.036), side), 0.038, useg=14, vseg=10)

    # cross bars
    frame.tube(p(-0.27, 0.19, 0.900), p(0.27, 0.19, 0.900), FRAME_R, seg=10)
    frame.tube(p(-0.25, 0.545, 0.300), p(0.25, 0.545, 0.300), FRAME_R, seg=10)
    frame.tube(p(-0.27, 0.20, 0.300), p(-0.25, 0.545, 0.300), FRAME_R, seg=10)
    frame.tube(p(0.27, 0.20, 0.300), p(0.25, 0.545, 0.300), FRAME_R, seg=10)
    frame.tube(p(-0.27, 0.20, 0.620), p(0.27, 0.20, 0.620), FRAME_R, seg=10)
    frame.tube(p(-0.255, 0.545, 0.620), p(0.255, 0.545, 0.620), FRAME_R, seg=10)

    # padded seat slung between the side rails
    seat.sphere(p(0, 0.378, 0.645), 0.20, scale=(1.15, 0.78, 0.16), useg=20, vseg=12)
    seat.box(p(0, 0.378, 0.628), (0.44, 0.29, 0.014))
    parts += [frame, seat, grips, tennis]

    # ---- front casters + trolley wheels -----------------------------------
    for side in ("L", "R"):
        fork = Part("Walker_Fork_%s" % side, M["metal"], bone="Caster_%s" % side)
        fork.tube(side_pos(p(0.25, 0.55, 0.170), side),
                  side_pos(p(0.25, 0.55, 0.110), side), 0.019, seg=10)
        fork.box(side_pos(p(0.25, 0.55, 0.110), side), (0.072, 0.046, 0.018))
        for xo in (-0.029, 0.029):
            fork.box(side_pos(p(0.25, 0.55, 0.082), side) + Vector((xo, 0, 0)),
                     (0.012, 0.040, 0.062))
        parts.append(fork)

        wheel = Part("Walker_Wheel_%s" % side, M["tyre"], bone="Wheel_%s" % side)
        c = side_pos(p(0.25, 0.55, 0.055), side)
        wheel.tube(c + Vector((-0.017, 0, 0)), c + Vector((0.017, 0, 0)),
                   0.055, seg=18)
        wheel.torus(c, 0.048, 0.013, axis="X", major_seg=18, minor_seg=6)
        parts.append(wheel)

        hub = Part("Walker_Hub_%s" % side, M["hub"], bone="Wheel_%s" % side)
        hub.tube(c + Vector((-0.021, 0, 0)), c + Vector((0.021, 0, 0)), 0.020, seg=10)
        for k in range(4):
            a = math.pi * k / 4
            d = Vector((0, math.cos(a), math.sin(a))) * 0.040
            hub.tube(c - d, c + d, 0.007, seg=6)
        parts.append(hub)

    # ---- rockets ----------------------------------------------------------
    rx, rz = ROCKET_X, ROCKET_Z
    for side in ("L", "R"):
        bone = "Rocket_%s" % side
        nose = side_pos(p(rx, 0.675, rz), side)
        body_f = side_pos(p(rx, 0.590, rz), side)
        body_b = side_pos(p(rx, 0.272, rz), side)
        nozzle_b = side_pos(p(rx, 0.210, rz), side)

        body = Part("Rocket_Body_%s" % side, M["rocket"], bone=bone)
        body.tube(body_f, body_b, 0.046, seg=16)
        body.tube(body_f, nose, 0.046, 0.004, seg=16)
        body.sphere(nose, 0.006, useg=8, vseg=6)
        # fins swept back around the tail, in the body colour so they read as
        # part of the silhouette rather than loose plates
        for k in range(3):
            a = radians(90 + k * 120)
            d = Vector((0, math.cos(a), math.sin(a)))
            centre = side_pos(p(rx, 0.304, rz), side) + d * 0.052
            body.box(centre, (0.008, 0.078, 0.042), rot=Matrix.Rotation(a, 4, "X"))
        parts.append(body)

        trim = Part("Rocket_Trim_%s" % side, M["rocket_trim"], bone=bone)
        for fwd in (0.545, 0.342):
            trim.torus(side_pos(p(rx, fwd, rz), side), 0.045, 0.008, axis="X",
                       major_seg=16, minor_seg=5)
        parts.append(trim)

        nz = Part("Rocket_Nozzle_%s" % side, M["nozzle"], bone=bone)
        nz.tube(body_b, nozzle_b, 0.038, 0.056, seg=16, caps=False)
        nz.torus(nozzle_b, 0.054, 0.007, axis="X", major_seg=16, minor_seg=5)
        parts.append(nz)

        glow = Part("Rocket_Glow_%s" % side, M["glow"], bone=bone)
        glow.tube(side_pos(p(rx, 0.226, rz), side),
                  side_pos(p(rx, 0.222, rz), side), 0.046, seg=16)
        parts.append(glow)

        # saddle brackets: a strap round the tube and a stub up to the side rail
        clamp = Part("Rocket_Clamp_%s" % side, M["metal"], bone=bone)
        for fwd in (0.500, 0.380):
            rail_x = 0.27 - 0.015 * (fwd - 0.20) / 0.345
            clamp.tube(side_pos(p(rx, fwd - 0.014, rz), side),
                       side_pos(p(rx, fwd + 0.014, rz), side), 0.050, seg=16)
            clamp.box(side_pos(p((rx + rail_x) * 0.5, fwd, rz + 0.052), side),
                      (rx - rail_x + 0.02, 0.026, 0.044))
        parts.append(clamp)

    return parts


# ---------------------------------------------------------------------------
# slippers (separate, interchangeable, authored around the ankle socket)
# ---------------------------------------------------------------------------

GROUND = -0.095  # socket sits at ankle height; sole plane relative to socket


def slipper_base(part_sole, part_upper):
    """Shared sole + vamp. Toe points forward (-Y); sole bottom sits on GROUND."""
    part_sole.box((0, 0, GROUND + 0.017), (0.108, 0.225, 0.034))
    for y in (-0.103, 0.103):
        part_sole.sphere((0, y, GROUND + 0.024), 0.054,
                         scale=(1.0, 0.55, 0.45), useg=14, vseg=8)

    # vamp over the toes, then the heel counter
    part_upper.sphere((0, -0.060, GROUND + 0.062), 0.060,
                      scale=(0.95, 1.32, 0.86), useg=16, vseg=10)
    part_upper.box((0, 0.022, GROUND + 0.058), (0.114, 0.120, 0.064))
    part_upper.sphere((0, 0.094, GROUND + 0.058), 0.056,
                      scale=(1.0, 0.62, 0.82), useg=14, vseg=8)


def slipper_classic(M):
    sole = Part("Slipper_Classic_Sole", M["slip_a_sole"])
    upper = Part("Slipper_Classic_Upper", M["slip_a"])
    slipper_base(sole, upper)
    # fluffy collar around the opening
    upper.torus((0, 0.040, GROUND + 0.094), 0.056, 0.024, axis="Z",
                major_seg=18, minor_seg=8, scale=(0.95, 1.05, 0.9))
    for k in range(8):
        a = 2 * math.pi * k / 8
        upper.sphere((math.cos(a) * 0.058, 0.040 + math.sin(a) * 0.062,
                      GROUND + 0.100), 0.022, useg=8, vseg=6)
    return [sole, upper]


def slipper_bunny(M):
    sole = Part("Slipper_Bunny_Sole", M["slip_a_sole"])
    upper = Part("Slipper_Bunny_Body", M["slip_b"])
    slipper_base(sole, upper)
    upper.torus((0, 0.040, GROUND + 0.094), 0.056, 0.022, axis="Z",
                major_seg=18, minor_seg=8, scale=(0.95, 1.05, 0.9))
    # ears flopping forward over the toe
    for s in (-1, 1):
        upper.tube((0.028 * s, -0.070, GROUND + 0.094),
                   (0.044 * s, -0.150, GROUND + 0.131), 0.020, 0.026, seg=10)
        upper.sphere((0.044 * s, -0.150, GROUND + 0.131), 0.026,
                     scale=(0.8, 1.3, 1.0), useg=10, vseg=8)

    pink = Part("Slipper_Bunny_Face", M["slip_b_pink"])
    pink.sphere((0, -0.118, GROUND + 0.064), 0.017, useg=10, vseg=8)          # nose
    for s in (-1, 1):
        pink.sphere((0.040 * s, -0.148, GROUND + 0.132), 0.017,
                    scale=(0.55, 1.15, 0.85), useg=8, vseg=6)                 # inner ear
    eyes = Part("Slipper_Bunny_Eyes", M["slip_b_eye"])
    for s in (-1, 1):
        eyes.sphere((0.030 * s, -0.101, GROUND + 0.088), 0.013, useg=10, vseg=8)
    return [sole, upper, pink, eyes]


def slipper_rocket(M):
    sole = Part("Slipper_Rocket_Sole", M["slip_c_metal"])
    upper = Part("Slipper_Rocket_Shell", M["slip_c"])
    slipper_base(sole, upper)
    upper.torus((0, 0.040, GROUND + 0.094), 0.055, 0.016, axis="Z",
                major_seg=18, minor_seg=6, scale=(0.95, 1.05, 0.9))
    # side intake fins
    for s in (-1, 1):
        upper.box((0.052 * s, 0.010, GROUND + 0.058), (0.014, 0.110, 0.044))

    metal = Part("Slipper_Rocket_Booster", M["slip_c_metal"])
    metal.tube((0, 0.055, GROUND + 0.050), (0, 0.135, GROUND + 0.050),
               0.034, 0.040, seg=14)
    metal.torus((0, 0.135, GROUND + 0.050), 0.039, 0.007, axis="Y",
                major_seg=14, minor_seg=5)
    glow = Part("Slipper_Rocket_Glow", M["slip_c_glow"])
    glow.tube((0, 0.128, GROUND + 0.050), (0, 0.132, GROUND + 0.050), 0.033, seg=14)
    return [sole, upper, metal, glow]


SLIPPER_VARIANTS = {
    "Slipper_Classic": slipper_classic,
    "Slipper_Bunny": slipper_bunny,
    "Slipper_Rocket": slipper_rocket,
}


def build_slippers(M, collection):
    built = {}
    for name, fn in SLIPPER_VARIANTS.items():
        parts = fn(M)
        objs = [part.to_object(collection) for part in parts]
        bpy.ops.object.select_all(action="DESELECT")
        for o in objs:
            o.select_set(True)
        bpy.context.view_layer.objects.active = objs[0]
        bpy.ops.object.join()
        obj = bpy.context.view_layer.objects.active
        obj.name = name
        obj.data.name = name + "_Mesh"
        bmesh_cleanup(obj)
        # snap the sole onto the floor plane: the socket sits at ankle height, so
        # GROUND is where z=0 of the world ends up once the slipper is attached
        min_z = min(v.co.z for v in obj.data.vertices)
        if abs(min_z - GROUND) > 1e-5:
            for v in obj.data.vertices:
                v.co.z += GROUND - min_z
            obj.data.update()
        unwrap(obj)
        try:
            bpy.ops.object.shade_auto_smooth(angle=radians(38))
        except Exception:  # noqa: BLE001
            pass
        built[name] = obj
    return built


# ---------------------------------------------------------------------------
# animation
# ---------------------------------------------------------------------------


class Animator:
    def __init__(self, arm_obj):
        self.obj = arm_obj
        if arm_obj.animation_data is None:
            arm_obj.animation_data_create()
        self.action = None

    # -- action lifecycle ---------------------------------------------------

    def begin(self, name):
        self.reset_pose()
        act = bpy.data.actions.new(name)
        act.use_fake_user = True
        self.obj.animation_data.action = act
        self._ensure_slot(act)
        self.action = act
        return act

    def _ensure_slot(self, act):
        ad = self.obj.animation_data
        if not hasattr(ad, "action_slot"):
            return  # pre-4.4 legacy actions
        try:
            if len(act.slots) == 0:
                try:
                    slot = act.slots.new(id_type="OBJECT", name=self.obj.name)
                except TypeError:
                    slot = act.slots.new("OBJECT", self.obj.name)
            else:
                slot = act.slots[0]
            ad.action_slot = slot
        except Exception as exc:  # noqa: BLE001
            print("  slot assignment fallback (%s)" % exc)

    def reset_pose(self):
        for pb in self.obj.pose.bones:
            pb.rotation_quaternion = Quaternion((1, 0, 0, 0))
            pb.location = Vector((0, 0, 0))
            pb.scale = Vector((1, 1, 1))

    # -- keying -------------------------------------------------------------

    def _to_bone_space(self, pb, world_vec):
        return pb.bone.matrix_local.to_3x3().inverted() @ Vector(world_vec)

    def rot(self, bone, frame, *axes, loc=None):
        """Rotate about *world* axes, e.g. rot("Spine", 12, ("X", 8), ("Z", -5)).

        +X = lean forward, +Y = roll to her left, +Z = yaw to her left.
        """
        pb = self.obj.pose.bones[bone]
        q = Quaternion((1, 0, 0, 0))
        for axis, deg in axes:
            local_axis = self._to_bone_space(pb, AXIS[axis]).normalized()
            q = q @ Quaternion(local_axis, radians(deg))
        pb.rotation_quaternion = q
        pb.keyframe_insert("rotation_quaternion", frame=frame)
        if loc is not None:
            pb.location = self._to_bone_space(pb, Vector(loc))
            pb.keyframe_insert("location", frame=frame)

    def loc(self, bone, frame, world_offset):
        pb = self.obj.pose.bones[bone]
        pb.location = self._to_bone_space(pb, Vector(world_offset))
        pb.keyframe_insert("location", frame=frame)

    def hold(self, bone, frames, *axes, loc=None):
        for f in frames:
            self.rot(bone, f, *axes, loc=loc)

    def spin_wheels(self, start, end, revolutions, step=1):
        """Roll both trolley wheels about their own long axis (world X)."""
        total = 360.0 * revolutions
        span = max(1, end - start)
        f = start
        while f <= end:
            t = (f - start) / span
            for side in ("L", "R"):
                self.rot("Wheel_%s" % side, f, ("X", total * t))
            f += step


def other(side):
    return "R" if side == "L" else "L"


def bone_for(base, side):
    return "%s_%s" % (base, side)


# --- individual clips -------------------------------------------------------


def clip_idle(a):
    a.begin("Idle")
    end = 60
    for f, breath in ((0, 0.0), (18, 2.4), (34, 0.4), (48, 1.6), (60, 0.0)):
        a.rot("Chest", f, ("X", -breath * 0.6))
        a.rot("Spine", f, ("X", breath * 0.35))
        a.rot("Head", f, ("X", breath * 0.5))
    for f, sway in ((0, 0.0), (20, 1.2), (40, -1.0), (60, 0.0)):
        a.rot("Hips", f, ("Y", sway))
        a.rot("Walker_Root", f, ("Y", sway * 0.35))
    for f, yaw in ((0, 0.0), (16, 5.0), (30, 5.0), (44, -4.0), (60, 0.0)):
        a.rot("Neck", f, ("Z", yaw * 0.4))
        a.rot("Head", f + 2 if f < end else f, ("Z", yaw))
    for side in ("L", "R"):
        for f, v in ((0, 0.0), (30, 1.5), (60, 0.0)):
            a.rot(bone_for("UpperArm", side), f, ("Y", v * SIDE_SIGN[side]))
            a.rot(bone_for("Rocket", side), f, ("X", v * 0.4))
    return end


def clip_drive(a):
    a.begin("Drive")
    end = 40
    a.spin_wheels(0, end, 2.0)
    for f, bob in ((0, 0.0), (10, 1.0), (20, 0.0), (30, 1.0), (40, 0.0)):
        a.rot("Spine", f, ("X", 7.0 + bob * 1.6))
        a.rot("Chest", f, ("X", 3.0 + bob * 1.2))
        a.rot("Head", f, ("X", -8.0 - bob * 1.0))
        a.rot("Hips", f, ("X", 3.0), loc=(0, 0, -0.012 * bob))
        a.rot("Walker_Root", f, ("X", 0.8 * bob))
    # scooting shuffle - feet alternate a short push
    for side, phase in (("L", 0), ("R", 20)):
        s = SIDE_SIGN[side]
        keys = [(0, 0.0), (10, 1.0), (20, 0.0), (30, -1.0), (40, 0.0)]
        for f, v in keys:
            ff = (f + phase) % 40
            a.rot(bone_for("Thigh", side), ff, ("X", -14.0 * v))
            a.rot(bone_for("Shin", side), ff, ("X", 16.0 * max(0.0, v)))
            a.rot(bone_for("Foot", side), ff, ("X", -6.0 * v))
        # make sure the wrapped keys close the loop cleanly
        a.rot(bone_for("Thigh", side), 40, ("X", -14.0 * keys_at(keys, (40 - phase) % 40)))
    for side in ("L", "R"):
        for f, v in ((0, 0.0), (20, 1.0), (40, 0.0)):
            a.rot(bone_for("Caster", side), f, ("Z", 2.5 * v))
    return end


def keys_at(keys, frame):
    """Linear sample of an authoring key list - used to close wrapped loops."""
    prev = keys[0]
    for k in keys:
        if k[0] >= frame:
            if k[0] == prev[0]:
                return k[1]
            t = (frame - prev[0]) / (k[0] - prev[0])
            return prev[1] + (k[1] - prev[1]) * t
        prev = k
    return keys[-1][1]


def clip_turn(a, side):
    """side = the direction she turns towards."""
    a.begin("Turn%s" % ("Left" if side == "L" else "Right"))
    end = 30
    sign = SIDE_SIGN[side]   # +Z yaw / +Y roll both go to her left
    inner, outer = side, other(side)

    a.spin_wheels(0, end, 1.2)
    key_frames = ((0, 0.0), (8, 1.12), (14, 1.0), (30, 1.0))
    for f, t in key_frames:
        a.rot("Hips", f, ("Y", 5.0 * sign * t), ("Z", 4.0 * sign * t))
        a.rot("Spine", f, ("X", 6.0), ("Y", 8.0 * sign * t), ("Z", 6.0 * sign * t))
        a.rot("Chest", f, ("Y", 5.0 * sign * t), ("Z", 5.0 * sign * t))
        a.rot("Neck", f, ("Y", -3.0 * sign * t), ("Z", 6.0 * sign * t))
        a.rot("Head", f, ("Y", -5.0 * sign * t), ("Z", 10.0 * sign * t), ("X", -6.0))
        a.rot("Walker_Root", f, ("Y", 7.0 * sign * t), ("Z", 13.0 * sign * t))
        # inner arm pulls the grip back, outer arm pushes it forward
        a.rot(bone_for("UpperArm", inner), f, ("X", 11.0 * t), ("Z", 6.0 * sign * t))
        a.rot(bone_for("LowerArm", inner), f, ("X", 8.0 * t))
        a.rot(bone_for("UpperArm", outer), f, ("X", -12.0 * t), ("Z", 5.0 * sign * t))
        a.rot(bone_for("LowerArm", outer), f, ("X", -6.0 * t))
        a.rot(bone_for("Thigh", inner), f, ("Y", 4.0 * sign * t))
        a.rot(bone_for("Thigh", outer), f, ("Y", 2.0 * sign * t))
        for s in ("L", "R"):
            a.rot(bone_for("Caster", s), f, ("Z", 28.0 * sign * t))
            a.rot(bone_for("Rocket", s), f, ("Z", 5.0 * sign * t))
    return end


def clip_attack(a, side):
    """Handbag-style swipe with one arm; the other keeps hold of the walker."""
    a.begin("Attack%s" % ("Left" if side == "L" else "Right"))
    end = 30
    # sign is the direction this arm sweeps: the right arm winds up to her
    # right (-Z) and strikes across to her left (+Z); the left arm mirrors it.
    sign = -SIDE_SIGN[side]
    arm_u = bone_for("UpperArm", side)
    arm_l = bone_for("LowerArm", side)
    hand = bone_for("Hand", side)
    hold_u = bone_for("UpperArm", other(side))
    hold_l = bone_for("LowerArm", other(side))

    # NB: the arms hang downwards, so +X swings a limb BACKWARDS and -X swings it
    # forward/up - the opposite of the upright spine bones.
    # 0 grip | 8 wind-up | 15 strike | 21 follow-through | 30 back to grip
    poses = {
        0:  dict(u=(0, 0, 0), l=(0, 0, 0), h=(0, 0, 0), chest=(0, 0, 0), head=(0, 0, 0)),
        8:  dict(u=(42, 0, -42 * sign), l=(34, 0, -12 * sign), h=(0, 0, -18 * sign),
                 chest=(-4, -4 * sign, -18 * sign), head=(-6, 0, -8 * sign)),
        15: dict(u=(-26, 0, 62 * sign), l=(-8, 0, 24 * sign), h=(0, 0, 25 * sign),
                 chest=(6, 6 * sign, 20 * sign), head=(4, 0, 12 * sign)),
        21: dict(u=(-32, 0, 88 * sign), l=(4, 0, 30 * sign), h=(0, 0, 18 * sign),
                 chest=(8, 5 * sign, 14 * sign), head=(6, 0, 8 * sign)),
        30: dict(u=(0, 0, 0), l=(0, 0, 0), h=(0, 0, 0), chest=(0, 0, 0), head=(0, 0, 0)),
    }
    for f, po in poses.items():
        a.rot(arm_u, f, ("X", po["u"][0]), ("Y", po["u"][1]), ("Z", po["u"][2]))
        a.rot(arm_l, f, ("X", po["l"][0]), ("Y", po["l"][1]), ("Z", po["l"][2]))
        a.rot(hand, f, ("X", po["h"][0]), ("Y", po["h"][1]), ("Z", po["h"][2]))
        a.rot("Chest", f, ("X", po["chest"][0]), ("Y", po["chest"][1]),
              ("Z", po["chest"][2]))
        a.rot("Spine", f, ("X", po["chest"][0] * 0.4), ("Z", po["chest"][2] * 0.5))
        a.rot("Head", f, ("X", po["head"][0]), ("Z", po["head"][2]))
        # supporting arm braces against the walker
        brace = 0.0 if f in (0, 30) else 1.0
        a.rot(hold_u, f, ("X", -6.0 * brace), ("Z", -4.0 * sign * brace))
        a.rot(hold_l, f, ("X", 8.0 * brace))
        # walker rocks under the effort
        a.rot("Walker_Root", f, ("Y", po["chest"][1] * 0.5), ("Z", po["chest"][2] * 0.3))
    return end


def clip_hit(a):
    a.begin("HitReact")
    end = 26
    poses = [
        (0,  0.0),
        (3, -1.0),   # impact, thrown back
        (8, -0.45),
        (13, 0.35),  # over-correct forward
        (19, -0.15),
        (26, 0.0),
    ]
    for f, t in poses:
        a.rot("Hips", f, ("X", 6.0 * t), loc=(0, -0.03 * t, 0.012 * abs(t)))
        a.rot("Spine", f, ("X", 16.0 * t), ("Y", 5.0 * t))
        a.rot("Chest", f, ("X", 12.0 * t), ("Y", 4.0 * t))
        a.rot("Neck", f, ("X", 10.0 * t))
        a.rot("Head", f, ("X", 22.0 * t), ("Z", -8.0 * t))
        for side in ("L", "R"):
            s = SIDE_SIGN[side]
            a.rot(bone_for("UpperArm", side), f, ("X", -30.0 * max(0.0, -t)),
                  ("Z", 22.0 * s * max(0.0, -t)))
            a.rot(bone_for("LowerArm", side), f, ("X", -25.0 * max(0.0, -t)))
            a.rot(bone_for("Thigh", side), f, ("X", 8.0 * t))
            a.rot(bone_for("Shin", side), f, ("X", -10.0 * t))
        a.rot("Walker_Root", f, ("X", 9.0 * t), ("Y", 3.0 * t))
    return end


def clip_boost(a):
    a.begin("Boost")
    end = 45
    a.spin_wheels(0, end, 4.0)
    for f in range(0, end + 1, 3):
        # high frequency rattle riding on top of a deep forward lean
        r = math.sin(f * 1.9) * 1.0
        r2 = math.cos(f * 2.6) * 1.0
        a.rot("Hips", f, ("X", 10.0), ("Y", 0.8 * r), loc=(0, 0, 0.006 * r2))
        a.rot("Spine", f, ("X", 16.0 + 1.2 * r), ("Y", 1.0 * r2))
        a.rot("Chest", f, ("X", 7.0 + 1.0 * r2))
        a.rot("Neck", f, ("X", -10.0))
        a.rot("Head", f, ("X", -18.0 + 1.5 * r), ("Z", 2.0 * r2))
        for side in ("L", "R"):
            s = SIDE_SIGN[side]
            a.rot(bone_for("UpperArm", side), f, ("X", 6.0), ("Y", 2.0 * s))
            a.rot(bone_for("LowerArm", side), f, ("X", -4.0 + 1.0 * r))
            a.rot(bone_for("Thigh", side), f, ("X", -6.0 + 1.0 * r2))
            a.rot(bone_for("Shin", side), f, ("X", 10.0))
            a.rot(bone_for("Foot", side), f, ("X", -14.0))
            a.rot(bone_for("Rocket", side), f, ("X", -2.0 + 1.6 * r),
                  ("Z", 1.4 * r2 * s))
        a.rot("Walker_Root", f, ("X", -2.0 + 0.9 * r2), ("Y", 0.6 * r))
    # close the loop exactly on the first pose
    return end


CLIP_BUILDERS = [
    ("Idle", clip_idle),
    ("Drive", clip_drive),
    ("TurnLeft", lambda a: clip_turn(a, "L")),
    ("TurnRight", lambda a: clip_turn(a, "R")),
    ("AttackLeft", lambda a: clip_attack(a, "L")),
    ("AttackRight", lambda a: clip_attack(a, "R")),
    ("HitReact", clip_hit),
    ("Boost", clip_boost),
]


# ---------------------------------------------------------------------------
# export
# ---------------------------------------------------------------------------

FBX_COMMON = dict(
    use_selection=True,
    apply_unit_scale=True,
    apply_scale_options="FBX_SCALE_NONE",
    use_space_transform=True,
    bake_space_transform=False,
    use_mesh_modifiers=True,
    mesh_smooth_type="FACE",
    use_tspace=True,
    add_leaf_bones=False,
    primary_bone_axis="Y",
    secondary_bone_axis="X",
    use_armature_deform_only=False,
    armature_nodetype="NULL",
    path_mode="COPY",
    axis_forward="-Z",
    axis_up="Y",
)


def export_fbx(filepath, objects, **overrides):
    filepath.parent.mkdir(parents=True, exist_ok=True)
    bpy.ops.object.select_all(action="DESELECT")
    for o in objects:
        o.select_set(True)
    bpy.context.view_layer.objects.active = objects[0]

    kwargs = dict(FBX_COMMON)
    kwargs.update(overrides)
    kwargs["filepath"] = str(filepath)
    valid = {pr.identifier for pr in bpy.ops.export_scene.fbx.get_rna_type().properties}
    kwargs = {k: v for k, v in kwargs.items() if k in valid}
    bpy.ops.export_scene.fbx(**kwargs)
    print("  wrote %s" % filepath)


# ---------------------------------------------------------------------------
# preview render
# ---------------------------------------------------------------------------


def render_previews(subject_objects):
    PREVIEW_DIR.mkdir(parents=True, exist_ok=True)
    scene = bpy.context.scene
    for engine in ("BLENDER_EEVEE_NEXT", "BLENDER_EEVEE", "BLENDER_WORKBENCH"):
        try:
            scene.render.engine = engine
            break
        except TypeError:
            continue
    scene.render.resolution_x = 720
    scene.render.resolution_y = 900
    scene.render.film_transparent = False
    try:
        scene.eevee.taa_render_samples = 16
    except AttributeError:
        pass

    world = bpy.data.worlds.new("PreviewWorld")
    world.use_nodes = True
    world.node_tree.nodes["Background"].inputs[0].default_value = (0.16, 0.18, 0.22, 1)
    scene.world = world

    for name, loc, energy in (
        ("Key", (2.6, -3.2, 3.4), 900),
        ("Fill", (-3.4, -2.2, 1.8), 320),
        ("Rim", (0.4, 3.6, 2.6), 500),
    ):
        light_data = bpy.data.lights.new(name, type="AREA")
        light_data.energy = energy
        light_data.size = 3.0
        light = bpy.data.objects.new(name, light_data)
        light.location = loc
        light.rotation_euler = (Vector((0, 0, 0.9)) - Vector(loc)).to_track_quat(
            "-Z", "Y").to_euler()
        scene.collection.objects.link(light)

    cam_data = bpy.data.cameras.new("PreviewCam")
    cam_data.lens = 55
    cam = bpy.data.objects.new("PreviewCam", cam_data)
    scene.collection.objects.link(cam)
    scene.camera = cam

    target = Vector((0, -0.15, 0.72))
    views = {
        "front_left": (2.0, -2.6, 1.55),
        "front": (0.0, -3.2, 1.30),
        "side": (3.1, -0.4, 1.35),
        "back": (-1.6, 2.6, 1.60),
        "detail_walker": (1.1, -1.25, 0.55),
        "detail_feet": (0.55, -0.85, 0.30),
    }
    aim = {"detail_walker": Vector((0, -0.35, 0.35)),
           "detail_feet": Vector((0, 0.0, 0.06))}
    for name, loc in views.items():
        cam_data.lens = 70 if name.startswith("detail") else 55
        cam.location = Vector(loc)
        cam.rotation_euler = (aim.get(name, target) - Vector(loc)).to_track_quat(
            "-Z", "Y").to_euler()
        scene.render.filepath = str(PREVIEW_DIR / ("%s.png" % name))
        bpy.ops.render.render(write_still=True)
        print("  preview -> %s" % scene.render.filepath)


# ---------------------------------------------------------------------------
# main
# ---------------------------------------------------------------------------


def main():
    bpy.ops.wm.read_factory_settings(use_empty=True)
    scene = bpy.context.scene
    scene.render.fps = FPS
    scene.unit_settings.system = "METRIC"
    scene.unit_settings.scale_length = 1.0
    collection = scene.collection

    print("Building materials...")
    M = build_materials()

    print("Building armature...")
    armature, segments = build_armature(collection)

    print("Building granny mesh...")
    granny = build_mesh_object("SK_Granny", granny_parts(M), collection, segments,
                               armature)
    print("  %d tris" % len(granny.data.loop_triangles) if granny.data.loop_triangles
          else "  granny built")

    print("Building walker mesh...")
    walker = build_mesh_object("SK_Walker", walker_parts(M), collection, segments,
                               armature)

    print("Adding sockets...")
    sockets = []
    for side in ("L", "R"):
        # ankle sockets are world-axis aligned on both feet, so one symmetric
        # slipper mesh drops onto either foot with an identity local transform
        sockets.append(add_socket(
            collection, armature, "Slipper_Socket_%s" % side, "Foot_%s" % side,
            side_pos(p(0.10, 0.00, 0.095), side)))
        sockets.append(add_socket(
            collection, armature, "Hand_Socket_%s" % side, "Hand_%s" % side,
            side_pos(p(0.27, 0.175, 0.945), side), size=0.04))
        # exhaust sockets face backwards so a particle system's forward axis
        # points down the plume
        sockets.append(add_socket(
            collection, armature, "FX_Exhaust_%s" % side, "Rocket_%s" % side,
            side_pos(p(ROCKET_X, 0.190, ROCKET_Z), side), yaw_deg=180.0, size=0.06,
            display="SINGLE_ARROW"))

    print("Building animation clips...")
    anim = Animator(armature)
    clip_ranges = {}
    for name, builder in CLIP_BUILDERS:
        end = builder(anim)
        clip_ranges[name] = (0, end)
        print("  %s: 0-%d" % (name, end))
    anim.reset_pose()
    armature.animation_data.action = None

    rig_objects = [armature, granny, walker] + sockets

    if DO_EXPORT:
        print("Exporting model...")
        scene.frame_set(0)
        export_fbx(MODEL_DIR / ("%s.fbx" % MODEL_NAME), rig_objects,
                   object_types={"ARMATURE", "MESH", "EMPTY"}, bake_anim=False)

        print("Exporting animation clips...")
        anim_objects = [armature] + sockets
        for name, _ in CLIP_BUILDERS:
            act = bpy.data.actions[name]
            armature.animation_data.action = act
            anim._ensure_slot(act)
            start, end = clip_ranges[name]
            scene.frame_start = start
            scene.frame_end = end
            scene.frame_set(start)
            export_fbx(MODEL_DIR / ("%s@%s.fbx" % (MODEL_NAME, name)), anim_objects,
                       object_types={"ARMATURE", "EMPTY"},
                       bake_anim=True,
                       bake_anim_use_all_bones=True,
                       bake_anim_use_nla_strips=False,
                       bake_anim_use_all_actions=False,
                       bake_anim_force_startend_keying=True,
                       bake_anim_step=1.0,
                       bake_anim_simplify_factor=0.0)
        armature.animation_data.action = None
        anim.reset_pose()
        scene.frame_set(0)

    print("Building slippers...")
    slippers = build_slippers(M, collection)

    if DO_EXPORT:
        for name, obj in slippers.items():
            export_fbx(SLIPPER_DIR / ("%s.fbx" % name), [obj],
                       object_types={"MESH"}, bake_anim=False)

    if DO_PREVIEW:
        # demo the swap: a different variant on each foot, third one parked
        socket_by_name = {s.name: s for s in sockets}
        demo = {"Slipper_Classic": "Slipper_Socket_L",
                "Slipper_Bunny": "Slipper_Socket_R"}
        for name, obj in slippers.items():
            if name in demo:
                attach_to_socket(obj, socket_by_name[demo[name]])
            else:
                obj.location = Vector((0.85, 0.55, 0.10))
        print("Rendering previews...")
        render_previews(rig_objects)

    bpy.ops.wm.save_as_mainfile(filepath=str(BLEND_OUT))
    print("Saved %s" % BLEND_OUT)
    print("Done.")


if __name__ == "__main__":
    main()
