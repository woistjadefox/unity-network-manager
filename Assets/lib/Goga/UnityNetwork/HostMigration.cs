using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Goga.UnityNetwork {

    public class HostMigration : MonoBehaviour {

        private Manager uNet;

        public float waitForRecreateServer = 3;
        public float waitForReconnect = 5;

        [HideInInspector]
        public bool isNewServer = false;

        
        void Start() {

            this.uNet = GetComponent<Manager>();
        }

       public void Migrate() {

            this.uNet.isReconnecting = true;
     
            // remove old server from top and shift 
            this.uNet.netPlayers.GetList().RemoveAt(0);

            NetPlayer newServerPlayer = this.uNet.netPlayers.GetList()[0];

            if (newServerPlayer != null) {

                string newServerGUID = newServerPlayer.guid;

                // check if i'm the next server host
                if (newServerGUID == Network.player.guid) {

                    Debug.Log("I have to be the new Server!");
                    this.isNewServer = true;
                    StartCoroutine(this.StartNewServer());

                } else {

                    Debug.Log("I have to reconnect to guid:" + newServerGUID);
                    StartCoroutine(this.ReconnectToNewServer(newServerGUID));
                }
            }
   
        }

        IEnumerator StartNewServer() {

            yield return new WaitForSeconds(this.waitForRecreateServer);

            if (this.uNet.isLanOnly) {
                this.uNet.RegisterGame(this.uNet.isLanOnly, this.uNet.GetActualHostLAN().gameName, this.uNet.GetActualHostLAN().comment, this.uNet.GetActualHostLAN().playerLimit);
            } else {
                this.uNet.RegisterGame(false, this.uNet.GetActualHost().gameName, this.uNet.GetActualHost().comment, this.uNet.GetActualHost().playerLimit);
            }

            // get ownership of all objects
            this.GetOwnerShip();

        }

        IEnumerator ReconnectToNewServer(string guid) {

            yield return new WaitForSeconds(this.waitForReconnect);

            this.uNet.ConnectPeer(guid);

        }

        void GetOwnerShip() {

            NetObject[] objs = FindObjectsOfType<NetObject>() as NetObject[];

            foreach (NetObject obj in objs) {
                obj.networkView.viewID = Network.AllocateViewID();
            }

        }

        void OnServerInitialized() {

            if (!enabled) {
                return;
            }

            this.uNet.isReconnecting = false;
            this.isNewServer = false;
        }

        void OnConnectedToServer() {

            if (!enabled) {
                return;
            }

            Debug.Log("HostMigration: Reconnect to the server done.. send rpc buffer from netObjects");
            
        }
    }

}
