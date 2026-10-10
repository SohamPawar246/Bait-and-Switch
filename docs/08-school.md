# The school and hiding

The school is what makes hiding possible, so it must be impossible to tell a player from a school fish by looks alone, and only possible by **behaviour**.

### Rules that keep players indistinguishable

1. **Same parts.** School fish are assembled from exactly the same parts kit and palette as players, with every part in use somewhere in the school.
2. **Same pipeline.** Every fish, school or player, is drawn from one server snapshot stream with the same interpolation. Only your own fish is predicted locally on your screen.
3. **Same speed range.** School fish cruise at about 2.5 units/s; players can swim faster (about 4). Swimming at school pace is the skill; going faster is visible.
4. **Hidden IDs.** Fish IDs are shuffled each round; no client is told which IDs belong to players except its own.

### School behaviour (server boids)

- **Flocking:** separation, alignment and cohesion with nearby fish, a cruise speed and a turn-rate limit so motion looks smooth and fishy.
- **Depth preference:** mostly midwater; the school drifts a little shallower at night.
- **Avoid moving hooks:** steer away from a hook moving faster than 1 unit/s within about 1.2 units. Still hooks are ignored.
- **Curiosity:** now and then a school fish near a still, baited hook swims over and nibbles for 0.3 to 0.55 s, then leaves. About 10% of the time it finishes the nibble and eats the bait. This creates the decoy twitches that make Anglers doubt.
- **Scatter:** a strike within 3 units, an ink cloud, a dashing fish nearby or a Leviathan within 4 units makes nearby school fish burst away for 2 s.
- **Population:** 150, constant. A landed school fish respawns from the side of the screen after 5 s.
- Use a simple spatial grid for neighbour lookups; 150 fish at 20 Hz is light work for one server core.

### Terrain: cover for fish, routes for hooks

- **Rock arches and outcrops** block hooks completely; the line bends around them. Fish can slip through gaps a hook can't follow.
- **Kelp forests** slow any hook inside them to half speed and partly hide the fish swimming in them (drawn behind the front fronds).
- Keep it to 3 or 4 features in the one arena, so the whole sea stays readable on the fixed camera.

### The tells (what good Anglers learn)

| Tell | Why it gives a player away |
| --- | --- |
| A nibble longer than 0.55 s | School fish almost never nibble that long |
| Dashing | School fish never dash |
| Swimming against the school | Players chasing bait break formation |
| Size | Growth makes a fish bigger than any school fish |
| Ink clouds and scatter | Only players use ink |

### Teaching aid: the blend meter

A small eye icon near the fish player's HUD shows how natural they look right now: open and red when they're faster than the school, dashing or isolated; half-closed and green when they match their neighbours' speed and heading. It teaches the hiding skill in the first round without a tutorial, and only the fish player sees it.
