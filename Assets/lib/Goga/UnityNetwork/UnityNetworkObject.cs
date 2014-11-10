using UnityEngine;
using System.Collections;
using System.Diagnostics;

namespace Goga.UnityNetwork {

    public enum SendType {
        Bool, Int, Float, String
    }

    public class SendObj {

        public SendType type;
        public bool boolData;
        public int intData;
        public float floatData;
        public string stringData;

        public SendObj(SendType type) {
            this.type = type;
        }
    }

    [RequireComponent(typeof(NetworkView))]

    public class UnityNetworkObject : MonoBehaviour {

        private UnityNetworkManager uNet;
        public string playerGuid;

        private StackFrame _frame;
        private string _callerMethod;

        void Start() {
            this.uNet = FindObjectOfType<UnityNetworkManager>();
        }

        // set the owner of the object
        [RPC]
        public void SetOwner(string guid) {

            this.playerGuid = guid;

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


        public bool RoleObserverBase(SendObj state, int senderID, bool broadcastToAll, bool allowLocalAction) {

            // get caller method name
            _frame = new StackFrame(2);
            _callerMethod = _frame.GetMethod().Name;


            if (Network.isClient && this.IsMine() && senderID != 1) {

                switch (state.type) {
                    case SendType.Bool: 
                        networkView.RPC(_callerMethod, RPCMode.Server, state.boolData, 0);
                        break;

                    case SendType.Int:
                        networkView.RPC(_callerMethod, RPCMode.Server, state.intData, 0);
                        break;

                    case SendType.Float:
                        networkView.RPC(_callerMethod, RPCMode.Server, state.floatData, 0);
                        break;

                    case SendType.String:
                        networkView.RPC(_callerMethod, RPCMode.Server, state.stringData, 0);
                        break;
                }


                if (allowLocalAction) {
                    return true;
                }
            }

            if (Network.isServer) {

                if (broadcastToAll) {

                    UnityEngine.Debug.Log("server broadcasting: " + _callerMethod);

                    switch (state.type) {
                        case SendType.Bool:
                            networkView.RPC(_callerMethod, RPCMode.OthersBuffered, state.boolData, 1);
                            break;

                        case SendType.Int:
                            networkView.RPC(_callerMethod, RPCMode.OthersBuffered, state.intData, 1);
                            break;

                        case SendType.Float:
                            networkView.RPC(_callerMethod, RPCMode.OthersBuffered, state.floatData, 1);
                            break;

                        case SendType.String:
                            networkView.RPC(_callerMethod, RPCMode.OthersBuffered, state.stringData, 1);
                            break;
                    }
                }

                return true;
            }

            if (senderID == 1 && !this.IsMine()) {
                return true;
            }

            if (!allowLocalAction && senderID == 1) {
                return true;

            }

            return false;
        }

        public bool RoleObserver(bool state, int senderID, bool broadcastToAll, bool allowLocalAction) {

            SendObj obj = new SendObj(SendType.Bool);
            obj.boolData = state;

            return this.RoleObserverBase(obj, senderID, broadcastToAll, allowLocalAction);
        }

        public bool RoleObserver(int state, int senderID, bool broadcastToAll, bool allowLocalAction) {
            
            SendObj obj = new SendObj(SendType.Int);
            obj.intData = state;

            return this.RoleObserverBase(obj, senderID, broadcastToAll, allowLocalAction);
        }

        public bool RoleObserver(float state, int senderID, bool broadcastToAll, bool allowLocalAction) {
            
            SendObj obj = new SendObj(SendType.Float);
            obj.floatData = state;

            return this.RoleObserverBase(obj, senderID, broadcastToAll, allowLocalAction);
        }

        public bool RoleObserver(string state, int senderID, bool broadcastToAll, bool allowLocalAction) {

            SendObj obj = new SendObj(SendType.String);
            obj.stringData = state;

            return this.RoleObserverBase(obj, senderID, broadcastToAll, allowLocalAction);
        }

    }
}
