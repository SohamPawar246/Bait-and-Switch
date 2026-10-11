# BRIEF — load first (supersedes re-reading docs/)
Written 2026-10-11 ~02:00 IST, onboarding session. Facts tagged [src] were checked against installed source;
Edgegap, Cloudflare, Unity install/URP/MPPM verified against official docs or local files on 2026-10-11; [UNVERIFIED] = not checked. Read a doc only when the task needs its details.

## 0. Versions (ground truth)
- Unity **6000.3.10f1** (ProjectSettings/ProjectVersion.txt). CLAUDE.md says 6000.3.1f1 — wrong.
- URP 17.3.0 · Cinemachine 3.1.7 · MPPM 2.0.2 · Input System 1.18.0 · ProBuilder 6.0.9 · com.unity.ai.assistant 2.20.0-pre.2 (official Unity MCP)
- Mirror **96.0.1** (Assets/Mirror/version.txt) · kcp2k **V1.41** · Edgegap plugin com.edgegap.unity-servers-plugin **3.0.9** (Assets/Mirror/Hosting/Edgegap) · DOTween free (Assets/Plugins/Demigiant)
- No asmdefs in Assets/_Game → game code = Assembly-CSharp, editor = Assembly-CSharp-Editor. Namespace `BaitSwitch.*`.

## 1. Jam rules → what to protect
- Online MP mandatory; dedicated servers MUST be on Edgegap. Ship Windows zip on itch. AI use must be disclosed. Teams ≤5.
- Deadline Sun 11 Oct 18:00 IST; we submit 17:15. Judges play 11–15 Oct, often alone → bots fill seats, broker starts servers on demand.
- Weights → systems:
  - Stability 25%: server-authoritative sim, snapshot fits MTU, prediction/interp, broker retries, server rotation at 52 min.
  - Fun 25%: hide-in-school + nibble/strike + tug; bots so a solo judge has a game.
  - Theme 20% ("No Limits"): Leviathan growth with no size cap; caught fish switch sides (no fixed teams).
  - Art/audio 15%: Blender-generated chibi parts, cel + ink outline, day/dusk/night light.
  - Innovation 15%: social-stealth among 150 lookalikes; Angler → back to fish via Leviathan boat bite.

## 2. Design on one screen (docs 00, 04, 05, 08)
- 6 seats/round (humans first, bots fill): 1 Angler + 5 fish. Match = 3 rounds × 120 s. Phases Day 0–50 s, Dusk 50–80, Night 80–120.
- Fish: WASD momentum swim, Space dash (tell), hold LMB nibble 0.6 s, E ink 3 s. Bait growth 1/1.5/2 by phase; each step +15% size, −3% speed.
- Growth 6 → Leviathan (2.5×), can't hide; needs 2 hooks to land; Bite boat 2 s → that Angler becomes a fish, Leviathan shrinks to growth 3.
- Caught fish → Angler (raincoat ritual, own dinghy ~3 s after landing). Anglers win if all fish converted before 120 s.
- School: 150 constant, boids (sep/align/cohesion, cruise 2.5 u/s, turn limit), avoids moving hooks, curiosity nibbles 0.3–0.55 s (10% eat), scatters 2 s.
- Indistinguishable rules (08:7-10): same parts, same snapshot + interpolation pipeline, IDs shuffled per round, only own fish predicted.
- Roles private: each client learns only its own role + own fish ID (04:11). Mid-round join takes over a bot fish invisibly; leaver → bot.
- Camera fixed side-on 2.5D. Sea: `seaSize` 40×22.5, surfaceY 0, seabedY −20 (GameConfig).

## 3. Architecture
```
Win client ──HTTPS /join, poll /status──► Cloudflare Worker + SeaLobby DO ──Edgegap API (token = Worker secret)──► Edgegap
    │                                                                                                    │ deploys
    └──────────── Mirror KCP/UDP (public IP : external port mapped to 7777) ─────────────► Unity Linux server (headless)
client → server : JoinMsg (rel, once) · InputMsg (unrel, 30 Hz)
server → client : RoundStartMsg (rel, private role/fish id) · SnapshotMsg (unrel, 15 Hz, ALL fish) · EventMsg (rel)
                  TugMsg (unrel, 10 Hz, tug participants) · LedgerMsg · ServerInfoMsg
server → Edgegap: self-delete via ARBITRIUM_DELETE_URL; stops taking matches at 52 min (60 min cap)
```
- Rates (GameConfig): simHz 30, snapshotHz 15, inputHz 30, tugHz 10, interpDelay 0.1 s.
- Server owns everything; fish/hooks are structs in our sim (no physics, no NetworkTransform, no player prefabs).

