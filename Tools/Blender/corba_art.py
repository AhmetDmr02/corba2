"""Hand-painted model kit for Corba. Run inside Blender.

Models are assembled from vertex-coloured primitives, joined, UV-unwrapped and baked
into a single painted albedo (top light, cool cavities, brush noise, warm edge highlights).
"""
import bpy
import bmesh
import math
import os
import random
from mathutils import Vector, Matrix, Euler

PROJECT = r"D:\Corba\Assets\Corba\Resources\Corba"
MODEL_DIR = os.path.join(PROJECT, "Models")
TEX_DIR = os.path.join(PROJECT, "Textures")
UI_DIR = os.path.join(PROJECT, "UI")

PARTS = []
RNG = random.Random(7)


def hexc(h):
    h = h.lstrip("#")
    return tuple(int(h[i:i + 2], 16) / 255.0 for i in (0, 2, 4))


def lin(c):
    return tuple(x / 12.92 if x <= 0.04045 else ((x + 0.055) / 1.055) ** 2.4 for x in c)


def clear():
    PARTS.clear()
    for o in list(bpy.data.objects):
        bpy.data.objects.remove(o, do_unlink=True)
    for m in list(bpy.data.meshes):
        if m.users == 0:
            bpy.data.meshes.remove(m)
    for m in list(bpy.data.materials):
        if m.users == 0:
            bpy.data.materials.remove(m)
    for i in list(bpy.data.images):
        if i.users == 0:
            bpy.data.images.remove(i)


def _finish(obj, color, bevel=0.0, wob=0.0, smooth=True, segs=2):
    if bevel > 0:
        mod = obj.modifiers.new("bev", "BEVEL")
        mod.width = bevel
        mod.segments = segs
        mod.limit_method = "ANGLE"
        bpy.context.view_layer.objects.active = obj
        bpy.ops.object.modifier_apply(modifier=mod.name)
    me = obj.data
    if wob > 0:
        for v in me.vertices:
            v.co += Vector((RNG.uniform(-wob, wob), RNG.uniform(-wob, wob), RNG.uniform(-wob, wob)))
    c = lin(hexc(color) if isinstance(color, str) else color)
    attr = me.color_attributes.get("Col") or me.color_attributes.new("Col", "FLOAT_COLOR", "POINT")
    for d in attr.data:
        d.color = (c[0], c[1], c[2], 1.0)
    for p in me.polygons:
        p.use_smooth = smooth
    PARTS.append(obj)
    return obj


def _place(obj, loc, rot, scale):
    obj.location = loc
    obj.rotation_euler = Euler(tuple(math.radians(a) for a in rot))
    obj.scale = scale
    bpy.context.view_layer.objects.active = obj
    obj.select_set(True)
    bpy.ops.object.transform_apply(location=False, rotation=True, scale=True)
    obj.select_set(False)


def box(size, loc=(0, 0, 0), rot=(0, 0, 0), color="#ffffff", bevel=0.04, wob=0.0, smooth=False):
    bpy.ops.mesh.primitive_cube_add(size=1, location=loc)
    o = bpy.context.active_object
    _place(o, loc, rot, size)
    return _finish(o, color, bevel, wob, smooth)


def cyl(r, h, loc=(0, 0, 0), rot=(0, 0, 0), color="#ffffff", verts=12, r2=None, bevel=0.03, wob=0.0, smooth=True, scale=(1, 1, 1)):
    if r2 is None:
        bpy.ops.mesh.primitive_cylinder_add(vertices=verts, radius=r, depth=h, location=loc)
    else:
        bpy.ops.mesh.primitive_cone_add(vertices=verts, radius1=r, radius2=r2, depth=h, location=loc)
    o = bpy.context.active_object
    _place(o, loc, rot, scale)
    return _finish(o, color, bevel, wob, smooth)


def ball(r, loc=(0, 0, 0), scale=(1, 1, 1), rot=(0, 0, 0), color="#ffffff", segs=14, rings=9, wob=0.0, smooth=True):
    bpy.ops.mesh.primitive_uv_sphere_add(segments=segs, ring_count=rings, radius=r, location=loc)
    o = bpy.context.active_object
    _place(o, loc, rot, scale)
    return _finish(o, color, 0, wob, smooth)


