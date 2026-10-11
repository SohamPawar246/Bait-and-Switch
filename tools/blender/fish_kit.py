"""Fish parts kit: bodies (with sockets), eyes, fins, tails, mouths, specials.

Export names match Unity's PartCatalogBuilder: <slot>_<slug> (e.g. body_round, eyes_three, fins_big_fins).
Bodies carry socket_* empties; every other part is modelled at the origin growing along local +Z.
Material slots: 0 = Fill (tinted in Unity), 1 = Detail (eyes, mouth, ink), 2 = Ink (preview outline only).
"""
import math
import bmesh
from mathutils import Matrix, Vector

import common as C
import style as S

FAM_BUDGET = {"bodies": "body", "eyes": "eyes", "fins": "fins", "tails": "tails", "mouths": "mouths", "specials": "specials"}


def _M(loc=(0, 0, 0), rot=None, scale=(1, 1, 1)):
    m = Matrix.Translation(Vector(loc))
    if rot:
        m = m @ rot
    return m @ Matrix.Diagonal((*scale, 1))


def _rx(deg):
    return Matrix.Rotation(math.radians(deg), 4, "X")


def _ry(deg):
    return Matrix.Rotation(math.radians(deg), 4, "Y")


def _rz(deg):
    return Matrix.Rotation(math.radians(deg), 4, "Z")


# ====================================================================== bodies

def _egg(bm, length, height, width, bias):
    """Scale a unit sphere into a chibi egg: fatter at the head (+X), squashed at the tail."""
    for v in bm.verts:
        x, y, z = v.co
        k = 1.0 + bias * x / 0.5  # x in -0.5..0.5
        v.co = Vector((x * length, y * width * k, z * height * k))
    return bm


def _body(name, length, height, width, bias=S.HEAD_BIAS, extras=None):
    coll = C.collection("bodies")
    bm = C.bm_uvsphere(0.5, 18, 10)
    _egg(bm, length, height, width, bias)
    ob = C.mesh_object(name, bm, coll)
    C.add_modifier(ob, "SUBSURF", levels=1, render_levels=1)
    C.bake_modifiers(ob)
    if extras:
        extras(ob)
    C.apply_transform(ob)
    # Sockets BEFORE finish(): the outline shell has flipped normals and would catch the ray casts.
    _sockets(ob, length, height, width)
    C.finish(ob)
    return C.tag(ob, "bodies", "body")


def _sockets(ob, length, height, width):
    front, n_front = C.surface_point(ob, (1, 0, 0))
    eyes, n_eyes = C.surface_point(ob, (1, 0, S.EYE_HEIGHT / max(1e-3, length / 2) + 0.15))
    mouth, n_mouth = C.surface_point(ob, (1, 0, -0.35))
    finL, nL = C.surface_point(ob, (0.15, 1, -0.15))
    finR, nR = C.surface_point(ob, (0.15, -1, -0.15))
    tail, n_tail = C.surface_point(ob, (-1, 0, 0))
    top, n_top = C.surface_point(ob, (0.1, 0, 1))
    belly, n_belly = C.surface_point(ob, (0, 0, -1))
    eL, nEL = C.surface_point(ob, S.EYE_RAY)
    eR, nER = C.surface_point(ob, (S.EYE_RAY[0], -S.EYE_RAY[1], S.EYE_RAY[2]))
    eC, nEC = C.surface_point(ob, S.EYE_C_RAY)
    C.add_socket(ob, "eye_L", eL, nEL)
    C.add_socket(ob, "eye_R", eR, nER)
    C.add_socket(ob, "eye_C", eC, nEC)
    C.add_socket(ob, "front", front, (1, 0, 0))
    C.add_socket(ob, "eyes", eyes, n_eyes.lerp(Vector((1, 0, 0)), 0.5))
    C.add_socket(ob, "mouth", mouth, n_mouth.lerp(Vector((1, 0, 0)), 0.5))
    C.add_socket(ob, "fin_L", finL, nL)
    C.add_socket(ob, "fin_R", finR, nR)
    C.add_socket(ob, "tail", tail, (-1, 0, 0))
    C.add_socket(ob, "top", top, n_top)
    C.add_socket(ob, "belly", belly, n_belly)
    # Raincoat and hat sit on these when the fish becomes an Angler
    C.add_socket(ob, "coat", Vector((0, 0, 0)), (0, 0, 1))
    C.add_socket(ob, "hat", top + Vector((0, 0, 0.02)), (0, 0, 1))


