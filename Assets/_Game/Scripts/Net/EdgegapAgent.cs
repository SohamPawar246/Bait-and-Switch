using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Networking;

namespace BaitSwitch.Net
{
    /// <summary>
    /// Server-only helper for living on the Edgegap free tier (docs/14-free-tier.md).
    /// - Reads the injected ARBITRIUM_* variables (location for the HUD panel, self-stop URL).
    /// - Idle self-stop: no humans for GameConfig.idleStopSeconds → stop.
    /// - Rotation: GameStateManager checks AcceptingNewMatches before starting a match; after the
    ///   last match it calls StopServer("rotation").
    /// - Tells the broker first (/server/closing) so new players are routed to a fresh server.
    /// App-version env vars to set on Edgegap (hidden): BROKER_URL, BROKER_SECRET.
    /// </summary>
    public class EdgegapAgent : MonoBehaviour
    {
        public static EdgegapAgent I { get; private set; }

        public string Location { get; private set; } = "Local";
        public string DeploymentId { get; private set; } = "local";
        public bool OnEdgegap { get; private set; }
        public float Uptime => Time.realtimeSinceStartup - _bootTime;
        public bool AcceptingNewMatches => Uptime < GameConfig.I.noNewMatchAfterMinutes * 60f;
        public bool Stopping { get; private set; }

        public event Action<string> OnStopping; // reason; GameStateManager broadcasts EventKind.Rotation

        float _bootTime;
        float _lastHumanSeen;
        int _humans;

        [Serializable] class Loc { public string city; public string country; }

        void Awake()
        {
            I = this;
            _bootTime = Time.realtimeSinceStartup;
            _lastHumanSeen = _bootTime;

            string id = Env("ARBITRIUM_REQUEST_ID");
            if (!string.IsNullOrEmpty(id))
            {
                OnEdgegap = true;
                DeploymentId = id;
                var raw = Env("ARBITRIUM_DEPLOYMENT_LOCATION");
                if (!string.IsNullOrEmpty(raw))
                {
                    try
                    {
                        var l = JsonUtility.FromJson<Loc>(raw);
                        Location = string.IsNullOrEmpty(l.country) ? l.city : $"{l.city}, {l.country}";
                    }
                    catch { Location = "the Deep"; }
                }
                Debug.Log($"[Edgegap] deployment {DeploymentId} in {Location}");
            }
        }

        /// <summary>GameStateManager calls this whenever the human count changes.</summary>
        public void SetHumanCount(int humans)
        {
            _humans = humans;
            if (humans > 0) _lastHumanSeen = Time.realtimeSinceStartup;
        }

        void Update()
        {
            if (Stopping || !OnEdgegap) return;
            if (_humans == 0 && Time.realtimeSinceStartup - _lastHumanSeen > GameConfig.I.idleStopSeconds)
                StopServer("idle");
        }

        public void StopServer(string reason)
        {
            if (Stopping) return;
            Stopping = true;
            Debug.Log("[Edgegap] stopping: " + reason);
            OnStopping?.Invoke(reason);
            StartCoroutine(StopRoutine());
        }

        IEnumerator StopRoutine()
        {
            // 1. Tell the broker so new joins wait for a fresh server instead of this one.
            string broker = Env("BROKER_URL"), secret = Env("BROKER_SECRET");
            if (!string.IsNullOrEmpty(broker))
            {
                var body = System.Text.Encoding.UTF8.GetBytes("{\"requestId\":\"" + DeploymentId + "\"}");
                using var req = new UnityWebRequest(broker.TrimEnd('/') + "/server/closing", "POST");
                req.uploadHandler = new UploadHandlerRaw(body);
                req.downloadHandler = new DownloadHandlerBuffer();
                req.SetRequestHeader("Content-Type", "application/json");
                req.SetRequestHeader("X-Server-Secret", secret ?? "");
                req.timeout = 10;
                yield return req.SendWebRequest();
            }

            // 2. Give clients a moment to show "Moving to a fresh server…".
            yield return new WaitForSecondsRealtime(3f);

            // 3. Ask Edgegap to stop this deployment (graceful SIGTERM follows).
            string url = Env("ARBITRIUM_DELETE_URL"), token = Env("ARBITRIUM_DELETE_TOKEN");
            if (!string.IsNullOrEmpty(url))
            {
                using var del = UnityWebRequest.Delete(url);
                del.SetRequestHeader("authorization", token ?? "");
                del.timeout = 10;
                yield return del.SendWebRequest();
                if (del.result != UnityWebRequest.Result.Success) Debug.LogWarning("[Edgegap] self-stop failed: " + del.error);
            }

            yield return new WaitForSecondsRealtime(5f);
            Application.Quit(0);
        }

        static string Env(string k) => Environment.GetEnvironmentVariable(k);
    }
}
