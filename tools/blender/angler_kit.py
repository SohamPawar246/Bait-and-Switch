"""Angler kit: raincoat + hood (worn over any fish body), hats, rods, hooks, baits, relics, lantern.
Hooks/baits hang from their eye at the origin and extend along -Z (down). Rods extend along +Z.
Export names follow Unity: hook_<slug>, bait_<slug>, rod_<slug>, relic_<slug>, hat_<slug>.
"""
import math
import bmesh
from mathutils import Matrix, Vector

import common as C
import style as S
from fish_kit import _M, _rx, _ry, _rz


def _coat_mats():
    # Fill = raincoat (tinted per Angler in Unity); preview shows the canonical yellow.
    return [C.toon_material("Fill_Coat", S.PALETTE["raincoat"]), C.detail_material(S.PALETTE["ink"], name="Void"),
            C.detail_material(S.PALETTE["raincoat"], 4.0, name="HoodEyes")]


def raincoat():
    """Oversized cult raincoat: bell coat, hem, rope belt, stubby sleeves, big hood with a drooping peak,
    a black void for a face and two glowing, slightly uneven pupils. Faces +X; Unity turns it toward the camera."""
    coll = C.collection("angler")
    H, R0, R1, HR = S.COAT_HEIGHT, S.COAT_RADIUS_BOTTOM, S.COAT_RADIUS_TOP, S.HOOD_RADIUS
    bm = C.bm_cone(R0, R1, H, 28, _M((0, 0, H * 0.5 - 0.15)))
    C.merge_into(bm, C.bm_cone(R0 + S.HEM_THICK, R0 + S.HEM_THICK * 0.6, S.HEM_THICK * 2, 28, _M((0, 0, -0.15 + S.HEM_THICK))), 0)
    # rope belt (Detail, dark)
    k = S.BELT_HEIGHT / H
    rb = R0 + (R1 - R0) * k
    C.merge_into(bm, C.bm_cone(rb + S.BELT_THICK, rb + S.BELT_THICK, S.BELT_THICK * 1.6, 28, _M((0, 0, -0.15 + S.BELT_HEIGHT))), 1)
    # stubby sleeves, angled forward-down (the right one holds the rod)
    for side in (1, -1):
        C.merge_into(bm, C.bm_cone(S.SLEEVE_RADIUS * 1.3, S.SLEEVE_RADIUS, S.SLEEVE_LEN, 12,
                                   _M((0.08, side * (R1 + 0.06), H * 0.42), _rx(side * 70) @ _ry(35))), 0)
    # hood: big rounded shell + drooping peak
    hz = H - 0.06
    C.merge_into(bm, C.bm_uvsphere(HR, 22, 12, _M((0.0, 0, hz), None, S.HOOD_STRETCH)), 0)
    C.merge_into(bm, C.bm_cone(HR * 0.42, 0.0, S.HOOD_PEAK_LEN, 14,
                               _M((-HR * 0.45, 0, hz + HR * 1.0 + S.HOOD_PEAK_LEN * 0.3), _ry(-S.HOOD_PEAK_DROOP_DEG))), 0)
    # the void: a deep black ellipsoid filling the hood's opening
    vs = S.VOID_SCALE
    C.merge_into(bm, C.bm_uvsphere(HR, 18, 10, _M((HR * 0.62, 0, hz - 0.02), None, vs)), 1)
    # two glowing pupils, slightly uneven (one higher and bigger): the only "face" an Angler has
    for y, dz, sc in ((-S.HOOD_PUPIL_SPREAD, 0.012, 1.12), (S.HOOD_PUPIL_SPREAD, -0.006, 0.9)):
        C.merge_into(bm, C.bm_uvsphere(S.HOOD_PUPIL * sc, 10, 6, _M((HR * 0.62 + HR * vs[0] * 0.92, y, hz + dz))), 2)
    # toggle buttons down the front
    for i in range(3):
        C.merge_into(bm, C.bm_uvsphere(0.022, 8, 4, _M((R0 * (0.66 + i * 0.1), 0, 0.02 + i * 0.12))), 1)
    ob = C.mesh_object("angler_raincoat", bm, coll, _coat_mats())
    C.add_socket(ob, "rod", Vector((0.16, -(R1 + 0.16), H * 0.32)), (0, 0, 1))
    C.finish(ob)
    return C.tag(ob, "angler", "angler")


