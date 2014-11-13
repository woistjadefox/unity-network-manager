using UnityEngine;
using System.Collections;

namespace Goga.UnityNetwork {

    public class HostMigration : MonoBehaviour {

        public float waitForRecreateServer = 3;
        public float waitForReconnect = 5;

        private Manager uNet;

        void Start() {

            this.uNet = GetComponent<Manager>();
        }

       public void Migrate() {

            this.uNet.isReconnecting = true;

            // remove actual server from the top
            IEnumerator enumeratorLast = this.uNet.connectedPlayers.Values.GetEnumerator();
            enumeratorLast.MoveNext();
            NetPlayer actualServer = (NetPlayer)enumeratorLast.Current;
            this.uNet.connectedPlayers.Remove(actualServer.guid);

            // get new server from the top
            IEnumerator enumerator = this.uNet.connectedPlayers.Values.GetEnumerator();
            enumerator.MoveNext();

            if (enumerator.Current != null) {

                NetPlayer newServerPlayer = (NetPlayer)enumerator.Current;

                string newServerGUID = newServerPlayer.guid;

                // check if i'm the next server host
                if (newServerGUID == Network.player.guid) {

                    Debug.Log("I have to be the new Server!");
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

            this.uNet.isReconnecting = false;

        }

        IEnumerator ReconnectToNewServer(string guid) {

            Debug.Log("connecting to new server...");
            yield return new WaitForSeconds(this.waitForReconnect);

            this.uNet.ConnectPeer(guid);

            this.uNet.isReconnecting = false;
        }

        void OnConnectedToServer() {

            Debug.Log("HostMigration: Reconnect to the server done.. send rpc buffer from netObjects");

            /*
            NetObject[] playerObjs = FindObjectsOfType<NetObject>() as NetObject[];

            foreach (NetObject obj in playerObjs) {

                if (obj.playerGuid == Network.player.guid) {

                    obj.SendLastRpcCalls();
                }

            }
            */

            
        }
    }

}