## 4. Verified facts & gotchas
### Mirror 96.0.1 / kcp2k 1.41 [src]
- KCP MTU is a **const 1200** (Assets/Mirror/Transports/KCP/KcpTransport.cs:63 `const int MTU = Kcp.MTU_DEF`). Not an inspector field; raising it means editing Mirror — don't.
- Unreliable max message = mtu − 5 (1 channel + 4 cookie) − 1 = **1194 B** (kcp2k/highlevel/KcpPeer.cs:122). Batch threshold = same (KcpTransport.cs:250).
- Reliable max = (1200 − 24 − 5) × (min(4096,255) − 1) − 1 = **297,433 B** (KcpPeer.cs:110-118). Reliable = head-of-line blocking; never for snapshots.
- Mirror batch overhead: 8 B timestamp per batch + varint size per message + 2 B message id. Usable single-message payload on unreliable ≈ **1182 B**.
- Oversize message is **dropped** with LogError "…larger than the max allowed message size in one batch…" (Core/NetworkConnection.cs:102).
- `byte[]` message fields allocate a new array on every receive (weaver reader) → use `ArraySegment<byte>` (valid only inside the handler; decode immediately).
- Headless: `NetworkManager.ConfigureHeadlessFrameRate()` sets `Application.targetFrameRate = sendRate` (NetworkManager.cs:663-670). **sendRate default 60** (:43) → set sendRate = 30 or the server runs 60 fps.
- `NetworkManager.autoCreatePlayer = true` by default (:105) → set false (we use messages, no player objects). `runInBackground = true` (:29).
- `headlessStartMode` default DoNothing (:35) → set to AutoStartServer for the Linux build.
- LatencySimulation transport: Assets/Mirror/Transports/Latency/LatencySimulation.cs. Fields: `latency` (ms, default 100), `jitter` (0–1, 0.02), `jitterSpeed`, `unreliableLoss` (%, 2), `unreliableScramble` (%, 2). Wrap KcpTransport and set it as NetworkManager.transport.
  For the 150 ms / 2% test: latency 150, unreliableLoss 2. Host's own local client bypasses the transport (no simulated lag) [UNVERIFIED: delivery timing of local conn].
- MPPM / Unity Transport network simulator does not affect Mirror KCP [UNVERIFIED, expected] → always use LatencySimulation.

### Unity 6.3 (local install + docs)
- **Linux build support is NOT installed.** `C:/Program Files/Unity/Hub/Editor/6000.3.10f1/Editor/Data/PlaybackEngines/` has only AndroidPlayer, WebGLSupport,
  windowsstandalonesupport (Mono only, no IL2CPP). → Unity Hub > Installs > 6000.3.10f1 > Add modules: **Linux Build Support (Mono)** + **Linux Dedicated Server Build Support**. Do it now (big download).
- Linux server build: Build Profiles "Linux Server" (UNITY_SERVER define); scripted `BuildPlayerOptions{target=StandaloneLinux64, subtarget=(int)StandaloneBuildSubtarget.Server}`. Mono backend fine.
- URP 17.3: Compatibility Mode (non-Render-Graph) is deprecated and hidden behind the `URP_COMPATIBILITY_MODE` define [src: PackageCache/com.unity.render-pipelines.universal/Runtime/Deprecated.cs:635-638]
  → write renderer features with `RecordRenderGraph(RenderGraph, ContextContainer)` only; prefer built-in Full Screen Pass Renderer Feature + Fullscreen Shader Graph for dither.
