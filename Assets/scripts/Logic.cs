using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Goga.UnityNetwork;


public class Logic : MonoBehaviour {

    public Manager uNet;
    public GameObject prefabPlayer;

    private bool gameRunning;

	// Use this for initialization
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


            // instantiate client players
            foreach (NetPlayer player in uNet.connectedPlayers.Values) {

                GameObject _player = Network.Instantiate(this.prefabPlayer, new Vector3(Random.Range(-8, 4), 0.6f, Random.Range(0, 5)), Quaternion.identity, 0) as GameObject;

                // tell player about it's property
                _player.networkView.RPC("SetOwner", RPCMode.AllBuffered, player.guid);

            }

            this.gameRunning = true;
        }

    }

    public void InstantiateNewPlayer() {

    }

    void OnStateChange(NetworkPeerType peerType) {

        switch (peerType) {
            case NetworkPeerType.Disconnected:

                this.gameRunning = false;
                break;
        }

    }
}
