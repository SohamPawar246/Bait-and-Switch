"""Export each approved part as its own FBX (with child sockets/pupils) and a transparent thumbnail.
Outline modifiers are skipped (Unity draws its own outline). Files land where PartCatalogBuilder looks.
"""
import math
import os
import bpy
from mathutils import Vector

import common as C
import style as S
import validate as V
import contact_sheet as CS


def _select_tree(ob):
    for o in bpy.context.view_layer.objects:
        o.select_set(False)
    for o in [ob] + list(ob.children_recursive):
        o.select_set(True)
    bpy.context.view_layer.objects.active = ob


def export_family(repo, family):
    mesh_dir = os.path.join(repo, S.EXPORT_MESH_DIR)
    thumb_dir = os.path.join(repo, S.EXPORT_THUMB_DIR)
    os.makedirs(mesh_dir, exist_ok=True)
    os.makedirs(thumb_dir, exist_ok=True)
    done = []
    for ob in V.roots(family):
        name = ob.get("export", ob.name)
        C.clean_names(ob)
        saved = ob.location.copy()
        ob.location = (0, 0, 0)
        bpy.context.view_layer.update()
        _select_tree(ob)
        bpy.ops.export_scene.fbx(
            filepath=os.path.join(mesh_dir, f"{name}.fbx"),
            use_selection=True, object_types={"MESH", "EMPTY"}, use_mesh_modifiers=False,
            apply_scale_options="FBX_SCALE_UNITS", axis_forward="-Z", axis_up="Y",
            bake_space_transform=True, add_leaf_bones=False, use_custom_props=False,
        )
        thumbnail(ob, os.path.join(thumb_dir, f"{name}.png"))
        ob.location = saved
        done.append(name)
    return done


def thumbnail(ob, path, res=256):
    scene = bpy.context.scene
    CS._setup(scene)
    scene.render.resolution_x = res
    scene.render.resolution_y = res
    scene.render.film_transparent = True
    hidden = []
    for o in scene.objects:
        if o.type == "MESH" and o is not ob and o not in ob.children_recursive and not o.hide_render:
            o.hide_render = True
            hidden.append(o)
    lo, hi = CS._bounds([ob])
    centre = (lo + hi) / 2
    cam = CS._camera("ThumbCam", True)
    offset = Vector((3, -4, 2.2))
    cam.location = centre + offset
    cam.rotation_euler = (-offset).to_track_quat("-Z", "Y").to_euler()
    cam.data.ortho_scale = max((hi - lo).length * 1.05, 0.1)
    scene.camera = cam
    scene.render.filepath = path
    bpy.ops.render.render(write_still=True)
    for o in hidden:
        o.hide_render = False
    scene.render.film_transparent = False
