"""Armatures and keyframed clips for Corba characters.

Parts are assigned rigidly to bones by where their centre sits relative to the rig spec, which
keeps the chunky toy look (no rubbery joints). Characters face +Y here, which is +Z in Unity.
"""
import math
import os
import bpy
from mathutils import Vector, Matrix, Quaternion

import corba_art as A

FPS = 24


def classify(part, spec):
    bb = [part.matrix_world @ Vector(c) for c in part.bound_box]
    c = sum(bb, Vector()) / 8
    if spec.get("all_body"):
        return "spine"
    if c.z >= spec["neck"] and abs(c.x) < spec["body_r"] * 1.6:
        return "head"
    if spec.get("legs", True) and c.z < spec["leg_top"]:
        if abs(c.x) < 0.03:
            return "root"
        return "leg.L" if c.x > 0 else "leg.R"
    if spec.get("arms", True) and abs(c.x) > spec.get("arm_x", spec["body_r"]) and c.z < spec["neck"] + 0.8:
        return "arm.L" if c.x > 0 else "arm.R"
    return "spine"


def build_armature(name, spec):
    arm_data = bpy.data.armatures.new(name + "_rig")
    rig = bpy.data.objects.new("Rig", arm_data)
    bpy.context.collection.objects.link(rig)
    bpy.context.view_layer.objects.active = rig
    bpy.ops.object.mode_set(mode="EDIT")
    eb = arm_data.edit_bones

    def bone(n, head, tail, parent=None):
        b = eb.new(n)
        b.head = Vector(head)
        b.tail = Vector(tail)
        b.roll = 0.0
        if parent:
            b.parent = eb[parent]
        return b

    lt, nk = spec["leg_top"], spec["neck"]
    bone("root", (0, 0, 0), (0, 0, max(0.1, lt * 0.5)))
    bone("spine", (0, 0, lt), (0, 0, nk), "root")
    bone("head", (0, 0, nk), (0, 0, nk + spec.get("head_len", 0.4)), "spine")
    sx, sz = spec["shoulder"]
    hx, hy, hz = spec["hand"]
    bone("arm.L", (sx, 0, sz), (hx, hy, hz), "spine")
    bone("arm.R", (-sx, 0, sz), (-hx, hy, hz), "spine")
    lx = spec.get("hip_x", 0.11)
    bone("leg.L", (lx, 0, lt), (lx, 0, 0.04), "root")
    bone("leg.R", (-lx, 0, lt), (-lx, 0, 0.04), "root")
    bpy.ops.object.mode_set(mode="OBJECT")
    return rig


def rig_parts(name, spec):
    """Tag current PARTS with bone groups, join, then parent to a new armature."""
    A.turn_parts()
    for p in A.PARTS:
        group = classify(p, spec)
        vg = p.vertex_groups.new(name=group)
        vg.add(list(range(len(p.data.vertices))), 1.0, "REPLACE")
    obj = A.join(name)
    rig = build_armature(name, spec)
    obj.parent = rig
    mod = obj.modifiers.new("Armature", "ARMATURE")
    mod.object = rig
    return obj, rig


# ---------------------------------------------------------------- posing helpers

def _world_quat(pb, axis, deg):
    m = pb.bone.matrix_local.to_3x3()
    r = Matrix.Rotation(math.radians(deg), 3, axis)
    return (m.inverted() @ r @ m).to_quaternion()


def pose(rig, frame, rots=None, root_z=0.0):
    """rots: {bone: [(axis, degrees), ...]} applied in world-aligned axes relative to rest."""
    rots = rots or {}
    for pb in rig.pose.bones:
        pb.rotation_mode = "QUATERNION"
        q = Quaternion()
        for axis, deg in rots.get(pb.name, []):
            q = _world_quat(pb, axis, deg) @ q
        pb.rotation_quaternion = q
        pb.keyframe_insert("rotation_quaternion", frame=frame)
        if pb.name == "root":
            pb.location = _root_offset(pb, root_z)
            pb.keyframe_insert("location", frame=frame)


def _root_offset(pb, z):
    m = pb.bone.matrix_local.to_3x3()
    return m.inverted() @ Vector((0, 0, z))


def new_action(rig, name):
    rig.animation_data_create()
    act = bpy.data.actions.new(name)
    act.use_fake_user = True
    rig.animation_data.action = act
    return act


