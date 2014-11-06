using UnityEngine;
using System.Collections;

namespace Goga.UnityNetwork {

    [RequireComponent(typeof(NetworkView))]
    public class UnityNetworkObject : MonoBehaviour {

        private UnityNetworkManager uNet;
        public string playerGuid;

        void Start() {
            this.uNet = FindObjectOfType<UnityNetworkManager>();
        }

        public UnityNetworkManager GetManager() {
            return this.uNet;
        }

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