def ico(r, loc=(0, 0, 0), scale=(1, 1, 1), rot=(0, 0, 0), color="#ffffff", sub=1, wob=0.0, smooth=False):
    bpy.ops.mesh.primitive_ico_sphere_add(subdivisions=sub, radius=r, location=loc)
    o = bpy.context.active_object
    _place(o, loc, rot, scale)
    return _finish(o, color, 0, wob, smooth)


def ring(r, thick, loc=(0, 0, 0), rot=(0, 0, 0), color="#ffffff", major_seg=20, minor_seg=8, scale=(1, 1, 1)):
    bpy.ops.mesh.primitive_torus_add(major_radius=r, minor_radius=thick, major_segments=major_seg, minor_segments=minor_seg, location=loc)
    o = bpy.context.active_object
    _place(o, loc, rot, scale)
    return _finish(o, color, 0, 0, True)


def bowl(r, h, loc=(0, 0, 0), color="#ffffff", inner="#000000", verts=16, thick=0.06, lip=True):
    """Open vessel: lathe profile with an inner wall."""
    bm = bmesh.new()
    prof = [(0.0, 0.0), (r * 0.72, 0.0), (r * 0.95, h * 0.18), (r, h * 0.6), (r * 0.98, h),
            (r - thick, h), (r - thick * 1.1, h * 0.6), (r * 0.7, thick), (0.0, thick)]
    rings_ = []
    for i in range(verts):
        a = 2 * math.pi * i / verts
        rings_.append([bm.verts.new((x * math.cos(a), x * math.sin(a), z)) for x, z in prof])
    for i in range(verts):
        a, b = rings_[i], rings_[(i + 1) % verts]
        for j in range(len(prof) - 1):
            try:
                bm.faces.new((a[j], b[j], b[j + 1], a[j + 1]))
            except ValueError:
                pass
    bmesh.ops.remove_doubles(bm, verts=bm.verts, dist=1e-4)
    me = bpy.data.meshes.new("bowl")
    bm.to_mesh(me)
    bm.free()
    o = bpy.data.objects.new("bowl", me)
    bpy.context.collection.objects.link(o)
    o.location = loc
    bpy.context.view_layer.objects.active = o
    o.select_set(True)
    bpy.ops.object.transform_apply(location=False, rotation=True, scale=True)
    o.select_set(False)
    _finish(o, color, 0, 0, True)
    if lip:
        ring(r - thick * 0.5, thick * 0.6, (loc[0], loc[1], loc[2] + h), color=color, major_seg=verts)
    if inner:
        cyl(r - thick * 1.2, 0.02, (loc[0], loc[1], loc[2] + thick + 0.01), color=inner, verts=verts, bevel=0)
    return o


def lathe(points, verts=16, loc=(0, 0, 0), color="#ffffff", scale=(1, 1, 1), rot=(0, 0, 0), cap=True, wob=0.0):
    bm = bmesh.new()
    rings_ = []
    for i in range(verts):
        a = 2 * math.pi * i / verts
        rings_.append([bm.verts.new((x * math.cos(a), x * math.sin(a), z)) for x, z in points])
    for i in range(verts):
        a, b = rings_[i], rings_[(i + 1) % verts]
        for j in range(len(points) - 1):
            try:
                bm.faces.new((a[j], b[j], b[j + 1], a[j + 1]))
            except ValueError:
                pass
    bmesh.ops.remove_doubles(bm, verts=bm.verts, dist=1e-4)
    if cap:
        bmesh.ops.holes_fill(bm, edges=bm.edges, sides=verts + 2)
    bmesh.ops.recalc_face_normals(bm, faces=bm.faces)
    me = bpy.data.meshes.new("lathe")
    bm.to_mesh(me)
    bm.free()
    o = bpy.data.objects.new("lathe", me)
    bpy.context.collection.objects.link(o)
    _place(o, loc, rot, scale)
    return _finish(o, color, 0, wob, True)


