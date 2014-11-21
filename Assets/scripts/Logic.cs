using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Goga.UnityNetwork;


public class Logic : MonoBehaviour {

    public Manager uNet;
    public Dealer dealer;
    public Lobby lobby;
    public GameObject prefabPlayer;

    private bool gameRunning;

	void Start () {

        this.gameRunning = false;
        uNet.newState += new ChangedCliendState(OnStateChange);

	}

    public bool IsGameRunning() {
        return this.gameRunning;
    }

    public void StartGame() {

        if (!this.IsGameRunning()) {

            Debug.Log("SERVER: start game");
            this.gameRunning = true;
        }

    }

    public void InstantiateMyPlayer() {

        this.dealer.RequestNetworkObject(PrefabType.Player, new Vector3(Random.Range(-8, 4), 0.6f, Random.Range(0, 5)), Quaternion.identity);
    }

    void OnStateChange(NetworkPeerType peerType) {

        switch (peerType) {
            case NetworkPeerType.Disconnected:

                this.gameRunning = false;
                break;
        }

    }

    void Update() {

        if (Input.GetKeyDown(KeyCode.Escape)) {

            this.lobby.ToggleLobby();
        }
    }
}
