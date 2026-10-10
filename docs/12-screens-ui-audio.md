# Screens, UI and audio

UI style: cards on aged parchment with dark ink outlines and wax-seal stamps, a gothic display font (Pirata One or IM Fell English SC, both free under the OFL) paired with a plain, readable body font, and part icons rendered from the Blender kit. All text follows the copy table in Art direction. Keep the in-game HUD minimal: the sea is the show.

### Screens

| Screen | Contents |
| --- | --- |
| Title | Logo over a live underwater scene with the school swimming; Play, Settings, Quit |
| Workbench (boot) | Customization (see above) + server status: "Summoning an Edgegap server near you…", then the server's city; **Dive in** when ready |
| Round intro | 5 s card: "You are a FISH. Hide. Eat. Survive." or "You are the ANGLER. Find the real fish." plus a one-line control reminder |
| Fish HUD | Dash and ink cooldown icons, growth pips (0 to 6), blend-meter eye, nibble ring at the bait, round clock with sun/moon icon, "Fish left: 4" |
| Angler HUD | Strike and sonar cooldowns, bait icon on your hook (full or empty), catches this round, round clock, fish left |
| Tug overlay | Tension on the line itself, tension bar by the boat, stamina bubbles over the fish; visible to everyone |
| Conversion | 3 s conversion ritual: the raincoat is pulled on, the eyes glow, "YOU HAVE BEEN SAVED." |
| Leviathan alert | Violet screen tint, "A LEVIATHAN RISES" for 2 s |
| Round ledger | 10 s card with each player's catches, escapes and points |
| Match results | Winner, titles, Play again |
| Server panel | Small corner tag: "Edgegap · Mumbai, India · 38 ms" (from the deployment's location variable) |
| Connection | "Reconnecting…" and "Moving to a fresh server…" with automatic retry; a clear error with a Retry button if the broker can't start a server |
| Settings | Master, music and effects volume; WASD or mouse-follow swimming; fullscreen |

### Main menu: a dithered sea

Inspired by the technique in "Sinking into dither" by ya\_demidov on Instagram: a top-down sea rendered as an ordered-dither halftone, shifting from day to night. We borrow the **technique**, not the scene. To use that video itself, you'd need the creator's permission and a credit; everything below is original and built from our own kit.

&#91;embedded content: Main menu layout · 16:9, rule of thirds\]

**The scene (original, from our lore).** Looking straight down at the water: the Angler's **empty dinghy** drifts with its lantern still lit, and a sou'wester hat floats beside it, the aftermath of a Leviathan. Ripples ring outward from the boat. Under the surface, shadows of the school slowly circle, and every 20 to 30 seconds a huge eye opens beneath the boat for a few seconds, then closes. The light cycles day → dusk → night over about 45 seconds, previewing the round's phases.

**How it's built (live in Unity, not a video).**

- An overhead orthographic camera over a water plane. A ripple shader draws concentric sine rings from the boat's position plus slow noise; the boat bobs, so the rings breathe.
- The school shadows are the real fish meshes rendered as dark silhouettes under the water plane, steered by the same boids code.
- A full-screen **ordered-dither pass** (Bayer 8×8 threshold, a 3- or 4-colour ramp per phase from our palette, adjustable pixel size) via a URP Full Screen Pass Renderer Feature. This single shader is what gives the halftone look.
- Day ramp: Abyss, Midwater, Shallows, Cream. Dusk: Abyss, Hook red, Raincoat. Night: Ink, Abyss, Glow mint for the lantern.
- It's the same scene, shader and assets as the game, so it costs about 2 hours, not an asset pipeline.

**Placement: the rule of thirds.** The subject sits right of centre; the **left third stays calm open water** for the UI. Ripple rings never cross into the menu column, so the text always sits on quiet texture.

**Buttons that belong to the picture.**

- Logo top-left in the gothic display font, cream, with the ampersand drawn as a fishhook. The tagline sits under it in the body font.
- A vertical stack, left-aligned: **Dive in** (primary), Workbench, Settings, Leave the shore (quit). No boxes by default: just cream text on a darker, slightly dithered band (Abyss at about 60% opacity, dithered with the same Bayer pattern so it reads as part of the image, not a UI overlay). Keep text contrast at 4.5:1 or better in every phase.
- **Hover:** a small hook icon slides in beside the label, the label sinks 2 px, and a ripple spreads from the menu edge across the water.
- **Click:** a splash sound, and the screen dissolves through the dither threshold into the workbench (animate the threshold; it's a free, beautiful transition).
- Bottom-left: the Edgegap status line in small type. Bottom-right: the jam credit. The mouse cursor is a small hook.
- Full keyboard and controller navigation; the focused item gets the same hook marker as hover.

**Sound:** water lapping against the hull, the lantern creaking on its hook, a distant bell buoy; at night, the faint choir.

### Cursors that belong to your faction

The cursor is a small character in its own right and changes with the faction you're looking at or playing.

| Context | Cursor | Idle | Hover | Click / select |
| --- | --- | --- | --- | --- |
| Main menu, before choosing | Cream fishhook, hotspot at the barb, hanging on a thin line that runs up to the top of the screen | Sways gently with mouse speed, like a weighted hook | Bobs once, line tightens | Hook "bites" (tilts 15° and back), a small ring of sparkles |
| Hatchery tab, playing a fish | A small tentacle tip with a sucker, hotspot at the tip, tinted your fish's skin colour | Slowly curls and uncurls; a tiny bubble every few seconds | Curls toward the item | Snaps into a tight curl, sparkles in your glow colour |
| Vestry tab, playing an Angler | The hook again, now with a tiny lit lantern charm, tinted your raincoat colour | Swings slightly behind fast movement | A glint runs along the hook | Bites, with wax-gold sparkles |
| In a round as an Angler | A thin aiming ring (the hook itself is in the water) | Pulses softly | — | Tightens on strike |
| In a round as a fish | The tentacle, shown only in mouse-follow swim mode or over UI | — | — | — |

**Switching faction is a tiny show.** Over 0.25 s the hook straightens and curls into the tentacle (or back), with a burst of 8 to 12 four-pointed sparkles that drift up and fade over 0.4 s, and a sound: a wet "plip" into Fish, a metal "clink" into Angler.

**How:** a software cursor (hide the system cursor; draw an Image on a top overlay canvas updated in `LateUpdate`), so it can animate and emit sparkles. Sparkles are a small pool of UI images animated with DOTween. A settings toggle returns to the normal system cursor for anyone who prefers it.

### Boot loading: the line goes down

The server wait lives in the workbench's bottom bar as a fishing line descending through four depth marks. Each mark matches a real step reported by the Cloudflare Worker, so progress is honest rather than a fake bar.

| Depth mark | Real event | Text |
| --- | --- | --- |
| 1 | `/join` sent | "Calling the Deep…" |
| 2 | Deployment starting; location known | "A vessel stirs near Mumbai…" |
| 3 | Deployment Ready; client retrying the connection | "Lighting the lanterns…" |
| 4 | Connected to the server | "The sea is open." |

Between marks the hook sinks slowly and bubbles rise, so it never looks frozen. On connect, the line jerks like a bite, sparkles burst at the hook, and **Dive in** starts pulsing. If a step takes much longer than usual, the text shifts to "The Deep is slow tonight…" and, after a timeout, to a Retry button. In the Hatchery the same line hangs through the tank; in the Vestry it hangs off the shrine's edge.

### When other players join

- **On the workbench:** each new player's creature appears on the congregation shelf. In the Hatchery it drops into a jar with a splash; in the Vestry a candle lights above a new pew and a hooded figure fades in. A bell tolls once, and a line appears: "\<name> has entered the water." Bots arrive the same way with their cult titles.
- **During a round:** a single ripple crosses the surface and the feed says "Something new swims among you." It never says which fish, so joining never breaks anyone's hiding.

### Transitions between every step

Three reusable effects carry every transition: the **dither dissolve** (animating the dither shader's threshold), the **camera dive** (the camera moves through the waterline), and the **parchment card**. Each lasts 1.5 s or less, is skippable with a click after the first time, and covers asynchronous scene loading, so the player never sees a frozen frame.

| From → to | Transition | Sound |
| --- | --- | --- |
| Launch → main menu | Black resolves into the dithered sea, pixel noise settling into water | Distant bell buoy |
| Main menu → workbench | Dither dissolve, then the camera dives from the overhead view down through the waterline into the Hatchery | Splash, then muffled underwater hum |
| Hatchery ↔ Vestry | Camera swings through the waterline | Muffled-to-clear sweep |
| Workbench → round (Dive in) | Wax seal stamp; the egg splits or the candles flare; the camera sinks into darkness while the round loads | Heavy thud, then a falling rumble |
| Round intro → play | The role card holds 3 s, then breaks into bubbles that rise and reveal the sea; a light ray sweeps across | Bubble rush |
| Day → dusk → night | Colour grading slides over 3 s; one line of subtitle ("The light leaves. Something else arrives.") | Music crossfade |
| Round end → ledger | A 0.3 s freeze, the image drains into dither, the ledger page slides up like a book opening | Quill scratch |
| Ledger → next round | Page turns, dither dissolve, the camera dives again | Page rustle, splash |
| Match end → results → Play again | Winner's page gets a wax seal; Play again dissolves straight back to the workbench for a quick rematch | Choir sting, thud |
| Server rotation or reconnect | Water rises to fill the screen; the descending-line loader returns with "Moving to a fresh server…" | Rising water, then the boot sounds |

Build these as one `TransitionManager` with the three effects and a queue, so every screen change goes through it and none is ever a hard cut. Budget about 2 hours in the Sunday juice block; the dither dissolve alone, used everywhere, already makes the game feel finished.

### Music

One track per light phase, crossfading with the round clock:

- **Day:** a slightly detuned music box over low strings.
- **Dusk:** the same tune, slower and a semitone flat.
- **Night:** sparse and eerie, with a low chanting choir when a Leviathan rises.

Sources: CC0 packs from Kenney, OpenGameArt and Freesound; CC-BY tracks (e.g. Kevin MacLeod) are fine with credit. List every source and licence in the credits.

### Sound effects

| Moment | Sound |
| --- | --- |
| Swim, dash | Soft bubble swish; sharp bubble burst |
| Nibble | Small munches, plus the line "tick" heard by the Angler (panned to the hook's position) |
| Strike | Whip-crack of the line; a splashy miss if nothing is there |
| Hooked | Twang + gasp |
| Tug | Rising whine pitched to tension, reel clicks, fish grunts |
| Snap / landed | Big twang and camera kick / splash and deck thud |
| Conversion | Raincoat rustle + a small cult "ding" |
| Sonar | Deep ping everyone hears |
| Leviathan | Roar, choir sting, wood creaking and a gulp as the boat goes under |

Underwater sounds go through a low-pass filter; surface sounds (gulls, creaking wood, rain at night) stay bright, so the two worlds feel different.
