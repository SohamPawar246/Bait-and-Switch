# Match and round flow

&#91;embedded content: Match flow · 3 rounds, 3 light phases each\]

A match is three rounds. Each round starts with one Angler and five fish (humans first, bots fill the rest) and ends when the last fish is caught or the clock hits 120 seconds.

### Round start

- **Seats:** 6 per round. Humans take seats first; bots fill the rest.
- **Who starts as Angler:** a random human who hasn't started as Angler yet this match; if all humans have, a bot. With one human, the human is a fish in round 1, the Angler in round 2, a fish in round 3.
- **Roles are private.** Each client learns only its own role and its own fish's ID. Anglers never learn which fish are players.
- **Spawn:** fish spawn inside the school at random midwater points; the Angler's dinghy spawns at the surface centre.

**The Deep's Appetite.** During the round intro, the Deep names one part from the kit ("Tonight the Deep craves: tentacles"). Anglers earn +5 for landing a player fish with that part; fish wearing it earn +5 if they survive. It gives Anglers a lead to hunt and makes customization choices matter every round.

### Light phases

| Phase | Time | Light | What changes |
| --- | --- | --- | --- |
| Day | 0 to 50 s | Bright, caustics near the surface | Hooks clearly visible; bait worth 1 growth |
| Dusk | 50 to 80 s | Orange sun, longer shadows | Hooks dimmer; bait worth 1.5 growth; school gets restless |
| Night | 80 to 120 s | Dark except lanterns, glow bait, angler lamps, bioluminescent fins | Hooks only visible near light (or with extra eyes); bait worth 2 growth |

The escalation is deliberate: early rounds teach, the dark end of every round creates panic and comebacks.

### Round end

- **Anglers win the round** if every fish is converted before 120 s.
- **Surviving fish win the round** if any fish is still swimming at 120 s.
- Individual scores carry across the round regardless (see Scoring). Converted fish keep everything they earned as fish.

### Joining and leaving

- **Join mid-round:** you take over a bot fish's body on the spot, invisibly. Perfect for hiding, zero waiting for judges.
- **Leave mid-round:** a bot takes over your body (fish or Angler) so the round stays balanced.
- **Server rotation:** if the Edgegap deployment nears its 60-minute free-tier cap, the server finishes the current match, shows "Moving to a fresh server", and clients reconnect through the broker automatically.
