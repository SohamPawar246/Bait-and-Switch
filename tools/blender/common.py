"""Shared helpers: scene reset, materials, mesh building from bmesh, modifier baking, sockets, finishing.
Prefer bpy.data + bmesh over bpy.ops so scripts behave the same from the MCP and from the command line.
"""
import math
import bmesh
import bpy
from mathutils import Matrix, Vector

import style as S


# ---------------------------------------------------------------- scene

def reset_scene():
    for ob in list(bpy.data.objects):
        bpy.data.objects.remove(ob, do_unlink=True)
    for coll in list(bpy.data.collections):
        bpy.data.collections.remove(coll)
    for block in (bpy.data.meshes, bpy.data.curves, bpy.data.materials, bpy.data.lattices, bpy.data.cameras, bpy.data.lights):
        for item in list(block):
            if item.users == 0:
                block.remove(item)


def collection(name):
    c = bpy.data.collections.get(name) or bpy.data.collections.new(name)
    if c.name not in bpy.context.scene.collection.children:
        bpy.context.scene.collection.children.link(c)
    return c


def hex_rgba(h, a=1.0):
    h = h.lstrip("#")
    srgb = [int(h[i:i + 2], 16) / 255 for i in (0, 2, 4)]
    lin = [c / 12.92 if c <= 0.04045 else ((c + 0.055) / 1.055) ** 2.4 for c in srgb]
    return (*lin, a)


# ---------------------------------------------------------------- materials

def toon_material(name, hex_color, emission=0.0):
    """Preview toon look in Blender: Diffuse → Shader to RGB → constant 2-step ramp (+ optional emission).
    Unity uses its own toon shader; this only makes contact sheets read like the game."""
    m = bpy.data.materials.get(name)
    if m:
        return m
    m = bpy.data.materials.new(name)
    m.use_nodes = True
    nt = m.node_tree
    nt.nodes.clear()
    out = nt.nodes.new("ShaderNodeOutputMaterial")
    diff = nt.nodes.new("ShaderNodeBsdfDiffuse")
    s2r = nt.nodes.new("ShaderNodeShaderToRGB")
    ramp = nt.nodes.new("ShaderNodeValToRGB")
    mix = nt.nodes.new("ShaderNodeMixRGB")
    emit = nt.nodes.new("ShaderNodeEmission")
    base = hex_rgba(hex_color)
    ramp.color_ramp.interpolation = "CONSTANT"
    ramp.color_ramp.elements[0].position = 0.0
    ramp.color_ramp.elements[0].color = (0.55, 0.55, 0.6, 1)
    ramp.color_ramp.elements[1].position = 0.35
    ramp.color_ramp.elements[1].color = (1, 1, 1, 1)
    mix.blend_type = "MULTIPLY"
    mix.inputs[0].default_value = 1.0
    mix.inputs[2].default_value = base
    diff.inputs[0].default_value = (1, 1, 1, 1)
    emit.inputs[1].default_value = 1.0
    nt.links.new(diff.outputs[0], s2r.inputs[0])
    nt.links.new(s2r.outputs[0], ramp.inputs[0])
    nt.links.new(ramp.outputs[0], mix.inputs[1])
    nt.links.new(mix.outputs[0], emit.inputs[0])
    if emission > 0:
        add = nt.nodes.new("ShaderNodeAddShader")
        glow = nt.nodes.new("ShaderNodeEmission")
        glow.inputs[0].default_value = base
        glow.inputs[1].default_value = emission
        nt.links.new(emit.outputs[0], add.inputs[0])
        nt.links.new(glow.outputs[0], add.inputs[1])
        nt.links.new(add.outputs[0], out.inputs[0])
    else:
        nt.links.new(emit.outputs[0], out.inputs[0])
    return m


def ink_material():
    m = bpy.data.materials.get("Ink")
    if m:
        return m
    m = bpy.data.materials.new("Ink")
    m.use_nodes = True
    nt = m.node_tree
    nt.nodes.clear()
    out = nt.nodes.new("ShaderNodeOutputMaterial")
    emit = nt.nodes.new("ShaderNodeEmission")
    emit.inputs[0].default_value = hex_rgba(S.PALETTE["ink"])
    nt.links.new(emit.outputs[0], out.inputs[0])
    m.use_backface_culling = True
    return m


def fill_material():
    return toon_material("Fill", S.PREVIEW_FILL)


def detail_material(hex_color=None, emission=0.0, name=None):
    return toon_material(name or f"Detail_{(hex_color or S.PALETTE['ink']).lstrip('#')}", hex_color or S.PALETTE["ink"], emission)


# ---------------------------------------------------------------- meshes

def mesh_object(name, bm, coll, mats=None):
    me = bpy.data.meshes.new(name)
    bm.to_mesh(me)
    bm.free()
    ob = bpy.data.objects.new(name, me)
    coll.objects.link(ob)
    for m in (mats or [fill_material(), detail_material()]):
        me.materials.append(m)
    return ob


