# Risks and mitigations

| Risk | Likelihood | Mitigation |
| --- | --- | --- |
| Hooking and dodging isn't fun | Medium | Offline prototype tonight; tune hook weight, catch radius and nibble time before any networking |
| Players are easy to spot in the school | Medium | One snapshot stream for all fish, shared parts kit, school pace matched; the blend meter teaches hiding; if still easy, raise school curiosity and add school "dart" moves |
| Anglers can never find anyone | Low to medium | Sonar, marks, the tells table; shorten night if it's too dark |
| The tug feels random | Medium | Both meters always visible; clear audio pitch; tune the per-second rates in `GameConfig` |
| Networking bugs late on Sunday | Medium | Online by 6:15 AM, redeploy often, sim written as plain C# structs that are easy to debug |
| Edgegap server dead during judging | High if ignored | Broker starts servers on demand, idle self-stop, rotation before 60 min, retry screens |
| Unity server cold start is slow | Medium | Workbench screen fills the wait; measure it tonight |
| Judges play alone | Certain | Bots fill all seats; bot takeover makes joining instant |
| Too much scope | High | Cut list above; never-cut list protects the core |
| Windows SmartScreen scares judges | Medium | First line of the itch page: "More info, then Run anyway" |
| Fatigue | High | Break at 9 AM; submit at 5:15 PM, not 5:59 |

### Submission notes to remember

- Disclose AI use: Claude for ideation, code, and the Blender scripts that generated the models.
- Credit every asset: Kenney (CC0), music and sound sources with licences, fonts (OFL).
- In the description, explain the Edgegap setup (on-demand servers near the player) and the one-stream netcode that makes players indistinguishable. Sponsor judges will notice both.
