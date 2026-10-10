# Art production with Claude + Blender MCP

The weird assets you got before are a known pattern, not bad luck. Real-world tests and research point to specific weaknesses:

- In one public test, asking Claude for a simple donut through Blender MCP took two hours and about 60% of a Max plan's session tokens, and the result still had clipping and colour artifacts.
- Testers report Claude losing track of where things are after about thirty scene operations, because the conversation is its only memory of the scene.
- Language models have no feel for edge flow, so precise organic modelling by hand-placed geometry goes badly.
- In benchmarks of LLM-written 3D modelling code, the most common failure is **spatial precision**: small position, scale and rotation mistakes. Errors drop when every dimension is a named variable, and iterative refinement loops beat one-shot generation. Sign and direction reversals are another frequent failure.
- A rendered image shows *that* something is wrong, not which line of code caused it, so visual feedback alone makes fixes oscillate.

The fix is a pipeline where Claude never "sculpts by chat": it edits one deterministic script, numbers catch the mistakes, and you only judge what passes.

&#91;embedded content: Asset loop · script, rebuild, check, review, export\]

### Each weakness and its fix

| Weakness | What it looks like | Fix |
| --- | --- | --- |
| Spatial precision | Eyes floating off the face, fins detached or buried | Sockets computed from the body's real surface (closest point on mesh), never hand-typed coordinates |
| Direction mix-ups | Fins pointing backwards, eyes on the back, model rotated after export | One axis convention in the rules; the validator checks facing |
| No eye for "cute" | Proportions off, uncanny faces | A proportion sheet of named constants, reference images, and variant grids where **you** pick |
| No edge-flow intuition | Lumpy, faceted, pinched meshes | Primitives + modifiers only (subdivision, lattice, simple deform, skin, curve bevel, voxel remesh); no hand-placed vertices, no booleans |
| Shading artifacts | Dark seams, faceting, inside-out patches | Shade smooth + Smooth by Angle, recalculated normals, applied scale; validator counts flipped faces |
| Clipping and z-fighting | Flicker and colour bleed where parts overlap | Fixed embed depths per part, tiny outward offsets, overlap check |
| Blender API drift | Scripts fail or silently misbehave on 4.x | Pin one Blender version (5.1+) in the rules; prefer `bpy.data` and `bmesh` over `bpy.ops`; note renamed material inputs and removed auto-smooth |
| Memory loss and token burn | Quality degrades; sessions run out | Script lives in git; every run rebuilds from empty; one part family per conversation |
| Overconfident self-review | "Looks great!" when it isn't | Validator must pass; Claude lists three concrete flaws before asking for approval |
| Single-angle screenshots | Fine from the front, broken from the side | A fixed four-view contact sheet in the toon preview look |
| Poly bloat | Heavy meshes; 150 fish slow Unity | Triangle budgets enforced by the validator |
| Export surprises | 100× scale, rotated 90°, wrong pivots in Unity | One tested export function; origin at each part's socket; test import early |

### One-time setup

1. **Blender** 5.1 or newer (required by the official Blender Lab MCP add-on), pinned to one exact version so scripts and API names stay stable.
2. A script-built `kit_template.blend`: toon preview material (Shader to RGB into a constant colour ramp), ink outline via Solidify with flipped normals, one fixed light rig, and four orthographic review cameras (front, side, top, three-quarter).
3. Folder layout in your repo: `tools/blender/style.py` (all constants), `parts/` (one file per part family), `validate.py`, `contact_sheet.py`, `export.py`, `build_all.py`.
4. A `refs/` folder with 3 to 6 reference images you choose: chibi follower proportions in the Cult of the Lamb style, cute fish art, raincoat silhouettes. Claude can read images, and references beat words for proportion.
5. Claude Code edits the files; the MCP is used only to run `build_all.py`, render the contact sheet, and return `report.json`.

### The workflow

1. **Proportion sheet first.** Claude writes `style.py` from the references: head-to-body ratio, eye diameter as a fraction of body height, eye height, fin size, outline thickness.
2. **Variant grids instead of artistry.** Build 9 hero fish from 9 parameter sets and render them as one 3×3 image. You pick the best ("4, but with 7's eyes"). Two or three rounds, then lock the constants. Choosing needs no drawing skill.
3. **One part family at a time:** bodies, eyes, fins and tails, specials, hood and raincoat, hooks and bait, set dressing. Each goes round the loop above until approved, then gets a git commit.
4. **Combination test.** Assemble 16 random fish from the kit into a 4×4 render. This catches clipping that only appears in certain part combinations.
5. **Unity check.** Import into a toon test scene. Fix only scale or rotation in Unity; anything else goes back to the script.

