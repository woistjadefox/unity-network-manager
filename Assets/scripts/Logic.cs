using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Goga;


public class Logic : MonoBehaviour {

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

        Debug.Log("SERVER: start game");

        // instantiate server player
        GameObject _hostPlayer = Network.Instantiate(this.prefabPlayer, new Vector3(Random.Range(-8, 4), 0.6f, Random.Range(0, 5)), Quaternion.identity, 0) as GameObject;
        _hostPlayer.networkView.RPC("SetOwner", RPCMode.AllBuffered, Network.player);

        // instantiate client players
        foreach (NetworkPlayer player in Network.connections) {

            GameObject _player = Network.Instantiate(this.prefabPlayer, new Vector3(Random.Range(-8, 4), 0.6f, Random.Range(0, 5)), Quaternion.identity, 0) as GameObject;
                
            // tell player about it's property
            _player.networkView.RPC("SetOwner", RPCMode.AllBuffered, player);

        }

        this.gameRunning = true;
    }
}