def hats():
    coll = C.collection("angler")
    out = []
    # Sou'wester: brim longer at the back
    bm = C.bm_cone(0.36, 0.34, 0.02, 24, _M((-0.04, 0, 0), None, (1.15, 1, 1)))
    C.merge_into(bm, C.bm_uvsphere(0.2, 18, 10, _M((0, 0, 0.06), None, (1, 1, 0.7))), 0)
    out.append(C.mesh_object("hat_sou_wester", bm, C.collection("angler")))
    # Bucket
    bm = C.bm_cone(0.3, 0.3, 0.02, 24)
    C.merge_into(bm, C.bm_cone(0.21, 0.18, 0.16, 20, _M((0, 0, 0.08))), 0)
    out.append(C.mesh_object("hat_bucket", bm, coll))
    # Pointed hood (a tall cone, cultish)
    out.append(C.mesh_object("hat_pointed_hood", C.bm_cone(0.24, 0.0, 0.5, 20, _M((0, 0, 0.22), _ry(-12))), coll))
    for ob in out:
        C.finish(ob)
        C.tag(ob, "angler", "angler")
    return out


def _rod(name, length, radius, bend=8, reel=True):
    coll = C.collection("angler")
    pts = [(0, 0, 0), (0.02, 0, length * 0.4), (0.06, 0, length * 0.75), (length * math.sin(math.radians(bend)), 0, length)]
    ob = C.make_curve_mesh(name, pts, [1.0, 0.8, 0.5, 0.3], radius, coll, [C.detail_material("#6B4A2E", name="Wood")])
    if reel:
        r = C.mesh_object(name + "_reel", C.bm_cone(0.05, 0.05, 0.04, 14, _M((0.04, 0, length * 0.15), _rx(90))), coll,
                          [C.detail_material("#8A8F96", name="Metal")])
        C.join_into(ob, [r])
    C.finish(ob)
    return C.tag(ob, "angler", "angler", closed=False)


def rods():
    L, r = S.ROD_LEN, S.ROD_RADIUS
    return [
        _rod("rod_short", L, r),
        _rod("rod_long", L * 1.35, r * 0.9, bend=12),
        _rod("rod_whip", L * 1.1, r * 0.7, bend=18),
        _rod("rod_heavy", L * 0.95, r * 1.6, bend=4),
    ]


def _j(scale=1.0, rot=0.0):
    s = S.HOOK_SIZE * scale
    pts = [(0, 0, 0), (0, 0, -s * 0.8), (s * 0.05, 0, -s * 1.15), (s * 0.35, 0, -s * 1.3), (s * 0.55, 0, -s * 1.05), (s * 0.5, 0, -s * 0.75)]
    return [(Matrix.Rotation(math.radians(rot), 3, "Z") @ Vector(p))[:] for p in pts]


def _hook(name, builders):
    coll = C.collection("hooks")
    mats = [C.detail_material("#B9BEC6", name="Steel"), C.detail_material(S.PALETTE["hook_red"], name="HookRed")]
    pieces = []
    for i, (pts, radii) in enumerate(builders):
        pieces.append(C.make_curve_mesh(f"{name}_{i}", pts, radii, S.HOOK_WIRE, coll, mats))
    ob = pieces[0]
    ob.name = name
    C.join_into(ob, pieces[1:])
    # eye loop at the top
    eye = C.mesh_object(name + "_eye", C.bm_cone(0.012, 0.012, 0.006, 12, _M((0, 0, 0.008), _rx(90))), coll, mats)
    C.join_into(ob, [eye])
    C.finish(ob, smooth=True)
    return C.tag(ob, "hooks", "hooks", closed=False)


def hooks():
    tip = [1.0, 1.0, 1.0, 0.9, 0.7, 0.25]
    out = [
        _hook("hook_j_hook", [(_j(), tip)]),
        _hook("hook_barbed", [(_j(), tip), ([(S.HOOK_SIZE * 0.5, 0, -S.HOOK_SIZE * 0.75), (S.HOOK_SIZE * 0.42, 0, -S.HOOK_SIZE * 0.88)], [0.9, 0.2])]),
        _hook("hook_big", [(_j(1.5), tip)]),
        _hook("hook_treble", [(_j(1.0, a), tip) for a in (0, 120, 240)]),
    ]
    # Circle hook: a 300-degree loop
    s = S.HOOK_SIZE
    circ = [(0, 0, 0), (0, 0, -s * 0.6)] + [(s * 0.35 * (1 - math.cos(a)), 0, -s * 0.6 - s * 0.35 * math.sin(a)) for a in [i * math.radians(300) / 8 for i in range(1, 9)]]
    out.append(_hook("hook_circle", [(circ, [1.0] * (len(circ) - 1) + [0.3])]))
    # Spiky: J hook plus little spikes along the shank
    spiky = _hook("hook_spiky", [(_j(), tip)])
    bm = bmesh.new()
    bm.from_mesh(spiky.data)
    for i in range(5):
        z = -S.HOOK_SIZE * (0.15 + i * 0.15)
        for side in (-1, 1):
            C.merge_into(bm, C.bm_cone(0.006, 0.0, 0.025, 5, _M((side * 0.012, 0, z), _ry(side * 70))), 1)
    bm.to_mesh(spiky.data)
    bm.free()
    C.fit_budget(spiky, "hooks")
    out.append(spiky)
    return out