- MPPM 2.0: namespace `Unity.Multiplayer.Playmode` (lowercase m), `CurrentPlayer.ReadOnlyTags()` → use `.Contains("Client")`. Tags created in Project Settings > Multiplayer > PlayMode > Player Tags
  (saved to ProjectSettings/VirtualProjectsConfig.json), assigned in Window > Play Mode > Scenarios; one tag per player. Its Network Simulator is a Multiplayer Tools/UTP feature (not installed) → irrelevant to Mirror; use LatencySimulation.
  https://docs.unity3d.com/Packages/com.unity.multiplayer.playmode@2.0/manual/instance-types/main-and-additional-editor-instances.html
- ProjectSettings: `activeInputHandler: 1` (new Input System only) → `Keyboard.current`. `runInBackground: 0` (Player setting; Mirror's NetworkManager.runInBackground=true sets it at runtime anyway).
- Unity MCP tools useful here: Unity_ReadConsole (errors), Unity_ManageEditor (play mode), Unity_ManageMenuItem (run Tools/Bait & Switch/*), Unity_RunCommand (editor C#), Unity_Profiler_* (GC alloc per frame, sample time of a ProfilerMarker) → use for Done criterion "tick < 33 ms, 0 GC".

### Edgegap (verified 2026-10-11, https://docs.edgegap.com/docs/api/dedicated-servers.md + /learn/orchestration/deployments.md)
- Deploy: `POST https://api.edgegap.com/v2/deployments`, header `Authorization: token <key>`. Required `application`, `version`, `users` (≥1 of `{user_type:"ip_address", user_data:{ip_address}}`
  or `{user_type:"geo_coordinates", user_data:{latitude, longitude}}`). Optional `environment_variables:[{key,value,is_hidden}]` (≤20), `tags`, `webhook_on_ready|on_error|on_terminated:{url}`. 202 → `request_id`. Worker matches.
- Status: `GET /v1/status/{request_id}` → `current_status`, `running` (true = ready), `error`, `fqdn`, `public_ip`, `ports{<portName>:{internal,external,protocol}}`, `removal_time`, `location{city,country,…}`. Worker matches.
- **App version port must be named `gameport`** (UDP 7777): worker reads `ports.gameport.external` (worker/src/index.ts:174,285,314). External port is randomized per deployment.
- Stop: `DELETE /v1/stop/{request_id}` (SIGTERM). Self-stop: `DELETE $ARBITRIUM_DELETE_URL` with header `Authorization: $ARBITRIUM_DELETE_TOKEN`. EdgegapAgent.cs:104-108 matches.
- List: `GET /v1/deployments?query={"filters":[{"field":"application","operator":"eq","value":…}],"order_by":[{"field":"created_at","order":"desc"}]}`; items use `ready`. Worker matches.
- Webhooks: ready / error / terminated; **not retried, may be lost** → worker's 12 s / 60 s alarm polling fallback is required (it exists).
- Env vars: ARBITRIUM_REQUEST_ID, _PUBLIC_IP, _HOST_ID, _DEPLOYMENT_TAGS, _DELETE_URL/_TOKEN, _CONTEXT_URL/_TOKEN, _PORT_GAMEPORT_INTERNAL/_EXTERNAL/_PROTOCOL,
  _DEPLOYMENT_LOCATION (JSON: city, country, continent, administrative_division, timezone, latitude, longitude), _PORTS_MAPPING (JSON `{ports:{gameport:{…}}}`).
- Free tier: **1 concurrent deployment, 60 min cap, ≤1.5 vCPU / 3 GB** (https://edgegap.com/resources/pricing). docs/13:48 (1 vCPU/2 GB) fits. Rate limits: deploy 40/s, status 20/s.

### Cloudflare (verified, https://developers.cloudflare.com/durable-objects/platform/pricing/)
- Workers Free: only SQLite-backed DOs → wrangler.jsonc `new_sqlite_classes:["SeaLobby"]` ✔. KV-style `ctx.storage.get/put` works on SQLite DOs (billed as rows).
- Free daily: 100k DO requests, 13,000 GB-s, 5M rows read, 100k rows written; each `setAlarm()` = 1 row written. Worker's 60 s ready heartbeat ≈ 1,440 alarms/day ✔.
- wrangler ^4.0.0, compatibility_date 2026-10-01.

### Art pipeline / Blender MCP
- Blender export → `Assets/_Game/Art/Parts/<slot>_<name>.fbx` + `Assets/_Game/Art/Thumbs/<slot>_<name>.png` (tools/blender/style.py:95-96), names e.g. `body_round`, `hook_j_hook`
  = PartCatalogBuilder key `slot.ToLower()_Slug(name)` (PartCatalogBuilder.cs:43-46) ✔. FBX: -Z forward, Y up, FBX_SCALE_UNITS, bake_space_transform (export.py:39-40). **No FBX/PNG exported yet.**
- Blender MCP = "MCP for Blender" (addon update: `uvx mcp-for-blender install-addon`). Blender wasn't running at check time → version/telemetry status UNVERIFIED.
  Asset imports (Poly Haven/Sketchfab/Poly Pizza) and generate_3d must be disclosed; Sketchfab licences vary per model [UNVERIFIED].

### DOTween / Cinemachine / butler [UNVERIFIED, standard APIs]
- Cinemachine 3: namespace `Unity.Cinemachine`, `CinemachineCamera` (not VirtualCamera), `Target.TrackingTarget`.
- DOTween: client only; always `.SetLink(gameObject)`; never run on headless server.
- butler: `butler push <dir> <user>/<game>:windows --userversion X` (BUTLER_API_KEY env for login).

## 5. Mismatches (file:line → fix)
1. **Snapshot exceeds the KCP limit → Mirror drops every snapshot.** SnapshotCodec.cs:43 = 9 B/fish. At 155 fish + 6 hooks + 6 boats:
   data 2+1395+1+36+1+30 = 1465 B; SnapshotMsg adds tick 4 + roundTime 4 + byte[] len 4 + id 2 → ~1479 B > 1182 usable.
   Fix (step 1): drop per-fish `id` (index = stable shuffled ID, implicit by order) → 7 B/fish, and split each tick into **2 parts**
   (fish halves; hooks/boats in part 0). Each part ≤ ~600 B, leaves room for growth. docs/13:18 (~1.3 KB, "fine for UDP") is wrong for MTU 1200.
2. SnapshotCodec.cs:74 `Encode` allocates `new byte[_w]` every snapshot; Encode/Decode take `List<>` + `foreach` → replace with array + count API writing into a caller buffer; return `ArraySegment<byte>`.
3. Messages.cs:62 `SnapshotMsg.data` is `byte[]` → allocates per receive on clients. Change to `ArraySegment<byte>`; add `byte part, partCount`, `ushort ackSeq` (see plan).
4. CLAUDE.md "NOT yet compiled" is stale: Assembly-CSharp.dll built 21:19 after scripts (20:30), part assets generated 21:26, console clean.
5. CLAUDE.md Unity version 6000.3.1f1 → 6000.3.10f1.
6. docs/08:20 says boids "at 20 Hz"; GameConfig simHz = 30 and docs/13:44 say 30 Hz. Use 30.
7. docs/13:11 says 10-byte appearance; CLAUDE.md says appearance = 21 bytes (PartCatalog). Code is truth (21 B, `PartCatalog.SlotCount`).
9. PartCatalogBuilder.cs:46 loads thumbnails as `Sprite`; PNGs import as Default textures in a 3D project → null.
   Fix: before loading, set `TextureImporter.textureType = Sprite` for files in Art/Thumbs (or an AssetPostprocessor for that folder).
10. Edgegap app version setup (dashboard/plugin) must name the port `gameport` / UDP / 7777, or the worker never finds the external port.
8. Headless frame rate: CLAUDE.md demands targetFrameRate 30 but Mirror overrides it with sendRate (60) → FIXED: BSNetworkManager overrides ConfigureHeadlessFrameRate() to simHz (sendRate stays 60 so batches flush every frame).

## 6. Compile state
- Unity console 2026-10-11 ~01:40: **0 errors, 0 warnings**. Editor.log: no `error CS`. Nothing to fix.

## 7. Not audited this session
- docs 01, 02, 03, 06, 07, 09, 10, 12, 14–19 not re-read; Copy.cs, PartCatalog.cs, EdgegapBroker.cs fully, worker logic beyond its Edgegap/DO calls,
  and Blender scripts beyond export naming were not audited. Audit each when its build step starts.
