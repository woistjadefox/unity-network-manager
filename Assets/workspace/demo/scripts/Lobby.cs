using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Goga.UnityNetwork;


public class Lobby : MonoBehaviour {

    // network handler
    private Manager uNet;

    // settings
    public bool allowPasswordProtection = true;
    public int minPlayers = 2;
    public int maxPlayers = 32;
    public int defaultPlayerSize = 2;

    // lobby window
    [HideInInspector]
    private int paddingLeft = 10;
    [HideInInspector]
    private int paddingTop = 10;

    #region temp vars
    private bool showLobby = true;
    private bool lanGame = false;
    private bool joinedLanGame = false;
    private string formGamename = "";
    private string formComment = "";
    private Vector2 lobbyScrollPosition = Vector2.zero;
    private Vector2 chatScrollPosition = Vector2.zero;
    private string _nickname = "MaxMuster";
    private string _errorString = "";
    private string _actualGameName = "";
    private string _chatForm = "";
    private int _chatCounter = 0;
    #endregion

    private Rect lobbyWin;
    private Rect errorWindow;
    private Rect connectingWindow;
    private Rect newGameWindow;
    private Rect actualGameWindow;
    private Rect chatWindow;
    private Rect chatInputWindow;
    private Rect nicknameWindow;

    void Start() {

        this.uNet = FindObjectOfType<Manager>();
        uNet.newState += new ChangedCliendState(OnStateChange);

        // set default size for lobby window
        lobbyWin = new Rect(paddingLeft, paddingTop, 600, 50);
    }

