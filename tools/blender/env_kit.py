"""Environment and props: rocks, rock arch, kelp, coral, seabed strip; shrine, candle, OFFERINGS crate,
fish skeleton, egg sac, jar, pegboard, pew. Origins sit on the ground (z = 0).
Export names: env_<name>, prop_<name>. Boats come from Kenney's Watercraft Kit (recolour in Unity).
"""
import math
import bpy
import bmesh
from mathutils import Vector

import common as C
import style as S
from fish_kit import _M, _rx, _ry, _rz


def _noise(name, scale):
    tex = bpy.data.textures.get(name) or bpy.data.textures.new(name, "CLOUDS")
    tex.noise_scale = scale
    return tex


def _rock(name, size, seed, squash=0.7):
    coll = C.collection("env")
    ob = C.mesh_object(name, C.bm_icosphere(size, 3, _M((0, 0, size * squash * 0.8), None, (1.1, 0.9, squash))), coll,
                       [C.detail_material("#3A4A5A", name="Rock")])
    tex = _noise(f"rock_noise_{seed}", 0.4 + seed * 0.07)
    C.add_modifier(ob, "DISPLACE", texture=tex, strength=size * 0.35)
    C.add_modifier(ob, "DECIMATE", ratio=0.35)
    C.bake_modifiers(ob)
    C.finish(ob, smooth=False)
    return C.tag(ob, "env", "env")


def _kelp(name, height, sway, seed):
    coll = C.collection("env")
    pts = [(math.sin(i * 0.9 + seed) * sway * (i / 6), 0, height * i / 6) for i in range(7)]
    ob = C.make_curve_mesh(name, pts, [1.0, 0.95, 0.85, 0.75, 0.6, 0.45, 0.2], 0.035, coll, [C.detail_material("#2F6B4A", name="Kelp")])
    for k in range(3):
        z = height * (0.3 + k * 0.22)
        leaf = C.mesh_object(f"{name}_leaf{k}", C.bm_uvsphere(0.5, 10, 6, _M((0.08 * (1 if k % 2 else -1), 0, z), _ry(60 if k % 2 else -60), (0.05, 0.015, 0.22))),
                             coll, [C.detail_material("#2F6B4A", name="Kelp")])
        C.join_into(ob, [leaf])
    C.finish(ob)
    return C.tag(ob, "env", "env", closed=False)


def _coral(name, seed):
    coll = C.collection("env")
    mb = bpy.data.metaballs.new(name + "_mb")
    mb.resolution = 0.05
    tmp = bpy.data.objects.new(name + "_tmp", mb)
    coll.objects.link(tmp)
    for i in range(7):
        e = mb.elements.new()
        a = seed + i * 1.7
        e.co = (math.cos(a) * 0.12 * (i % 3), math.sin(a) * 0.08, 0.1 + i * 0.07)
        e.radius = 0.12 - i * 0.008
    dg = bpy.context.evaluated_depsgraph_get()
    me = bpy.data.meshes.new_from_object(tmp.evaluated_get(dg), depsgraph=dg)
    bpy.data.objects.remove(tmp, do_unlink=True)
    bpy.data.metaballs.remove(mb)
    ob = bpy.data.objects.new(name, me)
    coll.objects.link(ob)
    me.materials.append(C.detail_material(S.PALETTE["coral"], name="Coral"))
    C.add_modifier(ob, "DECIMATE", ratio=0.4)
    C.bake_modifiers(ob)
    C.finish(ob)
    return C.tag(ob, "env", "env")


def env():
    coll = C.collection("env")
    out = [
        _rock("env_rock_small", 0.35, 1), _rock("env_rock_medium", 0.7, 2), _rock("env_rock_large", 1.2, 3, squash=0.55),
        _kelp("env_kelp_a", 2.4, 0.25, 0.0), _kelp("env_kelp_b", 3.2, 0.35, 1.3), _kelp("env_kelp_c", 1.8, 0.2, 2.6),
        _coral("env_coral_a", 0.0), _coral("env_coral_b", 2.2),
    ]
    # Rock arch: a thick tapered tube along a half circle (hooks can't pass, fish can swim under)
    pts = [(math.cos(math.radians(a)) * 1.6, 0, math.sin(math.radians(a)) * 1.4) for a in range(0, 181, 20)]
    arch = C.make_curve_mesh("env_rock_arch", pts, [1.4, 1.1, 0.9, 0.8, 0.75, 0.8, 0.9, 1.1, 1.4, 1.5], 0.32, coll,
                             [C.detail_material("#3A4A5A", name="Rock")])
    C.add_modifier(arch, "DISPLACE", texture=_noise("arch_noise", 0.5), strength=0.12)
    C.bake_modifiers(arch)
    C.finish(arch, smooth=False)
    out.append(C.tag(arch, "env", "env", closed=False))  # ends sit buried in the seabed
    return out


