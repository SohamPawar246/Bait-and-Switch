# BAIT & SWITCH — Game Design Document

Oct 10, 2026 · @Soham Pawar

## Overview

**BAIT & SWITCH** is an online hide-and-hook party game. Cute alien fish hide among a school of 150 lookalikes and sneak bait off hooks; the Hooded Anglers, a tiny fishing cult in yellow raincoats, try to spot the real players and reel them in. **Every fish that gets caught pulls on a raincoat and becomes an Angler.** Last fish swimming wins. Eat enough bait and you grow into a Leviathan that can drag a boat under, turning that Angler back into a fish.

Tagline: *"Hide in the school. Steal the bait. Don't get switched."*

### Design pillars

1. **Hide in plain sight.** Player fish are indistinguishable from the school. Acting natural is the core skill; breaking cover is always a choice.
2. **Greed versus safety.** Bait is how you grow and score, and it is exactly where the hook is.
3. **Two humans, one line.** The tug-of-war is the multiplayer moment: a real person on each end.
4. **Cute shapes, grown-up dread.** Chibi critters and a fishing cult; the sea darkens and gets stranger as the round goes on.

### Key facts

|  |  |
| --- | --- |
| Platform | Windows x64 client; Linux dedicated server on Edgegap |
| Engine | Unity (URP), Mirror netcode, KCP over UDP |
| Players | 1 to 6 humans; bots fill to 6 seats (1 Angler + 5 fish at round start) |
| Session | Match = 3 rounds of 120 s, about 7 minutes with results |
| Camera | Fixed side-on view of the whole sea (2.5D), same for everyone |
| Art | Toy-like 3D chibi, cel shading, ink outlines, generated in Blender via script |
| Theme line | "No limits": a fish in this sea has no size limit |

## Art direction and tone

Inspired by Cult of the Lamb: adorable shapes, a grim world, everything played completely straight. **The cuteness is the bait.** The audience is teens and adults; the game should feel unsettling and darkly funny, not childish. Maturity comes from **feeling**, not from gore: menace is implied, never shown.

### What "mature" means here

- **The themes are dark.** Predation, devotion, conversion. Getting caught isn't death: you are "saved" and join the cult that hunts your friends. The game never says this is bad, and that's what makes it creepy.
- **Implied, never shown.** A landed school fish goes into a crate stencilled OFFERINGS; the lid shuts; a muffled thud. When a Leviathan takes a boat, it happens under the waterline: bubbles, silence, then the Angler's hat floats up.
- **Restraint and silence.** Night is quiet. The music drops out a beat before a Leviathan rises. Tension comes from absence.
- **Nobody winks at the camera.** The cult is earnest, the fish are terrified, and the humour comes from both being played dead straight.

### The humour: deadpan and dark

- **A bureaucratic cult.** Catches are "tithes", the ledger is a Book of Tithes, souls are counted in tally marks, there is a quota.
- **Absurd contrast.** A fish in an oversized raincoat solemnly fishing for its own friends. Anglers bowing to the Leviathan that is about to eat them.
- **Physical comedy in the animation.** The desperate flop on deck, the raincoat that's three sizes too big, the hat bobbing up after a boat goes under.

### Copy: every line in this voice

| Moment | Text |
| --- | --- |
| Round intro, fish | "You are a fish. Do not be chosen." |
| Round intro, Angler | "You are an Angler. Bring them into the light." |
| Caught and converted | "YOU HAVE BEEN SAVED." |
| Line snaps (Angler / fish) | "The line has failed you." / "Not today." |
| Night falls | "The light leaves. Something else arrives." |
| Leviathan rises | "IT HUNGERS." |
| Boat dragged under | "The Deep has accepted your offering." |
| Fish win the round | "The sea keeps its secrets." |
| Anglers win the round | "All are saved. Praise the Deep." |
| Ledger header | "Book of Tithes, Round 2" |
| Loading tips | "Bait is a test of faith. Most fail." · "The school does not dash. Neither should you." · "Every Angler was a fish once. Every single one." · "Do not look the Leviathan in any of its eyes." |

Bot Anglers get cult titles instead of jokey names: Brother Haddock, Sister Brine, Deacon Krill, Mother Marrow.

### Visual choices that carry the feeling

- **Shapes stay chibi:** big heads, stubby fins, round bodies. That is the Cult of the Lamb lens; don't make the shapes "edgy".
- **The eyes do the unsettling work.** Slightly uneven sizes, wet highlights, pupils that dilate when a hook is near. Anglers have **no faces**: just a black void inside the hood with two glowing pupils.
- **Tiny needle teeth** show when a fish nibbles. Eating is not cute.
- **Seabed set dressing:** fish skeletons, rusted old hooks, sunken shrine stones, candles that impossibly burn underwater, ritual circles in the sand, a huge eye-shaped sigil.
- **The far background:** enormous silhouettes drifting very slowly. Never explained.
- **One night-time creep beat:** once per night, every school fish stops and turns to face the camera for half a second, then carries on. Cheap to build, impossible to forget.
- **The Leviathan:** the same chibi fish, just enormous; extra eyes open one at a time, and the mouth opens too wide.
- **Grade it like a grown-up game:** the same 12-colour palette but darker, with desaturated midtones and deep shadows; red only for hooks and danger, violet only for the Deep; film grain and a heavy vignette at night.
- **Low-key lighting:** warm lanterns against a cold sea, strong rim light on characters.

### Sound of the feeling

Day: a slightly detuned music box over low strings. Dusk: the same tune, slower, a semitone flat. Night: near silence, water pressure, a distant choir. Conversion: a muffled choral "aah" and a raincoat rustle. Never use a jaunty "cartoon" sound for anything that matters.

### Additions to the Blender kit

- An eye-asymmetry parameter, a needle-teeth mesh, and the Angler hood with a black void interior and emissive pupils.
- A set-dressing kit: fish skeleton, candle, shrine stone, ritual-circle and sigil decals, the OFFERINGS crate.
- In Unity: film grain, vignette, a colour-grading curve per phase, chromatic aberration on Leviathan moments.

### Boundaries

No blood, gore or on-screen killing; invented cult symbols only, never real-world religious ones. Mature in mood, not edgy, and comfortable for a jam audience.

## Art production with Claude + Blender MCP

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

## Reference study: Scale the Depths

**What it is.** A single-player fishing game by Glass Gecko Games: a tiny robot on a boat at Loch Ness catches fish, scales them on a cutting board, serves them to non-human customers (otters, herons, Nessie, a Kelpie), and spends the money on a better rod, hook, knife and bucket to fish deeper. It began as an entry to the GMTK Game Jam 2024 (theme "Built to Scale") and grew into a full Steam release in 2026.

**Why it matters to us.** It proves a jam game built around **one tactile fishing verb**, executed precisely and with charm, can win people over. It is also the closest existing game to our Angler controls.

### How its fishing works

- The line descends on its own; you steer the hook left and right (mouse-follow while holding the button, or A/D).
- For a few seconds underwater you thread the hook through a maze of rocks and plants and ram fish to damage them until they're caught.
- Unwanted catches clutter a limited bucket, so you aim, not spray.
- The music grows more layered the longer the hook stays underwater.

### What reviewers loved, and what they didn't

| Loved | Criticised |
| --- | --- |
| Satisfying, tactile fishing and processing | Hook controls feel fiddly (precise mouse mode turns slowly) |
| Charming, well-designed creatures and customers | The loop gets repetitive after a few hours |
| Music that builds while you fish | Parasite removal felt finicky and frustrating |
| Hidden secrets and lore, more than expected | Upgrades reset when moving to new areas, undermining progress |
| A tight loop where each part feeds the next | Thin tutorial early on |

### What we adopt

| Their idea | Our version | Where it goes |
| --- | --- | --- |
| Steering a hook through rocks and plants | **Terrain with cover:** rock arches block hooks; kelp forests slow hooks by half and partly hide fish inside them. Fish get places to hide; Anglers get a skill route to thread | School and hiding, balance sheet |
| Music that builds while the hook is down | **Tension layer:** on each Angler's client, a percussion layer builds while their hook is in the water and resets when they reel up; on top of the day-to-night score | Screens, UI and audio |
| Customers with hidden favourite fish | **The Deep's Appetite:** each round the Deep names a craving from the parts kit, e.g. "Tonight the Deep craves: tentacles". Anglers +5 for landing a player fish with that part; fish with that part +5 if they survive the round | Round start, scoring |
| A tactile processing minigame | **The Robing (stretch):** when a player fish is landed, the Angler drags the raincoat down over the flopping fish (about 2 s) while the fish wriggles; finishing fast gives +1. Pure flavour, never blocks play | Getting caught |
| Lore and secrets beyond the fishing | **Scraps of cult lore** in loading tips, ledger footers and Vestry props ("Rule 7: the Deep is never full") | Art direction copy |
| One tactile verb done well | Our verbs are the hook flick and the tug; they get the most tuning time | Build plan (prototype tonight) |

### What we avoid

- **Fiddly hook controls:** mouse-follow by default with a tunable spring, tested in tonight's prototype before anything else is built.
- **Finicky minigames:** the tug shows both meters clearly and resolves within about 10 seconds.
- **Repetition:** short rounds, role swaps, the Deep's Appetite and customization keep every round different.
- **A thin tutorial:** round intro cards, the blend meter, and loading tips teach as you play.
- **Long upgrade grinds:** none. Every judge gets the full game in their first match.

Style note: Scale the Depths is cozy pixel art; we borrow its mechanics and feel, not its look or tone.

