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
    """A swaying stem with broad alternating blades (cover for fish, drawn in front of them)."""
    coll = C.collection("env")
    stem_mat = C.detail_material("#24563B", name="KelpStem")
    blade_mat = C.detail_material("#3E8A55", name="Kelp")
    n = 9
    pts = [(math.sin(i * 0.8 + seed) * sway * (i / n), 0, height * i / n) for i in range(n + 1)]
    ob = C.make_curve_mesh(name, pts, [1.0 - 0.7 * i / n for i in range(n + 1)], S.KELP_STEM, coll, [stem_mat, blade_mat])
    blades = []
    for k in range(S.KELP_BLADES):
        t = (k + 1) / (S.KELP_BLADES + 1)
        i = t * n
        x = math.sin(i * 0.8 + seed) * sway * t
        side = 1 if k % 2 else -1
        L = S.KELP_BLADE_LEN * (1.0 - 0.35 * t)
        bm = C.bm_uvsphere(0.5, 12, 6, _M((x + side * L * 0.38, 0, height * t + L * 0.15), _ry(side * 62), (S.KELP_BLADE_WIDTH, 0.018, L)))
        C.set_material_index(bm, 1)
        blades.append(C.mesh_object(f"{name}_b{k}", bm, coll, [stem_mat, blade_mat]))
    C.join_into(ob, blades)
    C.finish(ob)
    return C.tag(ob, "env", "env", closed=False)


def _coral(name, seed, color):
    """Branching tube coral: tapered branches fanning up from a base, rounded tips."""
    coll = C.collection("env")
    mat = C.detail_material(color, name=f"Coral_{color.lstrip('#')}")
    parts = []
    for b in range(S.CORAL_BRANCHES):
        a = seed + b * (math.tau / S.CORAL_BRANCHES)
        lean = 0.35 + 0.25 * ((b * 7) % 3) / 2
        h = S.CORAL_HEIGHT * (0.7 + 0.3 * ((b * 5) % 4) / 3)
        dx, dy = math.cos(a) * lean, math.sin(a) * lean * 0.6
        pts = [(0, 0, 0), (dx * 0.15, dy * 0.15, h * 0.35), (dx * 0.45, dy * 0.45, h * 0.7), (dx * 0.6, dy * 0.6, h)]
        parts.append(C.make_curve_mesh(f"{name}_br{b}", pts, [1.0, 0.8, 0.65, 0.55], 0.08, coll, [mat]))
        parts.append(C.mesh_object(f"{name}_tip{b}", C.bm_uvsphere(0.055, 8, 5, _M((dx * 0.6, dy * 0.6, h))), coll, [mat]))
    ob = parts[0]
    ob.name = name
    C.join_into(ob, parts[1:])
    C.finish(ob)
    return C.tag(ob, "env", "env", closed=False)