def bodies():
    out = [
        _body("body_round", S.BODY_LEN, S.BODY_HEIGHT, S.BODY_WIDTH),
        _body("body_eel", S.BODY_LEN * S.EEL_LEN_MUL, S.BODY_HEIGHT * S.EEL_HEIGHT_MUL, S.BODY_WIDTH * 0.8, bias=0.08),
    ]

    def spikes(ob):
        bm = bmesh.new()
        bm.from_mesh(ob.data)
        golden = math.pi * (3 - math.sqrt(5))
        n = S.PUFFER_SPIKE_COUNT
        for i in range(n):
            yv = 1 - (i / (n - 1)) * 2
            r = math.sqrt(1 - yv * yv)
            th = golden * i
            d = Vector((math.cos(th) * r, yv, math.sin(th) * r))
            if d.x < -0.75:
                continue  # keep the tail root clear
            hit, loc, nrm, _ = ob.ray_cast(d * 5, -d)
            if not hit:
                continue
            rot = Vector((0, 0, 1)).rotation_difference(nrm).to_matrix().to_4x4()
            cone = C.bm_cone(0.016, 0.0, S.PUFFER_SPIKE_LEN, 5,Matrix.Translation(loc + nrm * S.PUFFER_SPIKE_LEN * 0.4) @ rot)
            C.merge_into(bm, cone, 0)
        bm.to_mesh(ob.data)
        bm.free()

    s = S.PUFFER_SCALE
    out.append(_body("body_puffer", S.BODY_LEN * s, S.BODY_HEIGHT * s * 1.05, S.BODY_WIDTH * s * 1.1, extras=spikes))
    out.append(_body("body_flatfish", S.BODY_LEN * 1.1, S.BODY_HEIGHT * S.FLAT_HEIGHT_MUL, S.BODY_WIDTH * S.FLAT_WIDTH_MUL, bias=0.05))

    def plates(ob):
        bm = bmesh.new()
        bm.from_mesh(ob.data)
        for i in range(S.SCUTE_COUNT):
            t = i / (S.SCUTE_COUNT - 1)
            d = Vector((0.35 - t * 0.85, 0, 1)).normalized()
            hit, loc, nrm, _ = ob.ray_cast(d * 5, -d)
            if not hit:
                continue
            rot = Vector((0, 0, 1)).rotation_difference(nrm).to_matrix().to_4x4()
            s = 0.11 * (1 - 0.35 * abs(t - 0.4))
            scute = C.bm_uvsphere(s, 10, 6, Matrix.Translation(loc) @ rot @ Matrix.Diagonal((1.0, 0.7, 0.32, 1)))
            C.merge_into(bm, scute, 1)
        bm.to_mesh(ob.data)
        bm.free()

    out.append(_body("body_plated", S.BODY_LEN, S.BODY_HEIGHT, S.BODY_WIDTH, extras=plates))
    return out


# ====================================================================== eyes
# Contract: an eyes part is an EMPTY root (e.g. eyes_two) with mesh children named eye_L / eye_R / eye_C.
# Each child is modelled at its socket origin growing along local +Z (out of the body) and is parented by Unity
# to the body's socket_eye_L / _R / _C. Each eyeball carries pupil_<n> children (Unity turns them toward hooks).

def _eye_mats():
    return [C.fill_material(), C.detail_material(S.PALETTE["cream"], name="EyeWhite")]


