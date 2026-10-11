// BAIT & SWITCH broker: one Cloudflare Worker + one Durable Object (SeaLobby).
// The Durable Object is the single source of truth for "is there a game server, and where".
// Every /join goes through it, so two players pressing Play at once can never start two deployments.
//
// Routes
//   POST /join                              -> { state: "ready", host, port, location, requestId }
//                                             | { state: "wait", stage, requestId?, location? }
//                                             | { state: "error", message }
//   GET  /status                            -> same shape as /join, never starts a server
//   POST /edgegap/webhook/:kind/:secret     -> Edgegap webhooks (kind = ready | error | terminated)
//   POST /server/closing                    -> called by our game server right before it self-stops
//                                             (header X-Server-Secret = SERVER_SECRET)
//   GET  /health                            -> "ok"
//
// Secrets (wrangler secret put): EDGEGAP_TOKEN, WEBHOOK_SECRET, SERVER_SECRET
// Vars (wrangler.jsonc):          EDGEGAP_APP, EDGEGAP_VERSION

import { DurableObject } from "cloudflare:workers";

export interface Env {
  SEA_LOBBY: DurableObjectNamespace<SeaLobby>;
  EDGEGAP_TOKEN: string;
  WEBHOOK_SECRET: string;
  SERVER_SECRET: string;
  EDGEGAP_APP: string;
  EDGEGAP_VERSION: string;
}

type Status = "none" | "starting" | "ready" | "stopping";

interface Lobby {
  status: Status;
  requestId: string | null;
  host: string | null;
  port: number | null;
  webPort?: number | null; // HTTPS port serving the WebGL game (TLS added by Edgegap)
  wsPort?: number | null;  // WSS port the browser client connects to
  location: string | null;
  since: number; // ms timestamp when the current status began
  lastError: string | null;
}

const EMPTY: Lobby = {
  status: "none",
  requestId: null,
  host: null,
  port: null,
  location: null,
  since: 0,
  lastError: null,
};

const EDGEGAP_API = "https://api.edgegap.com";

// shown while a sea wakes up for a browser player; refreshes itself until /play can redirect
const wakingPage = (error: string | null, location: string | null) =>
  new Response(
    `<!doctype html><html><head><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1">
<title>Bait &amp; Switch</title>${error ? "" : '<meta http-equiv="refresh" content="4">'}
<style>body{margin:0;height:100vh;display:flex;align-items:center;justify-content:center;background:#0b1620;color:#f4e9d8;
font-family:Georgia,serif;text-align:center}h1{font-size:3em;margin:0 0 .3em}p{opacity:.8;font-size:1.2em}</style></head>
<body><div><h1>Bait &amp; Switch</h1><p>${error ? error + " <a style='color:#f2c14e' href='/play'>Try again</a>" :
      "Calling the Deep" + (location ? " near " + location : "") + "&hellip; the sea opens in a moment."}</p></div></body></html>`,
    { headers: { "content-type": "text/html; charset=utf-8", "cache-control": "no-store" } },
  );
const POLL_STARTING_MS = 12_000;   // fallback poll while a server is starting or stopping
const POLL_READY_MS = 60_000;      // slow heartbeat while ready, to notice silent terminations
const START_TIMEOUT_MS = 5 * 60_000;
const STOP_TIMEOUT_MS = 3 * 60_000;

// browsers on other origins (the itch.io web build) may call /join and /status
const CORS = { "access-control-allow-origin": "*", "access-control-allow-methods": "GET, POST, OPTIONS", "access-control-allow-headers": "content-type" };

const json = (body: unknown, status = 200) =>
  new Response(JSON.stringify(body), {
    status,
    headers: { "content-type": "application/json", "cache-control": "no-store", ...CORS },
  });

// ---------------------------------------------------------------- Worker entry

export default {
  async fetch(req: Request, env: Env): Promise<Response> {
    const url = new URL(req.url);
    const lobby = env.SEA_LOBBY.get(env.SEA_LOBBY.idFromName("main"));
    const parts = url.pathname.split("/").filter(Boolean);

    try {
      if (req.method === "OPTIONS") return new Response(null, { status: 204, headers: CORS });
      if (req.method === "GET" && url.pathname === "/health") return new Response("ok", { headers: CORS });

      if (req.method === "POST" && url.pathname === "/join") {
        const ip = req.headers.get("CF-Connecting-IP") ?? "";
        return json(await lobby.join(ip, url.origin));
      }

      if (req.method === "GET" && url.pathname === "/status") {
        return json(await lobby.view());
      }

      // the permanent "play in your browser" link: wakes a sea, then sends you to its own https page on Edgegap
      if (req.method === "GET" && (url.pathname === "/play" || url.pathname === "/")) {
        const ip = req.headers.get("CF-Connecting-IP") ?? "";
        const r = (await lobby.join(ip, url.origin)) as { state: string; host?: string; webPort?: number; wsPort?: number; message?: string; location?: string };
        if (r.state === "ready" && r.host && r.webPort) {
          return Response.redirect(`https://${r.host}:${r.webPort}/?ws=${r.wsPort ?? ""}`, 302);
        }
        return wakingPage(r.state === "error" ? r.message ?? "The Deep did not answer." : null, r.location ?? null);
      }

      if (req.method === "POST" && parts[0] === "edgegap" && parts[1] === "webhook") {
        const kind = parts[2];
        const secret = parts[3];
        if (!secret || secret !== env.WEBHOOK_SECRET) return json({ error: "forbidden" }, 403);
        const payload = (await req.json().catch(() => ({}))) as Record<string, unknown>;
        await lobby.webhook(kind, payload);
        return json({ ok: true });
      }

      if (req.method === "POST" && url.pathname === "/server/closing") {
        if (req.headers.get("X-Server-Secret") !== env.SERVER_SECRET) return json({ error: "forbidden" }, 403);
        const body = (await req.json().catch(() => ({}))) as { requestId?: string };
        await lobby.closing(body.requestId ?? null);
        return json({ ok: true });
      }

      return json({ error: "not found" }, 404);
    } catch (e) {
      return json({ state: "error", message: String(e) }, 500);
    }
  },
};

