# The tug-of-war

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
