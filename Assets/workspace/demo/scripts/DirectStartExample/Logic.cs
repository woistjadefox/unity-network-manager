using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Goga.UnityNetwork;


public class Logic : MonoBehaviour {

    private Manager uNet;
    public GameObject prefabPlayer;

    private bool gameRunning;

	void Start () {

        this.gameRunning = false;

        this.uNet = FindObjectOfType<Manager>();
        uNet.newState += new ChangedCliendState(OnStateChange);

        // configure prefabs in dealer
        this.uNet.dealer.AddPrefab(NetworkPrefabs.Player, this.prefabPlayer);

	}

    public bool IsGameRunning() {
        return this.gameRunning;
    }

    public void StartGame() {

        if (!this.IsGameRunning()) {

            Debug.Log("ready to play..! ");
            this.gameRunning = true;

            this.InstantiateMyPlayer();
        }

    }

    public void InstantiateMyPlayer() {

        this.uNet.dealer.RequestNetworkObject(NetworkPrefabs.Player, new Vector3(Random.Range(-8, 4), 0.6f, Random.Range(0, 5)), Quaternion.identity);
    }

    void OnStateChange(NetworkPeerType peerType) {

        switch (peerType) {

            case NetworkPeerType.Server:

                if (!this.IsGameRunning()) {
                    this.StartGame();
                }

                break;

            case NetworkPeerType.Client:

                if (!this.IsGameRunning()) {
                    this.StartGame();
                }

                break;

            case NetworkPeerType.Disconnected:

                if (!this.uNet.isReconnecting) {
                    this.gameRunning = false;
                }

                break;
        }

    }

}