def _eye(name, coll, balls):
    """balls: list of (x, y, diam_mul) offsets in the socket frame (clusters use several)."""
    bm = bmesh.new()
    pupils = []
    for (x, y, dm) in balls:
        d = S.EYE_DIAM * dm
        r = d / 2
        zc = d * (0.5 - S.EYE_EMBED)
        big = dm > 0.7
        C.merge_into(bm, C.bm_uvsphere(r, 14 if big else 8, 8 if big else 5, _M((x, y, zc))), 1)
        pr = r * S.PUPIL_DIAM_MUL
        pbm = C.bm_uvsphere(pr, 10 if big else 6, 6 if big else 4, _M((0, 0, 0), None, (1, 1, S.PUPIL_FLAT)))
        # two wet highlights, upper-front: one big, one small (emissive)
        C.merge_into(pbm, C.bm_uvsphere(r * S.HIGHLIGHT_DIAM_MUL, 6, 4, _M((pr * 0.38, pr * 0.42, pr * 0.22))), 1)
        if big:
            C.merge_into(pbm, C.bm_uvsphere(r * S.HIGHLIGHT2_DIAM_MUL, 5, 3, _M((-pr * 0.35, -pr * 0.3, pr * 0.2))), 1)
        pupils.append((pbm, Vector((x, y, zc + r * 0.9))))
        # coloured iris ring pressed onto the front of the eyeball
        C.merge_into(bm, C.bm_uvsphere(r * S.IRIS_MUL, 14 if big else 8, 6 if big else 4,
                                       _M((x, y, zc + r * 0.86), None, (1, 1, 0.22))), 2)
    ob = C.mesh_object(name, bm, coll, _eye_mats() + [C.detail_material("#8A5A9E", name="Iris")])
    C.finish(ob)
    for i, (pbm, loc) in enumerate(pupils):
        p = C.mesh_object(f"pupil_{i}", pbm, coll,
                          [C.detail_material(S.PALETTE["ink"], name="Pupil"), C.detail_material(S.PALETTE["cream"], 3.0, name="Highlight")])
        C.finish(p)
        p.parent = ob
        p.location = loc
    return ob


def _lid(coll, d, cover, droop):
    """A heavy upper eyelid (Fill/skin): a dome capping the top of the eyeball, tilted for an uneven, sleepy stare."""
    r = d / 2 * 1.06
    zc = d * (0.5 - S.EYE_EMBED)
    bm = C.bm_uvsphere(r, 14, 8)
    bmesh.ops.bisect_plane(bm, geom=bm.verts[:] + bm.edges[:] + bm.faces[:], plane_co=(0, r * (1 - 2 * cover), 0), plane_no=(0, -1, 0), clear_outer=True)
    for v in bm.verts:
        v.co = _rx(droop).to_3x3() @ v.co + Vector((0, 0, zc))
    lid = C.mesh_object("lid", bm, coll, [C.fill_material()])
    C.add_modifier(lid, "SOLIDIFY", thickness=0.008, offset=1.0)
    C.bake_modifiers(lid)
    return lid


def _eyes(name, sockets, lids=None):
    """sockets: {"L": balls, "R": balls, "C": balls}; lids: {"L": cover}. Returns the EMPTY root."""
    import bpy
    coll = C.collection("eyes")
    root = bpy.data.objects.new(name, None)
    root.empty_display_size = 0.1
    coll.objects.link(root)
    for key, balls in sockets.items():
        e = _eye(f"eye_{key}", coll, balls)
        e.parent = root
        if lids and key in lids and balls:
            d = S.EYE_DIAM * balls[0][2]
            lid = _lid(coll, d, lids[key], 12 if key == "L" else -8)
            C.join_into(e, [lid])
    root["family"] = "eyes"
    root["budget"] = "eyes"
    root["closed"] = True
    root["export"] = name
    return root


