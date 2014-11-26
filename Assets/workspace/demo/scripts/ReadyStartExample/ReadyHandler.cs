using UnityEngine;
using System.Collections;
using Goga.UnityNetwork;

public class ReadyHandler : MonoBehaviour {

    private Manager uNet;
    public LogicReadyStart logic;
    public bool onlyStartIfFull = false;
    public int minimumPlayers = 2;


    private string readyState; 

	void Start () {

        this.uNet = FindObjectOfType<Manager>();
        this.uNet.onAllPlayersReady += new OnAllPlayersReady(OnAllPlayersReady);
	}

    void OnAllPlayersReady() {

        // check if ready trigger only starts if server is full
        if (this.onlyStartIfFull) {

            if (this.uNet.IsServerFull()) {
                this.StartGame();
            }

        } else {

            if (this.uNet.netPlayers.GetList().Count >= this.minimumPlayers) {
                this.StartGame();
            }

        }
    }

    void StartGame() {

        Debug.Log("all players are ready, server can start the game..");
        this.logic.StartGame();
    }

    void OnGUI() {

        if (Network.isServer || Network.isClient) {

            if (!this.logic.IsGameRunning()) {

                // ready button
                GUILayout.BeginArea(new Rect(Screen.width - 110, Screen.height - 60, 100, 30));

                if (uNet.GetNetworkPlayer().ready) {
                    readyState = "I'm not ready";
                } else {
                    readyState = "I'm ready";
                }

                if (GUILayout.Button(readyState, GUILayout.MaxWidth(100))) {
                    uNet.ToggleNetworkPlayerReadyState();
                }

                GUILayout.EndArea();
            }
        }

    }

}