### How to get clean organic shapes from code

| Part | Construction |
| --- | --- |
| Bodies | Sphere scaled to proportion + Subdivision level 2 + Lattice or Simple Deform for squash and taper |
| Eyes | Sphere eyeball embedded 30% into the body, separate pupil sphere (child), small emissive highlight |
| Fins and tails | Curve with bevel and taper, or a flattened subdivided plane with Solidify, attached at a root socket |
| Tentacles | Curve with tapered bevel, or the Skin modifier on a vertex chain |
| Needle teeth | An array of tiny cones along the mouth curve |
| Hood and raincoat | Cone + subdivision + Simple Deform taper; hood from half a sphere; inner faces get a pure black material for the void; no cloth simulation |
| Hooks | Thin bevelled Bézier curve with a cone point |
| Rocks, kelp, skeletons | Icosphere + noise displace + decimate; tapered curves; arrays of thin cylinders along a curve |
| Fusing pieces into one shell | Voxel Remesh + Smooth, never booleans |

Every mesh ends with: scale applied, shade smooth with the Smooth by Angle modifier, normals recalculated outward, origin at its socket, and only two material slots (`Fill`, `Detail`).

### The validator

`validate.py` runs after every build and writes `report.json`. Anything failing blocks the review step.

- Triangle count against the part's budget.
- Non-manifold edges and flipped normals.
- Scale applied (1, 1, 1) and origin at the socket.
- Facing: the eye centres sit in front of the body centre along the forward axis.
- Attachment: each part's root lies within a small tolerance of the body surface, embedded within its allowed depth.
- Symmetry: paired parts mirror within tolerance.
- Overall bounding box within the size spec.

### Rules file for Claude Code

Save this as `tools/blender/CLAUDE.md` so every session starts from the same rules:

```
# Blender asset rules (BAIT & SWITCH)
- Blender 5.1+ only (official Blender Lab MCP). Use bpy + bmesh; prefer bpy.data/bmesh over bpy.ops.
- Never model interactively. All geometry comes from scripts in tools/blender;
  every run clears the scene and rebuilds from nothing.
- Axes: fish face +X, up is +Z, the side camera looks along +Y. A fish body is 0.9 m long.
- Every dimension is a named constant in style.py. No magic numbers in part code.
- Primitives + modifiers only: subdivision, lattice, simple deform, skin, curve bevel,
  solidify, voxel remesh. No booleans. No hand-placed vertices.
- Attach parts at sockets computed from the body surface (closest_point_on_mesh),
  never hard-coded coordinates.
- Every mesh: apply scale, shade smooth + Smooth by Angle modifier, recalculate normals,
  origin at its socket, material slots Fill and Detail only.
- Budgets: whole fish <= 2500 tris, hook <= 300, set-dressing prop <= 800.
- Before coding a part: list its pieces and where each sits relative to its socket.
- After every build: run validate.py and contact_sheet.py. Never claim success
  unless validation passes; then name the 3 most visible flaws before asking for approval.
- Blender 4.x API: Principled BSDF uses "Emission Color" and "Specular IOR Level";
  mesh auto smooth was removed in 4.1 (use the Smooth by Angle modifier).
- One part family per conversation. Commit after each approved family.
- Do not use Hyper3D, Sketchfab or Poly Haven model tools.
```

### Budget tips

- Renders are the expensive part: one small contact sheet per iteration, not a stream of screenshots.
- Judge solid-shaded shape first; colour and outline come after the shape is approved.
- If a part fails the loop three times, simplify its construction rather than adding more tweaks.

Sources: [MindStudio: Blender MCP donut test](https://www.mindstudio.ai/blog/claude-blender-mcp-60-percent-tokens-donut-test-results), [MindStudio: what Blender MCP can and can't do](https://www.mindstudio.ai/blog/claude-blender-mcp-real-world-performance), [Claude drives Blender through MCP (Medium)](https://medium.com/@creativeaininja/claude-drives-blender-through-mcp-now-heres-what-actually-changes-684f28f78c7b), [3D-PreMise (arXiv)](https://arxiv.org/pdf/2401.06437), [CodeGen-3D benchmark](https://scholarworks.sjsu.edu/faculty_rsca/6835), [Snap spatial benchmark](https://eng.snap.com/spatial_intelligence), [The observability gap (arXiv)](https://arxiv.org/pdf/2603.26942), [Blender SuperSkill](https://skillsmp.com/creators/powerhouse90/blender-superskill/skills-blender-superskill).