Sources: [Steam store text (via Gamesplanet)](https://us.gamesplanet.com/lenovo/legion/game/scale-the-depths-steam-key--8431-1), [jam version on itch.io](https://serpexnessie.itch.io/scale-the-depths), [KeenGamer preview](https://www.keengamer.com/articles/previews/scale-the-depths-preview-catch-of-the-day/), [Bonus Action review](https://bonus-action.com/review/scale-the-depths-review-the-most-dangerous-loop-right-now/), [Tech-Gaming review](https://www.tech-gaming.com/scale-the-depths/), [Thumb Culture review](https://www.thumbculture.co.uk/scale-the-depths-pc-review/amp), [Vaporlens review summary](https://vaporlens.app/app/3198890/scale_the_depths).

## Match and round flow

&#91;embedded content: Match flow · 3 rounds, 3 light phases each\]

A match is three rounds. Each round starts with one Angler and five fish (humans first, bots fill the rest) and ends when the last fish is caught or the clock hits 120 seconds.

### Round start

- **Seats:** 6 per round. Humans take seats first; bots fill the rest.
- **Who starts as Angler:** a random human who hasn't started as Angler yet this match; if all humans have, a bot. With one human, the human is a fish in round 1, the Angler in round 2, a fish in round 3.
- **Roles are private.** Each client learns only its own role and its own fish's ID. Anglers never learn which fish are players.
- **Spawn:** fish spawn inside the school at random midwater points; the Angler's dinghy spawns at the surface centre.

**The Deep's Appetite.** During the round intro, the Deep names one part from the kit ("Tonight the Deep craves: tentacles"). Anglers earn +5 for landing a player fish with that part; fish wearing it earn +5 if they survive. It gives Anglers a lead to hunt and makes customization choices matter every round.

### Light phases

| Phase | Time | Light | What changes |
| --- | --- | --- | --- |
| Day | 0 to 50 s | Bright, caustics near the surface | Hooks clearly visible; bait worth 1 growth |
| Dusk | 50 to 80 s | Orange sun, longer shadows | Hooks dimmer; bait worth 1.5 growth; school gets restless |
| Night | 80 to 120 s | Dark except lanterns, glow bait, angler lamps, bioluminescent fins | Hooks only visible near light (or with extra eyes); bait worth 2 growth |

The escalation is deliberate: early rounds teach, the dark end of every round creates panic and comebacks.

### Round end

- **Anglers win the round** if every fish is converted before 120 s.
- **Surviving fish win the round** if any fish is still swimming at 120 s.
- Individual scores carry across the round regardless (see Scoring). Converted fish keep everything they earned as fish.

### Joining and leaving

- **Join mid-round:** you take over a bot fish's body on the spot, invisibly. Perfect for hiding, zero waiting for judges.
- **Leave mid-round:** a bot takes over your body (fish or Angler) so the round stays balanced.
- **Server rotation:** if the Edgegap deployment nears its 60-minute free-tier cap, the server finishes the current match, shows "Moving to a fresh server", and clients reconnect through the broker automatically.

## Playing as a fish

Your job: stay a fish. Blend into the school, steal bait when it's worth the risk, and outlast the Anglers.

### Controls

| Input | Action |
| --- | --- |
| WASD (or mouse-follow, toggle in settings) | Swim. Momentum-based: you accelerate and glide, like the school does |
| Space | **Dash**: a sharp burst with a bubble trail. Escapes hooks, but no school fish ever dashes, so it gives you away |
| Hold left mouse near bait | **Nibble**: fill a ring over 0.6 s to eat the bait |
| E | **Ink cloud**: a dark puff that hides you for 3 s. School fish scatter from it, so it also says "someone is here" |
| Hold left mouse near a boat (Leviathan only) | **Bite**: drag the boat under |

All numbers live in the balance sheet near the end of this document.

### Nibbling: the core risk

1. Bait hangs on each hook. Swim within nibble range and hold the button; a ring fills over 0.6 s.
2. Every nibble makes the Angler's line **twitch**. School fish nibble too, at random, so a twitch might be a 1-point school fish or a 10-point player.
3. If the ring completes, the bait is gone, you grow, and the Angler has an empty hook until they reel up to rebait.
4. If the Angler **strikes** while you're within the hook's catch radius, you're hooked and the tug-of-war starts.

The skill is timing and acting: approach like a school fish, nibble when the Angler looks elsewhere or just struck at someone else, and abort the moment the line tenses.

### Other ways to get hooked

- **Snag:** a spiky hook moving fast catches any fish it touches.
- **Strike while passing:** you don't need to be nibbling; anything inside the catch radius when the Angler strikes is fair game.

### Growth and the Leviathan ("no limits")

- Each bait eaten adds growth (1 by day, 1.5 at dusk, 2 at night). Every growth step makes you about 15% bigger and 3% slower. Bigger fish are easier to spot in the school, so growing is itself a risk.
- At 6 growth you **become a Leviathan**: about 2.5× your starting size, a violet glow, a roar, and a screen-wide "A LEVIATHAN RISES" alert with a choir sting.
- A Leviathan can't hide or nibble. **One hook can't hold it**: a single hook snaps after 1 s. Anglers must get **two hooks on it at once** and win a combined tug to land it.
- The Leviathan's goal: swim up under a boat and **Bite** for 2 s. The boat is dragged under, **that Angler turns back into a fish** (tiny, growth 0), and the Leviathan shrinks to growth 3, back into a normal fish that can hide again.
- This is the comeback engine: one greedy fish can undo a conversion.

### Getting caught

Landed fish flop desperately on the boat deck for 1.5 s, then the conversion ritual: a raincoat is pulled over them, the eyes glow, a muffled choir, and "YOU HAVE BEEN SAVED." They appear in their own dinghy as an Angler about 3 seconds after landing. Their custom body stays visible under the raincoat.

## Playing as an Angler

Your job: find the real fish among 150 fakes and reel them in. Every catch adds a teammate.

### Controls

| Input | Action |
| --- | --- |
| A / D | Row your dinghy along the surface |
| Mouse | The hook is pulled toward the cursor on a weighted line; flick to swing it |
| Left click | **Strike**: a sharp upward jerk; any fish inside the catch radius is hooked |
| Hold left mouse (while a fish is hooked) | **Reel** during the tug-of-war |
| Hold right mouse | **Reel up and rebait** (needed after bait is eaten or a school fish is landed) |
| Q | **Sonar ping**: a ring from your boat briefly outlines every player-seat fish (humans and bot seats) within range; everyone hears the ping |
| Middle click | **Mark** a suspicious fish for your fellow Anglers (a small eye icon for 3 s) |

### The hook: a skill toy on a rope

The hook is a physics body hanging from your rod. A spring pulls it toward the cursor, water drag slows it, and gravity sinks it, so it swings and lags behind the mouse instead of teleporting. A good Angler flicks it in arcs, parks it gently in the school, and keeps it still while fish nibble. Hook type and line type (see Customization) change its weight, sink speed and catch radius.

The line is drawn as a sagging rope (a short verlet chain rendered with a line renderer) that **twitches** visibly, with a tick sound, every time something nibbles your bait.

### Striking

- Strike when the line twitches. Inside the catch radius: hooked. A school fish is landed automatically in 1 s for 1 point (your bait is used up). A player-seat fish starts the tug-of-war.
- A **missed strike** jerks the hook through the water, scatters the nearby school for 2 s and puts strike on a 1 s cooldown. Spamming strikes empties your area of fish.
- Bait is gone after any catch or a completed nibble; hold right mouse to reel up and rebait. Reeling takes longer the deeper the hook is, so deep fishing is a commitment.

### Reading the school

The Angler's real skill is spotting the fish that moves wrong:

- Sudden **dashes** (school fish never dash).
- A fish that **lingers** by the bait longer than school fish do.
- **Ink clouds** and the school scattering around them.
- A fish that's **bigger** than it should be (growth).
- Sonar for confirmation, and marks to share suspicions with other Anglers.

### Several Anglers

Every converted fish gets its own dinghy, so later in the round the surface fills with little boats. Boats bump softly; lines don't tangle. Anglers win together, but scores are individual, so there's a friendly race for the big catches. The **Leviathan** is the one thing that forces real teamwork: two hooks on it at once, then reel together.

## The tug-of-war

The heart of the multiplayer: a few seconds where one human pulls against another. It runs entirely on the server; clients send inputs and draw the result.

### State

- **Distance D:** how far the fish is from the boat. Starts at the hook's distance.
- **Tension T:** 0 to 100 (or the snap threshold set by gear).
- **Stamina S:** the fish's, 0 to 100, starts full.
- **Pull P:** how hard the fish swims away from the boat, 0 to 1 (the dot product of the fish's input direction and the away-from-boat direction, clamped). Forced to 0 while exhausted.
- **Reel R:** 1 while the Angler holds the reel button, else 0.

### Rules per second (server tick)

| Situation | Tension | Distance | Stamina |
| --- | --- | --- | --- |
| Angler reels, fish pulls hard | rises fast (+35 + 45 × P) | reels in slowly (3.5 × (1 − 0.6 × P)) | drains (−25 × P) |
| Angler reels, fish rests | rises slowly (+35 − 20) | reels in at full speed (−3.5) | recovers (+15) |
| Angler eases off, fish pulls | falls a little (45 × P − 50) | line pays out (+1.5 × P) | drains |
| Neither | falls fast (−70) | holds | recovers |

Plus three events:

- **Fish dash during a tug:** +25 tension instantly (normal dash cooldown). The fish's big gamble.
- **Exhausted:** when stamina hits 0, the fish can't pull for 1.5 s (dizzy swirl over its head). The Angler's window.
- **Line fatigue:** after 6 s the snap threshold drops by 8 per second, and at 12 s the hook tears loose. Every tug resolves within about 10 seconds.

### Outcomes

| Result | Trigger | Effect |
| --- | --- | --- |
| **Landed** | D reaches 0.5 | Fish flops on deck and converts to an Angler (see Getting caught) |
| **Snap** | T reaches the snap threshold | Fish escapes (+3 points); the Angler's line is tangled for 3 s and must rebait |
| **Torn loose** | 12 s elapsed | Fish escapes, no escape points; the Angler rebaits |

### How it should feel

The Angler reels while the fish rests and eases off when it pulls, watching the tension; the fish alternates pulling and resting, trying to bait a greedy reel and then dash for the snap. Both players see both meters, so every second is a readable bluff.

### Feedback

- The line itself is the main UI: white → yellow → red with tension, vibrating harder as it climbs.
- A tension bar near the Angler's boat; stamina bubbles above the fish.
- Audio: a rising whine pitched to tension, reel clicks, little fish grunts on each pull, a big *twang* and camera kick on a snap, a splash and deck thud on landing.
- Participants only: a 0.08 s visual freeze at the moment of hooking and a slight camera push toward the fight (local effects, so other players' view stays fixed).

### Gear modifiers

- Barbed hook: snap threshold 120. Spiky hook: snap threshold 80.
- Steel line: never snaps (fatigue still ends it at 12 s), reel speed × 0.7.
- Spines (fish part): the fish's pull adds 15% more tension.
- Puffer body: 25% more stamina.

### Leviathan team catch

A Leviathan with two hooks in it enters a team tug: both Anglers' reel rates add up, each line tracks its own tension, and the Leviathan has 300 stamina. If either line snaps, the remaining single hook breaks after 1 s and the Leviathan is free.

## The school and hiding

The school is what makes hiding possible, so it must be impossible to tell a player from a school fish by looks alone, and only possible by **behaviour**.

### Rules that keep players indistinguishable

1. **Same parts.** School fish are assembled from exactly the same parts kit and palette as players, with every part in use somewhere in the school.
2. **Same pipeline.** Every fish, school or player, is drawn from one server snapshot stream with the same interpolation. Only your own fish is predicted locally on your screen.
3. **Same speed range.** School fish cruise at about 2.5 units/s; players can swim faster (about 4). Swimming at school pace is the skill; going faster is visible.
4. **Hidden IDs.** Fish IDs are shuffled each round; no client is told which IDs belong to players except its own.

### School behaviour (server boids)

- **Flocking:** separation, alignment and cohesion with nearby fish, a cruise speed and a turn-rate limit so motion looks smooth and fishy.
- **Depth preference:** mostly midwater; the school drifts a little shallower at night.
- **Avoid moving hooks:** steer away from a hook moving faster than 1 unit/s within about 1.2 units. Still hooks are ignored.
- **Curiosity:** now and then a school fish near a still, baited hook swims over and nibbles for 0.3 to 0.55 s, then leaves. About 10% of the time it finishes the nibble and eats the bait. This creates the decoy twitches that make Anglers doubt.
- **Scatter:** a strike within 3 units, an ink cloud, a dashing fish nearby or a Leviathan within 4 units makes nearby school fish burst away for 2 s.
- **Population:** 150, constant. A landed school fish respawns from the side of the screen after 5 s.
- Use a simple spatial grid for neighbour lookups; 150 fish at 20 Hz is light work for one server core.

### Terrain: cover for fish, routes for hooks

- **Rock arches and outcrops** block hooks completely; the line bends around them. Fish can slip through gaps a hook can't follow.
- **Kelp forests** slow any hook inside them to half speed and partly hide the fish swimming in them (drawn behind the front fronds).
- Keep it to 3 or 4 features in the one arena, so the whole sea stays readable on the fixed camera.

### The tells (what good Anglers learn)

| Tell | Why it gives a player away |
| --- | --- |
| A nibble longer than 0.55 s | School fish almost never nibble that long |
| Dashing | School fish never dash |
| Swimming against the school | Players chasing bait break formation |
| Size | Growth makes a fish bigger than any school fish |
| Ink clouds and scatter | Only players use ink |

### Teaching aid: the blend meter

A small eye icon near the fish player's HUD shows how natural they look right now: open and red when they're faster than the school, dashing or isolated; half-closed and green when they match their neighbours' speed and heading. It teaches the hiding skill in the first round without a tutorial, and only the fish player sees it.

## Customization

Every player builds **both** a fish and an Angler rig, since everyone plays both sides. Each gameplay part is a trade-off; colours and cosmetics are free choices. Choices persist between sessions (PlayerPrefs), and bots pick randomly.

### The workbench screen

Shown while the Edgegap server boots, so the wait becomes the most personal part of the game. The goal: **every click gets a visible, audible reaction from your creature**, and each faction's screen feels like a small ritual in its own place.

&#91;embedded content: Customization layout · shared by the Hatchery and the Vestry\]

### One scene, two places: the waterline between them

The workbench is one 3D diorama split by the waterline. **The Hatchery** (fish) sits below it: a dim, bioluminescent tank where your fish grows inside a translucent egg sac. **The Vestry** (Angler) sits above it: a candle-lit boathouse shrine with hooks on a pegboard, bait jars and spools on a shelf. Switching tabs swings the camera up or down **through the waterline** with a splash and a muffled-to-clear audio sweep.

Because every Angler is a fish in a raincoat, the Vestry shows **your own custom fish** standing in the robe. The two screens are the same creature, which makes both feel owned.

### Satisfaction rules (both factions)

1. **Instant, physical feedback.** A chosen part pops onto the creature with a squash-and-overshoot (about 0.15 s), a small particle burst (bubbles below, wax dust above) and a sound unique to that slot. Pitch varies a few percent so repeats never feel robotic.
2. **The creature tries out every part.** New fins: it swims a quick figure-of-eight. New eyes: they blink one by one and look at you. Tentacles: they curl and uncurl. Teeth: a chomp. New hook: the Angler lifts it to the candle and it glints, then a test cast whips across the shrine. New bait: it's dropped into a jar and wriggles. New line: the spool spins.
3. **Hover before you commit.** Hovering a part shows it ghosted on the creature, and the stat bars show green and red ticks for what would change. No surprises, no menus of numbers.
4. **It's alive the whole time.** Idle breathing, blinking, and its eyes follow your cursor. Drag to rotate, scroll to zoom.
5. **Colour feels like dye.** Picking a colour washes it over the body from head to tail in about 0.3 s, with a soft liquid sound.
6. **Randomize is a moment.** "Let the Deep decide" spins every slot like a slot machine, ticking, and lands with a choir sting. Hold it to keep rolling.
7. **Names with the game's voice.** A name field with a dice button that suggests deadpan names: "Doomed Gerald", "Kevin (Brine)", "Brother Unworthy".
8. **Nothing is lost.** Undo (Ctrl+Z), reset per slot, and choices saved between sessions.

### The stat card

No raw numbers: four bars on a parchment card that tick as they change.

| Faction | Bars |
| --- | --- |
| Fish ("Specimen card") | Speed, Stealth, Stamina, Bite |
| Angler ("Vows card") | Reach, Grip, Line strength, Lure |

### The confirm ritual

Pressing **Dive in** stamps a wax seal on the card with a heavy thud. Fish: the egg sac splits and your fish swims out of frame toward the sea. Angler: the candles flare and two eyes ignite inside the hood. Then the game loads. Two seconds, every time, and it never gets old.

### Waiting for the server, without feeling like waiting

- Server progress is **the descending fishing line (see "Boot loading: the line goes down" in Screens)**, with the server city printed when it's found.
- As other players connect, their creatures appear on a **congregation shelf** (jars below, pews above), so you can see who you'll play with. Social anticipation instead of a loading bar.
- When the server is ready, **Dive in** pulses gently. Nothing interrupts you; you go when you're done.

### Sound palette

| Faction | Part pop | Ambience |
| --- | --- | --- |
| Fish | Wet squelch, bubble pop, eye-blink "plip" | Low tank hum, distant whale-like groans |
| Angler | Wooden clack, metal hook clink, wax and fabric rustle | Rain on the roof, candle crackle, a faint choir |

### Build notes and scope

- A separate `Workbench` scene with both dioramas stacked vertically and a Cinemachine camera for each; switching blends between them.
- Reactions reuse `FishAnimator` with a "test" mode per part type; Angler reactions are short procedural motions (lift, glint, cast). DOTween (free) handles the pops and bars.
- Diorama props reuse the set-dressing kit: shrine stones, candles, the boat, plus a pegboard, jars and an egg sac.
- **Must have:** live preview, pops with sounds, hover previews, stat card, randomize, Dive in.
- **Nice to have:** per-part reactions, the waterline camera swing, the wax-seal ritual.
- **Stretch:** the congregation shelf and drag-to-dye.
- Budget about 3 hours inside Sunday's "Workbench and day-night" block; the must-haves fit in 90 minutes.

### Balance rules for every add-on

- Each gameplay option has **one clear advantage and one clear disadvantage**, on a different stat, so no option is strictly better.
- The first option in every slot is the **baseline**: no advantage, no disadvantage. Picking it is never a mistake.
- Caps stop stacking from breaking the game: total speed change is capped at −25% / +20%, total nibble range bonus at +0.6 units.
- The school and the bots pick every fish option in equal numbers, so no part is rare enough to give a player away.
- In playtests, track pick rates; anything chosen by more than 40% of players gets nerfed. Every number lives in its `PartDef`.

### Fish add-ons

Six gameplay slots plus cosmetics. Stat-card bars: **Speed** (top speed, acceleration, turning), **Stealth** (hitbox, tells, sonar), **Stamina** (tug endurance), **Bite** (nibble speed and range, Leviathan bite).

| Slot | Option | Advantage | Disadvantage |
| --- | --- | --- | --- |
| Body | Round | Baseline | — |
|  | Eel | +12% top speed | −20% stamina in tugs |
|  | Puffer | +25% stamina; puffs up when hooked | −10% top speed |
|  | Flatfish | Lying still on the seabed hides you from sonar | −15% acceleration; only works at the bottom |
|  | Plated | The first hook that catches you each round bounces off | −15% acceleration; sinks slowly when idle |
| Eyes | Two | Baseline (see hooks in the dark within 2 units) | — |
|  | Three | See hooks in the dark within 4 units | 5% bigger hitbox |
|  | Seven | See hooks in the dark within 6 units | 10% bigger hitbox |
|  | Cyclops | See every hook's catch radius as a faint ring | Night vision drops to 1 unit |
| Fins | Stubby | Baseline | — |
|  | Big fins | Dash goes 30% further | Dash cooldown +0.6 s |
|  | Wing fins | Turn 25% faster | −10% acceleration |
| Tail | Fan | Baseline | — |
|  | Fork | +12% acceleration | −15% stamina regeneration |
|  | Curl | +20% stamina regeneration | −10% acceleration |
|  | Thrasher | A dash during a tug adds +35 tension instead of +25 | Dash cooldown +0.4 s |
| Mouth | Small | Baseline | — |
|  | Needle teeth | Nibble 20% faster | Teeth flash while you nibble (a visible tell) |
|  | Big jaw | Nibble in 0.4 s instead of 0.6 s | Every nibble twitches the line twice as hard |
|  | Sucker | Nibble from 0.3 units further away | Nibble 15% slower |
| Special | None | Travel light: +5% top speed | — |
|  | Hammerhead | Leviathan bite takes 1.4 s; a dash into a hook knocks it 2 units away | Turn 15% slower |
|  | Tentacles | Nibble from 0.4 units further away | −8% top speed |
|  | Spines | +15% tension on every pull in a tug | Sonar outlines you twice as long |
|  | Angler lamp | Lights your surroundings at night (see hooks within 5 units) | You glow at night (so do lamp-wearing school fish) |
|  | Mimic skin | Every 10 s your colour shifts to match the nearest school fish | −15% stamina |
|  | Ink gland | Ink cooldown 8 s and a 30% bigger cloud | −10% stamina |
|  | Electric organ | Once per tug, dash to shock: the Angler can't reel for 1 s | −10% top speed; the sparks give you away |

**Fish cosmetics (free):** skin colour (4 skins plus tints), pattern (plain, stripes, spots, scars, freckles), night glow colour, eye colour.

### Angler add-ons

Five gameplay slots plus cosmetics. Stat-card bars: **Reach** (line length, sink speed, aim), **Grip** (catch radius, holding a fish), **Line strength** (snap threshold, reel speed), **Lure** (how strongly bait attracts).

| Slot | Option | Advantage | Disadvantage |
| --- | --- | --- | --- |
| Hook | J hook | Baseline (catch radius 0.6, snap at 100) | — |
|  | Spiky | Snags any fish it touches while moving fast | Snap threshold 80 |
|  | Barbed | Snap threshold 120 | Catch radius 0.45 |
|  | Big | Catch radius 0.8 | Sinks 30% slower; strike cooldown 1.3 s |
|  | Circle | A fish that finishes a full nibble hooks itself, no strike needed | Strike catch radius only 0.4 |
|  | Treble | Strike cooldown 0.7 s | Snap threshold 90 |
| Bait | Worm | Baseline | — |
|  | Glow orb | Lights 3 units around the hook at night; school more curious at night | Fish see your hook from far away |
|  | Chunky lure | School twice as curious | +1 extra growth for whoever eats it |
|  | Spinner | School fish ignore it, so only players nibble (no decoy twitches) | It flashes in daylight, and eating it gives +2 growth |
|  | Stink bait | Draws curious school fish from 10 units away | Constant decoy twitches to read through |
| Line | Mono | Baseline | — |
|  | Steel | Never snaps | Reel × 0.7; heavier swing |
|  | Silk | Reel × 1.3 | Snap threshold −15 |
|  | Ghost line | Invisible underwater; fish see only the hook and bait | Snap threshold −10 |
| Rod | Short | Baseline (max line 20 units) | — |
|  | Long | Max line 26 units | Hook follows the cursor 20% slower |
|  | Whip | Strike jerk 1.6 units (a bigger, faster strike) | Reel −10% |
|  | Heavy | Reel +20% | Hook spring −25% (sluggish aim) |
| Relic | None | Dinghy +15% speed | — |
|  | Sonar bell | Sonar cooldown 10 s | Sonar radius 9 units |
|  | Vigil lantern | Marks last 6 s and show through ink | At night your lantern lights a cone underwater that fish can see coming |
|  | Chum bucket | Press E: the school gathers at a spot for 6 s (20 s cooldown) | No sonar |
|  | Offering bowl | +1 extra point per school fish landed | Missed strikes scatter fish 50% wider |
|  | Deep weight | Hook sinks twice as fast | Reeling up to rebait is 30% slower |

**Angler cosmetics (free):** raincoat colour (6), hat (sou'wester, bucket, pointed hood, none), lantern flame colour, boat paint, rod finish, colour of the eyes inside the hood.

### Counters: picks that beat picks

These matchups give the customizer real decisions and give the ledger stories to tell.

| Matchup | Who has the edge, and why |
| --- | --- |
| Flatfish vs Sonar bell | Flatfish: lying on the seabed ignores sonar completely |
| Seven eyes or Angler lamp vs Glow orb at night | Fish: they see the hook long before it arrives |
| Electric organ vs Steel line | Fish: a shock is the only way to stall a line that can't snap |
| Spines or Thrasher vs Barbed hook | Fish: extra tension can push even a barbed line past 120 |
| Needle teeth or Big jaw vs Circle hook | Angler: a fast full nibble hooks you automatically |
| Plated vs Spiky hook | Fish: the first snag bounces off |
| Hammerhead vs Big hook | Fish: a dash knocks the slow-sinking hook out of the way |
| Any greedy fish vs Spinner | Angler: school fish never touch a spinner, so any nibble is a player |
| Mimic skin vs Chum bucket | Angler: chum packs the school tight, so a fish moving against the crowd stands out whatever its colour |

### Data and networking

- Each part is a `PartDef` ScriptableObject: ID, slot, prefab, thumbnail, display text, and stat modifiers.
- A full appearance packs into 21 bytes (one per slot: 10 fish, 11 Angler). The client sends it on join; the server validates it and includes it in the round-start appearance table for all fish, shuffled together with the school.

## Scoring and winning

The match winner is the player with the most points after 3 rounds. Rounds have a side winner (fish or Anglers) for drama, but points are individual so nobody is ever "out" of the match.

| Event | Points | Who |
| --- | --- | --- |
| Eat a bait | +2 | Fish |
| Escape a tug (snap) | +3 | Fish |
| Still swimming at 120 s | +5 | Fish |
| Last fish swimming (the only survivor at 120 s) | +5 bonus | Fish |
| Drag a boat under as a Leviathan | +10 | Fish |
| Land a school fish | +1 | Angler |
| Land a player-seat fish (human or bot seat) | +10 | Angler |
| A fish you marked is landed within 10 s | +2 | Angler |
| Land a Leviathan | +15 each | Every Angler with a hook in it |
| Anglers convert everyone before 120 s | +5 | The round's starting Angler |

Plus the Deep's Appetite: +5 to an Angler for landing a player fish with the craved part, and +5 to a surviving fish that wears it.

Converted fish keep the points they earned as fish and keep scoring as Anglers, so getting caught early is a setback, never a dead end.

### The ledger

After each round, a cream "cult ledger" card lists every player's catches, escapes and points with little stamped icons. After round 3 it adds titles: **Biggest Fish** (most growth), **Hook of Destiny** (most player catches), **Slipperiest** (most escapes) and **Leviathan** (dragged a boat under), then crowns the winner.

## Bots

Bots fill every empty seat, so one judge alone gets a full match. They run on the server inside the same simulation, use the same inputs a human would, and **never cheat**: a bot Angler doesn't know which fish are players; it reads the same tells a human does.

### Fish bots

A small state machine per bot, with a random **greed** value (0.2 to 0.6) so bots differ:

| State | Behaviour | Leaves when |
| --- | --- | --- |
| Blend | Swims as a boid with the school, at school pace | Greed roll succeeds near a still, baited hook (within 6 units) |
| Forage | Approaches the bait at school pace, nibbles; aborts if the hook moves or tension appears; 30% chance to abort at 0.4 s | Bait eaten, aborted, or hooked |
| Flee | Dashes away from a strike or fast hook within 2 units; inks if a hook keeps following | 2 s pass safely |
| Tug | Pulls about 1 s, rests about 0.7 s; dashes when the Angler's tension passes 60 and dash is ready | Landed or escaped |
| Leviathan | Heads for the nearest boat and bites | Boat dragged, landed, or round ends |

### Angler bots

- **Positioning:** row toward the densest part of the school (sample a coarse grid), park the hook inside it at a random depth, keep still 2 to 4 s, then drift somewhere new.
- **Reading tells:** remember any fish that dashed, inked, or is visibly larger in the last few seconds, and prefer parking the hook near them.
- **Striking:** react to a twitch after 250 to 450 ms; strike if the nibble has lasted over 0.35 s (or a 30% random gamble); miss on purpose about 25% of the time so a skilled human fish can survive.
- **Sonar:** use it whenever it's ready and at least two fish remain.
- **Tug:** reel while tension is under 65, release above 80, reel hard while the fish is exhausted.
- **Leviathan:** drop everything to hook it, especially if another Angler's hook is already in it.
- Rebait whenever the hook is empty.

### Presentation

- Angler bots show a small "BOT" tag over their boat. Fish bots show nothing in the water (that's the point), but the ledger lists them as bots.
- Bot Anglers take cult titles (Brother Haddock, Sister Brine, Deacon Krill, Mother Marrow); bot fish appear in the ledger as numbered offerings ("Fish No. 48").
- Tune bots to be slightly worse than an average human: the goal is a lively sea, not a hard one.

## Screens, UI and audio

UI style: cards on aged parchment with dark ink outlines and wax-seal stamps, a gothic display font (Pirata One or IM Fell English SC, both free under the OFL) paired with a plain, readable body font, and part icons rendered from the Blender kit. All text follows the copy table in Art direction. Keep the in-game HUD minimal: the sea is the show.

### Screens

| Screen | Contents |
| --- | --- |
| Title | Logo over a live underwater scene with the school swimming; Play, Settings, Quit |
| Workbench (boot) | Customization (see above) + server status: "Summoning an Edgegap server near you…", then the server's city; **Dive in** when ready |
| Round intro | 5 s card: "You are a FISH. Hide. Eat. Survive." or "You are the ANGLER. Find the real fish." plus a one-line control reminder |
| Fish HUD | Dash and ink cooldown icons, growth pips (0 to 6), blend-meter eye, nibble ring at the bait, round clock with sun/moon icon, "Fish left: 4" |
| Angler HUD | Strike and sonar cooldowns, bait icon on your hook (full or empty), catches this round, round clock, fish left |
| Tug overlay | Tension on the line itself, tension bar by the boat, stamina bubbles over the fish; visible to everyone |
| Conversion | 3 s conversion ritual: the raincoat is pulled on, the eyes glow, "YOU HAVE BEEN SAVED." |
| Leviathan alert | Violet screen tint, "A LEVIATHAN RISES" for 2 s |
| Round ledger | 10 s card with each player's catches, escapes and points |
| Match results | Winner, titles, Play again |
| Server panel | Small corner tag: "Edgegap · Mumbai, India · 38 ms" (from the deployment's location variable) |
| Connection | "Reconnecting…" and "Moving to a fresh server…" with automatic retry; a clear error with a Retry button if the broker can't start a server |
| Settings | Master, music and effects volume; WASD or mouse-follow swimming; fullscreen |

### Main menu: a dithered sea

Inspired by the technique in "Sinking into dither" by ya\_demidov on Instagram: a top-down sea rendered as an ordered-dither halftone, shifting from day to night. We borrow the **technique**, not the scene. To use that video itself, you'd need the creator's permission and a credit; everything below is original and built from our own kit.

&#91;embedded content: Main menu layout · 16:9, rule of thirds\]

**The scene (original, from our lore).** Looking straight down at the water: the Angler's **empty dinghy** drifts with its lantern still lit, and a sou'wester hat floats beside it, the aftermath of a Leviathan. Ripples ring outward from the boat. Under the surface, shadows of the school slowly circle, and every 20 to 30 seconds a huge eye opens beneath the boat for a few seconds, then closes. The light cycles day → dusk → night over about 45 seconds, previewing the round's phases.

**How it's built (live in Unity, not a video).**

- An overhead orthographic camera over a water plane. A ripple shader draws concentric sine rings from the boat's position plus slow noise; the boat bobs, so the rings breathe.
- The school shadows are the real fish meshes rendered as dark silhouettes under the water plane, steered by the same boids code.
- A full-screen **ordered-dither pass** (Bayer 8×8 threshold, a 3- or 4-colour ramp per phase from our palette, adjustable pixel size) via a URP Full Screen Pass Renderer Feature. This single shader is what gives the halftone look.
- Day ramp: Abyss, Midwater, Shallows, Cream. Dusk: Abyss, Hook red, Raincoat. Night: Ink, Abyss, Glow mint for the lantern.
- It's the same scene, shader and assets as the game, so it costs about 2 hours, not an asset pipeline.

**Placement: the rule of thirds.** The subject sits right of centre; the **left third stays calm open water** for the UI. Ripple rings never cross into the menu column, so the text always sits on quiet texture.

**Buttons that belong to the picture.**

- Logo top-left in the gothic display font, cream, with the ampersand drawn as a fishhook. The tagline sits under it in the body font.
- A vertical stack, left-aligned: **Dive in** (primary), Workbench, Settings, Leave the shore (quit). No boxes by default: just cream text on a darker, slightly dithered band (Abyss at about 60% opacity, dithered with the same Bayer pattern so it reads as part of the image, not a UI overlay). Keep text contrast at 4.5:1 or better in every phase.
- **Hover:** a small hook icon slides in beside the label, the label sinks 2 px, and a ripple spreads from the menu edge across the water.
- **Click:** a splash sound, and the screen dissolves through the dither threshold into the workbench (animate the threshold; it's a free, beautiful transition).
- Bottom-left: the Edgegap status line in small type. Bottom-right: the jam credit. The mouse cursor is a small hook.
- Full keyboard and controller navigation; the focused item gets the same hook marker as hover.

**Sound:** water lapping against the hull, the lantern creaking on its hook, a distant bell buoy; at night, the faint choir.

### Cursors that belong to your faction

The cursor is a small character in its own right and changes with the faction you're looking at or playing.

| Context | Cursor | Idle | Hover | Click / select |
| --- | --- | --- | --- | --- |
| Main menu, before choosing | Cream fishhook, hotspot at the barb, hanging on a thin line that runs up to the top of the screen | Sways gently with mouse speed, like a weighted hook | Bobs once, line tightens | Hook "bites" (tilts 15° and back), a small ring of sparkles |
| Hatchery tab, playing a fish | A small tentacle tip with a sucker, hotspot at the tip, tinted your fish's skin colour | Slowly curls and uncurls; a tiny bubble every few seconds | Curls toward the item | Snaps into a tight curl, sparkles in your glow colour |
| Vestry tab, playing an Angler | The hook again, now with a tiny lit lantern charm, tinted your raincoat colour | Swings slightly behind fast movement | A glint runs along the hook | Bites, with wax-gold sparkles |
| In a round as an Angler | A thin aiming ring (the hook itself is in the water) | Pulses softly | — | Tightens on strike |
| In a round as a fish | The tentacle, shown only in mouse-follow swim mode or over UI | — | — | — |

**Switching faction is a tiny show.** Over 0.25 s the hook straightens and curls into the tentacle (or back), with a burst of 8 to 12 four-pointed sparkles that drift up and fade over 0.4 s, and a sound: a wet "plip" into Fish, a metal "clink" into Angler.

**How:** a software cursor (hide the system cursor; draw an Image on a top overlay canvas updated in `LateUpdate`), so it can animate and emit sparkles. Sparkles are a small pool of UI images animated with DOTween. A settings toggle returns to the normal system cursor for anyone who prefers it.

### Boot loading: the line goes down

The server wait lives in the workbench's bottom bar as a fishing line descending through four depth marks. Each mark matches a real step reported by the Cloudflare Worker, so progress is honest rather than a fake bar.

| Depth mark | Real event | Text |
| --- | --- | --- |
| 1 | `/join` sent | "Calling the Deep…" |
| 2 | Deployment starting; location known | "A vessel stirs near Mumbai…" |
| 3 | Deployment Ready; client retrying the connection | "Lighting the lanterns…" |
| 4 | Connected to the server | "The sea is open." |

Between marks the hook sinks slowly and bubbles rise, so it never looks frozen. On connect, the line jerks like a bite, sparkles burst at the hook, and **Dive in** starts pulsing. If a step takes much longer than usual, the text shifts to "The Deep is slow tonight…" and, after a timeout, to a Retry button. In the Hatchery the same line hangs through the tank; in the Vestry it hangs off the shrine's edge.

### When other players join

- **On the workbench:** each new player's creature appears on the congregation shelf. In the Hatchery it drops into a jar with a splash; in the Vestry a candle lights above a new pew and a hooded figure fades in. A bell tolls once, and a line appears: "\<name> has entered the water." Bots arrive the same way with their cult titles.
- **During a round:** a single ripple crosses the surface and the feed says "Something new swims among you." It never says which fish, so joining never breaks anyone's hiding.

### Transitions between every step

Three reusable effects carry every transition: the **dither dissolve** (animating the dither shader's threshold), the **camera dive** (the camera moves through the waterline), and the **parchment card**. Each lasts 1.5 s or less, is skippable with a click after the first time, and covers asynchronous scene loading, so the player never sees a frozen frame.

| From → to | Transition | Sound |
| --- | --- | --- |
| Launch → main menu | Black resolves into the dithered sea, pixel noise settling into water | Distant bell buoy |
| Main menu → workbench | Dither dissolve, then the camera dives from the overhead view down through the waterline into the Hatchery | Splash, then muffled underwater hum |
| Hatchery ↔ Vestry | Camera swings through the waterline | Muffled-to-clear sweep |
| Workbench → round (Dive in) | Wax seal stamp; the egg splits or the candles flare; the camera sinks into darkness while the round loads | Heavy thud, then a falling rumble |
| Round intro → play | The role card holds 3 s, then breaks into bubbles that rise and reveal the sea; a light ray sweeps across | Bubble rush |
| Day → dusk → night | Colour grading slides over 3 s; one line of subtitle ("The light leaves. Something else arrives.") | Music crossfade |
| Round end → ledger | A 0.3 s freeze, the image drains into dither, the ledger page slides up like a book opening | Quill scratch |
| Ledger → next round | Page turns, dither dissolve, the camera dives again | Page rustle, splash |
| Match end → results → Play again | Winner's page gets a wax seal; Play again dissolves straight back to the workbench for a quick rematch | Choir sting, thud |
| Server rotation or reconnect | Water rises to fill the screen; the descending-line loader returns with "Moving to a fresh server…" | Rising water, then the boot sounds |

Build these as one `TransitionManager` with the three effects and a queue, so every screen change goes through it and none is ever a hard cut. Budget about 2 hours in the Sunday juice block; the dither dissolve alone, used everywhere, already makes the game feel finished.

### Music

One track per light phase, crossfading with the round clock:

- **Day:** a slightly detuned music box over low strings.
- **Dusk:** the same tune, slower and a semitone flat.
- **Night:** sparse and eerie, with a low chanting choir when a Leviathan rises.

Sources: CC0 packs from Kenney, OpenGameArt and Freesound; CC-BY tracks (e.g. Kevin MacLeod) are fine with credit. List every source and licence in the credits.

### Sound effects

| Moment | Sound |
| --- | --- |
| Swim, dash | Soft bubble swish; sharp bubble burst |
| Nibble | Small munches, plus the line "tick" heard by the Angler (panned to the hook's position) |
| Strike | Whip-crack of the line; a splashy miss if nothing is there |
| Hooked | Twang + gasp |
| Tug | Rising whine pitched to tension, reel clicks, fish grunts |
| Snap / landed | Big twang and camera kick / splash and deck thud |
| Conversion | Raincoat rustle + a small cult "ding" |
| Sonar | Deep ping everyone hears |
| Leviathan | Roar, choir sting, wood creaking and a gulp as the boat goes under |

Underwater sounds go through a low-pass filter; surface sounds (gulls, creaking wood, rain at night) stay bright, so the two worlds feel different.

## Netcode and code architecture

Unity + Mirror, KCP transport over UDP port 7777, one dedicated server per sea. The server simulates everything; clients send inputs and draw. Fish and hooks do **not** use Unity physics or `NetworkTransform`: they are plain structs integrated by our own code and sent in one packed snapshot, which is what keeps players indistinguishable from the school.

&#91;embedded content: Netcode ownership · client views, server simulation\]

### Messages

| Message | Direction | Rate / channel | Contents |
| --- | --- | --- | --- |
| `JoinMsg` | client → server | once, reliable | name, 10-byte appearance |
| `RoundStartMsg` | server → client | per round, reliable | your role, your fish ID (fish only), appearance table for all \~156 fish (shuffled IDs), seat list for the ledger |
| `InputMsg` | client → server | 30 Hz, unreliable | sequence, move (2 × int8), cursor position (2 × int16), button bits (dash, ink, nibble, strike, reel, rebait, sonar, mark, bite) |
| `SnapshotMsg` | server → client | 15 Hz, unreliable | server tick; per fish: ID, x, y (16-bit quantized), heading (1 byte), flags (dashing, inked, nibbling, hooked, size step); per hook: owner, x, y, state; per boat: x, tilt |
| `EventMsg` | server → client | reliable | hooked, snapped, landed, switched, Leviathan rises, boat dragged, sonar ping, mark, phase change, round end + ledger |
| `TugMsg` | server → clients in the tug | 10 Hz, unreliable | tension, stamina, distance, exhausted flag |

Snapshot size: about 156 fish × 8 bytes + hooks and boats ≈ 1.3 KB, about 20 KB/s per client at 15 Hz. Fine for UDP; no delta compression needed.

### Scripts

| Script | Side | Responsibility |
| --- | --- | --- |
| `BSNetworkManager` | both | Mirror NetworkManager subclass; seats, joins, leaves, bot takeover |
| `GameStateManager` | server | Round state machine (intro, day, dusk, night, ledger), roles, scores, win checks |
| `SeaSim` | server | Fish structs, school boids with a spatial grid, player-fish movement from inputs, growth, Leviathan |
| `HookSim` | server | Hook bodies (spring to cursor, drag, gravity), nibbles, strikes, catches, rebait |
| `TugSystem` | server | The tug-of-war rules and outcomes, team tugs |
| `FishBotBrain`, `AnglerBotBrain` | server | Bot state machines producing the same input as `InputMsg` |
| `SnapshotWriter` / `SnapshotReader` | server / client | Pack and unpack the snapshot |
| `SeaView` | client | Snapshot buffer, 100 ms interpolation, pooled fish visuals, local prediction for your own fish |
| `FishAssembler` | client | Builds a fish from appearance bytes (parts on sockets, MaterialPropertyBlock tint) |
| `FishAnimator` | client | Procedural bob, tail wag, squash and stretch, eye tracking, blink |
| `HookView` | client | Verlet line renderer, twitch, tension colour |
| `InputSender` | client | Reads keyboard and mouse, sends `InputMsg` |
| `DayNightCycle` | client | Sun, fog, colour grading and music from the synced round clock |
| `HUD`, `Workbench`, `Ledger` | client | UI screens |
| `EdgegapBroker` | client | Calls the Cloudflare Worker broker, polls, returns host and port |
| `EdgegapAgent` | server | Reads `ARBITRIUM_*` variables, reports location, self-stops when idle, rotates before 60 min |
| `GameConfig` | both | ScriptableObject with every number in the balance sheet |

### Implementation notes

- Server tick 30 Hz for the sim, snapshots every other tick. Headless server: `Application.targetFrameRate = 30`.
- Sim code is plain C# operating on arrays of structs, so the same `HookSim` math can run on the client to predict your own hook.
- Your own fish: apply inputs locally right away, then ease toward the server position (no hard snaps). Everything else is interpolated 100 ms behind.
- Lag compensation is skipped on purpose: strikes are judged on the server's current state, and the generous catch radius absorbs latency.
- Edgegap app version: UDP port 7777, 1 vCPU / 2 GB, restart policy Never, new image tag every build; the broker is specified in "The broker: a Cloudflare Worker" below.

## Living with the free tier

The free tier allows **one deployment at a time, for at most 60 minutes**. So no server ever runs permanently: servers start when someone wants to play, everybody shares that one server, and it stops itself before the cap. Judges never see any of this except a one-minute wait on the workbench.

&#91;embedded content: Server lifecycle on the Edgegap free tier\]

### Step by step

1. **Play pressed.** The client calls the Cloudflare Worker broker's `/join`. The broker lists our deployments and takes one of three paths:
   - a server is **running**: return its address and port; the player joins in seconds;
   - a server is **starting** (or rotating): return its ID; the client polls `/status` every 2 s;
   - **no server**: deploy a new one, passing the player's IP so Edgegap places it near them, then poll.
2. **Starting (about a minute).** Container boot plus Unity loading. The player is on the workbench customizing, so the wait is play time. Once Ready, the client retries connecting every second until the server accepts.
3. **Running.** Everyone who presses Play while it's up joins the same sea: up to 6 humans, bots in the empty seats. A 7th human waits as a spectator for the next free seat (unlikely in a 14-entrant jam).
4. **Idle self-stop.** If no human has been connected for 3 minutes, the server stops itself through `ARBITRIUM_DELETE_URL`. Nothing runs, so no free hours are wasted overnight.
5. **Rotation before the cap.** A match takes about 7 minutes, so **no new match starts after 52 minutes** of uptime. When the current match ends, the server tells clients "Moving to a fresh server" and stops itself. Clients automatically call `/join` again; the broker waits for the old deployment to finish stopping, then starts a new one. Players spend about a minute back on the workbench, keeping their creature and their seat in the queue.
6. **Safety net.** If anything goes wrong and Edgegap hard-stops the server at 60 minutes, clients see the disconnect, show "Reconnecting…", and go through the broker exactly as in step 1.

### Edge cases

| Situation | What happens |
| --- | --- |
| Two players press Play at the same moment with no server | Both calls try to deploy; the second fails on the one-deployment limit. The broker treats that error as "already starting", re-lists, and returns the first deployment's ID |
| A judge in Europe joins a server started near India | They connect normally with higher ping; the floaty water movement and generous catch radius hide most of it. The next fresh server is placed near whoever starts it |
| Edgegap can't allocate a server (error 422) | The client shows "The Deep is busy, retrying…" and retries a few times, then offers a Retry button |
| The broker is unreachable | The client shows a clear error with Retry; Cloudflare Workers' free tier (100,000 requests a day) is far more than this needs |
| You test a dev build during judging week | **Don't.** Your test deployment counts as the one allowed deployment and would block judges. Test locally in Docker instead, or only when nobody is likely playing |

### What you need to do during judging week

Nothing, if the above works. Check the Edgegap dashboard once a day for error deployments and your compute-hour usage. The jam brief says the free tier includes enough compute hours for the jam; the exact allowance wasn't in the docs I read, so confirm it in your dashboard on Saturday. Idle self-stop keeps usage to the minutes people actually play.

### During development

- Most testing happens locally: host + client in the editor, or the server container in Docker Desktop. Deploy to Edgegap only for integration tests, and stop each test deployment when done.
- The free tier allows 2 app versions: keep `dev` and `prod`. Point a version at a new image tag by editing it in the dashboard; no rebuild needed.
- If the limits ever become a problem, Edgegap's pay-as-you-go tier removes the 60-minute cap and adds caching for near-instant starts. It isn't needed for the jam.

## The broker: a Cloudflare Worker

One of the organizers suggested a Cloudflare Worker synced with Edgegap, so a server starts when a player joins. That is exactly the broker role in this design, and Cloudflare adds one real improvement: a **Durable Object** can act as a single gatekeeper, so two players pressing Play at once can never trigger two deploys. It replaces Vercel everywhere in this plan. If the organizer already has a working Worker, ask whether they'll share it; it could save an hour.

### Pieces

| Piece | Job |
| --- | --- |
| Worker `POST /join` | Called by the Unity client. Forwards to the Durable Object and returns either a server address or "wait" with a deployment ID |
| Worker `GET /status?id=` | Polled by the client every 2 s while waiting; answers from the Durable Object's state |
| Worker `POST /edgegap/webhook` | Receives Edgegap's ready, error and terminated webhooks (protected by a secret in the URL) |
| Worker `POST /server/closing` | Called by our game server just before an idle stop or rotation (shared secret), so the state flips to "stopping" immediately |
| Durable Object `SeaLobby` | One instance (`idFromName("main")`). Holds the single source of truth: `requestId`, `status` (none, starting, ready, stopping), `fqdn`, `port`, `location`, `updatedAt` |
| Secrets | `EDGEGAP_TOKEN` and `WEBHOOK_SECRET`, set with `wrangler secret put`; never in the client |

### How it stays synced with Edgegap

- **Webhooks first.** Every deploy request includes `webhook_on_ready`, `webhook_on_error` and `webhook_on_terminated` pointing at the Worker, so the lobby state updates the moment Edgegap changes.
- **Polling as a fallback.** Edgegap doesn't retry webhooks, so while the state is "starting" or "stopping", a Durable Object alarm checks `GET /v1/status/{id}` every 10 to 15 seconds until it settles.
- **Self-healing.** If a deploy call fails because a deployment already exists, the Durable Object lists deployments with the Edgegap API and adopts the one it finds.

### The /join logic (inside the Durable Object, so calls are processed one at a time)

1. Status **ready** → return the address and port.
2. Status **starting** → return "wait" and the deployment ID.
3. Status **stopping** → return "wait"; once the terminated webhook (or the alarm) confirms the old server is gone, the next call deploys a fresh one.
4. Status **none** → call `POST /v2/deployments` with the player's IP (from the `CF-Connecting-IP` header), the webhooks and a tag; set status to starting; return "wait".

### Free-tier fit

| Limit (Cloudflare free plan) | Value | Our use |
| --- | --- | --- |
| Worker requests | 100,000 per day | A join with about a minute of polling is around 30 requests, so thousands of joins a day fit |
| Worker CPU | 10 ms per request | Time spent waiting on Edgegap's API doesn't count; our logic is tiny |
| Durable Object requests | 100,000 per day | Same order as Worker requests |
| Durable Object storage | SQLite-backed only on the free plan | One small row of state |

Limits reset daily at 00:00 UTC.

### Setup outline

1. `npm create cloudflare@latest` (Worker, TypeScript).
2. Add the `SeaLobby` Durable Object class with a SQLite-backed migration in the wrangler config.
3. `wrangler secret put EDGEGAP_TOKEN` and `wrangler secret put WEBHOOK_SECRET`.
4. `wrangler deploy`; put the `workers.dev` URL in the Unity client's `EdgegapBroker`.
5. Test: no server → press Play twice from two clients at once → confirm exactly one deployment appears in the Edgegap dashboard.

The Unity client calls the Worker with `UnityWebRequest`; since it isn't a browser, no CORS setup is needed.

## Balance sheet

Starting values for `GameConfig`. They are educated first guesses, meant to be tuned in playtests; keep every one of them in the ScriptableObject, never hard-coded. Distances are in Unity units (a fish is about 0.9 units long).

### World and round

| Parameter | Value |
| --- | --- |
| Visible sea | 40 × 22.5 units; surface at y = 0, seabed at y = −20 |
| Fish base size / hitbox radius | 0.9 / 0.45 |
| Seats | 6 (1 Angler + 5 fish at start) |
| Rounds per match | 3 |
| Round intro / round / ledger | 5 s / 120 s / 10 s |
| Day / dusk / night | 0–50 s / 50–80 s / 80–120 s |

### Fish

| Parameter | Value |
| --- | --- |
| Acceleration / max speed / water drag | 25 u/s² / 4.0 u/s / 2.0 per s |
| Dash speed / duration / cooldown | 11 u/s / 0.25 s / 2.0 s |
| Ink radius / duration / cooldown | 2.5 u / 3 s / 12 s |
| Nibble range / nibble time | 0.7 u / 0.6 s |
| Growth per bait (day / dusk / night) | 1 / 1.5 / 2 |
| Size and speed per growth step | +15% size, −3% speed |
| Leviathan threshold / scale | 6 growth / 2.5× |
| Leviathan bite time / growth after a boat drag | 2.0 s / 3 |

### Angler and hook

| Parameter | Value |
| --- | --- |
| Dinghy speed | 3 u/s |
| Max line length | 20 u |
| Hook spring / drag / gravity | 30 / 4 per s / 6 u/s² |
| Strike jerk / catch radius / cooldown | 1.2 u in 0.15 s / 0.6 u / 1.0 s |
| Missed strike scatter | 3 u radius for 2 s |
| Reel-up speed / rebait time | 8 u/s / 0.5 s |
| School fish landing time | 1 s |
| Sonar radius / outline time / cooldown | 12 u / 0.5 s / 15 s |
| Mark duration / cooldown | 3 s / 5 s |

### Tug-of-war

| Parameter | Value |
| --- | --- |
| Tension: reel / fish pull / ease off / fish rests | +35 / +45 × P / −50 / −20 per s |
| Tension from a dash | +25 instant |
| Snap threshold | 100 (gear changes it) |
| Reel-in speed / line pay-out | 3.5 × (1 − 0.6 × P) / 1.5 × P u/s |
| Stamina drain / regen / exhausted time | 25 × P / 15 per s / 1.5 s |
| Line fatigue starts / rate / tear loose | 6 s / −8 threshold per s / 12 s |
| Leviathan stamina / single-hook break | 300 / 1 s |

### School

| Parameter | Value |
| --- | --- |
| Count / respawn delay | 150 / 5 s |
| Cruise speed | 2.5 u/s |
| Separation radius / neighbour radius | 0.8 u / 2.5 u |
| Avoid moving hooks | within 1.2 u when the hook moves faster than 1 u/s |
| Curiosity | 2% chance per s within 5 u of a still baited hook |
| School nibble length / chance to eat | 0.3–0.55 s / 10% |
| Scatter duration | 2 s |

### Bots and network

| Parameter | Value |
| --- | --- |
| Fish-bot greed | 0.2–0.6 (random per bot) |
| Angler-bot reaction / deliberate miss | 250–450 ms / 25% |
| Angler-bot tug: reel below / release above | 65 / 80 tension |
| Sim tick / snapshot / input / tug message | 30 / 15 / 30 / 10 Hz |
| Interpolation delay | 100 ms |
| Server idle self-stop / rotation | 180 s with no humans / no new match after 52 min uptime, stop after that match |

## The 15-hour build plan (Sunday 1:30 AM to 6 PM)

Everything in this document gets built; nothing is cut. It fits because **three Claude Code sessions run in parallel**: you drive the Unity session, while a Blender session builds the art kit and a third session builds the Cloudflare Worker, each mostly on its own. The order puts risk first (networking, then the core loop), so the game is submittable early and every later hour adds polish to something that already works. Submit at **5:15 PM**, leaving 45 minutes of buffer.

&#91;embedded content: Sunday plan · 3 parallel tracks, effort per step\]

### Tonight, before 1:30 AM (setup only, no game code)

- [ ] Install Unity 6 LTS with Linux Build Support (Mono + IL2CPP) and Linux Dedicated Server; Docker Desktop (restart after); Node.js; `wrangler`; itch's `butler`; Git.
- [ ] **Unity MCP:** Unity's official MCP ships in the AI Assistant package (open beta), needs Unity 6, and needs a Unity AI subscription or trial, though it doesn't consume Unity AI credits. Confirm you have access; if not, install the free community "MCP for Unity" (CoplayDev) instead.
- [ ] **Blender MCP:** the official Blender Lab add-on requires **Blender 5.1 or newer**. Install it from the Claude connector directory (or the link on blender.org/lab/mcp-server); it serves on port 9876. It runs arbitrary Python, so keep only your own files open.
- [ ] Edgegap MCP with `EDGEGAP_APP_ALLOWLIST=bait-and-switch`; Context7. Accounts: Edgegap, Cloudflare, itch.io.
- [ ] Download assets with licences noted: Kenney Watercraft Kit, Kenney sci-fi and interface SFX, a CC0 music pack with stems, the two OFL fonts, DOTween.
- [ ] Export this GDD as Markdown into `docs/`, split per system, and write the root `CLAUDE.md` (rules, folder map, "read only the doc you need").
- [ ] **Sleep from about 9 PM to 1 AM.** It's a 16-hour day; fatigue causes the expensive bugs.

### Effort rules

In Claude Code, effort mostly buys **more checking and verification**, not a smarter model; medium is the default, and higher levels cost more tokens and time. So:

- **Plan and debug at max, build at medium.** For a hard system, one max-effort message produces a file-by-file plan; switch to `/effort medium` to write the files from that plan.
- **Max** for: netcode architecture, the tug and round state machines, the Durable Object concurrency logic, the Blender generator architecture, the Leviathan team tug, hardening, and any bug that survives one medium attempt (especially desyncs).
- **Medium** for: everything else, including UI, juice, bots, content, art part families, tuning and builds.
- Max is set per session, so it doesn't linger; return to medium as soon as the hard part is done.

### Track A: Unity (you drive)

| # | Time | Build | Effort | Done when |
| --- | --- | --- | --- | --- |
| 0 | 1:30–1:50 | Kickoff: Unity 6 URP project, Mirror, Cinemachine, ProBuilder, DOTween, Edgegap plugin; start sessions B and C | Medium | Three sessions running |
| 1 | 1:50–3:20 | Netcode core: fish structs, school boids, player fish input, snapshot writer/reader, `SeaView` interpolation, own-fish prediction (greybox) | **Max** | Two editor instances see the same smooth sea |
| 2 | 3:20–4:30 | Angler: dinghy, hook rope physics, line view, nibble, twitch, strike, school catches, rebait; terrain colliders | Medium | Strike-and-dodge feels good with 2 clients |
| 3 | 4:30–5:30 | Ship online: Linux server build, Docker, Edgegap (via MCP), client → Worker → server, Windows zip, `butler` draft | Medium | **Checkpoint 1: a valid online entry** |
| 4 | 5:30–7:00 | Tug-of-war, conversion, `GameStateManager` (intro, day, dusk, night, ledger), scoring, the Deep's Appetite | **Max** | A full 3-round match plays out |
| 5 | 7:00–8:00 | Bots for both roles, bot takeover on join and leave | Medium | 1 human + bots plays a whole match |
| 6 | 8:00–8:30 | Latency test (150 ms, 2% loss) + eat | Medium | **Checkpoint 2: complete game loop** |
| 7 | 8:30–9:45 | Art in: import kit from B, `FishAssembler`, toon shader and outline, lighting, day-night, fog, caustics, all `PartDef` stats | Medium | The sea looks like the game |
| 8 | 9:45–11:15 | Workbench (both dioramas, all add-ons, stat card, reactions, randomize, wax seal), boot line loader, join animations, main menu dither scene, cursors | Medium | Boot-to-play is a polished flow |
| 9 | 11:15–12:30 | Growth, Leviathan, boat drag, team tug, sonar, ink, marks, every special ability and relic | **Max** | **Checkpoint 3: feature complete** |
| 10 | 12:30–1:45 PM | Playtest with 3–5 friends on Edgegap; fix list | Medium (max for desyncs) | Fixes merged and redeployed |
| 11 | 1:45–3:15 PM | Juice: `TransitionManager` (dither dissolve, dives, cards), music stems and tension layer, SFX, the Robing, night creep beat, copy text, ledger titles | Medium | Every screen change is smooth |
| 12 | 3:15–4:00 PM | Harden: 52-minute rotation, idle self-stop, reconnect, error screens, 15-minute soak test with bots, GC check, final prod deploy, Edgegap MCP read-only | **Max** | **Checkpoint 4: release candidate** |
| 13 | 4:00–5:15 PM | Submission: final builds, `butler push`, itch page (GIF, screenshots, description, controls, credits, AI disclosure), Indieconnect entry, clean-PC test | Medium | Submitted |

### Tracks B and C (run alongside)

| # | Time | Build | Effort | Your part |
| --- | --- | --- | --- | --- |
| B1 | 1:50–2:20 | Blender setup: `kit_template.blend`, `style.py`, `validate.py`, contact sheet, export function | **Max** | None |
| B2 | 2:20–3:30 | Hero fish variant grids, then lock style; fish parts (bodies, eyes, fins, tails, mouths, specials) | Medium | Pick variants (2–3 quick looks) |
| B3 | 3:30–5:30 | Angler kit (raincoat, hats, rods, all hooks and baits, lantern, relics), Leviathan details, menu dinghy and hat | Medium | Approve contact sheets |
| B4 | 5:30–7:30 | Environment (rocks, kelp, coral), set dressing (shrines, candles, OFFERINGS crate, skeletons), workbench props (egg sac, jars, pegboard, pews), caustics texture, thumbnails, final export | Medium | Approve contact sheets |
| C1 | 1:50–3:00 | Cloudflare Worker + `SeaLobby` Durable Object, webhooks, alarm fallback, `/join` and `/status`, deploy, double-Play test | **Max** | Paste the Edgegap token as a secret |
| C2 | 4:30–5:30 | Wire to the real Edgegap app; server `/server/closing` call | Medium | None |

Review Blender contact sheets while Unity compiles or Claude writes code; each look takes about a minute.

### If a step runs late

Nothing gets dropped; time moves instead. Borrow first from the 45-minute buffer, then from the juice block (step 11), which can shrink because the dither dissolve alone carries most transitions. Never skip the latency test (6), the friend playtest (10) or hardening (12): judges score multiplayer stability at 25%.

### Making multiplayer smooth (what judges notice)

- **Authority:** the server owns all fish, hooks, tugs and scores; clients send inputs only.
- **Smoothness:** 30 Hz server tick, 15 Hz snapshots, 100 ms interpolation buffer; your own fish and hook are predicted locally and eased toward the server, never snapped.
- **One stream for every fish,** so motion looks identical for players and the school.
- **Test like a judge:** Mirror's latency simulation at 150 ms and 2% loss; one judge alone; two judges pressing Play together; joining mid-round; leaving mid-tug; a server rotation; a 15-minute soak test.
- **A hidden debug overlay** (F3): ping, snapshot rate, interpolation buffer, server tick time. Remove or hide it in the release build.
- **No frame hitches:** pool fish visuals, bubbles and sparkles; avoid allocations in update loops; cap the server at 30 FPS.
- **Graceful failures:** every network error has a themed screen and a retry, never a frozen game.

### Credit-saving rules

1. Max effort only where marked; plan in max, build in medium.
2. One conversation per system; `/clear` between systems, `/compact` in long ones.
3. Ask for complete files in one go, from the doc; avoid back-and-forth edits.
4. **Scenes and prefabs via editor scripts:** have Claude write small C# editor scripts that build prefabs and scenes, instead of placing objects one by one through the Unity MCP. Use the MCP for console errors, play mode and quick checks.
5. Blender: scripts plus one contact sheet per iteration; never sculpt by chat.
6. Enable only the MCP servers each session needs (Unity session: Unity + Edgegap + Context7; Blender session: Blender only; Worker session: none, `wrangler` only).
7. Commit after every working step; roll back with checkpoints instead of asking Claude to undo.

Sources: [Claude Code effort levels: what max actually buys](https://www.mejba.me/blog/claude-code-effort-levels), [Claude Code /effort command](https://www.claudelog.com/faqs/what-is-slash-effort-command/), [Unity AI MCP getting started](https://unity.com/blog/unity-ai-mcp-how-to-get-started), [Official Blender MCP comparison](https://www.strayspark.studio/blog/official-blender-mcp-server-comparison-2026), [Blender Lab MCP requirements](https://hermes-agent.nousresearch.com/docs/plugins/blender), [Claude Blender connector in Claude Code](https://dev.classmethod.jp/en/articles/claude-blender-connector-desktop-and-code/).

### Working with Claude Code

- Start each session by giving Claude this document plus the relevant scripts. One conversation per system: sea sim, hooks, tug, bots, UI, Edgegap.
- Ask for complete C# files, not fragments; keep `GameConfig` as the single source of numbers.
- Commit to git after every working step, so a bad change costs minutes, not hours.

### Claude Code toolkit

Every MCP server costs context in every session (roughly 500 tokens per tool definition, though Claude Code defers loading tool lists until needed), so enable **only what a session needs**, per project, in `.mcp.json`. Aim for four servers or fewer at a time.

| Tool | What it saves you | Priority | Notes |
| --- | --- | --- | --- |
| **Edgegap MCP (official)** | Deploying, waiting for a server with proper backoff, and reading crash exit codes and logs, all from inside Claude Code | Must | Set `EDGEGAP_APP_ALLOWLIST=bait-and-switch` so it can't touch anything else; set `EDGEGAP_READ_ONLY=1` during judging week so nobody accidentally starts a test deployment |
| **Unity MCP** (you have it) | Reading the console, running play mode and tests, inspecting scenes | Must | Keep its tool surface small if it offers profiles; some Unity MCPs expose hundreds of tools |
| **Blender MCP** (you have it) | Running the art generator script and rendering contact sheets | Must (art days) | Disable it during coding sessions |
| **Context7** | Up-to-date docs and examples for Mirror, FishNet, Unity, Cloudflare Workers, so Claude doesn't use outdated APIs | Should | Only two tools, cheap to keep on |
| **wrangler CLI** (not an MCP) | Deploying the Worker, setting secrets, streaming live logs with `wrangler tail` | Must | Claude Code runs it in the terminal; no MCP needed |
| **itch.io butler CLI** (not an MCP) | One-command upload of each Windows build to itch, with patching | Should | `butler push <zip> <user>/<game>:windows`; turns every redeploy into seconds |
| **Serena** | Symbol-level code navigation and edits for C#, cheaper than reading whole files once the codebase grows | Optional | Heavy tool surface (about 23 tools); only worth it after the codebase passes a few thousand lines |
| **A browser** (Claude in Chrome or Playwright MCP) | Filling in the itch submission page, checking the Edgegap and Cloudflare dashboards | Optional | Use on Sunday afternoon only |
| **Sound-effect generation MCP** (e.g. ElevenLabs) | Custom SFX like the choir sting or reel clicks | Optional | AI-generated audio must be disclosed; CC0 packs from Kenney and Freesound are the safer default |
| **Figma** (connected here) | Laying out the workbench and HUD before coding | Skip | Faster to build the UI directly in Unity this weekend |

### Claude Code features that save the most time

- **Project memory, not a memory server.** A root `CLAUDE.md` with the rules and folder map, plus this GDD exported as Markdown into `docs/`, split per system. Sessions load only the file they need with `@docs/tug.md`. A knowledge-graph memory MCP adds little over this.
- **Custom commands** for repeated pipelines:
  - `/ship-server`: Linux server build, Docker build and push, point the Edgegap version at the new tag (via the Edgegap MCP).
  - `/ship-client`: Windows build, zip, `butler push`.
  - `/art-build`: run `build_all.py`, `validate.py` and the contact sheet in Blender.
- **Hooks:** after any edit to `tools/blender/*.py`, run the validator automatically; after C# edits, check the Unity console for compile errors.
- **Subagents in parallel:** one builds the Cloudflare Worker while the main session works in Unity.
- **Checkpoints and rewind:** roll back a bad change instead of asking Claude to undo it by hand.
- **Model choice:** a cheaper model for boilerplate (UI wiring, data classes); the strongest model for netcode, the tug-of-war and the Durable Object logic.
- **Context hygiene:** `/clear` between systems, `/compact` in long sessions, and plan mode before any system larger than one file.

Sources: [Edgegap MCP server (Glama listing)](https://glama.ai/mcp/servers/edgegap/edgegap-mcp/schema), [MCP ecosystem notes for Unity (tool-cost estimate, deferred tool lists)](https://github.com/Ambiguous-Interactive/unity-helpers/wiki/Guides-Mcp-Ecosystem), [MCP Bridge (Serena and Context7 tool counts)](https://lobehub.com/mcp/cristian-robert-mcp-bridge).

## Risks and mitigations

| Risk | Likelihood | Mitigation |
| --- | --- | --- |
| Hooking and dodging isn't fun | Medium | Offline prototype tonight; tune hook weight, catch radius and nibble time before any networking |
| Players are easy to spot in the school | Medium | One snapshot stream for all fish, shared parts kit, school pace matched; the blend meter teaches hiding; if still easy, raise school curiosity and add school "dart" moves |
| Anglers can never find anyone | Low to medium | Sonar, marks, the tells table; shorten night if it's too dark |
| The tug feels random | Medium | Both meters always visible; clear audio pitch; tune the per-second rates in `GameConfig` |
| Networking bugs late on Sunday | Medium | Online by 6:15 AM, redeploy often, sim written as plain C# structs that are easy to debug |
| Edgegap server dead during judging | High if ignored | Broker starts servers on demand, idle self-stop, rotation before 60 min, retry screens |
| Unity server cold start is slow | Medium | Workbench screen fills the wait; measure it tonight |
| Judges play alone | Certain | Bots fill all seats; bot takeover makes joining instant |
| Too much scope | High | Cut list above; never-cut list protects the core |
| Windows SmartScreen scares judges | Medium | First line of the itch page: "More info, then Run anyway" |
| Fatigue | High | Break at 9 AM; submit at 5:15 PM, not 5:59 |

### Submission notes to remember

- Disclose AI use: Claude for ideation, code, and the Blender scripts that generated the models.
- Credit every asset: Kenney (CC0), music and sound sources with licences, fonts (OFL).
- In the description, explain the Edgegap setup (on-demand servers near the player) and the one-stream netcode that makes players indistinguishable. Sponsor judges will notice both.

## Reference library

Every source used in planning, grouped by when you'll need it. Paste the relevant group (not the whole list) into a Claude Code session when you need it. Links marked *(not re-checked)* are well-known locations added from memory rather than fetched during research; verify them before relying on them.

### Our documents

- [BAIT & SWITCH — Game Design Document](https://claude.ai/code/artifact/7b49f6ae-fa33-444b-a260-ea57511aa73d) (this document)
- [Overpowered Jam #1 — Game Concept & Build Plan](https://claude.ai/code/artifact/bf7b49e8-fe55-472c-88f2-d36861d3cf86): the original brainstorm, the scored idea shortlists, the broker architecture diagram, and art-kit notes

### The jam

- [itch.io jam page](https://itch.io/jam/overpowered-studios-game-jam-1): submissions open 9 Oct 12:30 UTC to 11 Oct 12:30 UTC (6 PM IST)
- [Indieconnect jam page](https://www.indieconnect.in/game-jams/overpowered-studios-game-jam-1-muprgzr0): rules and the revealed theme "No Limits / Anything Goes"
- Discord: [link from the itch page](https://discord.gg/q38vzJHGfR); the brief listed a different invite (discord.gg/zVpkqyXSa), which looked truncated
- Key rules: online multiplayer mandatory; servers on Edgegap for judging and cash prizes; Windows exe or HTML5 build; teams up to 5; third-party assets with credit; AI use allowed but disclosed

### Edgegap cheat sheet (so you don't need to re-read the docs)

| Topic | What you need |
| --- | --- |
| Free tier | 60-minute max per deployment; 1 deployment at a time; 2 apps and 2 versions; 5 GB registry; up to 1.5 vCPU / 3 GB; no credit card |
| Matchmaker free tier | 3 hours runtime per restart (not usable unattended for judging week) |
| Auth | Header `Authorization: <token>`; create tokens in Dashboard → User Settings → Tokens; keep the token only as a Cloudflare Worker secret |
| Deploy | `POST https://api.edgegap.com/v2/deployments` with `application`, `version`, `users` (at least one, e.g. the player's IP), optional `tags`, `environment_variables`, `webhook_on_ready` / `webhook_on_error` / `webhook_on_terminated`; returns `request_id` |
| Status | `GET https://api.edgegap.com/v1/status/{request_id}` → `running`, `fqdn`, `public_ip`, `ports.gameport.external`, `location` |
| List | `GET https://api.edgegap.com/v1/deployments?query={filters...}` (filter by `status`, `application`, `version`, `tags`) |
| Stop | `DELETE https://api.edgegap.com/v1/stop/{request_id}`; from inside the server: `DELETE $ARBITRIUM_DELETE_URL` with header `Authorization: $ARBITRIUM_DELETE_TOKEN` |
| Rate limits | Deploy 40/s; status 20/s; list 10/s per organization |
| Injected variables | `ARBITRIUM_REQUEST_ID`, `ARBITRIUM_PUBLIC_IP`, `ARBITRIUM_DEPLOYMENT_LOCATION` (JSON: city, country, lat/long), `ARBITRIUM_PORT_GAMEPORT_INTERNAL` / `_EXTERNAL` / `_PROTOCOL`, `ARBITRIUM_PORTS_MAPPING`, `ARBITRIUM_DELETE_URL`, `ARBITRIUM_DELETE_TOKEN`, `ARBITRIUM_CONTEXT_URL`, `ARBITRIUM_CONTEXT_TOKEN` |
| Hostname | `{request_id}.pr.edgegap.net`; external port is random per deployment |
| Ports | Our game: UDP 7777 (KCP). For browsers you'd use WS with "TLS Upgrade" instead |
| App version settings | Restart policy "Never"; new image tag every build (never `latest`, never overwrite a tag); edit or duplicate a version without rebuilding |
| Readiness | A deployment reports Ready before Unity finishes loading; clients should retry connecting every 1 s for up to about a minute |
| Unity server build | Install Linux Build Support (Mono + IL2CPP) and Linux Dedicated Server modules; Mirror: tick "Auto Start Server"; FishNet: "Start on Headless" |
| Docker | Install Docker Desktop and restart; on ARM Macs add `--platform=linux/amd64`; don't use `VOLUME`; `EXPOSE` isn't needed |
| Lifetime | All cloud deployments end after 24 h; errors are free and auto-removed after 24 h |

Edgegap docs tip: add `.md` to any docs URL for a clean Markdown version, and [llms.txt](https://docs.edgegap.com/llms.txt) indexes the whole documentation. Both are cheaper for Claude to read than the HTML pages.

- [Getting started](https://docs.edgegap.com) · [Unity guide](https://docs.edgegap.com/unity.md) · [Godot guide](https://docs.edgegap.com/godot.md)
- [Deployments (lifecycle, variables, WS/TLS, webhooks, filters)](https://docs.edgegap.com/learn/orchestration/deployments.md)
- [Apps and versions (ports, restart policy, free-tier limits)](https://docs.edgegap.com/learn/orchestration/application-and-versions.md)
- [Dedicated Servers API reference](https://docs.edgegap.com/docs/api/dedicated-servers.md)
- [Matchmaking](https://docs.edgegap.com/learn/matchmaking)
- [Sample projects](https://docs.edgegap.com/docs/sample-projects.md): [Mirror Pong](https://docs.edgegap.com/docs/sample-projects/unity-netcodes/mirror-pong.md) · [FishNet Space Edge](https://docs.edgegap.com/docs/sample-projects/unity-netcodes/fishnet-on-edgegap.md) · [Unity NGO 2D shooter](https://docs.edgegap.com/docs/sample-projects/unity-netcodes/unity-netcode-on-edgegap.md)
- [Edgegap Unity plugin (GitHub)](https://github.com/edgegap/edgegap-unity-plugin)
- [Dashboard](https://app.edgegap.com/) · [Register](https://app.edgegap.com/auth/register) · [API tokens](https://app.edgegap.com/user-settings?tab=tokens) · [Status page](https://status.edgegap.com/) · [Edgegap Discord](https://discord.gg/MmJf8fWjnt)

### Netcode

- [Mirror: Edgegap hosting plugin guide](https://mirror-networking.gitbook.io/docs/hosting/edgegap-hosting-plugin-guide)
- [FishNet: ServerManager settings](https://fish-networking.gitbook.io/docs/manual/components/managers/server-manager)
- [Mirror documentation home](https://mirror-networking.gitbook.io/docs/) *(not re-checked)*
- Cloudflare: [Workers pricing and free limits](https://developers.cloudflare.com/workers/platform/pricing/) · [Durable Objects](https://developers.cloudflare.com/durable-objects/) · [Durable Objects pricing and free limits](https://developers.cloudflare.com/durable-objects/platform/pricing) · [Durable Objects on the free plan (changelog)](https://developers.cloudflare.com/changelog/2025-04-07-durable-objects-free-tier/)

### Art: style references

- [Unity blog: the recipe behind Cult of the Lamb](https://unity.com/blog/games/recipe-behind-smash-hit-cult-of-the-lamb): 2D art in a 3D Unity scene, lighting
- [Devolver: Cult of the Lamb x Don't Starve interview](https://devolverdigital.com/blog/post/Cult-of-the-Lamb-Dont-Starve-Together-Crossover-but-make-it-an-interview): cute lens on a dark world
- [Spine showcase (Cult of the Lamb quote)](https://en.esotericsoftware.com/spine-showcase)

### Art: tools and assets

- [Blender MCP (GitHub)](https://github.com/ahujasid/blender-mcp) · [Blender MCP wiki](https://github.com/ahujasid/blender-mcp/wiki) · [PyPI package](https://pypi.org/project/blender-mcp/1.8.2/)
- [Kenney Watercraft Kit (CC0, 40+ boats)](https://kenney-assets.itch.io/watercraft-kit) · [Kenney Pirate Kit (CC0)](https://opengameart.org/node/88264)
- [UnityURPToonLitShaderExample by ColinLeung-NiloCat (MIT)](https://github.com/ColinLeung-NiloCat/UnityURPToonLitShaderExample) *(not re-checked)*
- Fonts: [Pirata One](https://fonts.google.com/specimen/Pirata+One) · [IM Fell English SC](https://fonts.google.com/specimen/IM+Fell+English+SC) *(not re-checked)*

### Art: Claude + Blender weaknesses research

- [MindStudio: Blender MCP donut test (token burn, artifacts)](https://www.mindstudio.ai/blog/claude-blender-mcp-60-percent-tokens-donut-test-results)
- [MindStudio: what Blender MCP can and can't do](https://www.mindstudio.ai/blog/claude-blender-mcp-real-world-performance)
- [Medium: Claude drives Blender through MCP (memory after \~30 operations, edge flow)](https://medium.com/@creativeaininja/claude-drives-blender-through-mcp-now-heres-what-actually-changes-684f28f78c7b)
- [3D-PreMise (arXiv): spatial precision errors, named parameters help](https://arxiv.org/pdf/2401.06437)
- [CodeGen-3D benchmark: iterative beats one-shot](https://scholarworks.sjsu.edu/faculty_rsca/6835)
- [Snap spatial intelligence benchmark: sign and direction reversals](https://eng.snap.com/spatial_intelligence)
- [The observability gap (arXiv): renders don't reveal code causes](https://arxiv.org/pdf/2603.26942)
- [Blender SuperSkill: reference-first, deterministic build scripts](https://skillsmp.com/creators/powerhouse90/blender-superskill/skills-blender-superskill)

### Game reference: Scale the Depths

- [Steam page](https://store.steampowered.com/app/3198890/Scale_the_Depths/) · [Store description (Gamesplanet)](https://us.gamesplanet.com/lenovo/legion/game/scale-the-depths-steam-key--8431-1) · [Original GMTK 2024 jam version (itch.io)](https://serpexnessie.itch.io/scale-the-depths)
- Reviews: [KeenGamer](https://www.keengamer.com/articles/previews/scale-the-depths-preview-catch-of-the-day/) · [Bonus Action](https://bonus-action.com/review/scale-the-depths-review-the-most-dangerous-loop-right-now/) · [Tech-Gaming](https://www.tech-gaming.com/scale-the-depths/) · [Thumb Culture](https://www.thumbculture.co.uk/scale-the-depths-pc-review/amp) · [Vaporlens summary](https://vaporlens.app/app/3198890/scale_the_depths)

### Multiplayer jam lessons

- [itch comments: "no one was online", bots that join when you wait](https://itch.io/jam/brackeys-16/rate/4952001)
- [itch thread: bots allowed in multiplayer jams, clearly marked](https://itch.io/post/1743084)
- [itch thread: raters play alone and won't wait in lobbies](https://itch.io/post/5027921)

### Earlier options (kept for reference)

- [Godot web export docs (single-threaded export, no C#)](https://docs.godotengine.org/es/stable/tutorials/export/exporting_for_web.html)
- [Colyseus deployment docs](https://docs.colyseus.io/deployment/), from the dropped browser plan
