# Broker setup (about 10 minutes)

```bash
cd worker
npm install
npx wrangler login
npx wrangler secret put EDGEGAP_TOKEN    # paste the Edgegap API token
npx wrangler secret put WEBHOOK_SECRET   # any long random string
npx wrangler secret put SERVER_SECRET    # another long random string (also set on the Edgegap app version, see below)
npm run check                            # typecheck
npm run deploy                           # prints https://bait-and-switch-broker.<you>.workers.dev
```

Put the workers.dev URL into `EdgegapBroker.brokerUrl` in Unity.

On the Edgegap app version (`prod` and `dev`), add two app-version environment variables
(mark them hidden): `BROKER_URL` = the workers.dev URL, `BROKER_SECRET` = the SERVER_SECRET value.
The game server's `EdgegapAgent` uses them to call `/server/closing` before it stops itself.

Switch which Edgegap version players get by editing `EDGEGAP_VERSION` in `wrangler.jsonc` and redeploying.

## Tests
1. `curl https://<worker>/status` → `{"state":"wait","stage":"none"...}`
2. Two terminals at once: `curl -X POST https://<worker>/join` → exactly ONE deployment appears in the Edgegap dashboard.
3. Keep polling `/status` until `"state":"ready"` with host and port.
4. Stop the deployment in the dashboard → within ~1 minute `/status` returns to stage `none`.
5. `npm run tail` shows live logs while testing.

The only unverified detail is the exact `users` payload shape for the v2 deploy API; if deploys fail with
400, check `docs.edgegap.com/docs/api/dedicated-servers.md` and adjust `deploy()` in `src/index.ts`.
