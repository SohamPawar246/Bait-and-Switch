# Balance sheet

Starting values for `GameConfig`. They are educated first guesses, meant to be tuned in playtests; keep every one of them in the ScriptableObject, never hard-coded. Distances are in Unity units (a fish is about 0.9 units long).

### World and round

| Parameter | Value |
| --- | --- |
| Visible sea | 40 × 22.5 units; surface at y = 0, seabed at y = −20 |
| Fish base size / hitbox radius | 0.9 / 0.45 |
| Seats | 6 (1 Angler + 5 fish at start) |
| Rounds per match | 3 |
| Round intro / round / ledger | 5 s / 120 s / 10 s |
| Day / dusk / night | 0–50 s / 50–80 s / 80–120 s |

### Fish

| Parameter | Value |
| --- | --- |
| Acceleration / max speed / water drag | 25 u/s² / 4.0 u/s / 2.0 per s |
| Dash speed / duration / cooldown | 11 u/s / 0.25 s / 2.0 s |
| Ink radius / duration / cooldown | 2.5 u / 3 s / 12 s |
| Nibble range / nibble time | 0.7 u / 0.6 s |
| Growth per bait (day / dusk / night) | 1 / 1.5 / 2 |
| Size and speed per growth step | +15% size, −3% speed |
| Leviathan threshold / scale | 6 growth / 2.5× |
| Leviathan bite time / growth after a boat drag | 2.0 s / 3 |

### Angler and hook

| Parameter | Value |
| --- | --- |
| Dinghy speed | 3 u/s |
| Max line length | 20 u |
| Hook spring / drag / gravity | 30 / 4 per s / 6 u/s² |
| Strike jerk / catch radius / cooldown | 1.2 u in 0.15 s / 0.6 u / 1.0 s |
| Missed strike scatter | 3 u radius for 2 s |
| Reel-up speed / rebait time | 8 u/s / 0.5 s |
| School fish landing time | 1 s |
| Sonar radius / outline time / cooldown | 12 u / 0.5 s / 15 s |
| Mark duration / cooldown | 3 s / 5 s |

### Tug-of-war

| Parameter | Value |
| --- | --- |
| Tension: reel / fish pull / ease off / fish rests | +35 / +45 × P / −50 / −20 per s |
| Tension from a dash | +25 instant |
| Snap threshold | 100 (gear changes it) |
| Reel-in speed / line pay-out | 3.5 × (1 − 0.6 × P) / 1.5 × P u/s |
| Stamina drain / regen / exhausted time | 25 × P / 15 per s / 1.5 s |
| Line fatigue starts / rate / tear loose | 6 s / −8 threshold per s / 12 s |
| Leviathan stamina / single-hook break | 300 / 1 s |

### School

| Parameter | Value |
| --- | --- |
| Count / respawn delay | 150 / 5 s |
| Cruise speed | 2.5 u/s |
| Separation radius / neighbour radius | 0.8 u / 2.5 u |
| Avoid moving hooks | within 1.2 u when the hook moves faster than 1 u/s |
| Curiosity | 2% chance per s within 5 u of a still baited hook |
| School nibble length / chance to eat | 0.3–0.55 s / 10% |
| Scatter duration | 2 s |

### Bots and network

| Parameter | Value |
| --- | --- |
| Fish-bot greed | 0.2–0.6 (random per bot) |
| Angler-bot reaction / deliberate miss | 250–450 ms / 25% |
| Angler-bot tug: reel below / release above | 65 / 80 tension |
| Sim tick / snapshot / input / tug message | 30 / 15 / 30 / 10 Hz |
| Interpolation delay | 100 ms |
| Server idle self-stop / rotation | 180 s with no humans / no new match after 52 min uptime, stop after that match |