def leaf(length, width, loc, rot, color, bend=0.15):
    bm = bmesh.new()
    pts = []
    n = 6
    for i in range(n + 1):
        t = i / n
        w = math.sin(t * math.pi) * width * 0.5
        z = bend * math.sin(t * math.pi * 0.5) * length
        pts.append((bm.verts.new((-w, t * length, z)), bm.verts.new((0, t * length, z + w * 0.4)), bm.verts.new((w, t * length, z))))
    for i in range(n):
        a, b = pts[i], pts[i + 1]
        for k in range(2):
            try:
                bm.faces.new((a[k], a[k + 1], b[k + 1], b[k]))
            except ValueError:
                pass
    bmesh.ops.remove_doubles(bm, verts=bm.verts, dist=1e-5)
    me = bpy.data.meshes.new("leaf")
    bm.to_mesh(me)
    bm.free()
    o = bpy.data.objects.new("leaf", me)
    bpy.context.collection.objects.link(o)
    mod = o.modifiers.new("solid", "SOLIDIFY")
    mod.thickness = 0.02
    bpy.context.view_layer.objects.active = o
    bpy.ops.object.modifier_apply(modifier=mod.name)
    _place(o, loc, rot, (1, 1, 1))
    return _finish(o, color, 0, 0, True)


def limb(a, b, r, color, r2=None, verts=10, wob=0.0):
    a, b = Vector(a), Vector(b)
    d = b - a
    if r2 is None:
        bpy.ops.mesh.primitive_cylinder_add(vertices=verts, radius=r, depth=d.length)
    else:
        bpy.ops.mesh.primitive_cone_add(vertices=verts, radius1=r, radius2=r2, depth=d.length)
    o = bpy.context.active_object
    o.location = (a + b) / 2
    o.rotation_mode = "QUATERNION"
    o.rotation_quaternion = d.to_track_quat("Z", "Y")
    bpy.context.view_layer.objects.active = o
    o.select_set(True)
    bpy.ops.object.transform_apply(location=False, rotation=True, scale=True)
    o.select_set(False)
    o.rotation_mode = "XYZ"
    _finish(o, color, 0, wob, True)
    ball(r * 1.02, tuple(a), color=color, segs=verts, rings=6)
    ball((r2 if r2 is not None else r) * 1.02, tuple(b), color=color, segs=verts, rings=6)
    return o


def eyes(x, y, z, spread=0.07, size=0.035, color="#24160e"):
    ball(size, (x - spread, y, z), color=color, segs=8, rings=6)
    ball(size, (x + spread, y, z), color=color, segs=8, rings=6)
    ball(size * 0.35, (x - spread + 0.012, y - size * 0.8, z + 0.012), color="#ffffff", segs=6, rings=4)
    ball(size * 0.35, (x + spread + 0.012, y - size * 0.8, z + 0.012), color="#ffffff", segs=6, rings=4)


def turn_parts():
    """Rotate parts 180 degrees so recipes authored facing -Y face +Y (Unity +Z after export)."""
    rot = Matrix.Rotation(math.pi, 4, "Z")
    for p in PARTS:
        if p.get("turned"):
            continue
        p.matrix_world = rot @ p.matrix_world
        p["turned"] = True


def join(name):
    turn_parts()
    bpy.ops.object.select_all(action="DESELECT")
    for p in PARTS:
        p.select_set(True)
    bpy.context.view_layer.objects.active = PARTS[0]
    bpy.ops.object.join()
    o = bpy.context.active_object
    o.name = name
    o.data.name = name
    bpy.ops.object.transform_apply(location=True, rotation=True, scale=True)
    PARTS.clear()
    return o


def _sock(node, ident, out=False):
    coll = node.outputs if out else node.inputs
    for s in coll:
        if s.identifier == ident:
            return s
    return coll[ident]


