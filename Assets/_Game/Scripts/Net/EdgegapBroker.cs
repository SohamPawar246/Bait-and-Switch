using System;
using System.Collections;
using Mirror;
using UnityEngine;
using UnityEngine.Networking;

namespace BaitSwitch.Net
{
    /// <summary>
    /// Client side of "press Play → get a server" (docs/14-free-tier.md, docs/15-broker.md).
    /// Talks to the Cloudflare Worker, reports the four boot-loader stages, then connects Mirror
    /// with retries (Edgegap reports Ready before Unity has finished loading).
    /// UI listens to the events; nothing here draws anything.
    /// </summary>
    public class EdgegapBroker : MonoBehaviour
    {
        [Tooltip("The workers.dev URL printed by `npm run deploy`, no trailing slash")]
        public string brokerUrl = "https://bait-and-switch-broker.YOURNAME.workers.dev";

        [Header("Local testing")]
        [Tooltip("Skip the broker and connect to this address (e.g. a local Docker server)")]
        public bool useDirectAddress;
        public string directHost = "localhost";
        public ushort directPort = 7777;

        public float pollSeconds = 2f;
        public float slowAfterSeconds = 90f;
        public float giveUpSeconds = 240f;
        public float connectRetrySeconds = 1f;
        public float connectGiveUpSeconds = 75f;

        /// <summary>1 calling, 2 server starting, 3 server ready (connecting), 4 connected.</summary>
        public event Action<int, string> OnStage;
        public event Action<string> OnLocation;
        public event Action OnSlow;
        public event Action<string> OnError;
        public event Action OnConnected;

        public bool Busy { get; private set; }

        [Serializable]
        class Reply
        {
            public string state;     // ready | wait | error
            public string stage;     // none | starting | ready | stopping
            public string host;
            public int port;
            public string location;
            public string requestId;
            public string message;
        }

        public void Play()
        {
            if (Busy) return;
            StartCoroutine(PlayRoutine());
        }

        public void Cancel()
        {
            StopAllCoroutines();
            Busy = false;
            if (NetworkClient.active) NetworkManager.singleton.StopClient();
        }

        IEnumerator PlayRoutine()
        {
            Busy = true;
            string host = null; int port = 0;

            if (useDirectAddress)
            {
                host = directHost; port = directPort;
            }
            else
            {
                Stage(1, Copy.BootCalling);
                float started = Time.realtimeSinceStartup;
                bool slowSent = false;
                bool first = true;

                while (true)
                {
                    Reply r = null; string err = null;
                    yield return Request(first ? "POST" : "GET", first ? "/join" : "/status", x => r = x, e => err = e);

                    // /status never starts a server; if the old one finished stopping, ask again with /join.
                    if (r != null && r.state == "wait" && r.stage == "none") first = true;
                    else first = false;

                    if (err != null && Time.realtimeSinceStartup - started > 20f) { Fail(Copy.BootBrokerDown); yield break; }

                    if (r != null)
                    {
                        if (r.state == "error") { Fail(string.IsNullOrEmpty(r.message) ? Copy.BootBusy : r.message); yield break; }
                        if (!string.IsNullOrEmpty(r.location)) OnLocation?.Invoke(r.location);
                        if (r.state == "ready") { host = r.host; port = r.port; break; }
                        if (r.stage == "starting") Stage(2, string.Format(Copy.BootStirs, string.IsNullOrEmpty(r.location) ? Copy.BootSomewhere : r.location));
                        if (r.stage == "stopping") Stage(2, Copy.BootRotating);
                    }

                    float waited = Time.realtimeSinceStartup - started;
                    if (!slowSent && waited > slowAfterSeconds) { slowSent = true; OnSlow?.Invoke(); }
                    if (waited > giveUpSeconds) { Fail(Copy.BootTimeout); yield break; }
                    yield return new WaitForSecondsRealtime(pollSeconds);
                }
            }

            Stage(3, Copy.BootLanterns);
            yield return ConnectWithRetry(host, (ushort)port);
        }

        IEnumerator ConnectWithRetry(string host, ushort port)
        {
            var nm = NetworkManager.singleton;
            nm.networkAddress = host;
            if (Transport.active is PortTransport pt) pt.Port = port;

            float started = Time.realtimeSinceStartup;
            while (Time.realtimeSinceStartup - started < connectGiveUpSeconds)
            {
                if (!NetworkClient.active) nm.StartClient();

                float attempt = Time.realtimeSinceStartup;
                while (NetworkClient.active && !NetworkClient.isConnected && Time.realtimeSinceStartup - attempt < 5f)
                    yield return null;

                if (NetworkClient.isConnected)
                {
                    Stage(4, Copy.BootOpen);
                    Busy = false;
                    OnConnected?.Invoke();
                    yield break;
                }

                if (NetworkClient.active) nm.StopClient();
                yield return new WaitForSecondsRealtime(connectRetrySeconds);
            }
            Fail(Copy.BootTimeout);
        }

        IEnumerator Request(string method, string path, Action<Reply> ok, Action<string> fail)
        {
            using var req = new UnityWebRequest(brokerUrl + path, method);
            req.downloadHandler = new DownloadHandlerBuffer();
            if (method == "POST") { req.uploadHandler = new UploadHandlerRaw(new byte[0]); req.SetRequestHeader("Content-Type", "application/json"); }
            req.timeout = 15;
            yield return req.SendWebRequest();

            if (req.result != UnityWebRequest.Result.Success && req.responseCode != 500)
            {
                fail(req.error);
                yield break;
            }
            try { ok(JsonUtility.FromJson<Reply>(req.downloadHandler.text)); }
            catch (Exception e) { fail(e.Message); }
        }

        void Stage(int n, string text) => OnStage?.Invoke(n, text);

        void Fail(string msg)
        {
            Busy = false;
            Debug.LogWarning("[Broker] " + msg);
            OnError?.Invoke(msg);
        }
    }
}
