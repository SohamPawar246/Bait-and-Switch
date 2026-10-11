# Blender asset rules (BAIT & SWITCH)

## Already built (pre-written Saturday, never run yet — fix errors in place, don't rewrite)
`style.py` (all constants) · `common.py` (helpers) · `fish_kit.py` · `angler_kit.py` · `env_kit.py` ·
`validate.py` · `contact_sheet.py` · `export.py` · `build_all.py` (the only entry point).

## How to run (ONE Blender MCP execute call per iteration)
```python
exec(open(r"<REPO>/tools/blender/build_all.py").read(),
     {"__name__": "__main__", "REPO": r"<REPO>", "FAMILY": "bodies", "MODE": "build"})
```
MODE: `variants` (3x3 style-lock grid) · `build` (family + validate + one contact sheet) ·
`combo` (16 random assembled fish, clipping test) · `export` (FBX + thumbnails, only after I approve).
Read the printed JSON first; open `out/report.json` only if something failed.
Look at a contact sheet ONLY in build/variants/combo mode, once per iteration.

## Order on Sunday
1. `variants` → I pick one → copy its numbers into style.py.
2. `build` per family: bodies → eyes → fins → tails → mouths → specials → angler → hooks → env → props.
3. `combo` once after all fish families. 4. `export` with FAMILY="all".

## Headless (when the MCP isn't connected)
`"C:/Program Files/Blender Foundation/Blender 5.2/blender.exe" -b --factory-startup --python-expr "exec(open(r'<REPO>/tools/blender/build_all.py').read(), {'__name__': '__main__', 'REPO': r'<REPO>', 'FAMILY': 'eyes', 'MODE': 'check'})"`

## Contracts Unity relies on
- Bodies carry `socket_eye_L / socket_eye_R / socket_eye_C` (ray-cast on the head) plus front, mouth, fin_L/R, tail, top, belly, coat, hat.
- An eyes part is an EMPTY root (`eyes_two` …) with mesh children `eye_L / eye_R / eye_C`; each goes to the matching body socket.
  Each eye has `pupil_<n>` children (Unity turns them toward hooks).
- Raincoat has `socket_rod`. Camera-facing details on props/env sit on the -Y side.

## Rules
- Blender 5.1+ (official Blender Lab MCP). Use bpy + bmesh; prefer bpy.data/bmesh over bpy.ops
  (exceptions already in the code: render and FBX export).
- Never model interactively or "sculpt by chat". Change scripts, then rerun build_all.py.
- Axes: fish face +X, up +Z; the game camera looks along +Y. Body length 0.9 m.
- Every dimension is a named constant in style.py. No magic numbers in part code.
- Primitives + modifiers only. No booleans. No hand-placed vertices.
- Parts attach at socket_* empties computed from the body surface (ray casts), never typed coordinates.
- Every mesh goes through `common.finish()`. Material slots: Fill (tinted in Unity), Detail, Ink (preview only).
- Budgets live in `style.BUDGET`; validator enforces them.
- If validation fails: fix and rerun, max 2 rounds, then report what's left.
- When validation passes: name the 3 most visible flaws on the sheet, then wait for my verdict.
- If a Blender API name errors (5.x changes), check `dir()` on the object once, fix, move on.
- Style: Cult of the Lamb-like chibi read in 3D — big head, big glossy slightly uneven eyes set low,
  tiny needle teeth, cute shapes / sinister details. Angler hood interior = black void + two glowing pupils.
- Do not use Hyper3D, Sketchfab or Poly Haven model tools.
- Never run git. End with `Commit: art: <summary>` when a family is approved.
