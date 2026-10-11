"""Scene kit: the Angler's dinghy (game + menu), a seagull (menu diorama), a floating lantern float.
Same rules as the other kits: primitives + modifiers, named constants in style.py, every mesh through finish().
Dinghy: bow toward +X, up +Z, waterline at z = 0 (the hull dips below). Gull stands at its origin, faces +X.
Export names: boat_dinghy, prop_seagull.
"""
import math
import bmesh
import bpy
from mathutils import Vector

import common as C
import style as S
from fish_kit import _M, _rx, _ry, _rz


def _section(t, v, L, W, D):
    """Hull surface point. t: −1 stern … +1 bow; v: −1 port gunwale … 0 keel … +1 starboard gunwale.
    Clinker look: each strake steps outward a little (HULL_STEP)."""
    bow = max(0.0, t)
    stern = max(0.0, -t)
    half_w = W * 0.5 * (1.0 - bow ** 2.3) * (1.0 - 0.28 * stern ** 2)
    depth = D * (1.0 - 0.3 * bow ** 2 - 0.12 * stern ** 2)
    sheer = D * S.BOAT_SHEER * (bow ** 2 * 1.6 + stern ** 2 * 0.5)
    a = abs(v)
    strake = math.floor(min(0.999, a) * S.HULL_STRAKES)
    step = S.HULL_STEP * strake
    ang = a * math.pi * 0.5
    y = math.copysign(math.sin(ang) * (half_w + step), v)
    z = -depth * (math.cos(ang) ** 0.75) + sheer * (a ** 2)
    return Vector((t * L * 0.5, y, z))


def dinghy():
    """A clinker-built rowboat: lofted strakes, curved stem post, flat transom, interior ribs, two thwarts, a rail.
    Outside = Fill (weathered paint, tinted per Angler); inside = Wood; rim/stem/rail = Trim."""
    coll = C.collection("scene")
    L, W, D = S.BOAT_LEN, S.BOAT_WIDTH, S.BOAT_DEPTH
    paint = C.toon_material("Fill_Boat", S.BOAT_PAINT)
    wood = C.detail_material(S.BOAT_WOOD, name="Wood")
    trim = C.detail_material(S.BOAT_TRIM, name="Trim")
    dark = C.detail_material("#4A3222", name="WoodDark")

    bm = bmesh.new()
    nu, nv = S.HULL_STATIONS, S.HULL_SECTION
    grid = []
    for i in range(nu + 1):
        t = -1.0 + 2.0 * i / nu
        row = [bm.verts.new(_section(t, -1.0 + 2.0 * j / nv, L, W, D)) for j in range(nv + 1)]
        grid.append(row)
    for i in range(nu):
        for j in range(nv):
            bm.faces.new((grid[i][j], grid[i][j + 1], grid[i + 1][j + 1], grid[i + 1][j]))
    bm.faces.new([grid[0][j] for j in range(nv + 1)])          # transom
    bmesh.ops.remove_doubles(bm, verts=bm.verts, dist=1e-4)     # the bow edge closes itself
    ob = C.mesh_object("boat_dinghy", bm, coll, [paint, wood, trim, dark])
    C.add_modifier(ob, "SOLIDIFY", thickness=S.BOAT_HULL_THICK, offset=-1.0, use_rim=True, material_offset=1, material_offset_rim=2)
    C.bake_modifiers(ob)

    extra = []
    # strake seams outside (dark lines that follow the real hull curvature, both sides)
    for k in range(1, S.HULL_STRAKES):
        v = k / S.HULL_STRAKES
        for side in (1, -1):
            pts = [_section(-0.98 + 1.94 * q / 24, side * v, L, W, D) * 1.0 for q in range(25)]
            pts = [(p.x, p.y + side * 0.006, p.z) for p in pts]
            sm = C.make_curve_mesh(f"seam{k}{side}", pts, [1.0] * len(pts), 0.006, coll, [paint, wood, trim, dark])
            for f in sm.data.polygons:
                f.material_index = 3
            extra.append(sm)
    # gunwale rail along the sheer
    for side in (1, -1):
        pts = [_section(-1.0 + 2.0 * q / 30, side * 1.0, L, W, D) for q in range(31)]
        pts = [(p.x, p.y, p.z + 0.012) for p in pts]
        rail = C.make_curve_mesh(f"rail{side}", pts, [1.0] * len(pts), S.BOAT_RAIL, coll, [paint, wood, trim, dark])
        for f in rail.data.polygons:
            f.material_index = 2
        extra.append(rail)
    # curved stem post rising at the bow (the gull stands on it)
    stem_pts = [_section(1.0, 0.0, L, W, D) + Vector((0.0, 0, 0)),
                _section(0.99, 0.6, L, W, D) * Vector((1, 0, 1)) + Vector((0.03, 0, 0)),
                _section(1.0, 1.0, L, W, D) * Vector((1, 0, 1)) + Vector((0.06, 0, 0.06)),
                _section(1.0, 1.0, L, W, D) * Vector((1, 0, 1)) + Vector((0.08, 0, 0.16))]
    stem = C.make_curve_mesh("stem", [tuple(p) for p in stem_pts], [1.2, 1.1, 1.0, 0.9], 0.03, coll, [paint, wood, trim, dark])
    for f in stem.data.polygons:
        f.material_index = 2
    extra.append(stem)
    # interior ribs (frames) following the section
    for k in range(S.HULL_RIBS):
        t = -0.8 + 1.5 * k / (S.HULL_RIBS - 1)
        pts = [tuple(_section(t, -0.95 + 1.9 * q / 14, L, W * 0.93, D * 0.93)) for q in range(15)]
        rib = C.make_curve_mesh(f"rib{k}", pts, [1.0] * len(pts), 0.012, coll, [paint, wood, trim, dark])
        for f in rib.data.polygons:
            f.material_index = 3
        extra.append(rib)
    # thwarts (seats) and a keel strip
    for t in (-0.42, 0.12):
        p = _section(t, 0.0, L, W, D)
        hw = _section(t, 1.0, L, W, D).y
        b = C.mesh_object("thwart", C.bm_cube(1.0, _M((p.x, 0, -D * 0.18), None, (0.17, hw * 1.9, 0.035))), coll, [paint, wood, trim, dark])
        for f in b.data.polygons:
            f.material_index = 1
        extra.append(b)
    keel = [tuple(_section(-1.0 + 2.0 * q / 20, 0.0, L, W, D) + Vector((0, 0, -0.01))) for q in range(21)]
    kl = C.make_curve_mesh("keel", keel, [1.0] * 21, 0.02, coll, [paint, wood, trim, dark])
    for f in kl.data.polygons:
        f.material_index = 2
    extra.append(kl)
    for side in (1, -1):
        p = _section(-0.1, side * 1.0, L, W, D)
        extra.append(C.mesh_object("oarlock", C.bm_cone(0.02, 0.012, 0.06, 8, _M((p.x, p.y, p.z + 0.04))), coll, [trim, wood, paint, dark]))
    C.join_into(ob, extra)
    bow_top = _section(1.0, 1.0, L, W, D) * Vector((1, 0, 1)) + Vector((0.08, 0, 0.19))
    C.add_socket(ob, "seat", Vector((-L * 0.21, 0, -D * 0.12)), (0, 0, 1))     # the Angler sits here
    C.add_socket(ob, "bow", bow_top, (0, 0, 1))
    C.add_socket(ob, "rod", Vector((L * 0.05, -W * 0.5, 0.05)), (0, -0.6, 0.8))
    C.finish(ob)
    return C.tag(ob, "scene", "scene", closed=False)