def eyes():
    a = S.EYE_ASYM
    k = S.SEVEN_EYE_DIAM_MUL
    cl = 0.045  # cluster offset for the seven-eyed horror
    return [
        _eyes("eyes_two", {"L": [(0, 0, 1 + a)], "R": [(0, 0, 1 - a)]}, lids={"R": S.LID_COVER}),
        _eyes("eyes_three", {"L": [(0, 0, 0.92 + a)], "R": [(0, 0, 0.92 - a)], "C": [(0, 0, 0.55)]}, lids={"L": S.LID_COVER * 0.6}),
        _eyes("eyes_seven", {"L": [(0, 0, k * 1.5 * (1 + a)), (cl * 1.4, cl * 1.2, k), (-cl * 1.3, -cl, k * 0.8)],
                             "R": [(0, 0, k * 1.5 * (1 - a)), (-cl * 1.4, cl * 1.1, k * 0.9), (cl * 1.2, -cl, k * 0.75)],
                             "C": [(0, 0, k * 1.2)]}),
        _eyes("eyes_cyclops", {"C": [(0, 0, S.CYCLOPS_DIAM_MUL)]}),
    ]


# ====================================================================== fins

def _fan(length, width, scallops, spread_deg, sweep_deg):
    """A fan-shaped membrane in the local X/Z plane growing along +Z, with a scalloped outer edge (bmesh grid)."""
    bm = bmesh.new()
    rings, segs = 6, scallops * 6
    verts = []
    for i in range(rings + 1):
        t = i / rings
        row = []
        for j in range(segs + 1):
            u = j / segs
            ang = math.radians(-spread_deg / 2 + spread_deg * u + sweep_deg)
            scallop = 1.0 - 0.13 * (0.5 + 0.5 * math.cos(u * scallops * 2 * math.pi)) * t
            rad = length * (0.12 + 0.88 * t) * scallop
            row.append(bm.verts.new((math.sin(ang) * rad * (width / length), 0.0, math.cos(ang) * rad)))
        verts.append(row)
    for i in range(rings):
        for j in range(segs):
            bm.faces.new((verts[i][j], verts[i][j + 1], verts[i + 1][j + 1], verts[i + 1][j]))
    return bm


def _fin(name, length, width, thick, sweep=25):
    coll = C.collection("fins")
    bm = _fan(length, width * 1.6, S.FIN_SCALLOPS, 70, 0)
    # lie flat against the body side (local X/Y plane, facing the camera), sweeping back (−X) and drooping down
    m = (_rz(18 + sweep * 0.4) @ _rx(90) @ _ry(-90)).to_3x3()
    for v in bm.verts:
        v.co = m @ v.co + Vector((0, 0, thick * 1.5))
    ob = C.mesh_object(name, bm, coll)
    C.add_modifier(ob, "SOLIDIFY", thickness=thick, offset=0.0)
    C.add_modifier(ob, "SUBSURF", levels=1, render_levels=1)
    C.bake_modifiers(ob)
    C.finish(ob)
    return C.tag(ob, "fins", "fins")


def fins():
    return [
        _fin("fins_stubby", S.FIN_LEN, S.FIN_WIDTH, S.FIN_THICK),
        _fin("fins_big_fins", S.FIN_LEN * S.BIG_FIN_MUL, S.FIN_WIDTH * S.BIG_FIN_MUL, S.FIN_THICK),
        _fin("fins_wing_fins", S.FIN_LEN * S.WING_FIN_LEN_MUL, S.FIN_WIDTH * S.WING_FIN_WIDTH_MUL, S.FIN_THICK, sweep=45),
    ]


# ====================================================================== tails (local Y = up, thin along local X)

def _lobe(length, width, angle, thick=S.TAIL_THICK):
    return C.bm_uvsphere(0.5, 14, 8, _M((0, 0, 0), _rx(-angle), (thick, width, length)) @ Matrix.Translation((0, 0, 0.42)))


