# Customization

Every player builds **both** a fish and an Angler rig, since everyone plays both sides. Each gameplay part is a trade-off; colours and cosmetics are free choices. Choices persist between sessions (PlayerPrefs), and bots pick randomly.

### The workbench screen

Shown while the Edgegap server boots, so the wait becomes the most personal part of the game. The goal: **every click gets a visible, audible reaction from your creature**, and each faction's screen feels like a small ritual in its own place.

&#91;embedded content: Customization layout · shared by the Hatchery and the Vestry\]

### One scene, two places: the waterline between them

The workbench is one 3D diorama split by the waterline. **The Hatchery** (fish) sits below it: a dim, bioluminescent tank where your fish grows inside a translucent egg sac. **The Vestry** (Angler) sits above it: a candle-lit boathouse shrine with hooks on a pegboard, bait jars and spools on a shelf. Switching tabs swings the camera up or down **through the waterline** with a splash and a muffled-to-clear audio sweep.

Because every Angler is a fish in a raincoat, the Vestry shows **your own custom fish** standing in the robe. The two screens are the same creature, which makes both feel owned.

### Satisfaction rules (both factions)

1. **Instant, physical feedback.** A chosen part pops onto the creature with a squash-and-overshoot (about 0.15 s), a small particle burst (bubbles below, wax dust above) and a sound unique to that slot. Pitch varies a few percent so repeats never feel robotic.
2. **The creature tries out every part.** New fins: it swims a quick figure-of-eight. New eyes: they blink one by one and look at you. Tentacles: they curl and uncurl. Teeth: a chomp. New hook: the Angler lifts it to the candle and it glints, then a test cast whips across the shrine. New bait: it's dropped into a jar and wriggles. New line: the spool spins.
3. **Hover before you commit.** Hovering a part shows it ghosted on the creature, and the stat bars show green and red ticks for what would change. No surprises, no menus of numbers.
4. **It's alive the whole time.** Idle breathing, blinking, and its eyes follow your cursor. Drag to rotate, scroll to zoom.
5. **Colour feels like dye.** Picking a colour washes it over the body from head to tail in about 0.3 s, with a soft liquid sound.
6. **Randomize is a moment.** "Let the Deep decide" spins every slot like a slot machine, ticking, and lands with a choir sting. Hold it to keep rolling.
7. **Names with the game's voice.** A name field with a dice button that suggests deadpan names: "Doomed Gerald", "Kevin (Brine)", "Brother Unworthy".
8. **Nothing is lost.** Undo (Ctrl+Z), reset per slot, and choices saved between sessions.

### The stat card

No raw numbers: four bars on a parchment card that tick as they change.

| Faction | Bars |
| --- | --- |
| Fish ("Specimen card") | Speed, Stealth, Stamina, Bite |
| Angler ("Vows card") | Reach, Grip, Line strength, Lure |

### The confirm ritual

Pressing **Dive in** stamps a wax seal on the card with a heavy thud. Fish: the egg sac splits and your fish swims out of frame toward the sea. Angler: the candles flare and two eyes ignite inside the hood. Then the game loads. Two seconds, every time, and it never gets old.

### Waiting for the server, without feeling like waiting

- Server progress is **the descending fishing line (see "Boot loading: the line goes down" in Screens)**, with the server city printed when it's found.
- As other players connect, their creatures appear on a **congregation shelf** (jars below, pews above), so you can see who you'll play with. Social anticipation instead of a loading bar.
- When the server is ready, **Dive in** pulses gently. Nothing interrupts you; you go when you're done.

### Sound palette

| Faction | Part pop | Ambience |
| --- | --- | --- |
| Fish | Wet squelch, bubble pop, eye-blink "plip" | Low tank hum, distant whale-like groans |
| Angler | Wooden clack, metal hook clink, wax and fabric rustle | Rain on the roof, candle crackle, a faint choir |

### Build notes and scope

