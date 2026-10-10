"""Single entry point, run through the Blender MCP in ONE call:

    exec(open(r"<REPO>/tools/blender/build_all.py").read(),
         {"__name__": "__main__", "REPO": r"<REPO>", "FAMILY": "bodies", "MODE": "build"})

FAMILY: bodies | eyes | fins | tails | mouths | specials | angler | hooks | env | props | all
MODE:
  check     reset, build, validate only (no render, seconds) — use this to fix failures
  build     reset, build the family, validate, render ONE contact sheet  (default)
  variants  3x3 hero-fish grid from 9 parameter sets in VARIANTS (style lock, before anything else)
  combo     build all fish families, assemble 16 random fish, render a 4x4 sheet (clipping test)
  export    build the family, validate, and export FBX + thumbnails (only after approval)
Prints a short JSON summary; full details in tools/blender/out/report.json.
"""
import importlib
import json
import math
import os
import random
import sys

import bpy
from mathutils import Vector

REPO = globals().get("REPO") or os.environ.get("BS_REPO") or os.getcwd()
FAMILY = globals().get("FAMILY", "bodies")
MODE = globals().get("MODE", "build")
TOOLS = os.path.join(REPO, "tools", "blender")
if TOOLS not in sys.path:
    sys.path.insert(0, TOOLS)

import style, common, fish_kit, angler_kit, env_kit, validate, contact_sheet, export  # noqa: E402
for m in (style, common, fish_kit, angler_kit, env_kit, validate, contact_sheet, export):
    importlib.reload(m)  # pick up edits without restarting Blender

FAMILIES = {}
FAMILIES.update(fish_kit.FAMILIES)
FAMILIES.update(angler_kit.FAMILIES)
FAMILIES.update(env_kit.FAMILIES)

# Edit these 9 parameter sets for the style-lock grid (MODE="variants"); pick one, copy it into style.py.
VARIANTS = [
    dict(BODY_HEIGHT=h, EYE_DIAM=e, EYE_HEIGHT=y)
    for h in (0.55, 0.62, 0.70)
    for (e, y) in ((0.17, 0.04), (0.20, 0.06), (0.24, 0.02))
]


def build(family):
    names = list(FAMILIES) if family == "all" else [family]
    objs = []
    for n in names:
        objs += FAMILIES[n]()
    return objs


def summary(report, extra=None):
    s = {"family": report["family"], "passed": report["passed"],
         "failed": [{p["name"]: p["problems"]} for p in report["parts"] if not p["ok"]],
         "parts": len(report["parts"])}
    if extra:
        s.update(extra)
    print(json.dumps(s))
    return s


def assemble(body, parts_by_slot, rng):
    """Attach one random part per slot at the body's sockets (copies), for the combo test."""
    sockets = {c.name.split(".")[0][len("socket_"):]: c for c in body.children if c.name.startswith("socket_")}
    placed = []
    plan = [("eyes", "eyes"), ("mouths", "mouth"), ("fins", "fin_L"), ("fins", "fin_R"), ("tails", "tail")]
    special = rng.choice(parts_by_slot["specials"] + [None])
    if special is not None:
        where = {"special_hammerhead": "front", "special_tentacles": "mouth", "special_ink_gland": "belly"}.get(special.name, "top")
        plan.append((special, where))
    for src, sock in plan:
        part = src if not isinstance(src, str) else rng.choice(parts_by_slot[src])
        dup = part.copy()
        dup.data = part.data.copy()
        bpy.context.scene.collection.objects.link(dup)
        dup.parent = sockets[sock]
        dup.matrix_parent_inverse.identity()
        dup.location = (0, 0, 0)
        dup.rotation_euler = (0, 0, 0)
        dup["family"] = "combo_part"
        placed.append(dup)
    return placed


def run():
    if MODE == "variants":
        common.reset_scene()
        heroes = []
        for i, v in enumerate(VARIANTS):
            for k, val in v.items():
                setattr(style, k, val)
            body = fish_kit._body(f"variant_{i + 1}", style.BODY_LEN, style.BODY_HEIGHT, style.BODY_WIDTH)
            eyes = fish_kit._eyes(f"variant_{i + 1}_eyes", [(-style.EYE_SPREAD, 0, 1 + style.EYE_ASYM), (style.EYE_SPREAD, 0, 1 - style.EYE_ASYM)])
            sock = next(c for c in body.children if c.name.startswith("socket_eyes"))
            eyes.parent = sock
            eyes.location = (0, 0, 0)
            del eyes["family"]
            body["family"] = "variants"
            heroes.append(body)
        importlib.reload(style)
        path = contact_sheet.render_sheet(heroes, REPO, "variants")
        print(json.dumps({"mode": "variants", "sheet": path, "sets": VARIANTS}))
        return

    if MODE == "combo":
        common.reset_scene()
        slots = {f: FAMILIES[f]() for f in ("bodies", "eyes", "fins", "tails", "mouths", "specials")}
        rng = random.Random(7)
        fish = []
        for i in range(16):
            body = rng.choice(slots["bodies"])
            b = body.copy()
            b.data = body.data.copy()
            bpy.context.scene.collection.objects.link(b)
            for s in body.children:
                sc = s.copy()
                bpy.context.scene.collection.objects.link(sc)
                sc.parent = b
            b["family"] = "combo"
            assemble(b, slots, rng)
            fish.append(b)
        for coll_name in ("bodies", "eyes", "fins", "tails", "mouths", "specials"):
            c = bpy.data.collections.get(coll_name)
            if c:
                c.hide_render = True
        path = contact_sheet.render_sheet(fish, REPO, "combo")
        print(json.dumps({"mode": "combo", "sheet": path}))
        return

    common.reset_scene()
    build(FAMILY)
    report = validate.run(REPO, FAMILY)
    if MODE == "check":  # build + validate only, no render: the cheap way to iterate on failures
        summary(report)
        return
    if MODE == "export":
        if not report["passed"]:
            summary(report, {"exported": [], "note": "fix validation first"})
            return
        summary(report, {"exported": export.export_family(REPO, None if FAMILY == "all" else FAMILY)})
        return
    path = contact_sheet.family_sheet(REPO, None if FAMILY == "all" else FAMILY) if report["parts"] else None
    summary(report, {"sheet": path})


if __name__ == "__main__":
    run()
