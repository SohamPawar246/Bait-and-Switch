using Mirror;
using UnityEngine;

namespace BaitSwitch.Net
{
    /// <summary>
    /// Mirror NetworkManager for the sea. No player objects: everything is messages.
    /// Server: SeaServer owns the sim. Client (and host's client): SeaClient + LocalPredictor + SeaView.
    /// Editor: an MPPM player tagged "Client" auto-connects, tagged "Host" auto-hosts; otherwise use the HUD.
    /// </summary>
    public sealed class BSNetworkManager : NetworkManager
    {
        SeaServer _server;
        SeaClient _client;

        public override void Awake()
        {
            autoCreatePlayer = false;
            headlessStartMode = HeadlessStartOptions.AutoStartServer;
            // Views must exist on this object; an old scene without them shows fish only (or nothing).
            if (GetComponent<View.HookView>() == null)
                Debug.LogError("[BS] HookView missing on NetworkManager: this scene is out of date. Run Tools > Bait & Switch > Build Greybox.");
            _server = GetComponent<SeaServer>();
            _client = GetComponent<SeaClient>();
            base.Awake();
        }

        public override void Start()
        {
            base.Start();
#if UNITY_EDITOR
            if (NetworkServer.active || NetworkClient.active) return;
            string tags = MppmTags();
            if (tags.Contains("Client")) StartClient();
            else if (tags.Contains("Host"))
            {
                if (DevHud.PortFree(this)) StartHost();
                else { Debug.LogWarning("[BS] Port in use; another instance is hosting. Joining as client."); StartClient(); }
            }
#endif
        }

        /// <summary>Mirror sets the headless frame rate to sendRate (60); the server sim wants simHz.</summary>
        public override void ConfigureHeadlessFrameRate()
        {
            if (Utils.IsHeadless()) Application.targetFrameRate = GameConfig.I.simHz;
        }

        public override void OnStartServer()
        {
#if !UNITY_WEBGL || UNITY_EDITOR
            WebHost.StartIfHeadless(gameObject);   // the browser build, served from this very server
#endif
            _server.Begin();
            NetworkServer.RegisterHandler<JoinMsg>(_server.OnJoin);
            NetworkServer.RegisterHandler<InputMsg>(_server.OnInput);
        }

        public override void OnStopServer()
        {
            _server.End();
            var view = GetComponent<View.SeaView>();
            if (view != null && !NetworkClient.active) view.Clear();
            var hooks = GetComponent<View.HookView>();
            if (hooks != null && !NetworkClient.active) hooks.Clear();
        }

        public override void OnServerConnect(NetworkConnectionToClient conn) => _server.AddPeer(conn);

        public override void OnServerDisconnect(NetworkConnectionToClient conn)
        {
            _server.RemovePeer(conn);
            base.OnServerDisconnect(conn);
        }

        public override void OnStartClient()
        {
            NetworkClient.RegisterHandler<WelcomeMsg>(_client.OnWelcome);
            NetworkClient.RegisterHandler<SnapshotMsg>(_client.OnSnapshot);
            NetworkClient.RegisterHandler<EventMsg>(_client.OnEvent);
            NetworkClient.RegisterHandler<RoundStartMsg>(_client.OnRoundStart);
            NetworkClient.RegisterHandler<MatchStateMsg>(_client.OnMatchState);
            NetworkClient.RegisterHandler<LedgerMsg>(_client.OnLedger);
            NetworkClient.RegisterHandler<RosterMsg>(_client.OnRoster);
            NetworkClient.RegisterHandler<TugMsg>(_client.OnTug);
        }

        public override void OnClientConnect()
        {
            base.OnClientConnect();
            NetworkClient.Send(new JoinMsg { name = UI.GameSettings.Name, appearance = UI.GameSettings.Appearance });
        }

        public override void OnStopClient() => _client.Clear();

#if UNITY_EDITOR
        // MPPM's CurrentPlayer lives in the engine in Unity 6; read it by reflection so a namespace
        // change never breaks the build. Startup only, so the allocation doesn't matter.
        static string MppmTags()
        {
            foreach (var asm in System.AppDomain.CurrentDomain.GetAssemblies())
            {
                var t = asm.GetType("Unity.Multiplayer.Playmode.CurrentPlayer") ?? asm.GetType("Unity.Multiplayer.PlayMode.CurrentPlayer");
                var m = t?.GetMethod("ReadOnlyTags", System.Type.EmptyTypes);
                if (m == null) continue;
                var v = m.Invoke(null, null);
                if (v is string s) return s;
                if (v is System.Collections.IEnumerable e)
                {
                    var sb = new System.Text.StringBuilder();
                    foreach (var o in e) sb.Append(o).Append(',');
                    return sb.ToString();
                }
            }
            return "";
        }
#endif
    }
}
