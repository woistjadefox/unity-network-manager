using UnityEngine;
using System.Collections;

namespace Goga.UnityNetwork {

    [RequireComponent(typeof(NetworkView))]
    public class UnityNetworkObject : MonoBehaviour {

        private NetworkPlayer owner;
        public string playerId;
        public double lastDataTime;
        public Vector3 lastPos;

        public NetworkPlayer GetOwner() {
            return this.owner;
        }

        // set the owner of the object
        [RPC]
        public void SetOwner(NetworkPlayer player) {

            if (Network.peerType == NetworkPeerType.Client) {
                Debug.Log("CLIENT: RPC SetOwner came in with playerid:" + player.guid);
            } else {
                Debug.Log("SERVER: RPC SetOwner came in with playerid:" + player.guid);
            }

            this.owner = player;
            this.playerId = this.owner.guid;

        }
    }
}
