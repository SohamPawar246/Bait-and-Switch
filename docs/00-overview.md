# Overview

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