def _mix(nt, blend, fac, a, b, clamp=False):
    n = nt.nodes.new("ShaderNodeMix")
    n.data_type = "RGBA"
    n.blend_type = blend
    n.clamp_result = clamp
    f = _sock(n, "Factor_Float")
    if isinstance(fac, (int, float)):
        f.default_value = fac
    else:
        nt.links.new(fac, f)
    for ident, v in (("A_Color", a), ("B_Color", b)):
        s = _sock(n, ident)
        if isinstance(v, tuple):
            s.default_value = (*v, 1.0) if len(v) == 3 else v
        else:
            nt.links.new(v, s)
    return _sock(n, "Result_Color", True)


def _maprange(nt, value, a, b, c, d, clamp=True):
    n = nt.nodes.new("ShaderNodeMapRange")
    n.clamp = clamp
    nt.links.new(value, n.inputs["Value"])
    n.inputs["From Min"].default_value = a
    n.inputs["From Max"].default_value = b
    n.inputs["To Min"].default_value = c
    n.inputs["To Max"].default_value = d
    return n.outputs["Result"]


def paint_material(strokes=1.0, edge=1.0, ao_dist=0.35, scale=1.0):
    mat = bpy.data.materials.new("PaintBake")
    try:
        mat.use_nodes = True
    except Exception:
        pass
    nt = mat.node_tree
    nt.nodes.clear()
    out = nt.nodes.new("ShaderNodeOutputMaterial")
    emit = nt.nodes.new("ShaderNodeEmission")
    nt.links.new(emit.outputs[0], out.inputs["Surface"])

    col = nt.nodes.new("ShaderNodeVertexColor")
    col.layer_name = "Col"
    geo = nt.nodes.new("ShaderNodeNewGeometry")
    sep = nt.nodes.new("ShaderNodeSeparateXYZ")
    nt.links.new(geo.outputs["Normal"], sep.inputs[0])

    texco = nt.nodes.new("ShaderNodeTexCoord")
    noise = nt.nodes.new("ShaderNodeTexNoise")
    noise.inputs["Scale"].default_value = 3.5 / scale
    noise.inputs["Detail"].default_value = 3.0
    if "Distortion" in noise.inputs:
        noise.inputs["Distortion"].default_value = 1.2
    nt.links.new(texco.outputs["Object"], noise.inputs["Vector"])

    mapping = nt.nodes.new("ShaderNodeMapping")
    mapping.inputs["Scale"].default_value = (1.0, 1.0, 7.0)
    mapping.inputs["Rotation"].default_value = (0.4, 0.3, 0.9)
    nt.links.new(texco.outputs["Object"], mapping.inputs["Vector"])
    streak = nt.nodes.new("ShaderNodeTexNoise")
    streak.inputs["Scale"].default_value = 9.0 / scale
    streak.inputs["Detail"].default_value = 1.0
    nt.links.new(mapping.outputs[0], streak.inputs["Vector"])

    base = col.outputs["Color"]
    shadow = _mix(nt, "MULTIPLY", 1.0, base, (0.50, 0.46, 0.74))
    light = _mix(nt, "MULTIPLY", 1.0, base, (1.16, 1.09, 0.92))
    up = _maprange(nt, sep.outputs["Z"], -0.7, 0.85, 0.0, 1.0)
    lit = _mix(nt, "MIX", up, shadow, light)

    side = _maprange(nt, sep.outputs["X"], -1.0, 1.0, 0.0, 0.12)
    lit = _mix(nt, "SCREEN", side, lit, (1.0, 0.85, 0.6))

    ao = nt.nodes.new("ShaderNodeAmbientOcclusion")
    ao.inputs["Distance"].default_value = ao_dist
    ao.samples = 12
    cav = _maprange(nt, ao.outputs["AO"], 0.2, 1.0, 0.7, 0.0)
    lit = _mix(nt, "MULTIPLY", cav, lit, (0.42, 0.34, 0.52))

    lit = _mix(nt, "OVERLAY", 0.22 * strokes, lit, noise.outputs["Color"])
    sfac = _maprange(nt, streak.outputs["Fac"], 0.3, 0.7, 0.0, 0.22 * strokes)
    lit = _mix(nt, "SOFT_LIGHT", sfac, lit, (1.0, 0.95, 0.85))
    dab = _maprange(nt, noise.outputs["Fac"], 0.45, 0.75, 0.0, 0.12 * strokes)
    lit = _mix(nt, "MULTIPLY", dab, lit, (0.8, 0.75, 0.85))

    bev = nt.nodes.new("ShaderNodeBevel")
    bev.inputs["Radius"].default_value = 0.035 * scale
    bev.samples = 16
    dot = nt.nodes.new("ShaderNodeVectorMath")
    dot.operation = "DOT_PRODUCT"
    nt.links.new(bev.outputs["Normal"], dot.inputs[0])
    nt.links.new(geo.outputs["Normal"], dot.inputs[1])
    e = _maprange(nt, dot.outputs["Value"], 0.96, 0.6, 0.0, 0.38 * edge)
    lit = _mix(nt, "SCREEN", e, lit, (1.0, 0.93, 0.75))

    hsv = nt.nodes.new("ShaderNodeHueSaturation")
    hsv.inputs["Saturation"].default_value = 1.22
    hsv.inputs["Value"].default_value = 1.0
    nt.links.new(lit, hsv.inputs["Color"])
    nt.links.new(hsv.outputs["Color"], emit.inputs["Color"])
    emit.inputs["Strength"].default_value = 1.0
    return mat


