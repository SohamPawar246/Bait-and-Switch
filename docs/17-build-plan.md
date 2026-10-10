# The 15-hour build plan (Sunday 1:30 AM to 6 PM)

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
