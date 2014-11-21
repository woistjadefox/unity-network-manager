using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace Goga.UnityNetwork {

    public enum NetworkPrefabs {
        Player, Weapon, Bullet, Slot1, Slot2, Slot3, Slot4, Slot5, Slot6, Slot7, Slot8, Slot9, Slot10
    }

    public class Dealer : MonoBehaviour {

        public Manager uNet;

        private Dictionary<NetworkPrefabs, GameObject> netPrefabs = new Dictionary<NetworkPrefabs, GameObject>();

        void Start() {
            uNet.newState += new ChangedCliendState(OnStateChange);
        }

        public void AddPrefab(NetworkPrefabs type, GameObject prefab) {

            this.netPrefabs.Add(type, prefab);
        }

        GameObject GetPrefab(NetworkPrefabs type) {
            return this.netPrefabs[type];
        }

        public void SpreadNetObjRPCBuffersToPlayer(NetworkPlayer player) {

            NetObject[] objs = FindObjectsOfType<NetObject>();

            foreach (NetObject obj in objs) {
                obj.SendBufferedRPCCallsToPlayer(player);
            }

        }

        [RPC]
        void InstantiateNetworkObject(int prefab, Vector3 pos, Quaternion rot, string playerId, NetworkViewID viewId) {

            if (Network.isServer) {
                viewId = Network.AllocateViewID();
            }

            GameObject _prefab = this.GetPrefab((NetworkPrefabs)prefab);

            GameObject _obj = Instantiate(_prefab, pos, rot) as GameObject;

            // set player name
            _obj.networkView.viewID = viewId;
            _obj.GetComponent<NetObject>().playerGuid = playerId;
            _obj.GetComponent<NetObject>().type = (NetworkPrefabs)prefab;

            if (Network.isServer) {

                networkView.RPC("InstantiateNetworkObject", RPCMode.Others, prefab, pos, rot, playerId, viewId);
            }

        }

        public void RequestNetworkObject(NetworkPrefabs prefab, Vector3 pos, Quaternion rot) {

            if (Network.isServer) {

                this.InstantiateNetworkObject((int)prefab, pos, rot, Network.player.guid, Network.AllocateViewID());
 
            } else {
                networkView.RPC("InstantiateNetworkObject", RPCMode.Server, (int)prefab, pos, rot, Network.player.guid, Network.AllocateViewID());
            }

        }

        public void InstantiateAllNetworkObjects(NetworkPrefabs type, NetworkPlayer targetPlayer) {

            NetObject[] players = FindObjectsOfType<NetObject>();

            foreach (NetObject player in players) {

                if (player.type == type) {
                    
                    networkView.RPC("InstantiateNetworkObject", targetPlayer, (int)type, player.transform.position, player.transform.rotation, player.playerGuid, player.networkView.viewID);
                    player.SendBufferedRPCCallsToPlayer(targetPlayer);
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
