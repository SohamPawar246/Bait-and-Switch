"""One image per review: every part of a family in a grid, side view (top half) + three-quarter view (bottom half),
in the toon preview look. Writes out/contact_<family>.png. Also: variant grids and a 4x4 random-assembly test.
"""
import math
import os
import bpy
import numpy as np
from mathutils import Vector

import common as C
import style as S
import validate as V

RES = 1024


def _engine(scene):
    for eng in ("BLENDER_EEVEE_NEXT", "BLENDER_EEVEE"):
        try:
            scene.render.engine = eng
            return
        except TypeError:
            continue


def _setup(scene):
    _engine(scene)
    scene.render.resolution_x = RES
    scene.render.resolution_y = RES // 2
    scene.render.resolution_percentage = 100
    scene.render.film_transparent = False
    scene.render.image_settings.file_format = "PNG"
    world = scene.world or bpy.data.worlds.new("World")
    scene.world = world
    world.use_nodes = True
    bg = world.node_tree.nodes.get("Background")
    if bg:
        bg.inputs[0].default_value = C.hex_rgba("#2A3346")
        bg.inputs[1].default_value = 1.0
    if not bpy.data.objects.get("ReviewSun"):
        sun = bpy.data.objects.new("ReviewSun", bpy.data.lights.new("ReviewSun", "SUN"))
        sun.data.energy = 3.0
        sun.rotation_euler = (math.radians(50), math.radians(10), math.radians(-30))
        scene.collection.objects.link(sun)


def _camera(name, ortho):
    cam = bpy.data.objects.get(name)
    if not cam:
        cam = bpy.data.objects.new(name, bpy.data.cameras.new(name))
        bpy.context.scene.collection.objects.link(cam)
    cam.data.type = "ORTHO" if ortho else "PERSP"
    return cam


def _bounds(objs):
    pts = []
    for ob in objs:
        for o in [ob] + [c for c in ob.children_recursive if c.type == "MESH"]:
            pts += [o.matrix_world @ Vector(c) for c in o.bound_box]
    lo = Vector((min(p.x for p in pts), min(p.y for p in pts), min(p.z for p in pts)))
    hi = Vector((max(p.x for p in pts), max(p.y for p in pts), max(p.z for p in pts)))
    return lo, hi


def _render(path):
    bpy.context.scene.render.filepath = path
    bpy.ops.render.render(write_still=True)
    img = bpy.data.images.load(path, check_existing=False)
    px = np.array(img.pixels[:], dtype=np.float32).reshape(img.size[1], img.size[0], 4)
    bpy.data.images.remove(img)
    return px


def render_sheet(objs, repo, label):
    scene = bpy.context.scene
    _setup(scene)
    C.layout_grid(objs, spacing=max(1.2, max((o.dimensions.length for o in objs), default=1) * 1.1))
    lo, hi = _bounds(objs)
    centre = (lo + hi) / 2
    span = max(hi.x - lo.x, (hi.z - lo.z) * 2, 1.0) * 1.15

    side = _camera("ReviewSide", True)
    side.location = centre + Vector((0, -20, 0))
    side.rotation_euler = (math.radians(90), 0, 0)
    side.data.ortho_scale = span

    tq = _camera("ReviewThreeQuarter", True)
    tq.location = centre + Vector((14, -14, 9))
    tq.rotation_euler = (Vector((0, 0, 0)) - Vector((14, -14, 9))).to_track_quat("-Z", "Y").to_euler()
    tq.data.ortho_scale = span * 1.1

    out = os.path.join(repo, S.OUT_DIR)
    os.makedirs(out, exist_ok=True)
    scene.camera = side
    a = _render(os.path.join(out, "_side.png"))
    scene.camera = tq
    b = _render(os.path.join(out, "_tq.png"))

    sheet = np.concatenate([b, a], axis=0)  # Blender pixel rows are bottom-up: this puts the side view on top
    h, w = sheet.shape[:2]
    img = bpy.data.images.new(f"contact_{label}", w, h, alpha=True)
    img.pixels = sheet.ravel()
    path = os.path.join(out, f"contact_{label}.png")
    img.filepath_raw = path
    img.file_format = "PNG"
    img.save()
    bpy.data.images.remove(img)
    return path


def family_sheet(repo, family):
    return render_sheet(V.roots(family), repo, family)
