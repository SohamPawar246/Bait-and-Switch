"""Numeric checks that catch what Claude can't see. Writes out/report.json; anything failing blocks review.
Checks: triangle budget, transform applied, non-manifold edges and inside-out shells (closed parts),
body sockets present and facing correctly, pupils present for eyes.
"""
import json
import os
import bmesh
import bpy

import style as S
import common as C

REQUIRED_SOCKETS = ["front", "eyes", "eye_L", "eye_R", "eye_C", "mouth", "fin_L", "fin_R", "tail", "top", "belly", "coat", "hat"]


def roots(family=None):
    out = []
    for ob in bpy.data.objects:
        if ob.type in ("MESH", "EMPTY") and ob.parent is None and "family" in ob:
            if family in (None, "all", ob["family"]):
                out.append(ob)
    return out


def _mesh_children(ob):
    return [c for c in ob.children_recursive if c.type == "MESH"]


def check(ob):
    problems = []
    if ob.type == "EMPTY":  # eyes: an empty root with eye_* mesh children
        tris = sum(C.tri_count(c) for c in _mesh_children(ob))
        budget = S.BUDGET.get(ob.get("budget", ""), 1000)
        if tris > budget:
            problems.append(f"{tris} tris > budget {budget}")
        kids = [c for c in ob.children if c.type == "MESH"]
        if not kids or not all(c.name.split(".")[0] in ("eye_L", "eye_R", "eye_C") for c in kids):
            problems.append("eyes children must be named eye_L / eye_R / eye_C")
        if not all(any("pupil" in g.name for g in c.children) for c in kids):
            problems.append("every eye needs pupil children")
        return {"name": ob.name, "family": ob.get("family"), "tris": tris, "ok": not problems, "problems": problems}
    tris = C.tri_count(ob) + sum(C.tri_count(c) for c in _mesh_children(ob))
    budget = S.BUDGET.get(ob.get("budget", ""), 1000)
    if tris > budget:
        problems.append(f"{tris} tris > budget {budget}")

    if any(abs(v - 1) > 1e-4 for v in ob.scale) or any(abs(v) > 1e-4 for v in ob.rotation_euler):
        problems.append("transform not applied (scale/rotation)")

    bm = bmesh.new()
    bm.from_mesh(ob.data)
    if ob.get("closed", True):
        bad = sum(1 for e in bm.edges if not e.is_manifold)
        if bad:
            problems.append(f"{bad} non-manifold edges")
        vol = bm.calc_volume(signed=True)
        if vol < 0:
            problems.append("normals inside-out (negative volume)")
    zero = sum(1 for f in bm.faces if f.calc_area() < 1e-9)
    if zero:
        problems.append(f"{zero} zero-area faces")
    bm.free()

    if ob.get("family") == "bodies":
        names = {c.name.split(".")[0] for c in ob.children}
        missing = [s for s in REQUIRED_SOCKETS if f"socket_{s}" not in names]
        if missing:
            problems.append("missing sockets: " + ", ".join(missing))
        eye = next((c for c in ob.children if c.name.startswith("socket_eyes")), None)
        tail = next((c for c in ob.children if c.name.startswith("socket_tail")), None)
        if eye and tail and not (eye.matrix_world.translation.x - ob.matrix_world.translation.x > 0 > tail.matrix_world.translation.x - ob.matrix_world.translation.x):
            problems.append("facing wrong: eyes must be at +X, tail at -X")


    return {"name": ob.name, "family": ob.get("family"), "tris": tris, "ok": not problems, "problems": problems}


def run(repo, family=None):
    results = [check(ob) for ob in roots(family)]
    report = {"family": family, "passed": all(r["ok"] for r in results), "parts": results}
    out = os.path.join(repo, S.OUT_DIR)
    os.makedirs(out, exist_ok=True)
    with open(os.path.join(out, "report.json"), "w") as f:
        json.dump(report, f, indent=1)
    return report