def bm_uvsphere(radius=0.5, segs=24, rings=12, matrix=None):
    bm = bmesh.new()
    bmesh.ops.create_uvsphere(bm, u_segments=segs, v_segments=rings, radius=radius, matrix=matrix or Matrix())
    return bm


def bm_icosphere(radius=0.5, subdiv=2, matrix=None):
    bm = bmesh.new()
    bmesh.ops.create_icosphere(bm, subdivisions=subdiv, radius=radius, matrix=matrix or Matrix())
    return bm


def bm_cone(r1, r2, depth, segs=16, matrix=None, cap_ends=True):
    bm = bmesh.new()
    bmesh.ops.create_cone(bm, cap_ends=cap_ends, cap_tris=False, segments=segs, radius1=r1, radius2=r2,
                          depth=depth, matrix=matrix or Matrix())
    return bm


def bm_cube(size=1.0, matrix=None):
    bm = bmesh.new()
    bmesh.ops.create_cube(bm, size=size, matrix=matrix or Matrix())
    return bm


def merge_into(dst, src, mat_index=0):
    """Append bmesh src into dst, assigning material index. Frees src."""
    tmp = bpy.data.meshes.new("_tmp")
    for f in src.faces:
        f.material_index = mat_index
    src.to_mesh(tmp)
    src.free()
    dst.from_mesh(tmp)
    bpy.data.meshes.remove(tmp)
    return dst


def set_material_index(bm, idx):
    for f in bm.faces:
        f.material_index = idx
    return bm


def add_modifier(ob, kind, **props):
    mod = ob.modifiers.new(kind.lower(), kind)
    for k, v in props.items():
        setattr(mod, k, v)
    return mod


def bake_modifiers(ob):
    """Apply all modifiers without bpy.ops (works from any context)."""
    dg = bpy.context.evaluated_depsgraph_get()
    ev = ob.evaluated_get(dg)
    new = bpy.data.meshes.new_from_object(ev, preserve_all_data_layers=True, depsgraph=dg)
    old = ob.data
    ob.modifiers.clear()
    ob.data = new
    if old.users == 0:
        bpy.data.meshes.remove(old)
    return ob


def apply_transform(ob):
    """Bake rotation/scale into the mesh so the object ends with identity transform at its origin."""
    m = ob.matrix_basis.copy()
    loc = m.to_translation()
    ob.data.transform(Matrix.Translation(-loc) @ m)
    ob.matrix_basis = Matrix.Translation(loc)
    return ob


def finish(ob, smooth=True):
    """Every exported mesh: scale applied, smooth + sharp-by-angle, normals outward, outline preview."""
    apply_transform(ob)
    me = ob.data
    bm = bmesh.new()
    bm.from_mesh(me)
    bmesh.ops.remove_doubles(bm, verts=bm.verts, dist=1e-5)
    bmesh.ops.recalc_face_normals(bm, faces=bm.faces)
    bm.to_mesh(me)
    bm.free()
    if smooth:
        for p in me.polygons:
            p.use_smooth = True
        limit = math.radians(S.SMOOTH_ANGLE_DEG)
        try:
            me.set_sharp_from_angle(angle=limit)  # Blender 4.1+
        except Exception:
            pass
    add_outline(ob)
    return ob


def add_outline(ob):
    """Inverted hull preview (Solidify, flipped normals, ink material). Stripped on export."""
    ink = ink_material()
    if ink.name not in [m.name for m in ob.data.materials if m]:
        ob.data.materials.append(ink)
    idx = [m.name for m in ob.data.materials].index(ink.name)
    sol = add_modifier(ob, "SOLIDIFY", thickness=-S.OUTLINE, use_flip_normals=True, use_rim=False,
                       material_offset=idx, offset=1.0)
    sol.name = "Outline"
    return sol


# ---------------------------------------------------------------- sockets

def socket_basis(normal, up_hint=Vector((0, 0, 1))):
    """Rotation whose local +Z = normal and local +Y is as close to up_hint as possible.
    Face/side/back sockets: local X = sideways, local Y = world up. Top socket: local Y = world +X (forward)."""
    z = Vector(normal).normalized()
    up = Vector(up_hint)
    if abs(z.dot(up.normalized())) > 0.95:
        up = Vector((1, 0, 0))
    x = up.cross(z).normalized()
    y = z.cross(x).normalized()
    return Matrix((x, y, z)).transposed().to_4x4()


def add_socket(parent, name, location, normal, up_hint=Vector((0, 0, 1))):
    """Empty named socket_<name> parented to the body; local +Z points outward along `normal`."""
    e = bpy.data.objects.new(f"socket_{name}", None)
    e.empty_display_type = "ARROWS"
    e.empty_display_size = 0.08
    for c in parent.users_collection:
        c.objects.link(e)
    e.parent = parent
    e.matrix_world = parent.matrix_world @ (Matrix.Translation(location) @ socket_basis(normal, up_hint))
    return e


