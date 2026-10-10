# The broker: a Cloudflare Worker

One of the organizers suggested a Cloudflare Worker synced with Edgegap, so a server starts when a player joins. That is exactly the broker role in this design, and Cloudflare adds one real improvement: a **Durable Object** can act as a single gatekeeper, so two players pressing Play at once can never trigger two deploys. It replaces Vercel everywhere in this plan. If the organizer already has a working Worker, ask whether they'll share it; it could save an hour.

### Pieces

| Piece | Job |
| --- | --- |
| Worker `POST /join` | Called by the Unity client. Forwards to the Durable Object and returns either a server address or "wait" with a deployment ID |
| Worker `GET /status?id=` | Polled by the client every 2 s while waiting; answers from the Durable Object's state |
| Worker `POST /edgegap/webhook` | Receives Edgegap's ready, error and terminated webhooks (protected by a secret in the URL) |
| Worker `POST /server/closing` | Called by our game server just before an idle stop or rotation (shared secret), so the state flips to "stopping" immediately |
| Durable Object `SeaLobby` | One instance (`idFromName("main")`). Holds the single source of truth: `requestId`, `status` (none, starting, ready, stopping), `fqdn`, `port`, `location`, `updatedAt` |
| Secrets | `EDGEGAP_TOKEN` and `WEBHOOK_SECRET`, set with `wrangler secret put`; never in the client |

### How it stays synced with Edgegap

- **Webhooks first.** Every deploy request includes `webhook_on_ready`, `webhook_on_error` and `webhook_on_terminated` pointing at the Worker, so the lobby state updates the moment Edgegap changes.
- **Polling as a fallback.** Edgegap doesn't retry webhooks, so while the state is "starting" or "stopping", a Durable Object alarm checks `GET /v1/status/{id}` every 10 to 15 seconds until it settles.
- **Self-healing.** If a deploy call fails because a deployment already exists, the Durable Object lists deployments with the Edgegap API and adopts the one it finds.

### The /join logic (inside the Durable Object, so calls are processed one at a time)

1. Status **ready** → return the address and port.
2. Status **starting** → return "wait" and the deployment ID.
3. Status **stopping** → return "wait"; once the terminated webhook (or the alarm) confirms the old server is gone, the next call deploys a fresh one.
4. Status **none** → call `POST /v2/deployments` with the player's IP (from the `CF-Connecting-IP` header), the webhooks and a tag; set status to starting; return "wait".

### Free-tier fit

| Limit (Cloudflare free plan) | Value | Our use |
| --- | --- | --- |
| Worker requests | 100,000 per day | A join with about a minute of polling is around 30 requests, so thousands of joins a day fit |
| Worker CPU | 10 ms per request | Time spent waiting on Edgegap's API doesn't count; our logic is tiny |
| Durable Object requests | 100,000 per day | Same order as Worker requests |
| Durable Object storage | SQLite-backed only on the free plan | One small row of state |

Limits reset daily at 00:00 UTC.

### Setup outline

1. `npm create cloudflare@latest` (Worker, TypeScript).
2. Add the `SeaLobby` Durable Object class with a SQLite-backed migration in the wrangler config.
3. `wrangler secret put EDGEGAP_TOKEN` and `wrangler secret put WEBHOOK_SECRET`.
4. `wrangler deploy`; put the `workers.dev` URL in the Unity client's `EdgegapBroker`.
5. Test: no server → press Play twice from two clients at once → confirm exactly one deployment appears in the Edgegap dashboard.

The Unity client calls the Worker with `UnityWebRequest`; since it isn't a browser, no CORS setup is needed.
