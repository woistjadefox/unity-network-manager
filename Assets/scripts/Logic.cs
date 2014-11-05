using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Goga.UnityNetwork;


public class Logic : MonoBehaviour {

    public UnityNetworkManager uNet;
    public GameObject prefabPlayer;

    private bool gameRunning;

	// Use this for initialization
	void Awake () {

        this.gameRunning = false;
	}

    public bool IsGameRunning() {
        return this.gameRunning;
    }

    public void StartGame() {


        if (!this.IsGameRunning()) {

            Debug.Log("SERVER: start game");


            // instantiate client players
            foreach (UnityNetworkPlayer player in uNet.connectedPlayers.Values) {

                GameObject _player = Network.Instantiate(this.prefabPlayer, new Vector3(Random.Range(-8, 4), 0.6f, Random.Range(0, 5)), Quaternion.identity, 0) as GameObject;

                // tell player about it's property
                _player.networkView.RPC("SetOwner", RPCMode.AllBuffered, player.guid);

            }

            this.gameRunning = true;
        }

    }

    public void InstantiateNewPlayer() {

    }
}