def env():
    coll = C.collection("env")
    out = [
        _rock("env_rock_small", 0.35, 1), _rock("env_rock_medium", 0.7, 2), _rock("env_rock_large", 1.2, 3, squash=0.55),
        _kelp("env_kelp_a", 3.0, 0.3, 0.0), _kelp("env_kelp_b", 4.2, 0.4, 1.3), _kelp("env_kelp_c", 2.2, 0.25, 2.6),
        _coral("env_coral_a", 0.0, S.PALETTE["coral"]), _coral("env_coral_b", 2.2, S.PALETTE["lilac"]),
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

    sigil_ink = C.detail_material(S.PALETTE["ink"], name="Sigil")
    sigil_glow = C.detail_material(S.PALETTE["leviathan"], 3.5, name="SigilGlow")

    # Shrine: stacked standing stones, a glowing eye sigil on the camera face (-Y), two candles at its foot
    Hs = S.SHRINE_HEIGHT
    bm = C.bm_cube(1.0, _M((0, 0, Hs * 0.36), None, (0.5, 0.32, Hs * 0.72)))
    C.merge_into(bm, C.bm_cube(1.0, _M((0, 0, Hs * 0.8), _ry(4), (0.62, 0.36, Hs * 0.16))), 0)
    C.merge_into(bm, C.bm_cube(1.0, _M((0.02, 0, Hs * 0.94), _ry(-6), (0.34, 0.26, Hs * 0.12))), 0)
    sg = S.SIGIL_SIZE
    C.merge_into(bm, C.bm_uvsphere(0.5, 16, 8, _M((0, -0.165, Hs * 0.5), _rx(90), (sg, sg * 0.55, 0.03))), 1)
    C.merge_into(bm, C.bm_uvsphere(sg * 0.22, 12, 6, _M((0, -0.18, Hs * 0.5), None, (1, 0.35, 1))), 2)
    C.merge_into(bm, C.bm_uvsphere(sg * 0.09, 8, 4, _M((0, -0.19, Hs * 0.5), None, (1, 0.4, 1.6))), 1)
    for x in (-0.18, 0.18):
        C.merge_into(bm, C.bm_cone(0.035, 0.032, 0.12, 8, _M((x, -0.22, 0.06))), 3)
        C.merge_into(bm, C.bm_cone(0.016, 0.0, 0.05, 6, _M((x, -0.22, 0.145))), 4)
    ob = C.mesh_object("prop_shrine", bm, coll, [stone, sigil_ink, sigil_glow, wax, flame])
    out.append(ob)

    # Candle: thick wax with drips on a dish; the flame burns impossibly underwater
    r, h = S.CANDLE_RADIUS, S.CANDLE_HEIGHT
    bm = C.bm_cone(r * 2.0, r * 1.8, 0.025, 16, _M((0, 0, 0.0125)))
    C.merge_into(bm, C.bm_cone(r, r * 0.92, h, 14, _M((0, 0, 0.025 + h * 0.5))), 0)
    for k in range(5):
        a = k * 1.3
        C.merge_into(bm, C.bm_uvsphere(r * 0.28, 6, 4, _M((math.cos(a) * r * 0.95, math.sin(a) * r * 0.95, 0.025 + h * (0.6 + 0.08 * (k % 3))), None, (1, 1, 2.2))), 0)
    C.merge_into(bm, C.bm_uvsphere(r * 0.45, 10, 6, _M((0, 0, 0.025 + h + r * 0.75), None, (1, 1, 2.0))), 1)
    out.append(C.mesh_object("prop_candle", bm, coll, [wax, flame]))

    # OFFERINGS crate: planked box (the stencil is a decal in Unity)
    cx, cy, cz = S.CRATE_SIZE
    bm = C.bm_cube(1.0, _M((0, 0, cz * 0.5), None, (cx, cy, cz)))
    plank = C.detail_material("#4A3220", name="Plank")
    for i in range(3):
        z = cz * (0.2 + i * 0.3)
        C.merge_into(bm, C.bm_cube(1.0, _M((0, -cy * 0.5 - 0.01, z), None, (cx * 1.02, 0.025, cz * 0.12))), 1)
    for x in (-cx * 0.45, cx * 0.45):
        C.merge_into(bm, C.bm_cube(1.0, _M((x, -cy * 0.5 - 0.015, cz * 0.5), None, (0.07, 0.03, cz * 1.02))), 1)
    out.append(C.mesh_object("prop_offerings_crate", bm, coll, [wood, plank]))

    # Fish skeleton: spine, ribs, tail bones and a skull with a dark eye socket (camera side)
    parts = [C.make_curve_mesh("prop_fish_skeleton", [(-0.55, 0, 0.06), (-0.1, 0, 0.11), (0.4, 0, 0.1)], [0.6, 1, 0.9], 0.028, coll, [wax, sigil_ink])]
    for i in range(7):
        x = -0.4 + i * 0.12
        hh = 0.2 - abs(x + 0.05) * 0.18
        for sgn in (1, -1):
            parts.append(C.make_curve_mesh(f"rib{i}{sgn}", [(x, 0, 0.11), (x + 0.05, 0, 0.11 + sgn * hh), (x + 0.12, 0, 0.11 + sgn * hh * 0.75)],
                                           [1, 0.8, 0.4], 0.012, coll, [wax, sigil_ink]))
    for sgn in (1, -1):
        parts.append(C.make_curve_mesh(f"tailbone{sgn}", [(-0.55, 0, 0.06), (-0.68, 0, 0.06 + sgn * 0.12)], [1, 0.4], 0.014, coll, [wax, sigil_ink]))
    parts.append(C.mesh_object("skull", C.bm_uvsphere(0.13, 14, 8, _M((0.5, 0, 0.12), None, (1.25, 0.85, 0.95))), coll, [wax, sigil_ink]))
    sock = C.bm_uvsphere(0.045, 10, 6, _M((0.55, -0.095, 0.15), None, (1, 0.5, 1)))
    C.set_material_index(sock, 1)
    parts.append(C.mesh_object("eye_socket", sock, coll, [wax, sigil_ink]))
    sk = parts[0]
    C.join_into(sk, parts[1:])
    out.append(sk)

    # Egg sac: a cluster of pale eggs, each with a dark watching eye
    bm = bmesh.new()
    golden = math.pi * (3 - math.sqrt(5))
    for i in range(S.EGG_COUNT):
        t = i / max(1, S.EGG_COUNT - 1)
        a = golden * i
        rr = 0.16 * math.sqrt(t)
        p = Vector((math.cos(a) * rr, math.sin(a) * rr * 0.7, S.EGG_RADIUS + 0.22 * (1 - t) + 0.05))
        C.merge_into(bm, C.bm_uvsphere(S.EGG_RADIUS, 10, 6, _M(tuple(p))), 0)
        C.merge_into(bm, C.bm_uvsphere(S.EGG_RADIUS * 0.32, 8, 4, _M((p.x, p.y - S.EGG_RADIUS * 0.85, p.z), None, (1, 0.4, 1))), 1)
    out.append(C.mesh_object("prop_egg_sac", bm, coll, [C.detail_material("#C9E8D8", 0.4, name="EggSac"), sigil_ink]))
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
        C.finish(ob, smooth=ob.name not in ("prop_offerings_crate", "prop_pegboard", "prop_pew"))
        C.tag(ob, "props", "props", closed=ob.name not in ("prop_jar", "prop_fish_skeleton", "prop_shrine", "prop_offerings_crate"))
    return out


FAMILIES = {"env": env, "props": props}
