# Setup — tonight (no Claude needed) and the 1:30 AM kickoff

Unity 6.3.1f1, Blender and both MCPs are already installed. This pack already contains the rules,
the docs, the broker, the art pipeline and the Unity data/networking layer, so tonight is pure clicking.

## Tonight (about 90 minutes)

### 1. Repo (5 min)
- [ ] The Unity project folder is the repo root. This pack is extracted into it, so `Assets/_Game/` sits
      inside your real `Assets/`, and `CLAUDE.md`, `.claude/`, `docs/`, `tools/`, `worker/` sit next to `Assets/`.
- [ ] Check `.claude/settings.json` is present: it blocks Claude from every git write command and removes
      the Co-Authored-By trailer. Run `claude` **from the project root** and type `/permissions` to see the deny list.
- [ ] Commit yourself (`chore: starter pack`) and push.

### 2. Unity project (25 min)
- [ ] Project already created (Unity 6.3.1f1, Universal 3D template = URP). Optional: delete the template's
      sample scene/assets folder if you don't want it.
- [ ] Install modules if missing: Linux Build Support (Mono + IL2CPP), Linux Dedicated Server, Windows Build Support.
- [ ] Packages: **Mirror** (Asset Store or OpenUPM), **Cinemachine**, **ProBuilder**, **DOTween** (Asset Store, free),
      **Multiplayer Play Mode**, **Edgegap plugin** (`https://github.com/edgegap/edgegap-unity-plugin.git` via Add package from git URL).
- [ ] Let it compile. If any pre-written script errors, note the errors for the 1:30 session — don't fix by hand.
- [ ] Run **Tools > Bait & Switch > Build Part Catalog**. Console should say "Part catalog built: 99 parts".
- [ ] Commit (`chore: unity project + packages`).

### 3. Edgegap (20 min)
- [ ] Edgegap dashboard: create app `bait-and-switch`, versions `dev` and `prod`: port 7777 **UDP** named `gameport`,
      1 vCPU / 2 GB, restart policy **Never**. Note your free compute-hour allowance.
- [ ] Create an API token (User Settings → Tokens). Keep it in a password manager.
- [ ] **Pipeline proof:** in a scratch project, open Edgegap's Mirror Pong sample, build + containerize + push
      with the Unity plugin, deploy from the dashboard, connect a Windows build, then **stop the deployment**.
      Write down the cold-start time. (Docker Desktop must be running.)

### 4. Broker (10 min) — follow `worker/README.md`
- [ ] `npm install`, `wrangler login`, three secrets, `npm run deploy`. Paste the URL into a note.
- [ ] On both Edgegap versions add hidden env vars `BROKER_URL` and `BROKER_SECRET`.
- [ ] `curl https://<worker>/status` returns JSON. (Don't call /join yet; it starts a server.)

### 5. Assets (15 min) — into `Assets/_Game/ThirdParty/`, one line each in `CREDITS.md`
- [ ] Kenney Watercraft Kit, Kenney Interface Sounds, Kenney Impact Sounds.
- [ ] A CC0 music pack with day / dusk / night feel (OpenGameArt, Freesound).
- [ ] Fonts: Pirata One + a plain body font (both OFL, Google Fonts).

### 6. MCPs (5 min)
- [ ] `claude mcp list` from the repo: Unity, Blender, Edgegap (with `EDGEGAP_APP_ALLOWLIST=bait-and-switch`), Context7.
- [ ] Blender: the MCP add-on server is running (port 9876).

### 7. Sleep 9 PM → 1 AM.

## 1:30 AM — open three terminals in the repo, paste these

**Session A — Unity** (`/effort max` for this first message only)
```
Read CLAUDE.md, docs/13-netcode.md, docs/08-school.md, docs/05-fish.md and the pre-written Net/ and Config/ scripts.
Plan step 1 of docs/17-build-plan.md (netcode core: SeaSim, school boids, player fish input, snapshot send/receive,
SeaView interpolation, own-fish prediction, greybox) in at most 15 lines, file by file. Stop after the plan.
```
Then `/effort medium` and: `Implement the plan.`

**Session B — Blender** (medium)
```
Read tools/blender/CLAUDE.md. Run build_all.py with MODE="variants" (REPO = this repo's absolute path) and show me the sheet.
```

**Session C — nothing to build.** The broker is done. Use this terminal for `npm run tail` (live Worker logs)
and quick one-off questions at low effort.

## Habits that keep usage low
- `/feature <what> <docs files>` and `/fix <what happens>` — both cap planning, testing and replies.
- `/clear` when switching systems. Paste error text instead of asking Claude to go find it.
- You test gameplay; Claude only checks compile errors once per task.
- Commit after each "Commit:" line Claude gives you.