- A separate `Workbench` scene with both dioramas stacked vertically and a Cinemachine camera for each; switching blends between them.
- Reactions reuse `FishAnimator` with a "test" mode per part type; Angler reactions are short procedural motions (lift, glint, cast). DOTween (free) handles the pops and bars.
- Diorama props reuse the set-dressing kit: shrine stones, candles, the boat, plus a pegboard, jars and an egg sac.
- **Must have:** live preview, pops with sounds, hover previews, stat card, randomize, Dive in.
- **Nice to have:** per-part reactions, the waterline camera swing, the wax-seal ritual.
- **Stretch:** the congregation shelf and drag-to-dye.
- Budget about 3 hours inside Sunday's "Workbench and day-night" block; the must-haves fit in 90 minutes.

### Balance rules for every add-on

- Each gameplay option has **one clear advantage and one clear disadvantage**, on a different stat, so no option is strictly better.
- The first option in every slot is the **baseline**: no advantage, no disadvantage. Picking it is never a mistake.
- Caps stop stacking from breaking the game: total speed change is capped at −25% / +20%, total nibble range bonus at +0.6 units.
- The school and the bots pick every fish option in equal numbers, so no part is rare enough to give a player away.
- In playtests, track pick rates; anything chosen by more than 40% of players gets nerfed. Every number lives in its `PartDef`.

### Fish add-ons

Six gameplay slots plus cosmetics. Stat-card bars: **Speed** (top speed, acceleration, turning), **Stealth** (hitbox, tells, sonar), **Stamina** (tug endurance), **Bite** (nibble speed and range, Leviathan bite).

| Slot | Option | Advantage | Disadvantage |
| --- | --- | --- | --- |
| Body | Round | Baseline | — |
|  | Eel | +12% top speed | −20% stamina in tugs |
|  | Puffer | +25% stamina; puffs up when hooked | −10% top speed |
|  | Flatfish | Lying still on the seabed hides you from sonar | −15% acceleration; only works at the bottom |
|  | Plated | The first hook that catches you each round bounces off | −15% acceleration; sinks slowly when idle |
| Eyes | Two | Baseline (see hooks in the dark within 2 units) | — |
|  | Three | See hooks in the dark within 4 units | 5% bigger hitbox |
|  | Seven | See hooks in the dark within 6 units | 10% bigger hitbox |
|  | Cyclops | See every hook's catch radius as a faint ring | Night vision drops to 1 unit |
| Fins | Stubby | Baseline | — |
|  | Big fins | Dash goes 30% further | Dash cooldown +0.6 s |
|  | Wing fins | Turn 25% faster | −10% acceleration |
| Tail | Fan | Baseline | — |
|  | Fork | +12% acceleration | −15% stamina regeneration |
|  | Curl | +20% stamina regeneration | −10% acceleration |
|  | Thrasher | A dash during a tug adds +35 tension instead of +25 | Dash cooldown +0.4 s |
| Mouth | Small | Baseline | — |
|  | Needle teeth | Nibble 20% faster | Teeth flash while you nibble (a visible tell) |
|  | Big jaw | Nibble in 0.4 s instead of 0.6 s | Every nibble twitches the line twice as hard |
|  | Sucker | Nibble from 0.3 units further away | Nibble 15% slower |
| Special | None | Travel light: +5% top speed | — |
|  | Hammerhead | Leviathan bite takes 1.4 s; a dash into a hook knocks it 2 units away | Turn 15% slower |
|  | Tentacles | Nibble from 0.4 units further away | −8% top speed |
|  | Spines | +15% tension on every pull in a tug | Sonar outlines you twice as long |
|  | Angler lamp | Lights your surroundings at night (see hooks within 5 units) | You glow at night (so do lamp-wearing school fish) |
|  | Mimic skin | Every 10 s your colour shifts to match the nearest school fish | −15% stamina |
|  | Ink gland | Ink cooldown 8 s and a 30% bigger cloud | −10% stamina |
|  | Electric organ | Once per tug, dash to shock: the Angler can't reel for 1 s | −10% top speed; the sparks give you away |

**Fish cosmetics (free):** skin colour (4 skins plus tints), pattern (plain, stripes, spots, scars, freckles), night glow colour, eye colour.

### Angler add-ons