// ---------------------------------------------------------------- Durable Object

export class SeaLobby extends DurableObject<Env> {
  private lobby: Lobby = { ...EMPTY };
  private loaded = false;

  private async load() {
    if (this.loaded) return;
    this.lobby = (await this.ctx.storage.get<Lobby>("lobby")) ?? { ...EMPTY };
    this.loaded = true;
  }

  private async save(patch: Partial<Lobby>) {
    const statusChanged = patch.status !== undefined && patch.status !== this.lobby.status;
    this.lobby = { ...this.lobby, ...patch, since: statusChanged ? Date.now() : this.lobby.since };
    await this.ctx.storage.put("lobby", this.lobby);
    await this.schedule();
  }

  private async schedule() {
    const s = this.lobby.status;
    if (s === "starting" || s === "stopping") await this.ctx.storage.setAlarm(Date.now() + POLL_STARTING_MS);
    else if (s === "ready") await this.ctx.storage.setAlarm(Date.now() + POLL_READY_MS);
    else await this.ctx.storage.deleteAlarm();
  }

  private reply() {
    const l = this.lobby;
    if (l.status === "ready" && l.host && l.port) {
      return { state: "ready", host: l.host, port: l.port, webPort: l.webPort ?? null, wsPort: l.wsPort ?? null, location: l.location, requestId: l.requestId };
    }
    if (l.status === "none" && l.lastError) {
      return { state: "error", message: l.lastError };
    }
    return { state: "wait", stage: l.status, requestId: l.requestId, location: l.location };
  }

  // ---- RPC methods (calls to a Durable Object are processed one at a time)

  async view() {
    await this.load();
    return this.reply();
  }

  async join(ip: string, origin: string) {
    await this.load();
    if (this.lobby.status === "none") await this.deploy(ip, origin);
    else if (this.lobby.status === "ready" && this.lobby.requestId && !this.lobby.webPort) {
      // the ready webhook may not list every port: fetch the browser ports once
      const st = await this.edgegapStatus(this.lobby.requestId);
      if (st && st.webPort) await this.save({ webPort: st.webPort, wsPort: st.wsPort });
    }
    return this.reply();
  }

  async closing(requestId: string | null) {
    await this.load();
    if (!requestId || requestId === this.lobby.requestId) {
      await this.save({ status: "stopping", host: null, port: null });
    }
  }

  async webhook(kind: string, p: Record<string, unknown>) {
    await this.load();
    const id = typeof p.request_id === "string" ? p.request_id : null;
    if (id && this.lobby.requestId && id !== this.lobby.requestId) return; // stale deployment

    if (kind === "ready") {
      const ports = p.ports as Record<string, { external?: number }> | undefined;
      const loc = p.location as { city?: string; country?: string } | undefined;
      await this.save({
        status: "ready",
        host: (p.fqdn as string) ?? null,
        port: ports?.gameport?.external ?? null,
        webPort: ports?.web?.external ?? null,
        wsPort: ports?.ws?.external ?? null,
        location: loc ? [loc.city, loc.country].filter(Boolean).join(", ") : null,
        lastError: null,
      });
    } else if (kind === "error") {
      await this.save({ ...EMPTY, lastError: "The Deep is busy. Try again." });
    } else if (kind === "terminated") {
      await this.save({ ...EMPTY });
    }
  }

  // ---- Fallback polling (Edgegap doesn't retry webhooks)

  async alarm() {
    await this.load();
    const l = this.lobby;
    if (!l.requestId) {
      await this.save({ ...EMPTY });
      return;
    }

    const st = await this.edgegapStatus(l.requestId);

    if (st === null) {
      // 404 or removed: the deployment is gone.
      await this.save({ ...EMPTY });
      return;
    }

    if (st.running && st.host && st.port) {
      if (l.status === "starting") {
        await this.save({ status: "ready", host: st.host, port: st.port, webPort: st.webPort, wsPort: st.wsPort, location: st.location });
        return;
      }
    } else if (st.error) {
      await this.save({ ...EMPTY, lastError: "The Deep is busy. Try again." });
      return;
    }

    const age = Date.now() - l.since;
    if (l.status === "starting" && age > START_TIMEOUT_MS) {
      await this.stopDeployment(l.requestId);
      await this.save({ ...EMPTY, lastError: "The Deep did not answer. Try again." });
      return;
    }
    if (l.status === "stopping" && age > STOP_TIMEOUT_MS) {
      await this.save({ ...EMPTY });
      return;
    }
    await this.schedule();
  }

