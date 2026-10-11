using System.Net;
using System.Net.Sockets;
using Mirror;
using UnityEngine;

namespace BaitSwitch.Net
{
    /// <summary>
    /// Dev-only connect panel (replaces Mirror's NetworkManagerHUD until the real menu exists).
    /// Checks that the UDP port is free before hosting so a second instance never throws a SocketException.
    /// </summary>
    public sealed class DevHud : MonoBehaviour
    {
        public Vector2 offset = new Vector2(10f, 10f);
        string _status = "";

        void OnGUI()
        {
            var nm = NetworkManager.singleton;
            if (nm == null || Application.isBatchMode) return;
            GUILayout.BeginArea(new Rect(offset.x, offset.y, 260f, 200f));
            if (!NetworkClient.active && !NetworkServer.active)
            {
                nm.networkAddress = GUILayout.TextField(nm.networkAddress);
                if (GUILayout.Button("Host (server + play)")) TryStart(nm, true);
                if (GUILayout.Button("Client")) { _status = ""; nm.StartClient(); }
                if (GUILayout.Button("Server only")) TryStart(nm, false);
            }
            else
            {
                string mode = NetworkServer.active && NetworkClient.active ? "Host" : NetworkServer.active ? "Server" : "Client";
                GUILayout.Label($"{mode} · {nm.networkAddress} · {(NetworkClient.isConnected || NetworkServer.active ? "connected" : "connecting…")}");
                var client = nm.GetComponent<SeaClient>();
                if (client != null && NetworkClient.active) GUILayout.Label($"Role: {client.Role}   (F1 switch)");
                if (GUILayout.Button("Stop"))
                {
                    if (NetworkServer.active && NetworkClient.active) nm.StopHost();
                    else if (NetworkServer.active) nm.StopServer();
                    else nm.StopClient();
                }
            }
            if (_status.Length > 0) GUILayout.Label(_status);
            GUILayout.EndArea();
        }

        void TryStart(NetworkManager nm, bool host)
        {
            if (!PortFree(nm))
            {
                _status = "Port in use: another instance is already hosting. Press Client.";
                return;
            }
            _status = "";
            if (host) nm.StartHost(); else nm.StartServer();
        }

        /// <summary>True if our transport's UDP port can be bound (same dual-mode bind KCP uses).</summary>
        public static bool PortFree(NetworkManager nm)
        {
            ushort port = 7777;
            if (Transport.active is PortTransport pt) port = pt.Port;
            else if (nm.transport is PortTransport pt2) port = pt2.Port;
            try
            {
                using var s = new Socket(AddressFamily.InterNetworkV6, SocketType.Dgram, ProtocolType.Udp);
                s.DualMode = true;
                s.Bind(new IPEndPoint(IPAddress.IPv6Any, port));
                return true;
            }
            catch (SocketException) { return false; }
        }
    }
}