Five gameplay slots plus cosmetics. Stat-card bars: **Reach** (line length, sink speed, aim), **Grip** (catch radius, holding a fish), **Line strength** (snap threshold, reel speed), **Lure** (how strongly bait attracts).

| Slot | Option | Advantage | Disadvantage |
| --- | --- | --- | --- |
| Hook | J hook | Baseline (catch radius 0.6, snap at 100) | — |
|  | Spiky | Snags any fish it touches while moving fast | Snap threshold 80 |
|  | Barbed | Snap threshold 120 | Catch radius 0.45 |
|  | Big | Catch radius 0.8 | Sinks 30% slower; strike cooldown 1.3 s |
|  | Circle | A fish that finishes a full nibble hooks itself, no strike needed | Strike catch radius only 0.4 |
|  | Treble | Strike cooldown 0.7 s | Snap threshold 90 |
| Bait | Worm | Baseline | — |
|  | Glow orb | Lights 3 units around the hook at night; school more curious at night | Fish see your hook from far away |
|  | Chunky lure | School twice as curious | +1 extra growth for whoever eats it |
|  | Spinner | School fish ignore it, so only players nibble (no decoy twitches) | It flashes in daylight, and eating it gives +2 growth |
|  | Stink bait | Draws curious school fish from 10 units away | Constant decoy twitches to read through |
| Line | Mono | Baseline | — |
|  | Steel | Never snaps | Reel × 0.7; heavier swing |
|  | Silk | Reel × 1.3 | Snap threshold −15 |
|  | Ghost line | Invisible underwater; fish see only the hook and bait | Snap threshold −10 |
| Rod | Short | Baseline (max line 20 units) | — |
|  | Long | Max line 26 units | Hook follows the cursor 20% slower |
|  | Whip | Strike jerk 1.6 units (a bigger, faster strike) | Reel −10% |
|  | Heavy | Reel +20% | Hook spring −25% (sluggish aim) |
| Relic | None | Dinghy +15% speed | — |
|  | Sonar bell | Sonar cooldown 10 s | Sonar radius 9 units |
|  | Vigil lantern | Marks last 6 s and show through ink | At night your lantern lights a cone underwater that fish can see coming |
|  | Chum bucket | Press E: the school gathers at a spot for 6 s (20 s cooldown) | No sonar |
|  | Offering bowl | +1 extra point per school fish landed | Missed strikes scatter fish 50% wider |
|  | Deep weight | Hook sinks twice as fast | Reeling up to rebait is 30% slower |

**Angler cosmetics (free):** raincoat colour (6), hat (sou'wester, bucket, pointed hood, none), lantern flame colour, boat paint, rod finish, colour of the eyes inside the hood.

### Counters: picks that beat picks

These matchups give the customizer real decisions and give the ledger stories to tell.

| Matchup | Who has the edge, and why |
| --- | --- |
| Flatfish vs Sonar bell | Flatfish: lying on the seabed ignores sonar completely |
| Seven eyes or Angler lamp vs Glow orb at night | Fish: they see the hook long before it arrives |
| Electric organ vs Steel line | Fish: a shock is the only way to stall a line that can't snap |
| Spines or Thrasher vs Barbed hook | Fish: extra tension can push even a barbed line past 120 |
| Needle teeth or Big jaw vs Circle hook | Angler: a fast full nibble hooks you automatically |
| Plated vs Spiky hook | Fish: the first snag bounces off |
| Hammerhead vs Big hook | Fish: a dash knocks the slow-sinking hook out of the way |
| Any greedy fish vs Spinner | Angler: school fish never touch a spinner, so any nibble is a player |
| Mimic skin vs Chum bucket | Angler: chum packs the school tight, so a fish moving against the crowd stands out whatever its colour |

### Data and networking

- Each part is a `PartDef` ScriptableObject: ID, slot, prefab, thumbnail, display text, and stat modifiers.
- A full appearance packs into 21 bytes (one per slot: 10 fish, 11 Angler). The client sends it on join; the server validates it and includes it in the round-start appearance table for all fish, shuffled together with the school.
