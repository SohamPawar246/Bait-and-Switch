#if !UNITY_WEBGL || UNITY_EDITOR
using System;
using System.IO;
using System.Net;
using System.Text;
using System.Threading;
using UnityEngine;

namespace BaitSwitch.Net
{
    /// <summary>
    /// Headless server only: serves the WebGL build (app/web next to the server binary) over plain HTTP on webPort.
    /// Edgegap's TLS-upgrade sidecar turns that into https://&lt;fqdn&gt;:&lt;external&gt;/, so the server's own Edgegap
    /// address is a link you can open and play in a browser. The page connects back over WSS (SimpleWebTransport on the
    /// "ws" port, also TLS-upgraded); /ports.json tells it which external port that is.
    /// </summary>
    public sealed class WebHost : MonoBehaviour
    {
        public int webPort = 8080;

        HttpListener _http;
        Thread _thread;
        string _root;
        volatile bool _run;

        public static void StartIfHeadless(GameObject host)
        {
            if (!Application.isBatchMode) return;
            string root = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "web"));
            if (!File.Exists(Path.Combine(root, "index.html"))) { Debug.Log("[Web] no WebGL build at " + root + "; browser play disabled"); return; }
            var w = host.GetComponent<WebHost>() ?? host.AddComponent<WebHost>();
            w.Begin(root);
        }

        void Begin(string root)
        {
            if (_run) return;
            _root = root;
            try
            {
                _http = new HttpListener();
                _http.Prefixes.Add($"http://*:{webPort}/");
                _http.Start();
            }
            catch (Exception e) { Debug.LogWarning("[Web] could not listen on " + webPort + ": " + e.Message); return; }
            _run = true;
            _thread = new Thread(Loop) { IsBackground = true, Name = "WebHost" };
            _thread.Start();
            Debug.Log($"[Web] serving {root} on :{webPort}");
        }

        void OnDestroy()
        {
            _run = false;
            try { _http?.Stop(); } catch { }
        }

        void Loop()
        {
            while (_run)
            {
                HttpListenerContext ctx;
                try { ctx = _http.GetContext(); }
                catch { if (!_run) return; continue; }
                ThreadPool.QueueUserWorkItem(_ => Serve(ctx));
            }
        }

        static string Env(string k) => Environment.GetEnvironmentVariable(k);

        /// <summary>The external port Edgegap mapped to our "ws" port (env per port, or the JSON mapping).</summary>
        static string WsPort()
        {
            string v = Env("ARBITRIUM_PORT_WS_EXTERNAL");
            if (!string.IsNullOrEmpty(v)) return v;
            string map = Env("ARBITRIUM_PORTS_MAPPING");
            if (string.IsNullOrEmpty(map)) return "";
            int i = map.IndexOf("\"ws\"", StringComparison.Ordinal);
            if (i < 0) return "";
            int e = map.IndexOf("\"external\"", i, StringComparison.Ordinal);
            if (e < 0) return "";
            int colon = map.IndexOf(':', e);
            var sb = new StringBuilder();
            for (int k = colon + 1; k < map.Length; k++)
            {
                char c = map[k];
                if (char.IsDigit(c)) sb.Append(c);
                else if (sb.Length > 0) break;
            }
            return sb.ToString();
        }

        static string Mime(string path)
        {
            switch (Path.GetExtension(path).ToLowerInvariant())
            {
                case ".html": return "text/html; charset=utf-8";
                case ".js": return "application/javascript";
                case ".wasm": return "application/wasm";
                case ".json": return "application/json";
                case ".css": return "text/css";
                case ".png": return "image/png";
                case ".ico": return "image/x-icon";
                case ".mp4": return "video/mp4";
                default: return "application/octet-stream";
            }
        }

        void Serve(HttpListenerContext ctx)
        {
            var res = ctx.Response;
            try
            {
                string rel = Uri.UnescapeDataString(ctx.Request.Url.AbsolutePath).TrimStart('/');
                res.AddHeader("Access-Control-Allow-Origin", "*");
                if (rel == "ports.json")
                {
                    byte[] j = Encoding.UTF8.GetBytes("{\"ws\":\"" + WsPort() + "\"}");
                    res.ContentType = "application/json";
                    res.AddHeader("Cache-Control", "no-store");
                    res.ContentLength64 = j.Length;
                    res.OutputStream.Write(j, 0, j.Length);
                    return;
                }
                if (rel.Length == 0) rel = "index.html";
                string full = Path.GetFullPath(Path.Combine(_root, rel));
                if (!full.StartsWith(_root, StringComparison.Ordinal) || !File.Exists(full)) { res.StatusCode = 404; return; }
                // Unity's gzip build: Build/*.gz go out with Content-Encoding so the browser inflates them itself
                bool gz = full.EndsWith(".gz", StringComparison.OrdinalIgnoreCase);
                res.ContentType = Mime(gz ? full.Substring(0, full.Length - 3) : full);
                if (gz) res.AddHeader("Content-Encoding", "gzip");
                res.AddHeader("Cache-Control", rel == "index.html" ? "no-cache" : "public, max-age=3600");
                using (var f = File.OpenRead(full))
                {
                    res.ContentLength64 = f.Length;
                    f.CopyTo(res.OutputStream, 1 << 16);
                }
            }
            catch (Exception e) { try { res.StatusCode = 500; } catch { } Debug.LogWarning("[Web] " + e.Message); }
            finally { try { res.OutputStream.Close(); } catch { } }
        }
    }
}
#endif
