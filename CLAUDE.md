# BAIT & SWITCH — project memory

Jam: Overpowered Studios Game Jam #1. Deadline Sun 11 Oct, 6:00 PM IST. We submit at 5:15 PM.
Game: online hide-and-hook party game. Cute alien fish hide in a school of 150 lookalikes and steal bait;
the Hooded Anglers (a fishing cult) hook them; every caught fish becomes an Angler. Last fish swimming wins.
Full design lives in `docs/`. **Read only the doc file(s) the current task needs. Never read all of docs/.**

## Git: hands off (hard rule)
- **Never** run `git add`, `git commit`, `git push`, `git pull`, `git fetch`, `git merge`, `git rebase`,
  `git reset`, `git checkout`, `git switch`, `git stash` or `gh`. The developer does all git work.
- Read-only git is fine when useful: `git status`, `git diff`, `git log`.
- When a feature is done, end your reply with a **one-line commit message suggestion** only, in this form:
  `Commit: <type>: <short summary>` (types: feat, fix, art, net, ui, chore). No body, no trailers,
  **no Co-Authored-By line**, no "Generated with" line.

## Token discipline (hard rules)
- **Do not test after every edit.** No play-mode runs, builds, screenshots, contact sheets or repeated
  console reads mid-task. Write the whole change first.
- Verify **once** at the end of a task: one Unity console check for compile errors (or one validator run
  in Blender). If it's clean, stop.
- If there are errors: fix them, check once more, then **stop and report**. Max 2 fix rounds without me.
- Never launch builds, Docker, deployments or the Edgegap/Cloudflare CLIs unless I ask in that message.
- Never take screenshots or contact sheets unless I ask, or the task is an art review I requested.
- I do the gameplay testing. End each task with a **"Test:" list of at most 5 bullets** for me to try.
- Don't re-read files you just wrote. Don't open files "to understand the project" beyond what the task needs.
- Plans: at most 10 lines. Replies: short. No summaries of code you wrote; just files changed + Test + Commit.
- Ask me rather than guess when a design decision isn't in docs/.

## Effort
- Default: medium. I'll switch to max for planning hard systems and stubborn bugs.
- At max effort: produce the plan and stop for my go-ahead; I'll switch to medium to implement.

## Folder map
- Repo root = the Unity 6.3 (6000.3.1f1) URP project (`Assets/`, `Packages/`, `ProjectSettings/`).
  Mirror netcode, KCP over UDP 7777. Windows client + Linux dedicated server.
  **All our game code and assets live in `Assets/_Game/`.** Don't touch other `Assets/` folders
  (template samples, third-party packages) unless asked.
- `worker/` — Cloudflare Worker + `SeaLobby` Durable Object: the Edgegap broker (already written).
- `tools/blender/` — art generator scripts (has its own CLAUDE.md with art rules).
- `docs/` — the design document split by system.

## Docs index (docs/)
00-overview · 01-art-direction · 02-art-production · 03-scale-the-depths · 04-match-flow · 05-fish ·
06-angler · 07-tug · 08-school · 09-customization · 10-scoring · 11-bots · 12-screens-ui-audio ·
13-netcode · 14-free-tier · 15-broker · 16-balance · 17-build-plan (incl. toolkit) · 18-risks · 19-references
(`GDD-full.md` is the whole thing; never load it into a session.)

## Non-negotiables
- Server authoritative. Clients send `InputMsg` only; the server owns fish, hooks, tugs, scores.
- Fish and hooks are plain C# structs in our own sim, not Unity physics or NetworkTransform.
  All fish (school + players + bots) go in ONE `SnapshotMsg` so players are indistinguishable.
- Sim 30 Hz, snapshots 15 Hz, 100 ms interpolation; own fish and hook predicted and eased, never snapped.
- Every tunable number lives in `GameConfig` or a `PartDef`. Never hard-code numbers.
- All player-facing text comes from `Copy.cs` (deadpan cult voice). Add new lines there.
- Headless server: `Application.targetFrameRate = 30`. No allocations in per-frame loops; pool all visuals.
- Scenes and prefabs are built by editor scripts in `Assets/_Game/Scripts/Editor/`,
  not by placing objects one call at a time through the Unity MCP.
- Unity MCP: use it for console errors, play-mode toggles I request, and reading scene state.
  Prefer writing C# files over many small MCP edit calls.
- The Edgegap token never goes in the client or the repo; it exists only as a Worker secret.
- Edgegap: app `bait-and-switch`, versions `prod` (judges) and `dev`; a new image tag every build.

## Already written (pre-built Saturday; extend these, never re-create them)
Unity, under `Assets/_Game/Scripts/` (written carefully but NOT yet compiled — if Unity reports
errors in them, fix those lines in place):
- `Config/GameConfig.cs` — every balance number (`GameConfig.I`), plus `Phase` and `Role` enums.
- `Config/PartDef.cs` — `Slot`, `Stat`, `Ability` flags, `PartDef`, `StatBlock` (resolved stats + caps).
- `Config/PartCatalog.cs` — all 53 gameplay add-ons + 46 cosmetics as data (`Specs()`), lookup,
  `Resolve(appearance, faction, statBlock)`, `RandomAppearance(rng)`. Appearance = 21 bytes.
- `Editor/PartCatalogBuilder.cs` — menu Tools > Bait & Switch > Build Part Catalog: creates the
  PartDef assets, Resources/PartCatalog and Resources/GameConfig, wires FBX + thumbnails by name.
- `Net/Messages.cs` — JoinMsg, InputMsg (+ `Buttons`), RoundStartMsg, SnapshotMsg, EventMsg (+ `EventKind`),
  TugMsg, LedgerMsg, ServerInfoMsg, `Quant` (1/256 unit fixed point).
- `Net/SnapshotCodec.cs` — FishSnap/HookSnap/BoatSnap, `FishFlags`, `HookState`, Encode/Decode (9 B per fish).
- `Net/EdgegapBroker.cs` — client: Worker /join → poll /status → 4 boot stages → Mirror connect with retries.
  `useDirectAddress` for local Docker testing.
- `Net/EdgegapAgent.cs` — server: location for the HUD, idle self-stop, `AcceptingNewMatches` (52 min),
  `StopServer()` (tells the broker, then self-deletes via ARBITRIUM_DELETE_URL).
- `Core/Copy.cs` — every player-facing string, tips, bot titles, name suggestions.
Worker: `worker/` is complete and typechecked (see worker/README.md). Don't rewrite it.
Blender: `tools/blender/` is complete and was run headless in Blender 5.2: all 10 families build and pass
validation; `combo` renders assembled fish. See tools/blender/CLAUDE.md.
