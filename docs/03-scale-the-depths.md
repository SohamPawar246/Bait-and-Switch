# Reference study: Scale the Depths

**What it is.** A single-player fishing game by Glass Gecko Games: a tiny robot on a boat at Loch Ness catches fish, scales them on a cutting board, serves them to non-human customers (otters, herons, Nessie, a Kelpie), and spends the money on a better rod, hook, knife and bucket to fish deeper. It began as an entry to the GMTK Game Jam 2024 (theme "Built to Scale") and grew into a full Steam release in 2026.

**Why it matters to us.** It proves a jam game built around **one tactile fishing verb**, executed precisely and with charm, can win people over. It is also the closest existing game to our Angler controls.

### How its fishing works

- The line descends on its own; you steer the hook left and right (mouse-follow while holding the button, or A/D).
- For a few seconds underwater you thread the hook through a maze of rocks and plants and ram fish to damage them until they're caught.
- Unwanted catches clutter a limited bucket, so you aim, not spray.
- The music grows more layered the longer the hook stays underwater.

### What reviewers loved, and what they didn't

| Loved | Criticised |
| --- | --- |
| Satisfying, tactile fishing and processing | Hook controls feel fiddly (precise mouse mode turns slowly) |
| Charming, well-designed creatures and customers | The loop gets repetitive after a few hours |
| Music that builds while you fish | Parasite removal felt finicky and frustrating |
| Hidden secrets and lore, more than expected | Upgrades reset when moving to new areas, undermining progress |
| A tight loop where each part feeds the next | Thin tutorial early on |

### What we adopt

| Their idea | Our version | Where it goes |
| --- | --- | --- |
| Steering a hook through rocks and plants | **Terrain with cover:** rock arches block hooks; kelp forests slow hooks by half and partly hide fish inside them. Fish get places to hide; Anglers get a skill route to thread | School and hiding, balance sheet |
| Music that builds while the hook is down | **Tension layer:** on each Angler's client, a percussion layer builds while their hook is in the water and resets when they reel up; on top of the day-to-night score | Screens, UI and audio |
| Customers with hidden favourite fish | **The Deep's Appetite:** each round the Deep names a craving from the parts kit, e.g. "Tonight the Deep craves: tentacles". Anglers +5 for landing a player fish with that part; fish with that part +5 if they survive the round | Round start, scoring |
| A tactile processing minigame | **The Robing (stretch):** when a player fish is landed, the Angler drags the raincoat down over the flopping fish (about 2 s) while the fish wriggles; finishing fast gives +1. Pure flavour, never blocks play | Getting caught |
| Lore and secrets beyond the fishing | **Scraps of cult lore** in loading tips, ledger footers and Vestry props ("Rule 7: the Deep is never full") | Art direction copy |
| One tactile verb done well | Our verbs are the hook flick and the tug; they get the most tuning time | Build plan (prototype tonight) |

### What we avoid

- **Fiddly hook controls:** mouse-follow by default with a tunable spring, tested in tonight's prototype before anything else is built.
- **Finicky minigames:** the tug shows both meters clearly and resolves within about 10 seconds.
- **Repetition:** short rounds, role swaps, the Deep's Appetite and customization keep every round different.
- **A thin tutorial:** round intro cards, the blend meter, and loading tips teach as you play.
- **Long upgrade grinds:** none. Every judge gets the full game in their first match.

Style note: Scale the Depths is cozy pixel art; we borrow its mechanics and feel, not its look or tone.

Sources: [Steam store text (via Gamesplanet)](https://us.gamesplanet.com/lenovo/legion/game/scale-the-depths-steam-key--8431-1), [jam version on itch.io](https://serpexnessie.itch.io/scale-the-depths), [KeenGamer preview](https://www.keengamer.com/articles/previews/scale-the-depths-preview-catch-of-the-day/), [Bonus Action review](https://bonus-action.com/review/scale-the-depths-review-the-most-dangerous-loop-right-now/), [Tech-Gaming review](https://www.tech-gaming.com/scale-the-depths/), [Thumb Culture review](https://www.thumbculture.co.uk/scale-the-depths-pc-review/amp), [Vaporlens review summary](https://vaporlens.app/app/3198890/scale_the_depths).