def baits():
    coll = C.collection("hooks")
    out = []
    worm = C.make_curve_mesh("bait_worm", [(0.02, 0, -0.03), (0.05, 0, -0.07), (0.02, 0, -0.11), (0.06, 0, -0.15), (0.03, 0, -0.18)],
                             [0.8, 1.0, 1.0, 0.9, 0.5], 0.014, coll, [C.detail_material("#E07A8A", name="Worm")])
    out.append(worm)
    out.append(C.mesh_object("bait_glow_orb", C.bm_uvsphere(0.04, 16, 8, _M((0.03, 0, -0.12))), coll,
                             [C.detail_material(S.PALETTE["glow_mint"], 5.0, name="GlowBait")]))
    lure = C.bm_uvsphere(0.5, 16, 8, _M((0.03, 0, -0.12), _ry(90), (0.05, 0.035, 0.12)))
    C.merge_into(lure, C.bm_uvsphere(0.012, 8, 4, _M((0.03, 0.02, -0.09))), 1)
    out.append(C.mesh_object("bait_chunky_lure", lure, coll, [C.detail_material(S.PALETTE["hook_red"], name="Lure"), C.detail_material(S.PALETTE["cream"], name="LureEye")]))
    spin = C.bm_uvsphere(0.5, 14, 6, _M((0.03, 0, -0.1), _rz(25), (0.07, 0.008, 0.04)))
    C.merge_into(spin, C.bm_cone(0.004, 0.004, 0.12, 6, _M((0.03, 0, -0.09))), 0)
    out.append(C.mesh_object("bait_spinner", spin, coll, [C.detail_material("#E8E2D0", 1.5, name="Chrome")]))
    stink = C.bm_icosphere(0.045, 2, _M((0.03, 0, -0.12)))
    out.append(C.mesh_object("bait_stink_bait", stink, coll, [C.detail_material("#7A8C3A", name="Stink")]))
    for ob in out:
        if ob.name == "bait_stink_bait":
            tex = __import__("bpy").data.textures.new("stink_noise", "CLOUDS")
            tex.noise_scale = 0.03
            C.add_modifier(ob, "DISPLACE", texture=tex, strength=0.02)
            C.bake_modifiers(ob)
        C.finish(ob)
        C.tag(ob, "hooks", "hooks")
    return out


def relics():
    coll = C.collection("angler")
    metal = C.detail_material("#8A8F96", name="Metal")
    out = []
    bell = C.bm_cone(0.09, 0.04, 0.12, 18, _M((0, 0, 0.06)))
    C.merge_into(bell, C.bm_uvsphere(0.04, 12, 6, _M((0, 0, 0.12))), 0)
    out.append(C.mesh_object("relic_sonar_bell", bell, coll, [metal]))
    out.append(lantern("relic_vigil_lantern", 1.4))
    bucket = C.bm_cone(0.12, 0.1, 0.16, 18, _M((0, 0, 0.08)), cap_ends=True)
    out.append(C.mesh_object("relic_chum_bucket", bucket, coll, [C.detail_material("#6B4A2E", name="Wood")]))
    bowl = C.bm_uvsphere(0.13, 18, 10, _M((0, 0, 0.05), None, (1, 1, 0.45)))
    out.append(C.mesh_object("relic_offering_bowl", bowl, coll, [C.detail_material("#B9A27A", name="Brass")]))
    weight = C.bm_uvsphere(0.07, 14, 8, _M((0, 0, -0.07), None, (1, 1, 1.4)))
    out.append(C.mesh_object("relic_deep_weight", weight, coll, [metal]))
    for ob in out:
        if "lantern" not in ob.name:
            C.finish(ob)
            C.tag(ob, "angler", "angler")
    return out


def lantern(name="angler_lantern", scale=1.0):
    coll = C.collection("angler")
    s = S.LANTERN_SIZE * scale
    bm = C.bm_cone(s * 0.55, s * 0.55, s * 0.12, 12, _M((0, 0, 0)))
    C.merge_into(bm, C.bm_cone(s * 0.6, 0.0, s * 0.5, 12, _M((0, 0, s * 1.15))), 0)
    for a in range(4):
        ang = math.radians(45 + a * 90)
        C.merge_into(bm, C.bm_cone(s * 0.05, s * 0.05, s * 0.9, 6, _M((math.cos(ang) * s * 0.45, math.sin(ang) * s * 0.45, s * 0.5))), 0)
    C.merge_into(bm, C.bm_uvsphere(s * 0.3, 12, 6, _M((0, 0, s * 0.45))), 1)
    ob = C.mesh_object(name, bm, coll, [C.detail_material("#2B2B30", name="Iron"), C.detail_material(S.PALETTE["raincoat"], 6.0, name="Flame")])
    C.finish(ob)
    return C.tag(ob, "angler", "angler")


def angler():
    return [raincoat(), lantern()] + hats() + rods() + relics()


FAMILIES = {"angler": angler, "hooks": lambda: hooks() + baits()}
