using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Goga.UnityNetwork;


public class LogicReadyStart : MonoBehaviour {

    public Manager uNet;
    public GameObject prefabPlayer;
    public bool lockServerAfterStart = true;
   
    private bool gameRunning;

	void Start () {

        this.gameRunning = false;

        // attach onStateChange event
        uNet.newState += new ChangedCliendState(OnStateChange);

        // configure prefabs in dealer
        this.uNet.dealer.AddPrefab(NetworkPrefabs.Player, this.prefabPlayer);

	}

    public bool IsGameRunning() {
        return this.gameRunning;
    }

    public void StartGame() {

        if (!this.IsGameRunning()) {

            Debug.Log("Logic: start game");

            if (this.lockServerAfterStart) {
                // set player limit to actual ammount (-1 for the server client)
                this.uNet.ChangePlayerLimit(this.uNet.netPlayers.GetList().Count - 1);
            }

            this.InstantiateMyPlayer();

            this.gameRunning = true;
        }

    }

    public void InstantiateMyPlayer() {

        this.uNet.dealer.RequestNetworkObject(NetworkPrefabs.Player, new Vector3(Random.Range(-8, 4), 0.6f, Random.Range(0, 5)), Quaternion.identity);
    }

    void OnServerInitialized() {

        // check if a host migration happend
        if (this.IsGameRunning()) {

            // set player limit to actual ammount
            this.uNet.ChangePlayerLimit(this.uNet.netPlayers.GetList().Count - 1);
        }
    }


    void OnStateChange(NetworkPeerType peerType) {

        switch (peerType) {

            case NetworkPeerType.Disconnected:

                if (!this.uNet.isReconnecting) {
                    this.gameRunning = false;
                }

                break;
        }

    }

}
