# Bots

Bots fill every empty seat, so one judge alone gets a full match. They run on the server inside the same simulation, use the same inputs a human would, and **never cheat**: a bot Angler doesn't know which fish are players; it reads the same tells a human does.

### Fish bots

A small state machine per bot, with a random **greed** value (0.2 to 0.6) so bots differ:

| State | Behaviour | Leaves when |
| --- | --- | --- |
| Blend | Swims as a boid with the school, at school pace | Greed roll succeeds near a still, baited hook (within 6 units) |
| Forage | Approaches the bait at school pace, nibbles; aborts if the hook moves or tension appears; 30% chance to abort at 0.4 s | Bait eaten, aborted, or hooked |
| Flee | Dashes away from a strike or fast hook within 2 units; inks if a hook keeps following | 2 s pass safely |
| Tug | Pulls about 1 s, rests about 0.7 s; dashes when the Angler's tension passes 60 and dash is ready | Landed or escaped |
| Leviathan | Heads for the nearest boat and bites | Boat dragged, landed, or round ends |

### Angler bots

- **Positioning:** row toward the densest part of the school (sample a coarse grid), park the hook inside it at a random depth, keep still 2 to 4 s, then drift somewhere new.
- **Reading tells:** remember any fish that dashed, inked, or is visibly larger in the last few seconds, and prefer parking the hook near them.
- **Striking:** react to a twitch after 250 to 450 ms; strike if the nibble has lasted over 0.35 s (or a 30% random gamble); miss on purpose about 25% of the time so a skilled human fish can survive.
- **Sonar:** use it whenever it's ready and at least two fish remain.
- **Tug:** reel while tension is under 65, release above 80, reel hard while the fish is exhausted.
- **Leviathan:** drop everything to hook it, especially if another Angler's hook is already in it.
- Rebait whenever the hook is empty.

### Presentation

- Angler bots show a small "BOT" tag over their boat. Fish bots show nothing in the water (that's the point), but the ledger lists them as bots.
- Bot Anglers take cult titles (Brother Haddock, Sister Brine, Deacon Krill, Mother Marrow); bot fish appear in the ledger as numbered offerings ("Fish No. 48").
- Tune bots to be slightly worse than an average human: the goal is a lively sea, not a hard one.
