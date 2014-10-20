using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Goga;

public class UILobby : MonoBehaviour {

    // network handler
    public UnityNetworkManager uNet;
    public int minPlayers = 2;
    public int maxPlayers = 32;
    public int defaultPlayerSize = 2;

    public int paddingLeft = 120;
    public int paddingTop = 10;

    private string formGamename = "";
    private string formComment = "";

    private Rect lobbyWin;

    private Vector2 lobbyScrollPosition = Vector2.zero;
    private Vector2 chatScrollPosition = Vector2.zero;

    private string _nickname = "MaxMuster";
    private string _readyState;
    private string _errorString = "";

    private string _chatForm = "";

    private int _chatCounter = 0;

	void Start () {

        uNet.newState += new ChangedCliendState(OnStateChange);

        lobbyWin = new Rect(paddingLeft, paddingTop, 600, 50);
	}
	
	void FixedUpdate () {
       
        // update host list every second
        if (Time.frameCount % 60 == 0) {

            uNet.UpdateHostList();
        }

	}

    void OnGUI() {

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

        // lobby window
        lobbyWin = GUILayout.Window(50, lobbyWin, (int windowID) => {

            if (uNet.GetLobbyList().Length > 0) {
                GUILayout.Label("Available Games");
            }
            else {
                GUILayout.Label("No open Games available");
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

                GUILayout.Box("<b>" +host.connectedPlayers + " / " + host.playerLimit + "</b>", GUILayout.MaxWidth(50));

                if (uNet.GetPeerType() == NetworkPeerType.Disconnected && host.connectedPlayers < host.playerLimit) {
                    if (GUILayout.Button("join", GUILayout.MaxWidth(80))) {
                        this.Join(host);
                    }
                }

                GUILayout.EndHorizontal();
            }

            if (lobbyWin.height > 200) {
                GUILayout.EndScrollView();
            }

        }, "Lobby", GUILayout.Height(50), GUILayout.MinWidth(400));


        // new game window
        if (uNet.GetPeerType() == NetworkPeerType.Disconnected) {

            GUILayout.Window(51, new Rect(paddingLeft, lobbyWin.height + 20, 300, 125), (int windowID) => {

                GUILayout.BeginVertical(GUILayout.MaxWidth(300));

                GUILayout.BeginHorizontal();
                GUILayout.Label("Gamename:", GUILayout.MaxWidth(80));
                formGamename = GUILayout.TextField(formGamename, 30, GUILayout.MaxWidth(195));
                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal();
                GUILayout.Label("Comment:", GUILayout.MaxWidth(80));
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
                        this.CreateGame(formGamename, formComment, this.defaultPlayerSize);
                        this._errorString = "";
                    }
                    
                }

                GUILayout.EndVertical();

            }, "Create New Game", GUILayout.MaxWidth(300));

        }

        // actual game window & chat
        if (uNet.GetPeerType() == NetworkPeerType.Client || uNet.GetPeerType() == NetworkPeerType.Server) {

            // actual game window
            GUILayout.Window(52, new Rect(paddingLeft, lobbyWin.height + 20, 295, 100), (int windowID) => {

                // title & disconnect button
                GUILayout.BeginHorizontal();

                GUILayout.Label("Connected Players");

                if (GUILayout.Button("disconnect", GUILayout.MaxWidth(80))) {
                    this.Disconnect();
                }

                GUILayout.EndHorizontal();


                // list players
                if (uNet.connectedPlayers.Count > 0) {
                    foreach (UnityNetworkPlayer player in uNet.connectedPlayers.Values) {

                        GUILayout.BeginHorizontal();

                        if (player == uNet.GetNetworkPlayer()) {
                            GUILayout.Box("<b><color=brown>" + player.name + "</color></b>");
                        }
                        else {
                            GUILayout.Box(player.name);
                        }

                        if (player.ready) {
                            GUILayout.Box("<b><color=green>ready</color></b>", GUILayout.MaxWidth(45));
                        }

                        GUILayout.EndHorizontal();
                    }
                }

                // ready button
                if (uNet.GetNetworkPlayer().ready) {
                    _readyState = "I'm not ready";
                }
                else {
                    _readyState = "I'm ready";
                }

                if (GUILayout.Button(_readyState, GUILayout.MaxWidth(100))) {
                    uNet.ToggleNetworkPlayerReadyState();
                }

            }, "Actual Game", GUILayout.MinWidth(300));

            // chat window
            GUILayout.Window(53, new Rect(paddingLeft + 310, lobbyWin.height + 20, 295, 150), (int windowID) => {

                chatScrollPosition = GUILayout.BeginScrollView(chatScrollPosition, GUILayout.MinHeight(150));

                foreach (LobbyChatMessage msg in uNet.lobbyChat) {
                    string _color = "teal";

                    if (msg.author == uNet.playerName) {
                        _color = "brown";
                    }

                    GUILayout.Box("<i><size=10><color="+ _color +">" + msg.date.Hour + ":" + msg.date.Minute + " " + msg.author + ":</color></size></i> " + msg.content);
                }

                GUILayout.EndScrollView();

                GUILayout.BeginHorizontal();
                
                GUI.SetNextControlName("ChatForm");
                _chatForm = GUILayout.TextField(_chatForm, GUILayout.MaxWidth(215));

                if (GUILayout.Button("send", GUILayout.MaxWidth(40)) || GUI.GetNameOfFocusedControl() == "ChatForm" && Event.current.isKey && Event.current.keyCode == KeyCode.Return) {
                    
                    uNet.SendLobbyChatMessage(_chatForm);
                    _chatForm = "";
                    GUI.FocusWindow(53);
                    GUI.FocusControl("ChatForm");
                }

                GUILayout.EndHorizontal();

                if (_chatCounter < uNet.lobbyChat.Count) {
                    chatScrollPosition.y = Mathf.Infinity;
                    _chatCounter = uNet.lobbyChat.Count;
                }

            }, "Chat", GUILayout.MinWidth(290));

        }


        // nickname window
        GUILayout.Window(54, new Rect(Screen.width - 150, 10, 140, 50), (int windowID) => {

            GUILayout.BeginHorizontal();

            _nickname = GUILayout.TextField(_nickname, 20, GUILayout.MaxWidth(85));

            if (GUILayout.Button("ok", GUILayout.MaxWidth(25))) {

                if (_nickname != uNet.playerName && _nickname != "" && _nickname != "MaxMuster" && _nickname.Length > 2) {
                    
                    Debug.Log("set new name");

                    if (Network.peerType == NetworkPeerType.Client || Network.peerType == NetworkPeerType.Server) {
                        uNet.SendLobbyChatMessage("changed name to '"+_nickname+"'");
                    }

                    uNet.playerName = _nickname;
                    uNet.SpreadNetworkPlayer();

                    
                }
                else {
                    this._errorString = "Choose a right Nickname! Min. 3 characters";
                }
            }

            GUILayout.EndHorizontal();

        }, "Nickname");

    }

    void OnStateChange(NetworkPeerType peerType) {

        switch (peerType)
        {

            case NetworkPeerType.Server:

                //Debug.Log("I am a server");
                break;

            case NetworkPeerType.Client:

                //Debug.Log("I am a client");
                break;


            case NetworkPeerType.Connecting:

                //Debug.Log("I am connecting");
                break;

            case NetworkPeerType.Disconnected:

                this._chatCounter = 0;

                //Debug.Log("I am diconnected");
                break;
        }

    }

    public void CreateGame(string name, string comment, float playerSize) {

        uNet.RegisterGame(name, comment, playerSize);
    }

    public void Join(HostData host) {
        uNet.ConnectPeer(host);
    }

    public void Disconnect() {
        uNet.DisconnectPeer();
    }
}