def set_engine(names):
    sc = bpy.context.scene
    for n in names:
        try:
            sc.render.engine = n
            return n
        except TypeError:
            continue
    return sc.render.engine


def bake(obj, name, res=512, strokes=1.0, edge=1.0, ao_dist=0.35, samples=24):
    bpy.ops.object.select_all(action="DESELECT")
    obj.select_set(True)
    bpy.context.view_layer.objects.active = obj
    dims = max(obj.dimensions)
    scale = max(0.6, min(2.5, dims / 2.0))

    bpy.ops.object.mode_set(mode="EDIT")
    bpy.ops.mesh.select_all(action="SELECT")
    bpy.ops.uv.smart_project(angle_limit=math.radians(60), island_margin=0.012, scale_to_bounds=False)
    bpy.ops.object.mode_set(mode="OBJECT")
    try:
        bpy.ops.object.shade_smooth_by_angle(angle=math.radians(50))
    except Exception:
        pass

    obj.data.materials.clear()
    mat = paint_material(strokes, edge, ao_dist * scale, scale)
    obj.data.materials.append(mat)
    img = bpy.data.images.new(name, res, res, alpha=False)
    img.generated_color = (0.5, 0.5, 0.5, 1)
    tex = mat.node_tree.nodes.new("ShaderNodeTexImage")
    tex.image = img
    mat.node_tree.nodes.active = tex
    tex.select = True

    sc = bpy.context.scene
    set_engine(["CYCLES"])
    sc.cycles.samples = samples
    try:
        sc.cycles.device = "GPU"
    except Exception:
        pass
    sc.render.bake.margin = 12
    sc.render.bake.use_clear = True
    bpy.ops.object.bake(type="EMIT")

    os.makedirs(TEX_DIR, exist_ok=True)
    path = os.path.join(TEX_DIR, name + ".png")
    img.filepath_raw = path
    img.file_format = "PNG"
    img.save()

    preview = bpy.data.materials.new(name + "_preview")
    try:
        preview.use_nodes = True
    except Exception:
        pass
    pnt = preview.node_tree
    pnt.nodes.clear()
    o = pnt.nodes.new("ShaderNodeOutputMaterial")
    em = pnt.nodes.new("ShaderNodeEmission")
    t = pnt.nodes.new("ShaderNodeTexImage")
    t.image = img
    pnt.links.new(t.outputs["Color"], em.inputs["Color"])
    pnt.links.new(em.outputs[0], o.inputs["Surface"])
    obj.data.materials.clear()
    obj.data.materials.append(preview)
    bpy.data.materials.remove(mat)
    return path


