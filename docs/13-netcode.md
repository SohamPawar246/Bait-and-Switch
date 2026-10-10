# Netcode and code architecture

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
