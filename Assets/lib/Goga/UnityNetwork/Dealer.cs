using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace Goga.UnityNetwork {

    public enum PrefabType {
        Player
    }

    public class RPCCallObject {

        public GameObject prefab;
        public Vector3 pos;
        public Quaternion rot;
        public int groupId = 0;

    }

    public class Dealer : MonoBehaviour {

        public Manager uNet;
        public GameObject prefabPlayer;

        public List<RPCCallObject> rpcCallBuffer = new List<RPCCallObject>();


        // Use this for initialization
        void Start() {
            uNet.newState += new ChangedCliendState(OnStateChange);
        }

        GameObject GetPrefab(PrefabType type) {

            switch (type) {
                case PrefabType.Player:
                    return this.prefabPlayer;
            }

            return null;
        }

        [RPC]
        void InstantiateNetworkObject(int prefab, Vector3 pos, Quaternion rot, string playerId, NetworkViewID viewId) {

            if (Network.isServer) {
                viewId = Network.AllocateViewID();
            }

            GameObject _prefab = this.GetPrefab((PrefabType)prefab);

            GameObject _obj = Instantiate(_prefab, pos, rot) as GameObject;

            Debug.Log("playerid:" + playerId);

            // set player name
            _obj.networkView.viewID = viewId;
            _obj.GetComponent<NetObject>().playerGuid = playerId;
            _obj.GetComponent<NetObject>().type = (PrefabType)prefab;

            if (Network.isServer) {

                networkView.RPC("InstantiateNetworkObject", RPCMode.Others, prefab, pos, rot, playerId, viewId);
            }

        }

        public void RequestNetworkObject(PrefabType prefab, Vector3 pos, Quaternion rot) {

            if (Network.isServer) {

                this.InstantiateNetworkObject((int)prefab, pos, rot, Network.player.guid, Network.AllocateViewID());
 
            } else {
                networkView.RPC("InstantiateNetworkObject", RPCMode.Server, (int)prefab, pos, rot, Network.player.guid, Network.AllocateViewID());
            }

        }

        public void InstantiateAllNetworkObjects(PrefabType type, NetworkPlayer targetPlayer) {

            NetObject[] players = FindObjectsOfType<NetObject>();

            foreach (NetObject player in players) {

                if (player.type == type) {
                    networkView.RPC("InstantiateNetworkObject", targetPlayer, (int)type, player.transform.position, player.transform.rotation, player.playerGuid, player.networkView.viewID);
                }
            }
        }

        void OnStateChange(NetworkPeerType peerType) {

            switch (peerType) {
                case NetworkPeerType.Disconnected:
                    break;
            }

        }

    }
}
