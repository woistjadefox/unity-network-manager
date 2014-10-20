using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Goga;

public class UILobby : MonoBehaviour {

    // network handler
    public UnityNetworkManager uNet;
    public int minPlayers = 2;
    public int maxPlayers = 32;
    public Texture2D readyImg;


    private string formGamename = "";
    private string formComment = "";

    private Rect lobbyWin = new Rect(10, 10, 600, 50);

    private Vector2 lobbyScrollPosition = Vector2.zero;

    private string _nickname = "MaxMuster";
    private string _readyState;
    private string _errorString = "";

    private float sliderPlayers = 2;

	void Start () {

        uNet.newState += new ChangedCliendState(OnStateChange);
	}
	
	void FixedUpdate () {

        if (Time.frameCount % 120 == 0) {

            uNet.UpdateHostList();
        }
	}

    void OnGUI() {

        if (Time.frameCount < 120) {
            return;
        }

        GUILayout.BeginVertical();

        // error window
        if (this._errorString != "") {

            GUILayout.Window(99, new Rect((Screen.width / 2) - 125, (Screen.height / 2) - 50, 250, 50), (int windowID) => {

                GUI.skin.box.wordWrap = true;
                GUILayout.Box("<b><color=red>" + _errorString + "</color></b>", GUILayout.MaxWidth(250));

                if (GUILayout.Button("okay", GUILayout.MaxWidth(80))) {
                    this._errorString = "";
                }

            }, "Error", GUILayout.MaxWidth(250), GUILayout.MaxHeight(400));

        }

        // lobby window
        lobbyWin = GUILayout.Window(0, lobbyWin, (int windowID) => {

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

            GUILayout.Window(1, new Rect(10, lobbyWin.height + 20, 300, 125), (int windowID) => {

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
                GUILayout.Label("Players: " + sliderPlayers.ToString(), GUILayout.MaxWidth(80));
                sliderPlayers = GUILayout.HorizontalSlider(sliderPlayers, this.minPlayers, this.maxPlayers);
                sliderPlayers = (int)sliderPlayers;
                GUILayout.EndHorizontal();

                if (GUILayout.Button("create", GUILayout.MaxWidth(80))) {

                    if (formGamename == "") {
                        this._errorString = "choose a right Gamename!";
                    } else {
                        this.CreateGame(formGamename, formComment, sliderPlayers);
                        this._errorString = "";
                    }
                    
                }

                GUILayout.EndVertical();

            }, "Create New Game", GUILayout.MaxWidth(300));

        }

        // actual game window
        if (uNet.GetPeerType() == NetworkPeerType.Client || uNet.GetPeerType() == NetworkPeerType.Server) {

            GUILayout.Window(2, new Rect(10, lobbyWin.height + 20, 600, 100), (int windowID) => {

                GUILayout.BeginHorizontal();

                GUILayout.Label("Connected Players");

                if (GUILayout.Button("disconnect", GUILayout.MaxWidth(80))) {
                    this.Disconnect();
                }

                GUILayout.EndHorizontal();
                
                foreach (UnityNetworkPlayer player in uNet.connectedPlayers.Values) {

                    GUILayout.BeginHorizontal();

                    if(player == uNet.GetNetworkPlayer()){
                        GUILayout.Box("<b><color=brown>"+player.name+"</color></b>");
                    } else {
                        GUILayout.Box(player.name);
                    }

                    if (player.ready) {
                        GUILayout.Box(this.readyImg, GUILayout.MaxWidth(22), GUILayout.MaxHeight(22));
                    }

                    GUILayout.EndHorizontal();
                }

                GUILayout.BeginVertical();

                if (uNet.GetNetworkPlayer().ready) {
                    _readyState = "I'm not ready";
                }
                else {
                    _readyState = "I'm ready";
                }

                if (GUILayout.Button(_readyState, GUILayout.MaxWidth(100))) {

                    uNet.ToggleNetworkPlayerReadyState();
                }

                GUILayout.EndVertical();

            }, "Actual Game", GUILayout.MinWidth(400));

        }



        // nickname window
        GUILayout.Window(3, new Rect(Screen.width - 150, 10, 140, 50), (int windowID) => {

            GUILayout.BeginHorizontal();

            _nickname = GUILayout.TextField(_nickname, 20, GUILayout.MaxWidth(85));

            if (GUILayout.Button("ok", GUILayout.MaxWidth(25))) {

                if (_nickname != "" && _nickname != "MaxMuster" && _nickname.Length > 2) {
                    Debug.Log("set new name");
                    uNet.playerName = _nickname;
                    uNet.SpreadNetworkPlayer();
                }
                else {
                    this._errorString = "Choose a right Nickname! Min. 3 characters";
                }
            }

            GUILayout.EndHorizontal();

        }, "Nickname");

        GUILayout.EndVertical();
    }

    void OnStateChange(NetworkPeerType peerType) {

        switch (peerType)
        {

            case NetworkPeerType.Server:

                Debug.Log("I am a server");
                break;

            case NetworkPeerType.Client:

                Debug.Log("I am a client");
                break;


            case NetworkPeerType.Connecting:

                Debug.Log("I am connecting");
                break;

            case NetworkPeerType.Disconnected:

                Debug.Log("I am diconnected");
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