def props():
    coll = C.collection("props")
    stone = C.detail_material("#6E6A64", name="Stone")
    wood = C.detail_material("#6B4A2E", name="Wood")
    wax = C.detail_material(S.PALETTE["cream"], name="Wax")
    flame = C.detail_material(S.PALETTE["raincoat"], 6.0, name="CandleFlame")
    out = []

    # Shrine stone with an eye sigil
    bm = C.bm_cube(1.0, _M((0, 0, 0.45), None, (0.5, 0.25, 0.9)))
    C.merge_into(bm, C.bm_uvsphere(0.5, 16, 8, _M((0, 0.13, 0.6), _rx(90), (0.22, 0.12, 0.03))), 1)
    C.merge_into(bm, C.bm_uvsphere(0.04, 10, 6, _M((0, 0.14, 0.6))), 2)
    ob = C.mesh_object("prop_shrine", bm, coll, [stone, C.detail_material(S.PALETTE["ink"], name="Sigil"), C.detail_material(S.PALETTE["leviathan"], 3.0, name="SigilGlow")])
    C.add_modifier(ob, "BEVEL", width=0.04, segments=2)
    C.bake_modifiers(ob)
    out.append(ob)

    # Candle (burns impossibly underwater)
    bm = C.bm_cone(0.05, 0.045, 0.22, 12, _M((0, 0, 0.11)))
    C.merge_into(bm, C.bm_cone(0.025, 0.0, 0.07, 8, _M((0, 0, 0.26))), 1)
    out.append(C.mesh_object("prop_candle", bm, coll, [wax, flame]))

    # OFFERINGS crate (stencil text is a decal in Unity)
    ob = C.mesh_object("prop_offerings_crate", C.bm_cube(0.6, _M((0, 0, 0.3), None, (1.3, 1, 0.9))), coll, [wood])
    C.add_modifier(ob, "BEVEL", width=0.025, segments=1)
    C.bake_modifiers(ob)
    out.append(ob)

    # Fish skeleton: a spine with ribs and a skull
    parts = [C.make_curve_mesh("prop_fish_skeleton", [(-0.5, 0, 0.05), (0, 0, 0.1), (0.45, 0, 0.08)], [0.6, 1, 0.8], 0.02, coll, [wax])]
    for i in range(6):
        x = -0.35 + i * 0.13
        h = 0.18 - abs(x) * 0.15
        parts.append(C.make_curve_mesh(f"rib{i}", [(x, 0, 0.1), (x + 0.04, 0, 0.1 + h), (x + 0.1, 0, 0.12 + h * 0.6)], [1, 0.8, 0.4], 0.01, coll, [wax]))
    parts.append(C.mesh_object("skull", C.bm_uvsphere(0.11, 12, 8, _M((0.52, 0, 0.1), None, (1.2, 0.8, 0.9))), coll, [wax]))
    sk = parts[0]
    C.join_into(sk, parts[1:])
    out.append(sk)

    # Workbench dioramas
    out.append(C.mesh_object("prop_egg_sac", C.bm_uvsphere(0.35, 20, 12, _M((0, 0, 0.35), None, (1, 1, 1.15))), coll,
                             [C.detail_material("#C9E8D8", 0.6, name="EggSac")]))
    jar = C.bm_cone(0.16, 0.16, 0.36, 18, _M((0, 0, 0.18)), cap_ends=False)
    C.merge_into(jar, C.bm_cone(0.1, 0.1, 0.05, 16, _M((0, 0, 0.385))), 1)
    out.append(C.mesh_object("prop_jar", jar, coll, [C.detail_material("#9FC7C2", 0.2, name="Glass"), wood]))
    peg = C.bm_cube(1.0, _M((0, 0, 0.6), None, (1.2, 0.04, 0.8)))
    for r in range(4):
        for c in range(6):
            C.merge_into(peg, C.bm_cone(0.012, 0.012, 0.08, 6, _M((-0.5 + c * 0.2, -0.05, 0.3 + r * 0.18), _rx(90))), 1)
    out.append(C.mesh_object("prop_pegboard", peg, coll, [wood, C.detail_material("#8A8F96", name="Metal")]))
    pew = C.bm_cube(1.0, _M((0, 0, 0.25), None, (1.4, 0.35, 0.06)))
    C.merge_into(pew, C.bm_cube(1.0, _M((0, -0.16, 0.5), None, (1.4, 0.05, 0.5))), 0)
    for x in (-0.6, 0.6):
        C.merge_into(pew, C.bm_cube(1.0, _M((x, 0, 0.12), None, (0.06, 0.35, 0.25))), 0)
    out.append(C.mesh_object("prop_pew", pew, coll, [wood]))

    for ob in out:
        C.finish(ob, smooth=ob.name not in ("prop_shrine", "prop_offerings_crate", "prop_pegboard", "prop_pew"))
        C.tag(ob, "props", "props", closed=ob.name not in ("prop_jar", "prop_fish_skeleton"))
    return out


FAMILIES = {"env": env, "props": props}