def export(obj, name):
    os.makedirs(MODEL_DIR, exist_ok=True)
    bpy.ops.object.select_all(action="DESELECT")
    obj.select_set(True)
    bpy.context.view_layer.objects.active = obj
    path = os.path.join(MODEL_DIR, name + ".fbx")
    bpy.ops.export_scene.fbx(
        filepath=path, use_selection=True, object_types={"MESH"}, use_mesh_modifiers=True,
        mesh_smooth_type="FACE", axis_forward="-Z", axis_up="Y", apply_scale_options="FBX_SCALE_UNITS",
        bake_space_transform=True, use_custom_props=False, path_mode="STRIP", embed_textures=False,
        use_tspace=False, add_leaf_bones=False, bake_anim=False)
    return path


def render_icon(obj, name, size=256, view=(-1.0, 1.35, 0.9), outline=5):
    import numpy as np
    sc = bpy.context.scene
    cam_data = bpy.data.cameras.new("IconCam")
    cam_data.type = "ORTHO"
    cam = bpy.data.objects.new("IconCam", cam_data)
    bpy.context.collection.objects.link(cam)
    bb = [obj.matrix_world @ Vector(c) for c in obj.bound_box]
    center = sum(bb, Vector()) / 8
    radius = max((v - center).length for v in bb)
    d = Vector(view).normalized()
    cam.location = center + d * (radius * 4)
    cam.rotation_euler = (-d).to_track_quat("-Z", "Y").to_euler()
    cam_data.ortho_scale = radius * 2.05
    cam_data.clip_end = radius * 10
    sc.camera = cam

    set_engine(["BLENDER_WORKBENCH"])
    sh = sc.display.shading
    sh.light = "FLAT"
    sh.color_type = "TEXTURE"
    sc.render.film_transparent = True
    sc.render.resolution_x = size
    sc.render.resolution_y = size
    sc.render.resolution_percentage = 100
    sc.render.image_settings.file_format = "PNG"
    sc.render.image_settings.color_mode = "RGBA"
    try:
        sc.view_settings.view_transform = "Standard"
    except TypeError:
        pass
    hidden = []
    for o in sc.objects:
        if o not in (obj, cam) and not o.hide_render:
            o.hide_render = True
            hidden.append(o)
    tmp = os.path.join(bpy.app.tempdir, "icon_tmp.png")
    sc.render.filepath = tmp
    bpy.ops.render.render(write_still=True)
    for o in hidden:
        o.hide_render = False
    bpy.data.objects.remove(cam, do_unlink=True)

    img = bpy.data.images.load(tmp, check_existing=False)
    w, h = img.size
    px = np.array(img.pixels[:], dtype=np.float32).reshape(h, w, 4)
    a = px[:, :, 3]
    grow = a.copy()
    for dy in range(-outline, outline + 1):
        for dx in range(-outline, outline + 1):
            if dx * dx + dy * dy <= outline * outline:
                grow = np.maximum(grow, np.roll(np.roll(a, dy, 0), dx, 1))
    ink = np.array([0.23, 0.14, 0.08], dtype=np.float32)
    outp = np.zeros_like(px)
    outp[:, :, :3] = px[:, :, :3] * a[:, :, None] + ink * (1 - a[:, :, None])
    outp[:, :, 3] = grow
    sh_mask = np.roll(np.roll(grow, -4, 0), 3, 1) * 0.35
    outp[:, :, 3] = np.maximum(outp[:, :, 3], sh_mask)
    outp[:, :, :3] = np.where((grow < 0.01)[:, :, None] & (sh_mask > 0)[:, :, None], ink * 0.5, outp[:, :, :3])
    save_rgba(outp, os.path.join(UI_DIR, "icon_" + name + ".png"))
    bpy.data.images.remove(img)


def save_rgba(arr, path):
    import numpy as np
    h, w = arr.shape[:2]
    img = bpy.data.images.new("tmp_save", w, h, alpha=True)
    img.pixels.foreach_set(np.clip(arr, 0, 1).astype(np.float32).ravel())
    img.filepath_raw = path
    img.file_format = "PNG"
    os.makedirs(os.path.dirname(path), exist_ok=True)
    img.save()
    bpy.data.images.remove(img)


def finish(name, res=512, icon=False, **kw):
    obj = join(name)
    bake(obj, name, res, **kw)
    export(obj, name)
    if icon:
        render_icon(obj, name)
    return obj
