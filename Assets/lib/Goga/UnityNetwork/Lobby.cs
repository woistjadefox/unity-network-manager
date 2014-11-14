using UnityEngine;
using System.Collections;
using System.Collections.Generic;


namespace Goga.UnityNetwork {

    public class Lobby : MonoBehaviour {

        // network handler
        public Logic logic;
        private Manager uNet;
        private AutoDiscovery uDiscovery;
        private Chat chat;

        public int minPlayers = 2;
        public int maxPlayers = 32;
        public int defaultPlayerSize = 2;

        public int paddingLeft = 120;
        public int paddingTop = 10;

        private bool lanGame = false;
        private string formGamename = "";
        private string formComment = "";

        private Rect lobbyWin;

        private Vector2 lobbyScrollPosition = Vector2.zero;
        private Vector2 chatScrollPosition = Vector2.zero;

        private bool joinedLanGame = false;
        private string _nickname = "MaxMuster";
        private string _readyState;
        private string _errorString = "";
        private string _actualGameName = "";
        private string _chatForm = "";
        private int _chatCounter = 0;

        private bool showLobby = true;

        public void ToggleLobby() {

            if (showLobby) {
                showLobby = false;
            } else {
                showLobby = true;
            }
        }

        void Start() {

            this.uNet = GetComponent<Manager>();
            this.uDiscovery = GetComponent<AutoDiscovery>();
            this.chat = GetComponent<Chat>();

            uNet.newState += new ChangedCliendState(OnStateChange);
            uNet.onAllPlayersReady += new OnAllPlayersReady(OnAllPlayersReady);

            lobbyWin = new Rect(paddingLeft, paddingTop, 600, 50);
        }

        void FixedUpdate() {

            // update host list every second
            if (Time.frameCount % 60 == 0) {

                uNet.UpdateHostList();
            }

        }