    #region methods
    public void ToggleLobby() {

        if (showLobby) {
            showLobby = false;
        } else {
            showLobby = true;
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
    }
    #endregion

    #region loop methods

    void FixedUpdate() {

        // update host list every second
        if (Time.frameCount % 60 == 0) {
            uNet.UpdateHostList();
        }
    }

    void Update() {

        if (Input.GetKeyDown(KeyCode.Tab)) {
            this.ToggleLobby();
        }
    }

    void UpdateRects() {

        errorWindow = new Rect((Screen.width) - 260, (Screen.height) - 110, 250, 50);
        connectingWindow = new Rect((Screen.width) - 260, (Screen.height) - 110, 250, 50);
        newGameWindow = new Rect(paddingLeft, lobbyWin.height + 20, 300, 125);
        actualGameWindow = new Rect(paddingLeft, lobbyWin.height + 20, 295, 180);
        chatWindow = new Rect(paddingLeft + 310, lobbyWin.height + 20, 295, 150);
        chatInputWindow = new Rect(paddingLeft + 345, lobbyWin.height + 205, 285, 30);
        nicknameWindow = new Rect(Screen.width - 150, 10, 140, 50);
    }

    void OnGUI() {

        if (!this.showLobby) {
            return;
        }

        GUI.depth = -99;
        GUI.skin.box.alignment = TextAnchor.MiddleLeft;
        GUI.skin.box.wordWrap = true;

        this.UpdateRects();

        // error window
        if (this._errorString != "") {

            GUILayout.Window(98, this.errorWindow, (int windowID) => {

                GUILayout.Box("<b><color=red>" + _errorString + "</color></b>", GUILayout.MaxWidth(250));

                if (GUILayout.Button("okay", GUILayout.MaxWidth(80))) {
                    this._errorString = "";
                }

            }, "Error", GUILayout.MaxWidth(250), GUILayout.MaxHeight(200));

        }

        // connecting window
        if (uNet.GetPeerType() == NetworkPeerType.Connecting || uNet.isConnecting) {
            GUILayout.Window(97, this.connectingWindow, (int windowID) => {

                GUILayout.Box("<b>Connecting...</b>", GUILayout.MaxWidth(250));
                    
                if (GUILayout.Button("cancel")) {
                    this.Disconnect();
                }

            }, "Connecting", GUILayout.MaxWidth(250), GUILayout.MaxHeight(200));
        }

        // nickname window
        GUILayout.Window(55, this.nicknameWindow, (int windowID) => {

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

                    if (this.uNet.chat && this.uNet.chat.enabled) {

                        if (Network.peerType == NetworkPeerType.Client || Network.peerType == NetworkPeerType.Server) {
                            uNet.chat.SendLobbyChatMessage("changed name to '" + _nickname + "'");
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

        // lobby window
        lobbyWin = GUILayout.Window(50, lobbyWin, (int windowID) => {

            if (uNet.isOnline) {

                GUILayout.BeginHorizontal();

                // internet hosts
                if (uNet.GetLobbyList().Length > 0) {
                    GUILayout.Label("Available Internet Games");
                } else {
                    GUILayout.Label("No open Internet Games available");
                }

                if (GUILayout.Button("refresh", GUILayout.MaxWidth(80))) {

                    uNet.UpdateHostList();
                }

                GUILayout.EndHorizontal();

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
            if (this.uNet.discovery.GetLanHostData().Length > 0) {
                GUILayout.Label("Available LAN Games");
            } else {
                GUILayout.Label("No open LAN Games available");
            }

            foreach (HostDataLAN host in uNet.discovery.GetLanHostData()) {

                try {
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
                
                } catch (System.ArgumentException e) {
                    e.Equals(null);
                }
               
            }

            if (lobbyWin.height > 200) {
                GUILayout.EndScrollView();
            }

        }, "Lobby", GUILayout.Height(50), GUILayout.MinWidth(400));

        // new game window
        if (uNet.GetPeerType() == NetworkPeerType.Disconnected && !uNet.isConnecting) {

            GUILayout.Window(51, this.newGameWindow, (int windowID) => {

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
            GUILayout.Window(52, this.actualGameWindow, (int windowID) => {

                // title & disconnect button
                GUILayout.BeginHorizontal();

                GUILayout.Label("Connected Players");

                if (GUILayout.Button("disconnect", GUILayout.MaxWidth(80))) {
                    this.Disconnect();
                }

                GUILayout.EndHorizontal();


                // list players
                if (uNet.netPlayers.GetList().Count > 0) {

                    foreach (NetPlayer player in uNet.netPlayers.GetList()) {

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

            // chat window

            if (this.uNet.chat && this.uNet.chat.enabled) {

                GUILayout.Window(53, this.chatWindow, (int windowID) => {

                    chatScrollPosition = GUILayout.BeginScrollView(chatScrollPosition, GUILayout.MinHeight(150));

                    // autoscroll if new content is here
                    if (_chatCounter < uNet.chat.lobbyChat.Count) {
                        chatScrollPosition.y = Mathf.Infinity;
                        _chatCounter = uNet.chat.lobbyChat.Count;
                    }

                    foreach (LobbyMessage msg in uNet.chat.lobbyChat) {

                        string _color = "white";

                        if (msg.guid == uNet.GetNetworkPlayer().guid) {
                            _color = "brown";
                        }

                        GUILayout.Box("<i><size=10><color=" + _color + ">" + msg.date.Hour + ":" + msg.date.Minute + " " + msg.author + ":</color></size></i> " + msg.content);
                    }

                    GUILayout.EndScrollView();

                }, "Chat", GUILayout.MinWidth(290));

                // chat form input
                GUILayout.BeginArea(this.chatInputWindow);
                GUILayout.BeginHorizontal();

                GUI.SetNextControlName("ChatForm");
                _chatForm = GUILayout.TextField(_chatForm, 100, GUILayout.MaxWidth(215));

                if (GUILayout.Button("send", GUILayout.MaxWidth(40)) || GUI.GetNameOfFocusedControl() == "ChatForm" && Event.current.isKey && Event.current.keyCode == KeyCode.Return) {

                    uNet.chat.SendLobbyChatMessage(_chatForm);
                    _chatForm = "";
                }

                GUILayout.EndHorizontal();
                GUILayout.EndArea();

            }
                
        }

    }

    #endregion

    #region events

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

                if (!this.uNet.isReconnecting) {
                    this.joinedLanGame = false;
                }

                break;
        }

    }

    #endregion



}

