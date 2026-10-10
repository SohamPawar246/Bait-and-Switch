# Playing as an Angler

Your job: find the real fish among 150 fakes and reel them in. Every catch adds a teammate.

### Controls

| Input | Action |
| --- | --- |
| A / D | Row your dinghy along the surface |
| Mouse | The hook is pulled toward the cursor on a weighted line; flick to swing it |
| Left click | **Strike**: a sharp upward jerk; any fish inside the catch radius is hooked |
| Hold left mouse (while a fish is hooked) | **Reel** during the tug-of-war |
| Hold right mouse | **Reel up and rebait** (needed after bait is eaten or a school fish is landed) |
| Q | **Sonar ping**: a ring from your boat briefly outlines every player-seat fish (humans and bot seats) within range; everyone hears the ping |
| Middle click | **Mark** a suspicious fish for your fellow Anglers (a small eye icon for 3 s) |

### The hook: a skill toy on a rope

The hook is a physics body hanging from your rod. A spring pulls it toward the cursor, water drag slows it, and gravity sinks it, so it swings and lags behind the mouse instead of teleporting. A good Angler flicks it in arcs, parks it gently in the school, and keeps it still while fish nibble. Hook type and line type (see Customization) change its weight, sink speed and catch radius.

The line is drawn as a sagging rope (a short verlet chain rendered with a line renderer) that **twitches** visibly, with a tick sound, every time something nibbles your bait.

### Striking

- Strike when the line twitches. Inside the catch radius: hooked. A school fish is landed automatically in 1 s for 1 point (your bait is used up). A player-seat fish starts the tug-of-war.
- A **missed strike** jerks the hook through the water, scatters the nearby school for 2 s and puts strike on a 1 s cooldown. Spamming strikes empties your area of fish.
- Bait is gone after any catch or a completed nibble; hold right mouse to reel up and rebait. Reeling takes longer the deeper the hook is, so deep fishing is a commitment.

### Reading the school

The Angler's real skill is spotting the fish that moves wrong:

- Sudden **dashes** (school fish never dash).
- A fish that **lingers** by the bait longer than school fish do.
- **Ink clouds** and the school scattering around them.
- A fish that's **bigger** than it should be (growth).
- Sonar for confirmation, and marks to share suspicions with other Anglers.

### Several Anglers

Every converted fish gets its own dinghy, so later in the round the surface fills with little boats. Boats bump softly; lines don't tangle. Anglers win together, but scores are individual, so there's a friendly race for the big catches. The **Leviathan** is the one thing that forces real teamwork: two hooks on it at once, then reel together.