def seagull():
    """A plump standing gull (white body, grey wings with ink tips, yellow beak and legs, one beady eye per side)."""
    coll = C.collection("scene")
    white = C.toon_material("Fill_Gull", S.GULL_WHITE)
    grey = C.detail_material(S.GULL_GREY, name="GullWing")
    ink = C.detail_material(S.PALETTE["ink"], name="GullInk")
    beak = C.detail_material(S.GULL_BEAK, name="GullBeak")
    h = S.GULL_HEIGHT
    bm = C.bm_uvsphere(0.5, 18, 10, _M((0, 0, h * 0.55), _ry(-12), (h * 0.95, h * 0.5, h * 0.52)))       # body
    C.merge_into(bm, C.bm_uvsphere(h * 0.2, 14, 8, _M((h * 0.38, 0, h * 0.92))), 0)                      # head
    C.merge_into(bm, C.bm_uvsphere(0.5, 10, 6, _M((-h * 0.48, 0, h * 0.6), _ry(-18), (h * 0.35, h * 0.2, h * 0.06))), 1)  # tail
    for side in (1, -1):                                                                                      # folded wings
        C.merge_into(bm, C.bm_uvsphere(0.5, 14, 8, _M((-h * 0.08, side * h * 0.22, h * 0.66), _ry(-14) @ _rx(side * 8), (h * 0.78, h * 0.08, h * 0.3))), 1)
        C.merge_into(bm, C.bm_uvsphere(0.5, 8, 5, _M((-h * 0.42, side * h * 0.2, h * 0.68), _ry(-20), (h * 0.3, h * 0.07, h * 0.12))), 2)  # ink wingtips
        C.merge_into(bm, C.bm_uvsphere(h * 0.035, 8, 5, _M((h * 0.46, side * h * 0.15, h * 0.97))), 2)    # beady eyes
        C.merge_into(bm, C.bm_cone(h * 0.025, h * 0.018, h * 0.32, 6, _M((0.0, side * h * 0.1, h * 0.14))), 3)  # legs
        C.merge_into(bm, C.bm_uvsphere(0.5, 6, 4, _M((h * 0.05, side * h * 0.1, h * 0.01), None, (h * 0.16, h * 0.08, h * 0.02))), 3)  # feet
    C.merge_into(bm, C.bm_cone(h * 0.06, 0.0, h * 0.26, 8, _M((h * 0.62, 0, h * 0.89), _ry(100))), 3)     # beak
    ob = C.mesh_object("prop_seagull", bm, coll, [white, grey, ink, beak])
    C.finish(ob)
    return C.tag(ob, "scene", "scene", closed=False)


def scene():
    return [dinghy(), seagull()]


FAMILIES = {"scene": scene}
