using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using JsonFx.Json;


namespace Goga {

    public class UnityNetworkPlayer {
        public string guid;
        public string name;
        public bool ready = false;
    }

    public delegate void ChangedCliendState(NetworkPeerType state);

    [RequireComponent(typeof(NetworkView))]

    public class UnityNetworkManager : MonoBehaviour {

        public string gameName;
        public string playerName;

        public Dictionary<string, UnityNetworkPlayer> connectedPlayers = new Dictionary<string, UnityNetworkPlayer>();

        private NetworkPeerType lastPeerType;
        public event ChangedCliendState newState;

        HostData[] lobbyList;

        #region getter & setter

        // get peerType
        public NetworkPeerType GetPeerType() {
            return Network.peerType;
        }

        // get all hosts from lobby list
        public HostData[] GetLobbyList() {

            return this.lobbyList;
        }

        // get a single host from lobby list [guid]
        public HostData GetHostData(string guid) {

            HostData tmpHost = new HostData();

            foreach (HostData host in this.GetLobbyList()) {

                if (host.guid == guid) {
                    tmpHost = host;
                }
            }

            return tmpHost;
        }

        // get all string properties from all hosts [propertyname]
        public List<string> GetLobbyListRAW(string property) {

            List<string> _tmpEntries = new List<string>();

            foreach (HostData server in this.GetLobbyList()) {

                _tmpEntries.Add((string)server.GetType().GetProperty(property).GetValue(server, null));
            }

            return _tmpEntries;
        }

        // get player object
        public UnityNetworkPlayer GetNetworkPlayer() {
            return this.connectedPlayers[Network.player.guid];
        }

        // toggle player ready state
        public void ToggleNetworkPlayerReadyState() {

            if (this.GetNetworkPlayer().ready) {
                this.GetNetworkPlayer().ready = false;
            }
            else {
                this.GetNetworkPlayer().ready = true;
            }

            this.SpreadNetworkPlayer();
        }

        #endregion getter & setter

        void Awake() {

            Application.runInBackground = true;

            MasterServer.ClearHostList();

            this.playerName = "MaxMuster";
        }

        void FixedUpdate() {

            // check player state every second
            if (Time.frameCount % 60 == 0) {

                // check list of games
                this.lobbyList = MasterServer.PollHostList();
            }

            // check if state changed and do actions
            if (this.lastPeerType != Network.peerType)
            {
                this.StateChanged();
            }

        }

        // get new hostlist data from master server
        public void UpdateHostList() {

            MasterServer.RequestHostList(this.gameName);
        }

        // event for state changes
        void StateChanged() {

            switch (Network.peerType) {
                case NetworkPeerType.Disconnected:
                    this.connectedPlayers.Clear();
                    Debug.Log("cleard list");
                    break;
            }

            // send notification
            this.newState(Network.peerType);

            // set lastPeerType to actual
            this.lastPeerType = Network.peerType;
        }

        // register a new server to the master server
        public void RegisterGame(string name, string comment, float playerSize) {

            // Use NAT punchthrough if no public IP present
            Network.InitializeServer((int)playerSize-1, 25002, !Network.HavePublicAddress());
            MasterServer.RegisterHost(this.gameName, name, comment);

            UnityNetworkPlayer _serverHost = new UnityNetworkPlayer();
            _serverHost.guid = Network.player.guid;
            _serverHost.name = this.playerName;

            this.connectedPlayers.Add(Network.player.guid, _serverHost);

        }

        public void DisconnectPeer(){

            Network.Disconnect(200);

            if (Network.peerType == NetworkPeerType.Server) {
                MasterServer.UnregisterHost();
            }
        }

        public void ConnectPeer(HostData host){

            Network.Connect(host);
        }

        public void SpreadNetworkPlayer() {

            if (this.connectedPlayers.ContainsKey(Network.player.guid)) {

                this.connectedPlayers[Network.player.guid].name = this.playerName;

                string _player = new JsonWriter().Write(this.GetNetworkPlayer());
                networkView.RPC("UpdateNetworkPlayer", RPCMode.All, _player);
            }
        }

        #region RPC functions

        [RPC]
        void AddConnectedPlayer(string playerObj) {


            UnityNetworkPlayer _player = new JsonReader().Read<UnityNetworkPlayer>(playerObj);

            if (!this.connectedPlayers.ContainsKey(_player.guid)) {

                this.connectedPlayers.Add(_player.guid, _player);
            }
        }

        [RPC]
        void RemoveConnectedPlayer(string guid) {

            if (this.connectedPlayers.ContainsKey(guid)) {

                this.connectedPlayers.Remove(guid);
            }
        }

        [RPC]
        void RegisterPlayerOnServer(string playerObj) {

            // tell everybody about new player
            networkView.RPC("AddConnectedPlayer", RPCMode.All, playerObj);
        }

        [RPC]
        void UpdateNetworkPlayer(string playerObj) {

            UnityNetworkPlayer _player = new JsonReader().Read<UnityNetworkPlayer>(playerObj);

            // check if player exists, if not create it
            if (this.connectedPlayers.ContainsKey(_player.guid)) {
                this.connectedPlayers[_player.guid] = _player;
            } else {
                this.AddConnectedPlayer(playerObj);
            }
            
        }

        #endregion RPC functions

        void OnConnectedToServer() {

            UnityNetworkPlayer _host = new UnityNetworkPlayer();
            _host.guid = Network.player.guid;
            _host.name = this.playerName;

            string _hostJson = new JsonWriter().Write(_host);

            networkView.RPC("RegisterPlayerOnServer", RPCMode.Server, _hostJson);
        }

        void OnPlayerConnected(NetworkPlayer player) {

            // tell the player who is in
            foreach (UnityNetworkPlayer existingPlayer in connectedPlayers.Values) {

                string _existingPlayerJson = new JsonWriter().Write(existingPlayer);
                networkView.RPC("AddConnectedPlayer", player, _existingPlayerJson);
            }
        }

        void OnPlayerDisconnected(NetworkPlayer player) {

            networkView.RPC("RemoveConnectedPlayer", RPCMode.All, player.guid);
            Network.RemoveRPCs(player);
            Network.DestroyPlayerObjects(player);
        }

        void OnDisable() {
            this.DisconnectPeer();
        }

        void OnApplicationQuit() {
            this.DisconnectPeer();
        }
    }
}