def make_clips(rig, kind):
    """kind: 'chef', 'unit', 'float'."""
    clips = []

    def walk(name, frames, leg, arm, bob, lean=0.0, arms_down=0.0, arms_fixed=False):
        new_action(rig, name)
        half = frames // 2
        for f, s in ((0, 1), (half, -1), (frames, 1)):
            r = {
                "leg.L": [("X", leg * s)],
                "leg.R": [("X", -leg * s)],
                "spine": [("X", -lean), ("Z", 4 * s)],
                "head": [("X", lean * 0.4)],
            }
            if arms_fixed:
                r["arm.L"] = [("X", 3 * s)]
                r["arm.R"] = [("X", -3 * s)]
            else:
                r["arm.L"] = [("X", -arms_down - arm * s)]
                r["arm.R"] = [("X", -arms_down + arm * s)]
            pose(rig, f, r, 0.0)
        for f in (half // 2, half + half // 2):
            r = {"spine": [("X", -lean)], "head": [("X", lean * 0.4)]}
            if arms_fixed:
                r["arm.L"] = []
                r["arm.R"] = []
            else:
                r["arm.L"] = [("X", -arms_down)]
                r["arm.R"] = [("X", -arms_down)]
            pose(rig, f, r, bob)
        clips.append(name)

    def idle(name, arms_down=0.0, arms_fixed=False):
        new_action(rig, name)
        for f, s in ((0, 0), (24, 1), (48, 0)):
            r = {"spine": [("X", -3 * s)], "head": [("X", 4 * s), ("Y", 3 * s)]}
            r["arm.L"] = [("X", (0 if arms_fixed else -arms_down) + 4 * s)]
            r["arm.R"] = [("X", (0 if arms_fixed else -arms_down) + 4 * s)]
            pose(rig, f, r, 0.025 * s)
        clips.append(name)

    if kind == "float":
        new_action(rig, "Idle")
        for f, s in ((0, 0), (24, 1), (48, 0)):
            pose(rig, f, {"spine": [("Z", 6 * s), ("X", -4 * s)]}, 0.12 * s)
        clips.append("Idle")
        new_action(rig, "Walk")
        for f, s in ((0, 0), (12, 1), (24, 0)):
            pose(rig, f, {"spine": [("X", -10), ("Z", 8 * (s - 0.5))]}, 0.15 * s)
        clips.append("Walk")
        new_action(rig, "Hit")
        pose(rig, 0, {}, 0)
        pose(rig, 3, {"spine": [("X", 18)]}, 0.1)
        pose(rig, 8, {}, 0)
        clips.append("Hit")
        return clips

    if kind == "chef":
        idle("Idle", arms_down=65)
        walk("Walk", 16, 30, 24, 0.07, lean=4, arms_down=65)
        walk("Run", 12, 44, 40, 0.11, lean=14, arms_down=55)
        idle("CarryIdle", arms_fixed=True)
        walk("CarryWalk", 16, 26, 0, 0.05, lean=2, arms_fixed=True)
        walk("CarryRun", 12, 40, 0, 0.09, lean=10, arms_fixed=True)
        new_action(rig, "Shove")
        pose(rig, 0, {"arm.R": [("X", -65)], "arm.L": [("X", -65)]})
        pose(rig, 4, {"spine": [("Z", 22)], "arm.R": [("X", -20)], "arm.L": [("X", -70)]})
        pose(rig, 7, {"spine": [("Z", -30), ("X", -12)], "arm.R": [("X", 15), ("Z", -20)], "arm.L": [("X", -70)]}, 0.03)
        pose(rig, 14, {"arm.R": [("X", -65)], "arm.L": [("X", -65)]})
        clips.append("Shove")
        new_action(rig, "Cheer")
        for f, s in ((0, 0), (8, 1), (16, 0.6), (24, 1), (32, 0)):
            pose(rig, f, {"arm.L": [("X", 30 + 110 * s)], "arm.R": [("X", 30 + 110 * s)], "head": [("X", 12 * s)]}, 0.2 * s)
        clips.append("Cheer")
    else:
        idle("Idle")
        walk("Walk", 16, 32, 26, 0.08, lean=6)
        new_action(rig, "Attack")
        pose(rig, 0, {})
        pose(rig, 6, {"arm.R": [("X", 70)], "spine": [("X", 10), ("Z", 15)]})
        pose(rig, 10, {"arm.R": [("X", -55)], "spine": [("X", -18), ("Z", -10)]}, -0.03)
        pose(rig, 18, {})
        clips.append("Attack")
        new_action(rig, "Cheer")
        for f, s in ((0, 0), (8, 1), (16, 0)):
            pose(rig, f, {"arm.L": [("X", 120 * s)], "arm.R": [("X", 120 * s)]}, 0.25 * s)
        clips.append("Cheer")

    new_action(rig, "Hit")
    pose(rig, 0, {})
    pose(rig, 3, {"spine": [("X", 22)], "head": [("X", 15)]}, 0.02)
    pose(rig, 9, {})
    clips.append("Hit")
    return clips


def export_rigged(obj, rig, name):
    os.makedirs(A.MODEL_DIR, exist_ok=True)
    bpy.ops.object.select_all(action="DESELECT")
    obj.select_set(True)
    rig.select_set(True)
    bpy.context.view_layer.objects.active = rig
    rig.animation_data.action = None
    for pb in rig.pose.bones:
        pb.rotation_quaternion = Quaternion()
        pb.location = Vector()
    path = os.path.join(A.MODEL_DIR, name + ".fbx")
    bpy.context.scene.render.fps = FPS
    bpy.ops.export_scene.fbx(
        filepath=path, use_selection=True, object_types={"MESH", "ARMATURE"}, use_mesh_modifiers=False,
        mesh_smooth_type="FACE", axis_forward="-Z", axis_up="Y", apply_scale_options="FBX_SCALE_UNITS",
        bake_space_transform=False, add_leaf_bones=False, primary_bone_axis="Y", secondary_bone_axis="X",
        armature_nodetype="NULL", bake_anim=True, bake_anim_use_all_actions=True,
        bake_anim_use_nla_strips=False, bake_anim_force_startend_keying=True, bake_anim_step=1.0,
        bake_anim_simplify_factor=0.0, path_mode="STRIP", embed_textures=False)
    return path


def build_character(name, recipe, spec, kind, res=512):
    A.clear()
    for a in list(bpy.data.actions):
        bpy.data.actions.remove(a)
    recipe()
    obj, rig = rig_parts(name, spec)
    A.bake(obj, name, res)
    make_clips(rig, kind)
    export_rigged(obj, rig, name)
    A.render_icon(obj, name)
    return obj, rig
