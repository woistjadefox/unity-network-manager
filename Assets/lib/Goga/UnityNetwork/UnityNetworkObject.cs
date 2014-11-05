using UnityEngine;
using System.Collections;

namespace Goga.UnityNetwork {

    [RequireComponent(typeof(NetworkView))]
    public class UnityNetworkObject : MonoBehaviour {

        public string playerGuid;

        public bool IsMine() {

            if (Network.player.guid == this.playerGuid) {
                return true;
            }

            return false;
        }

        // set the owner of the object
        [RPC]
        public void SetOwner(string guid) {

            this.playerGuid = guid;

        }
    }
}
