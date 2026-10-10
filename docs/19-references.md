# Reference library

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