def make_curve_mesh(name, points, radii, bevel, coll, mats=None, resolution=4, closed=False):
    """Tapered tube along points (poly spline with per-point radius), converted to a mesh object."""
    cu = bpy.data.curves.new(name + "_curve", "CURVE")
    cu.dimensions = "3D"
    cu.bevel_depth = bevel
    cu.bevel_resolution = 2
    cu.use_fill_caps = True
    sp = cu.splines.new("NURBS")
    sp.points.add(len(points) - 1)
    for p, co, r in zip(sp.points, points, radii):
        p.co = (co[0], co[1], co[2], 1.0)
        p.radius = r
    sp.use_endpoint_u = True
    sp.order_u = min(4, len(points))
    sp.resolution_u = resolution
    sp.use_cyclic_u = closed
    tmp = bpy.data.objects.new(name + "_tmp", cu)
    coll.objects.link(tmp)
    dg = bpy.context.evaluated_depsgraph_get()
    me = bpy.data.meshes.new_from_object(tmp.evaluated_get(dg), depsgraph=dg)
    bpy.data.objects.remove(tmp, do_unlink=True)
    bpy.data.curves.remove(cu)
    me.name = name
    ob = bpy.data.objects.new(name, me)
    coll.objects.link(ob)
    for m in (mats or [fill_material(), detail_material()]):
        me.materials.append(m)
    return ob


def join_into(target, others):
    """Merge mesh objects into target (keeps target origin). Frees the others."""
    bm = bmesh.new()
    bm.from_mesh(target.data)
    inv = target.matrix_world.inverted()
    for o in others:
        me = o.data.copy()
        me.transform(inv @ o.matrix_world)
        # remap material indices by name
        names = [m.name for m in target.data.materials]
        for m in me.materials:
            if m and m.name not in names:
                target.data.materials.append(m)
                names.append(m.name)
        remap = [names.index(m.name) if m else 0 for m in me.materials]
        for p in me.polygons:
            p.material_index = remap[p.material_index] if remap else 0
        bm.from_mesh(me)
        bpy.data.meshes.remove(me)
        bpy.data.objects.remove(o, do_unlink=True)
    bm.to_mesh(target.data)
    bm.free()
    return target


def fit_budget(ob, key):
    """If the root mesh alone is over its triangle budget, decimate it (outline re-added after)."""
    limit = S.BUDGET.get(key)
    child_tris = sum(tri_count(c) for c in ob.children_recursive if c.type == "MESH")
    tris = tri_count(ob)
    if not limit or tris + child_tris <= limit:
        return ob
    room = max(limit * 0.92 - child_tris, limit * 0.3)
    for m in [m for m in ob.modifiers if m.name == "Outline"]:
        ob.modifiers.remove(m)
    add_modifier(ob, "DECIMATE", ratio=max(0.05, min(1.0, room / tris)))
    bake_modifiers(ob)
    add_outline(ob)
    return ob


def tag(ob, family, budget, export_name=None, closed=True):
    fit_budget(ob, budget)
    ob["family"] = family
    ob["budget"] = budget
    ob["closed"] = closed
    ob["export"] = export_name or ob.name
    return ob


def surface_point(ob, direction, origin=Vector((0, 0, 0))):
    """Point and normal where a ray from far outside along -direction hits the body surface.
    Call before finish(): the Outline modifier's flipped shell would otherwise be hit first."""
    d = Vector(direction).normalized()
    start = origin + d * 10.0
    hit, loc, normal, _ = ob.ray_cast(start, -d)
    if not hit:
        ok, loc, normal, _ = ob.closest_point_on_mesh(origin + d * 0.3)
    return Vector(loc), Vector(normal)


def tri_count(ob):
    return sum(len(p.vertices) - 2 for p in ob.data.polygons)


def layout_grid(objs, spacing=1.4, cols=None):
    cols = cols or max(1, math.ceil(math.sqrt(len(objs))))
    for i, ob in enumerate(objs):
        r, c = divmod(i, cols)
        ob.location = Vector((c * spacing, 0, -r * spacing))
    bpy.context.view_layer.update()  # refresh matrix_world before anyone reads bounds


def clean_names(root):
    """Strip Blender's .001 suffixes in one part's tree so Unity finds socket_eyes, pupil_0, etc.
    Any other object holding the clean name is renamed out of the way first."""
    for ob in [root] + list(root.children_recursive):
        base = ob.name.split(".")[0]
        if ob.name == base:
            continue
        other = bpy.data.objects.get(base)
        if other and other is not ob:
            other.name = base + "_x"
        ob.name = base