        void OnGUI() {

            if (!this.showLobby)
                return;

            GUI.depth = 99;
            GUI.skin.box.alignment = TextAnchor.MiddleLeft;
            GUI.skin.box.wordWrap = true;

            // error window
            if (this._errorString != "") {

                GUILayout.Window(98, new Rect((Screen.width) - 260, (Screen.height) - 110, 250, 50), (int windowID) => {

                    GUILayout.Box("<b><color=red>" + _errorString + "</color></b>", GUILayout.MaxWidth(250));

                    if (GUILayout.Button("okay", GUILayout.MaxWidth(80))) {
                        this._errorString = "";
                    }

                }, "Error", GUILayout.MaxWidth(250), GUILayout.MaxHeight(200));

            }

            // connecting window
            if (uNet.GetPeerType() == NetworkPeerType.Connecting || uNet.isConnecting) {
                GUILayout.Window(97, new Rect((Screen.width) - 260, (Screen.height) - 110, 250, 50), (int windowID) => {

                    GUILayout.Box("<b>Connecting...</b>", GUILayout.MaxWidth(250));

                }, "Connecting", GUILayout.MaxWidth(250), GUILayout.MaxHeight(200));
            }

            // lobby window
            lobbyWin = GUILayout.Window(50, lobbyWin, (int windowID) => {

                if (uNet.isOnline) {

                    // internet hosts
                    if (uNet.GetLobbyList().Length > 0) {
                        GUILayout.Label("Available Internet Games");
                    } else {
                        GUILayout.Label("No open Internet Games available");
                    }

                    if (lobbyWin.height > 200) {
                        lobbyScrollPosition = GUILayout.BeginScrollView(lobbyScrollPosition, GUILayout.MinHeight(200));
                    }

                    foreach (HostData host in uNet.GetLobbyList()) {

                        GUILayout.BeginHorizontal();

                        GUILayout.Box("<b>" + host.gameName + "</b>");

                        if (host.comment != "") {
                            GUILayout.Box("<b>" + host.comment + "</b>");
                        }

                        GUILayout.Box("<b>" + host.connectedPlayers + " / " + host.playerLimit + "</b>", GUILayout.MaxWidth(50));

                        if (uNet.GetPeerType() == NetworkPeerType.Disconnected && !uNet.isConnecting && host.connectedPlayers < host.playerLimit) {
                            if (GUILayout.Button("join", GUILayout.MaxWidth(80))) {
                                this.Join(host);
                            }
                        }

                        GUILayout.EndHorizontal();
                    }
                }

                // lan discovery hosts
                if (uDiscovery.GetLanHostData().Length > 0) {
                    GUILayout.Label("Available LAN Games");
                } else {
                    GUILayout.Label("No open LAN Games available");
                }

                foreach (HostDataLAN host in uDiscovery.GetLanHostData()) {

                    GUILayout.BeginHorizontal();

                    GUILayout.Box("<b>" + host.gameName + "</b>");

                    if (host.comment != "") {
                        GUILayout.Box("<b>" + host.comment + "</b>");
                    }

                    GUILayout.Box("<b>" + host.connectedPlayers + " / " + host.playerLimit + "</b>", GUILayout.MaxWidth(50));

                    if (uNet.GetPeerType() == NetworkPeerType.Disconnected && !uNet.isConnecting && host.connectedPlayers < host.playerLimit) {
                        if (GUILayout.Button("join", GUILayout.MaxWidth(80))) {
                            this.JoinLAN(host);
                        }
                    }

                    GUILayout.EndHorizontal();
                }




                if (lobbyWin.height > 200) {
                    GUILayout.EndScrollView();
                }

            }, "Lobby", GUILayout.Height(50), GUILayout.MinWidth(400));




            // new game window
            if (uNet.GetPeerType() == NetworkPeerType.Disconnected && !uNet.isConnecting) {

                GUILayout.Window(51, new Rect(paddingLeft, lobbyWin.height + 20, 300, 125), (int windowID) => {

                    GUILayout.BeginVertical(GUILayout.MaxWidth(300));

                    GUILayout.BeginHorizontal();
                    GUILayout.Label("LAN only", GUILayout.MaxWidth(80));

                    lanGame = GUILayout.Toggle(lanGame, "");

                    GUILayout.EndHorizontal();

                    GUILayout.BeginHorizontal();
                    GUILayout.Label("Gamename:", GUILayout.MaxWidth(80));
                    GUI.SetNextControlName("GamenameForm");
                    formGamename = GUILayout.TextField(formGamename, 30, GUILayout.MaxWidth(195));
                    GUILayout.EndHorizontal();

                    GUILayout.BeginHorizontal();
                    GUILayout.Label("Comment:", GUILayout.MaxWidth(80));
                    GUI.SetNextControlName("CommentForm");
                    formComment = GUILayout.TextField(formComment, 45, GUILayout.MaxWidth(195));
                    GUILayout.EndHorizontal();

                    GUILayout.BeginHorizontal();
                    GUILayout.Label("Players: " + this.defaultPlayerSize.ToString(), GUILayout.MaxWidth(80));
                    this.defaultPlayerSize = (int)GUILayout.HorizontalSlider(this.defaultPlayerSize, this.minPlayers, this.maxPlayers);

                    GUILayout.EndHorizontal();

                    if (GUILayout.Button("create", GUILayout.MaxWidth(80))) {

                        if (formGamename == "") {
                            this._errorString = "choose a right Gamename!";
                        } else {

                            this._errorString = "";
                            this.CreateGame(lanGame, formGamename, formComment, this.defaultPlayerSize);
                        }

                    }

                    GUILayout.EndVertical();

                }, "Create New Game", GUILayout.MaxWidth(300));

            }

            // actual game window & chat
            if (uNet.GetPeerType() == NetworkPeerType.Client || uNet.GetPeerType() == NetworkPeerType.Server && !uNet.isConnecting) {


                if (this.joinedLanGame) {
                    _actualGameName = uNet.GetActualHostLAN().gameName;
                } else {
                    _actualGameName = uNet.GetActualHost().gameName;
                }

                // actual game window
                GUILayout.Window(52, new Rect(paddingLeft, lobbyWin.height + 20, 295, 180), (int windowID) => {

                    // title & disconnect button
                    GUILayout.BeginHorizontal();

                    GUILayout.Label("Connected Players");

                    if (GUILayout.Button("disconnect", GUILayout.MaxWidth(80))) {
                        this.Disconnect();
                    }

                    GUILayout.EndHorizontal();



                    // list players
                    if (uNet.connectedPlayers.Count > 0) {

                        foreach (NetPlayer player in uNet.connectedPlayers.Values) {

                            GUILayout.BeginHorizontal();

                            if (player == uNet.GetNetworkPlayer()) {
                                GUILayout.Box("<b><color=brown>" + player.name + "</color></b>");
                            } else {
                                GUILayout.Box(player.name);
                            }

                            GUILayout.Box(uNet.GetNetworkPlayerPing(player).ToString());

                            if (player.ready) {
                                GUILayout.Box("<b><color=green>ready</color></b>", GUILayout.MaxWidth(45));
                            }

                            GUILayout.EndHorizontal();
                        }
                    }


                }, _actualGameName, GUILayout.MinWidth(300), GUILayout.MinHeight(180));

                // ready button
                GUILayout.BeginArea(new Rect(paddingLeft, lobbyWin.height + 205, 100, 30));

                if (uNet.GetNetworkPlayer().ready) {
                    _readyState = "I'm not ready";
                } else {
                    _readyState = "I'm ready";
                }

                if (GUILayout.Button(_readyState, GUILayout.MaxWidth(100))) {
                    this.ToggleReadyState();
                }

                GUILayout.EndArea();


                // chat window

                if (this.chat && this.chat.enabled) {

                    GUILayout.Window(53, new Rect(paddingLeft + 310, lobbyWin.height + 20, 295, 150), (int windowID) => {

                        chatScrollPosition = GUILayout.BeginScrollView(chatScrollPosition, GUILayout.MinHeight(150));

                        // autoscroll if new content is here
                        if (_chatCounter < chat.lobbyChat.Count) {
                            chatScrollPosition.y = Mathf.Infinity;
                            _chatCounter = chat.lobbyChat.Count;
                        }

                        foreach (LobbyMessage msg in chat.lobbyChat) {
                            string _color = "teal";

                            if (msg.author == uNet.playerName) {
                                _color = "brown";
                            }

                            GUILayout.Box("<i><size=10><color=" + _color + ">" + msg.date.Hour + ":" + msg.date.Minute + " " + msg.author + ":</color></size></i> " + msg.content);
                        }

                        GUILayout.EndScrollView();

                    }, "Chat", GUILayout.MinWidth(290));

                    // chat form input
                    GUILayout.BeginArea(new Rect(paddingLeft + 345, lobbyWin.height + 205, 285, 30));
                    GUILayout.BeginHorizontal();

                    GUI.SetNextControlName("ChatForm");
                    _chatForm = GUILayout.TextField(_chatForm, 100, GUILayout.MaxWidth(215));

                    if (GUILayout.Button("send", GUILayout.MaxWidth(40)) || GUI.GetNameOfFocusedControl() == "ChatForm" && Event.current.isKey && Event.current.keyCode == KeyCode.Return) {

                        chat.SendLobbyChatMessage(_chatForm);
                        _chatForm = "";
                    }

                    GUILayout.EndHorizontal();
                    GUILayout.EndArea();

                }
                
            }


            // nickname window
            GUILayout.Window(55, new Rect(Screen.width - 150, 10, 140, 50), (int windowID) => {

                GUILayout.BeginHorizontal();

                GUI.SetNextControlName("NameForm");
                _nickname = GUILayout.TextField(_nickname, 20, GUILayout.MaxWidth(85));

                if (UnityEngine.Event.current.type == EventType.Repaint) {
                    if (GUI.GetNameOfFocusedControl() == "NameForm") {
                        if (_nickname == "MaxMuster") {
                            _nickname = "";
                        }
                    }
                }

                if (GUILayout.Button("ok", GUILayout.MaxWidth(25)) || GUI.GetNameOfFocusedControl() == "NameForm" && Event.current.isKey && Event.current.keyCode == KeyCode.Return) {

                    if (_nickname != "" && _nickname != "MaxMuster" && _nickname.Length > 2) {

                        if (_nickname == this.uNet.playerName)
                            return;

                        Debug.Log("set new name");

                        if (this.chat && this.chat.enabled) {

                            if (Network.peerType == NetworkPeerType.Client || Network.peerType == NetworkPeerType.Server) {
                                chat.SendLobbyChatMessage("changed name to '" + _nickname + "'");
                            }
                        }

                        uNet.playerName = _nickname;
                        uNet.SpreadNetworkPlayer();

                    } else {
                        this._errorString = "Choose a right Nickname! Min. 3 characters";
                    }
                }

                GUILayout.EndHorizontal();

                if (uNet.isOnline) {
                    GUILayout.Label("online");
                } else {
                    GUILayout.Label("offline");
                }

            }, "Nickname");

        }

