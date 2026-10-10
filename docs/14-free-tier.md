# Living with the free tier

The free tier allows **one deployment at a time, for at most 60 minutes**. So no server ever runs permanently: servers start when someone wants to play, everybody shares that one server, and it stops itself before the cap. Judges never see any of this except a one-minute wait on the workbench.

&#91;embedded content: Server lifecycle on the Edgegap free tier\]

### Step by step

1. **Play pressed.** The client calls the Cloudflare Worker broker's `/join`. The broker lists our deployments and takes one of three paths:
   - a server is **running**: return its address and port; the player joins in seconds;
   - a server is **starting** (or rotating): return its ID; the client polls `/status` every 2 s;
   - **no server**: deploy a new one, passing the player's IP so Edgegap places it near them, then poll.
2. **Starting (about a minute).** Container boot plus Unity loading. The player is on the workbench customizing, so the wait is play time. Once Ready, the client retries connecting every second until the server accepts.
3. **Running.** Everyone who presses Play while it's up joins the same sea: up to 6 humans, bots in the empty seats. A 7th human waits as a spectator for the next free seat (unlikely in a 14-entrant jam).
4. **Idle self-stop.** If no human has been connected for 3 minutes, the server stops itself through `ARBITRIUM_DELETE_URL`. Nothing runs, so no free hours are wasted overnight.
5. **Rotation before the cap.** A match takes about 7 minutes, so **no new match starts after 52 minutes** of uptime. When the current match ends, the server tells clients "Moving to a fresh server" and stops itself. Clients automatically call `/join` again; the broker waits for the old deployment to finish stopping, then starts a new one. Players spend about a minute back on the workbench, keeping their creature and their seat in the queue.
6. **Safety net.** If anything goes wrong and Edgegap hard-stops the server at 60 minutes, clients see the disconnect, show "Reconnecting…", and go through the broker exactly as in step 1.

### Edge cases

| Situation | What happens |
| --- | --- |
| Two players press Play at the same moment with no server | Both calls try to deploy; the second fails on the one-deployment limit. The broker treats that error as "already starting", re-lists, and returns the first deployment's ID |
| A judge in Europe joins a server started near India | They connect normally with higher ping; the floaty water movement and generous catch radius hide most of it. The next fresh server is placed near whoever starts it |
| Edgegap can't allocate a server (error 422) | The client shows "The Deep is busy, retrying…" and retries a few times, then offers a Retry button |
| The broker is unreachable | The client shows a clear error with Retry; Cloudflare Workers' free tier (100,000 requests a day) is far more than this needs |
| You test a dev build during judging week | **Don't.** Your test deployment counts as the one allowed deployment and would block judges. Test locally in Docker instead, or only when nobody is likely playing |

### What you need to do during judging week

Nothing, if the above works. Check the Edgegap dashboard once a day for error deployments and your compute-hour usage. The jam brief says the free tier includes enough compute hours for the jam; the exact allowance wasn't in the docs I read, so confirm it in your dashboard on Saturday. Idle self-stop keeps usage to the minutes people actually play.

### During development

- Most testing happens locally: host + client in the editor, or the server container in Docker Desktop. Deploy to Edgegap only for integration tests, and stop each test deployment when done.
- The free tier allows 2 app versions: keep `dev` and `prod`. Point a version at a new image tag by editing it in the dashboard; no rebuild needed.
- If the limits ever become a problem, Edgegap's pay-as-you-go tier removes the 60-minute cap and adds caching for near-instant starts. It isn't needed for the jam.
