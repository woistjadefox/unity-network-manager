using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using JsonFx.Json;


namespace Goga.UnityNetwork {

    public delegate void ChangedCliendState(NetworkPeerType state);
    public delegate void OnAllPlayersReady();

    [RequireComponent(typeof(NetworkView))]
    public class Manager : MonoBehaviour {

        public int serverPort = 25002;
        public string gameName;
        public string playerName;
        public string onlineCheckIp = "8.8.8.8"; // google dns server
        public float onlineCheckRate = 10f;
        public int onlineCheckPing;
        public bool isOnline;
        public bool isConnecting;
        public bool isReconnecting;
        private bool _isDisconnecting;

        public NetPlayerController netPlayers = new NetPlayerController();

        private NetworkPeerType lastPeerType;
        public event ChangedCliendState newState;
        public event OnAllPlayersReady onAllPlayersReady;
        private bool allPlayersReady;

        public bool isLanOnly = false;
        public HostData[] lobbyList;
        private HostData actualHost = null;
        private HostDataLAN actualHostLAN = null;

        public JsonReader jReader = new JsonReader();
        public JsonWriter jWriter = new JsonWriter();

        /* addons */
        [HideInInspector]
        public HostMigration migration;
        [HideInInspector]
        public Chat chat;
        [HideInInspector]
        public Dealer dealer;
        [HideInInspector]
        public AutoDiscovery discovery;


        void Awake() {

            Application.runInBackground = true;

            // clear hostlist
            MasterServer.ClearHostList();

            // set security
            Network.InitializeSecurity();

            // create empty lobbylist
            lobbyList = new HostData[]{};

            // defaults
            this.playerName = "MaxMuster";
            this.isConnecting = false;
            this.isOnline = false;

            StartCoroutine(CheckInternetConnection());

            /* load addons ******************************/

            // dealer plugin
            this.discovery = GetComponent<AutoDiscovery>();
            if (this.discovery && !this.discovery.enabled) {
                this.discovery = null;
            }

            // dealer plugin
            this.dealer = GetComponent<Dealer>();
            if (this.dealer && !this.dealer.enabled) {
                this.dealer = null;
            }

            // migration plugin
            this.migration = GetComponent<HostMigration>();
            if (this.migration && !this.migration.enabled) {
                this.migration = null;
            }

            // chat plugin
            this.chat = GetComponent<Chat>();
            if (this.chat && !this.chat.enabled) {
                this.chat = null;
            }

        }

        #region getter & setter
        
        // get name of joined server
        public HostData GetActualHost() {
            return this.actualHost;
        }

        // get name of joined server
        public HostDataLAN GetActualHostLAN() {
            return this.actualHostLAN;
        }

        // set name of joined server
        public void SetActualHost(HostData host) {
            this.actualHost = host;
        }

        // set name of joined lan server
        public void SetActualHostLAN(HostDataLAN host) {
            this.actualHostLAN = host;
        }

        // additional connecting function since NetworkPeerType.Connecting is not working
        public void SetIsConnecting(bool state) {

            this.isConnecting = state;
        }

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

        // get own player object
        public NetPlayer GetNetworkPlayer() {

            if (this.netPlayers.Exists(Network.player.guid)) {
                return this.netPlayers.Get(Network.player.guid);

            }
            else {
                return new NetPlayer() { guid = "0", name = "000", ready = false };
            }
        }

        // get specific player object
        public NetPlayer GetNetworkPlayer(string guid) {

            if (this.netPlayers.Exists(guid)) {
                return this.netPlayers.Get(guid);

            } else {
                return new NetPlayer() { guid = "0", name = "000", ready = false };
            }
        }

        // get ping of player
        public int GetNetworkPlayerPing(NetPlayer player) {

            for (int i = 0; i < Network.connections.Length; i++) {

                if (player.guid == Network.connections[i].guid) {
                    return Network.GetLastPing(Network.connections[i]);
                }
            }

            return 0;
        }