        void OnStateChange(NetworkPeerType peerType) {

            switch (peerType) {

                case NetworkPeerType.Server:
                    break;

                case NetworkPeerType.Client:
                    break;


                case NetworkPeerType.Connecting:
                    break;

                case NetworkPeerType.Disconnected:

                    this._chatCounter = 0;
                    this.joinedLanGame = false;
                    break;
            }

        }

        void OnAllPlayersReady() {

   
            if (Network.peerType == NetworkPeerType.Server) {

                if (uNet.connectedPlayers.Count >= this.minPlayers) {

                    Debug.Log("all players are ready");
                    GameObject.Find("_Logic").GetComponent<Logic>().StartGame();

                } else {
                    this._errorString = "At least " + this.minPlayers + " players needed to start a game";
                }
            } 
        }

        public void CreateGame(bool lan, string name, string comment, float playerSize) {

            if (lan) {
                this.joinedLanGame = true;
            } else {

                if (!uNet.isOnline) {

                    this._errorString = "you need to be online to create a internet game!";
                    return;
                }
            }

            uNet.RegisterGame(lan, name, comment, playerSize);
            uNet.UpdateHostList();
        }

        public void ToggleReadyState() {
            uNet.ToggleNetworkPlayerReadyState();
            //this.logic.InstantiateMyPlayer();
        }

        public void Join(HostData host) {
            uNet.ConnectPeer(host);
        }

        public void JoinLAN(HostDataLAN host) {
            this.joinedLanGame = true;
            uNet.ConnectPeerLAN(host);
        }

        public void Disconnect() {
            this.joinedLanGame = false;
            uNet.DisconnectPeer();
            uNet.UpdateHostList();
        }
    }
}
