# Step 1 — netcode core (plan, signatures only)
Scope: school + player fish, input → prediction → snapshot → interpolation. No hooks, tug, scoring, rounds, UI, art, audio, deploy.
Compile fixes: none (console clean). Budget: 155 fish × 7 B = 1085 B → 2 parts of ~78 fish ≈ 546 B + 12 B header (+ ≤66 B hooks/boats in part 0 later) ≪ 1182 B usable (BRIEF §4).

## Edits to existing files
- `Net/Messages.cs`: `SnapshotMsg { uint tick; ushort ackSeq; byte part, partCount; ArraySegment<byte> data; }` (drop roundTime, byte[]).
  `InputMsg` += `byte redundancy` packing: send last 3 inputs per message (`InputMsg` holds seq of newest; add `sbyte mx1,my1,mx2,my2; ushort btn1,btn2`). New `WelcomeMsg { ushort ownIndex; uint tick; }` (reliable, private).
- `Net/SnapshotCodec.cs`: `int EncodeFish(FishSnap[] src, int start, int count, byte[] dst, int off)`; `int DecodeFish(ArraySegment<byte> src, FishSnap[] dst, int start)`.
  7 B/fish: i16 x, i16 y, u8 heading, u8 flags, u8 size. Index implicit. Keep hook/boat encoders (array-based) for later. No List, no `new`.
- `Config/GameConfig.cs` new tunables: `maxFish=156` (school+seats), `snapshotParts=2`, `inputRedundancy=3`, `snapshotBufferLen=32`, `inputBufferLen=64`,
  `correctionHalfLife=0.1f` s, `correctionSnapDist=4f` u, `clockNudge=0.02f` (±2% render clock), `boidNeighborRadius=2f`, `boidCellSize=2f`,
  `wAlign=1f, wCohesion=0.6f, wSeparate=1.5f, wWander=0.3f, wBounds=2f`, `wanderHz=0.5f`, `serverSendRate=30`.

## New files (Assets/_Game/Scripts/)
- `Sim/FishMotor.cs` (shared server+client): `static void Step(ref FishBody b, Vector2 move, bool dash, float dt, GameConfig c)` — accel/drag/maxSpeed/turnRate, clamp to sea bounds. `struct FishBody { Vector2 pos, vel; float heading; byte flags, size; }`.
- `Sim/BoidsGrid.cs`: preallocated counting-sort grid. `void Build(FishBody[] f, int n)`; `int CellStart(int cx,int cy)`, `int CellCount(...)`, `int[] Sorted` — caller loops 3×3 cells inline (no delegates/LINQ).
- `Sim/SeaSim.cs` (server, pure C#): `FishBody[] fish; int[] owner /* -1 school, else connId */; uint tick;`
  `SeaSim(GameConfig c, int seed)`; `void Tick(float dt)` (boids for owner==-1, FishMotor for owned with latest input); `int Claim(int connId)` = random school index (no pop-in, index stable); `void Release(int idx)`.
  `void ApplyInput(int idx, in InputMsg m)` → stores move/buttons, `lastSeq[idx]`. ProfilerMarker "SeaSim.Tick".
- `Net/SeaServer.cs` (server MonoBehaviour): fixed accumulator `while(acc >= 1/simHz) Tick()` (max 5 steps/frame); every simHz/snapshotHz ticks: encode parts once into `byte[][] partBuf`,
  then per ready conn `conn.Send(new SnapshotMsg{…, ackSeq = lastSeq[ownIdx(conn)]}, Channels.Unreliable)` (ack is the only per-client field; 0 for spectators). Input handler: dedupe by seq, apply newest redundant inputs in order.
- `Net/SeaClient.cs` (client): handlers for Snapshot/Welcome. `SnapshotRing` of `snapshotBufferLen` slots `{uint tick; FishSnap[] fish; byte partsMask;}` keyed `tick % len`, preallocated.
  Render clock: `double renderTick` advanced by dt·simHz·(1±clockNudge) toward `latestTick − interpDelay·simHz`; hard reset if off > 0.25 s. Missing part → hold last pos for that half.
- `Net/LocalPredictor.cs` (client): reads `Keyboard.current` WASD each frame; own 30 Hz accumulator → builds InputMsg(seq++), stores in ring, `FishMotor.Step` on predicted body, sends unreliable.
  On snapshot with ackSeq: rebase to server body at that tick, replay inputs seq>ack, compute error; visual offset decays with correctionHalfLife (snap only if > correctionSnapDist).
  Same-frame response: visual = committed body + provisional `FishMotor.Step(copy, currentInput, acc)` each frame.
- `View/SeaView.cs` (client): pool of `maxFish` greybox instances created once; each LateUpdate sets transforms from SeaClient interpolation (lerp pos, LerpAngle heading); own index uses LocalPredictor visual.
- `Net/BSNetworkManager.cs` : NetworkManager. `autoCreatePlayer=false`, `sendRate=serverSendRate`, `headlessStartMode=AutoStartServer`. OnStartServer → create SeaSim/SeaServer, register InputMsg;
  OnServerConnect → `Claim`, send WelcomeMsg; OnServerDisconnect → `Release`; OnStartClient → register client handlers. Headless Awake: targetFrameRate = 30.
  Host mode: host client uses the same SeaClient/LocalPredictor path via local connection (acks ≈ 0 error); server code never reads client state. Editor: MPPM tag "Client" → StartClient, else StartHost.
- `Editor/GreyboxBuilder.cs`: menu `Tools/Bait & Switch/Build Greybox` → Scenes/Sea.unity (NetworkManager GO: BSNetworkManager + KcpTransport + LatencySimulation (disabled-by-default toggle, latency 150, loss 2), NetworkManagerHUD;
  side-on ortho Camera; light; sea bounds quad; SeaView), Prefabs/FishGreybox.prefab (capsule + nose, URP Lit), adds scene to Build Settings.

## Done checks (you test)
Profiler marker "SeaSim.Tick" < 2 ms expected; GC Alloc column 0 in SeaServer/SeaClient/SeaView/LocalPredictor after warm-up.

## Top risks
1. Clock/interp jitter under 150 ms/2% → render-clock nudging + 32-slot buffer + per-part hold; tune interpDelay if underruns.
2. Player motion distinguishable (players 4 u/s vs school 2.5; school too regular) → shared FishMotor caps, wander noise on school; same flags/encoding.
3. Host-mode/local-connection quirks (double handler registration, timing) → register handlers only in OnStartServer/OnStartClient, guard with `NetworkServer.active`.