  // ---- Edgegap API

  private auth() {
    if (!this.env.EDGEGAP_TOKEN) throw new Error("EDGEGAP_TOKEN is not set (wrangler secret put EDGEGAP_TOKEN)");
    const t = this.env.EDGEGAP_TOKEN.trim();
    return t.toLowerCase().startsWith("token ") ? t : `token ${t}`;
  }

  private webhookUrl(origin: string, kind: string) {
    return `${origin}/edgegap/webhook/${kind}/${this.env.WEBHOOK_SECRET}`;
  }

  private async deploy(ip: string, origin: string) {
    const body: Record<string, unknown> = {
      application: this.env.EDGEGAP_APP,
      version: this.env.EDGEGAP_VERSION,
      // Edgegap places the server near these users. Check the exact user_data shape in
      // docs.edgegap.com/docs/api/dedicated-servers.md if the API rejects it.
      users: ip ? [{ user_type: "ip_address", user_data: { ip_address: ip } }] : [{ user_type: "geo_coordinates", user_data: { latitude: 18.52, longitude: 73.86 } }],
      tags: ["bait-and-switch"],
      webhook_on_ready: { url: this.webhookUrl(origin, "ready") },
      webhook_on_error: { url: this.webhookUrl(origin, "error") },
      webhook_on_terminated: { url: this.webhookUrl(origin, "terminated") },
    };

    const res = await fetch(`${EDGEGAP_API}/v2/deployments`, {
      method: "POST",
      headers: { Authorization: this.auth(), "Content-Type": "application/json" },
      body: JSON.stringify(body),
    });

    if (res.ok) {
      const data = (await res.json()) as { request_id: string };
      await this.save({ status: "starting", requestId: data.request_id, host: null, port: null, location: null, lastError: null });
      return;
    }

    // Most likely the free tier's one-deployment limit: something is already running. Adopt it.
    const adopted = await this.adoptExisting();
    if (!adopted) {
      const text = await res.text().catch(() => "");
      console.log("deploy failed", res.status, text);
      await this.save({ ...EMPTY, lastError: "The Deep is busy. Try again." });
    }
  }

  private async adoptExisting(): Promise<boolean> {
    const query = JSON.stringify({
      filters: [{ field: "application", operator: "eq", value: this.env.EDGEGAP_APP }],
      order_by: [{ field: "created_at", order: "desc" }],
    });
    const res = await fetch(`${EDGEGAP_API}/v1/deployments?query=${encodeURIComponent(query)}`, {
      headers: { Authorization: this.auth() },
    });
    if (!res.ok) return false;
    const data = (await res.json()) as {
      data?: { request_id: string; fqdn: string; ready: boolean; ports?: Record<string, { external?: number }> }[];
    };
    const d = data.data?.[0];
    if (!d) return false;
    if (d.ready && d.ports?.gameport?.external) {
      await this.save({ status: "ready", requestId: d.request_id, host: d.fqdn, port: d.ports.gameport.external,
                        webPort: d.ports.web?.external ?? null, wsPort: d.ports.ws?.external ?? null, lastError: null });
    } else {
      await this.save({ status: "starting", requestId: d.request_id, host: null, port: null, lastError: null });
    }
    return true;
  }

  private async edgegapStatus(id: string): Promise<
    { running: boolean; error: boolean; host: string | null; port: number | null; webPort: number | null; wsPort: number | null; location: string | null } | null
  > {
    const res = await fetch(`${EDGEGAP_API}/v1/status/${id}`, { headers: { Authorization: this.auth() } });
    if (res.status === 404 || res.status === 410) return null;
    if (!res.ok) return { running: false, error: false, host: null, port: null, webPort: null, wsPort: null, location: null }; // transient: keep polling
    const s = (await res.json()) as {
      running?: boolean;
      error?: boolean;
      fqdn?: string;
      removal_time?: string | null;
      current_status?: string;
      ports?: Record<string, { external?: number }>;
      location?: { city?: string; country?: string };
    };
    const status = (s.current_status ?? "").toLowerCase();
    if (s.removal_time || status.includes("terminated") || status.includes("stopped")) return null;
    return {
      running: !!s.running,
      error: !!s.error,
      host: s.fqdn ?? null,
      port: s.ports?.gameport?.external ?? null,
      webPort: s.ports?.web?.external ?? null,
      wsPort: s.ports?.ws?.external ?? null,
      location: s.location ? [s.location.city, s.location.country].filter(Boolean).join(", ") : null,
    };
  }

  private async stopDeployment(id: string) {
    await fetch(`${EDGEGAP_API}/v1/stop/${id}`, { method: "DELETE", headers: { Authorization: this.auth() } }).catch(() => {});
  }
}
