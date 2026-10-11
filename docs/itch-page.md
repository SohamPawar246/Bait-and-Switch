# Bait & Switch — itch.io page

**Tagline:** Hide in the school. Steal the bait. Don't get switched.

## About
An online hide-and-hook party game for up to 6 players (bots fill empty seats).
You are a cute alien fish hiding in a school of 150 lookalikes, stealing bait from the Hooded Anglers, a fishing cult.
Get hooked and you are *saved*: you put on the hood and fish for your old friends.
Last fish swimming wins. Three rounds, each one runs Day → Dusk → Night.

Theme, **No Limits**: no fixed teams (every catch switches sides), and the Leviathan keeps growing with no size cap.

## How to play
1. Unzip and run `BaitAndSwitch.exe`. Type your name, then press **Dive in**.
   The game finds or starts a dedicated server on Edgegap. The first boot can take up to a minute.
2. Fish win by staying hidden: swim like the school, nibble bait for points, and never dash where an Angler can see.
3. Anglers win by spotting the fish that swim *wrong*: hook them, then win the tug-of-war.
4. The **Workbench** builds your fish and your Angler. Each part changes your stats, and the specimen sheet shows how.
5. **How to play** opens the Book of Rites: six short pages.

## Controls
| Fish | Angler |
|---|---|
| WASD: swim | A / D: row |
| Hold LMB: nibble bait | Mouse: aim the hook |
| Space: dash | LMB: strike / reel |
| E: ink cloud | RMB: rebait |
| Hold Tab: find yourself | |
| | Q: sonar ping · MMB: mark a fish |
| Esc: pause / settings | |

If the servers are unreachable, choose **Fish alone with the bots**, or **Host a local sea** for LAN play.

## Tech
- Unity 6.3 URP, Mirror + KCP (UDP), server-authoritative 30 Hz sim, 15 Hz snapshots.
- Dedicated Linux servers on **Edgegap**, started on demand by a Cloudflare Worker broker.

## Credits
- Game: Soham Pawar (design, direction, testing).
- Mirror (MIT), kcp2k (MIT), DOTween (free licence, Demigiant).
- Fonts: Pirata One and Crimson Text (SIL Open Font License).
- Music and sound effects: synthesized entirely from code (`tools/audio/synth.py`): no samples, no sound libraries.
- Studio intro video: by the team.
- Hosting: Edgegap. Broker: Cloudflare Workers.

## AI disclosure
This game was made with AI assistance (Anthropic Claude, via Claude Code):
- Most C# gameplay, netcode, UI and shader code, the Cloudflare Worker broker, and the build and deploy scripts were written by Claude under the developer's direction, then tested and tuned by the developer.
- The 3D models (fish parts, boat, gull, props) were made procedurally in Blender by Python scripts that Claude wrote. No AI image or 3D generators were used.
- The music and sound effects are synthesized by a Python script Claude wrote (music box, strings, formant choir, bubbles, splashes); no AI audio generators and no samples.
- The in-game text (cult voice lines, tips, names) was drafted by Claude and edited by the developer.
- The game design and art direction are the developer's.