def _tail(name, lobes, extra=None):
    """Tail fins as scalloped fan membranes in the local Y/Z plane (up = local Y), joined at a fleshy root."""
    coll = C.collection("tails")
    bm = bmesh.new()
    for (length, width, ang) in lobes:
        fan = _fan(length * 1.08, width * 2.6, max(2, S.TAIL_SCALLOPS // max(1, len(lobes) - 1)), 64, 0)
        rot = _rx(-ang) @ _rz(90)
        for v in fan.verts:
            v.co = rot.to_3x3() @ v.co
        C.merge_into(bm, fan, 0)
    ob = C.mesh_object(name, bm, coll)
    C.add_modifier(ob, "SOLIDIFY", thickness=S.TAIL_THICK * 0.7, offset=0.0)
    C.add_modifier(ob, "SUBSURF", levels=1, render_levels=1)
    C.bake_modifiers(ob)
    stub = C.mesh_object(name + "_root", C.bm_uvsphere(0.5, 12, 8, _M((0, 0, 0.03), None, (S.TAIL_THICK * 2.6, S.TAIL_SPREAD * 0.4, 0.11))), coll)
    C.join_into(ob, [stub])
    if extra:
        extra(ob)
    C.finish(ob)
    return C.tag(ob, "tails", "tails")


def tails():
    L, W = S.TAIL_LEN, S.TAIL_SPREAD
    return [
        _tail("tail_fan", [(L, W * 0.55, 28), (L, W * 0.55, -28), (L * 0.9, W * 0.5, 0)]),
        _tail("tail_fork", [(L * 1.25, W * 0.32, 40), (L * 1.25, W * 0.32, -40)]),
        _tail("tail_curl", [(L * 0.8, W * 0.35, 15), (L * 0.55, W * 0.3, 70), (L * 0.4, W * 0.25, 120)]),
        _tail("tail_thrasher", [(L * 1.2, W * 0.6, 35), (L * 1.2, W * 0.6, -35), (L * 1.1, W * 0.55, 0), (L * 0.8, W * 0.4, 60)]),
    ]


# ====================================================================== mouths (local X sideways, Y up, +Z out)

def _mouth_slit(bm, width, height):
    slit = C.bm_uvsphere(0.5, 16, 8, _M((0, 0, -S.MOUTH_DEPTH), None, (width, height, S.MOUTH_DEPTH * 3)))
    C.merge_into(bm, slit, 1)


def _teeth(bm, width, count, length, down=True):
    for i in range(count):
        t = (i / (count - 1)) - 0.5
        x = t * width * 0.85
        y = 0.012 - abs(t) * 0.01
        rot = _rx(180 if down else 0) @ _rx(-70)
        cone = C.bm_cone(S.TOOTH_RADIUS, 0.0, length * (1.0 - abs(t) * 0.5), 6, _M((x, y, 0.004), rot))
        C.merge_into(bm, cone, 0)


def _mouth(name, build):
    coll = C.collection("mouths")
    bm = bmesh.new()
    build(bm)
    ob = C.mesh_object(name, bm, coll, [C.detail_material(S.PALETTE["cream"], name="Teeth"), C.detail_material(S.PALETTE["ink"], name="MouthInk")])
    C.finish(ob, smooth=True)
    return C.tag(ob, "mouths", "mouths", closed=False)


def mouths():
    w = S.MOUTH_WIDTH
    return [
        _mouth("mouth_small", lambda bm: _mouth_slit(bm, w * 0.7, 0.025)),
        _mouth("mouth_needle_teeth", lambda bm: (_mouth_slit(bm, w, 0.035), _teeth(bm, w, S.TEETH_COUNT, S.TOOTH_LEN))),
        _mouth("mouth_big_jaw", lambda bm: (_mouth_slit(bm, w * S.JAW_MUL, 0.07), _teeth(bm, w * S.JAW_MUL, S.TEETH_COUNT + 4, S.TOOTH_LEN * 1.4),
                                            _teeth(bm, w * S.JAW_MUL * 0.8, S.TEETH_COUNT, S.TOOTH_LEN * 1.1, down=False))),
        _mouth("mouth_sucker", lambda bm: C.merge_into(bm, C.bm_cone(S.SUCKER_RADIUS, S.SUCKER_RADIUS * 0.7, 0.03, 18, _M((0, 0, 0.005)), cap_ends=False), 1)),
    ]


# ====================================================================== specials

def specials():
    coll = C.collection("specials")
    out = []

    # Hammerhead: a wide bar at the front socket (local X = sideways)
    bm = C.bm_uvsphere(0.5, 20, 10, _M((0, 0, S.HAMMER_THICK * 0.25), None, (S.HAMMER_SPAN, S.HAMMER_THICK * 0.8, S.HAMMER_THICK)))
    ob = C.mesh_object("special_hammerhead", bm, coll)
    C.finish(ob)
    out.append(C.tag(ob, "specials", "specials"))

    # Tentacles: from the mouth socket, hanging and curling down (local -Y)
    parts = []
    for i in range(S.TENTACLE_COUNT):
        x = (i / (S.TENTACLE_COUNT - 1) - 0.5) * S.MOUTH_WIDTH * 1.6
        L = S.TENTACLE_LEN * (0.85 + 0.3 * ((i * 37) % 7) / 7)
        pts = [(x, 0, 0.01), (x * 1.1, -L * 0.3, 0.05), (x * 1.3, -L * 0.7, 0.02), (x * 1.2, -L * 0.95, -0.06), (x, -L * 0.85, -0.12)]
        parts.append(C.make_curve_mesh(f"tent_{i}", pts, [1.0, 0.85, 0.6, 0.35, 0.15], S.TENTACLE_RADIUS, coll))
    ob = parts[0]
    ob.name = "special_tentacles"
    C.join_into(ob, parts[1:])
    C.finish(ob)
    out.append(C.tag(ob, "specials", "specials"))

    # Spines: along the top socket, fanned fore-aft (top socket local Y = world forward)
    bm = bmesh.new()
    for i in range(S.SPINE_COUNT):
        t = i / (S.SPINE_COUNT - 1) - 0.5
        lean = -t * 50 - 15
        C.merge_into(bm, C.bm_cone(0.02, 0.0, S.SPINE_LEN * (1 - abs(t) * 0.8), 6,
                                    _M((0, t * S.BODY_LEN * 0.6, S.SPINE_LEN * 0.35), _rx(lean))), 0)
    ob = C.mesh_object("special_spines", bm, coll)
    C.finish(ob, smooth=False)
    out.append(C.tag(ob, "specials", "specials"))

    # Angler lamp: a stalk from the top socket bending forward, emissive bulb
    stalk = C.make_curve_mesh("special_angler_lamp", [(0, 0, 0), (0, 0.05, S.LAMP_STALK * 0.6), (0, 0.18, S.LAMP_STALK), (0, 0.28, S.LAMP_STALK * 0.85)],
                              [1.0, 0.8, 0.6, 0.5], 0.014, coll)
    bulb = C.mesh_object("lamp_bulb", C.bm_uvsphere(S.LAMP_BULB, 16, 8, _M((0, 0.3, S.LAMP_STALK * 0.8))), coll,
                         [C.detail_material(S.PALETTE["glow_mint"], 4.0, name="Glow")])
    C.join_into(stalk, [bulb])
    C.finish(stalk)
    out.append(C.tag(stalk, "specials", "specials"))

    # Ink gland: a dark sac at the belly socket
    ob = C.mesh_object("special_ink_gland", C.bm_uvsphere(0.08, 14, 8, _M((0, 0, 0.03), None, (1.3, 0.9, 0.8))), coll,
                       [C.detail_material(S.PALETTE["abyss"], name="InkSac")])
    C.finish(ob)
    out.append(C.tag(ob, "specials", "specials"))

    # Electric organ: three glowing nodules along the top
    bm = bmesh.new()
    for i in range(3):
        C.merge_into(bm, C.bm_uvsphere(0.035, 10, 6, _M((0, (i - 1) * 0.14, 0.02))), 0)
    ob = C.mesh_object("special_electric_organ", bm, coll, [C.detail_material("#7FD8FF", 3.0, name="Spark")])
    C.finish(ob)
    out.append(C.tag(ob, "specials", "specials"))
    return out


FAMILIES = {
    "bodies": bodies, "eyes": eyes, "fins": fins, "tails": tails, "mouths": mouths, "specials": specials,
}