        public NetworkPlayer GetUnityNetworkPlayer(string guid) {

            for (int i = 0; i < Network.connections.Length; i++) {

                if (guid == Network.connections[i].guid) {
                    return Network.connections[i];
                }
            }

            return new NetworkPlayer();
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

        void Update() {

            // check if state changed and do actions
            if (this.lastPeerType != Network.peerType){
                this.StateChanged();
            }
        }

        void FixedUpdate() {

            // check if all players are ready
            if (Network.isServer || Network.isClient) {

                if (this.AllPlayersReadyCheck()) {

                    if (!allPlayersReady) {

                        this.allPlayersReady = true;
                        this.onAllPlayersReady();
                    }

                } else {
                    this.allPlayersReady = false;
                }
            }
        }

        // get new hostlist data from master server
        public void UpdateHostList() {

            if (this.isOnline) { 

                // get list from masterserver
                MasterServer.RequestHostList(this.gameName);

                // sync local list with master list
                this.lobbyList = MasterServer.PollHostList();
            }
        }

        // event for state changes
        void StateChanged() {

            switch (Network.peerType) {

                case NetworkPeerType.Disconnected:

                    Network.isMessageQueueRunning = false;
                    this._isDisconnecting = false;
                    this.CleanUp();
                    break;

                case NetworkPeerType.Connecting:
                    break;
            }

            // send notification
            this.newState(Network.peerType);

            // set lastPeerType to actual
            this.lastPeerType = Network.peerType;
        }

        // check if all palyers are ready
        bool AllPlayersReadyCheck() {

            foreach (NetPlayer player in this.netPlayers.GetList()) {

                if (!player.ready) {
                    return false;
                }
            }

            return true;
        }

        // check if server is full
        public bool IsServerFull() {

            if (this.isLanOnly) {
                if (this.GetActualHostLAN().playerLimit != this.netPlayers.GetList().Count) {
                    return false;
                }
            } else {
                if (this.GetActualHost().playerLimit != this.netPlayers.GetList().Count) {
                    return false;
                }
            }

            return true;
        }

        // register a new server to the master server
        public void RegisterGame(bool lan, string name, string comment, float playerSize) {

            this.SetIsConnecting(true);

            // register game to master server if it's not lan only
            if (lan) {

                this.isLanOnly = true;

                HostDataLAN tmpHost = new HostDataLAN("empty");
                tmpHost.gameName = name;
                tmpHost.comment = comment;
                tmpHost.playerLimit = (int)playerSize;

                this.SetActualHostLAN(tmpHost);

                Network.InitializeServer((int)playerSize - 1, this.serverPort, false);
            
            } else {

                // create host entry
                HostData tmpHost = new HostData();
                tmpHost.gameName = name;
                tmpHost.comment = comment;
                tmpHost.playerLimit = (int)playerSize;

                this.SetActualHost(tmpHost);

                // Use NAT punchthrough if no public IP present
                Network.InitializeServer((int)playerSize - 1, this.serverPort, !Network.HavePublicAddress());
                MasterServer.RegisterHost(this.gameName, name, comment);
            }


            NetPlayer _serverHost = new NetPlayer();
            _serverHost.guid = Network.player.guid;
            _serverHost.name = this.playerName;

            if (!this.netPlayers.Exists(Network.player.guid)) {
                this.netPlayers.Add(_serverHost);
            }

        }

        // to change settings for master server
        public void ReregisterServer(HostData host) {

            // not implemented yet
        }

        public void ChangePlayerLimit(int size) {

            if (Network.isServer) {
                Network.maxConnections = size;
            }

        }

        public void DisconnectPeer(){

            this._isDisconnecting = true;

            Network.Disconnect(200);

            if (Network.peerType == NetworkPeerType.Server) {
                MasterServer.UnregisterHost();
            }
        }

        public void ConnectPeer(HostData host){

            this.SetIsConnecting(true);
            Network.Connect(host);
            this.SetActualHost(host);
        }

        public void ConnectPeerLAN(HostDataLAN host) {
            this.SetIsConnecting(true);
            Network.Connect(host.ip, host.port);
            this.SetActualHostLAN(host);
        }

        public void ConnectPeer(string guid) {

            this.SetIsConnecting(true);


            if (this.isLanOnly) {
                Network.Connect(this.GetUnityNetworkPlayer(guid).ipAddress, this.serverPort);

            } else {
                Network.Connect(guid);
            }

        }

        #region RPC call functions
        public void SpreadNetworkPlayer() {

            if (this.netPlayers.Exists(Network.player.guid)) {

                this.netPlayers.Get(Network.player.guid).name = this.playerName;

                string _player = jWriter.Write(this.GetNetworkPlayer());
                networkView.RPC("UpdateNetworkPlayer", RPCMode.All, _player);
            }
        }



        #endregion RPC call functions

        #region RPC functions

        [RPC]
        void AddConnectedPlayer(string playerObj) {

            NetPlayer _player = jReader.Read<NetPlayer>(playerObj);

            if (!this.netPlayers.Exists(_player.guid)) {

                this.netPlayers.Add(_player);
            }

            this.netPlayers.SortList();
        }

        [RPC]
        void RemoveConnectedPlayer(string guid) {

            if (this.netPlayers.Exists(guid)) {

                this.netPlayers.Remove(guid);
            }
        }

        [RPC]
        void RegisterPlayerOnServer(string playerObj) {

            // tell everybody about new player
            networkView.RPC("AddConnectedPlayer", RPCMode.All, playerObj);
        }

        [RPC]
        void UpdateNetworkPlayer(string playerObj) {

            NetPlayer _player = jReader.Read<NetPlayer>(playerObj);

            // check if player exists, if not create it
            if (this.netPlayers.Exists(_player.guid)) {

                this.netPlayers.Update(_player.guid, _player);

            } else {
                this.AddConnectedPlayer(playerObj);
            }
            
        }


        #endregion RPC functions

        IEnumerator CheckInternetConnection() {

            while (true) {

                Ping pingIP = new Ping(this.onlineCheckIp);

                yield return new WaitForSeconds(1);

                if (pingIP.isDone) {
                    this.onlineCheckPing = pingIP.time;
                    this.isOnline = true;
                } else {
                    Debug.Log("no inet connection..");
                    this.onlineCheckPing = 0;
                    this.isOnline = false;

                    if (Network.isServer && !this.isLanOnly) {
                        this.DisconnectPeer();
                    }
                }

                yield return new WaitForSeconds(this.onlineCheckRate);
            }


        }

        void CleanUp() {

            this.RemoveAllNetworkObjects();

            if (!this.isReconnecting) {

                if (this.chat) {
                    this.chat.ClearMessages();
                }

                this.netPlayers.GetList().Clear();
                this.SetActualHost(null);
                this.SetActualHostLAN(null);
                this.isLanOnly = false;
            }

            Debug.Log("CleanUp done..");
        }

        void RemoveAllNetworkObjects() {

            NetObject[] playerObjs = FindObjectsOfType(typeof(NetObject)) as NetObject[];

            foreach (NetObject obj in playerObjs) {
                
                if(this.migration){

                    // remove only disconnected objs if reconnecting as server
                    if (this.isReconnecting && this.migration.isNewServer) {

                        if (!this.netPlayers.Exists(obj.playerGuid)) {
                            Destroy(obj.gameObject);
                        }

                    } else {
                        Destroy(obj.gameObject);
                    }

                } else {

                    Destroy(obj.gameObject);
                }

            }
        }

        void OnServerInitialized() {
     
            this.SetIsConnecting(false);
        }

        void OnConnectedToServer() {

            NetPlayer _host = new NetPlayer();
            _host.guid = Network.player.guid;
            _host.name = this.playerName;

            string _hostJson = jWriter.Write(_host);

            networkView.RPC("RegisterPlayerOnServer", RPCMode.Server, _hostJson);

            this.SetIsConnecting(false);
        }

        void OnDisconnectedFromServer() {

            this.SetIsConnecting(false);

            if (!this.migration || Network.isServer || this._isDisconnecting) {

                Debug.Log("disconnected from server");
                this.isReconnecting = false;
                this.SetActualHost(null);
                this.SetActualHostLAN(null);

            } else {

                this.SetIsConnecting(true);

                if (this.migration) {
                    this.migration.Migrate();
                }

            }

        } 

        void OnFailedToConnect() {
            this.SetIsConnecting(false);
            this.SetActualHost(null);
            this.SetActualHostLAN(null);
            this.isReconnecting = false;
        }

        void OnPlayerConnected(NetworkPlayer player) {

            // tell the player who is in
            foreach (NetPlayer existingPlayer in netPlayers.GetList()) {

                string _existingPlayerJson = jWriter.Write(existingPlayer);
                networkView.RPC("AddConnectedPlayer", player, _existingPlayerJson);
            }

            if (this.dealer) {
                GetComponent<Dealer>().InstantiateAllNetworkObjects(NetworkPrefabs.Player, player);
            }

        }

        void OnPlayerDisconnected(NetworkPlayer player) {

            networkView.RPC("RemoveConnectedPlayer", RPCMode.All, player.guid);

            NetObject[] playerObjs = FindObjectsOfType<NetObject>() as NetObject[];
            
            foreach (NetObject obj in playerObjs) {

                if (obj.playerGuid == player.guid) {

                    Network.RemoveRPCs(obj.networkView.viewID);
                    Network.Destroy(obj.networkView.viewID);
                }

            }

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
