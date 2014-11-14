using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;

namespace Goga.UnityNetwork {

    public class RPCCall {

        public string method;
        public RPCMode mode;
        public object[] data;

        public RPCCall(string method, RPCMode mode, object[] data) {

            this.method = method;
            this.mode = mode;
            this.data = data;
        }
    }

    [RequireComponent(typeof(NetworkView))]

    public class NetObject : MonoBehaviour {

        private Manager uNet;
        public string playerGuid;
        public PrefabType type;

        private StackFrame _frame;
        private string _callerMethod;

        public Dictionary<string, RPCCall> lastRPCCalls = new Dictionary<string, RPCCall>();

        void Awake() {

            Network.isMessageQueueRunning = false;
        }

        void Start() {
            this.uNet = FindObjectOfType<Manager>();
            Network.isMessageQueueRunning = true;
        }

        // set the owner of the object
        [RPC]
        public void SetOwner(string guid) {

            this.playerGuid = guid;
        }

        public void SendRPCCall(RPCCall call){
            networkView.RPC(call.method, call.mode, call.data);
        }

        public void SendLastRPCCalls() {

            foreach (RPCCall call in lastRPCCalls.Values) {
                this.SendRPCCall(call);
            }
        }

        public Manager GetManager() {
            return this.uNet;
        }

        public bool IsMine() {

            if (Network.player.guid == this.playerGuid) {
                return true;
            }

            return false;
        }

        public bool RoleObserver(object[] data, bool broadcastToAll, bool allowLocalAction) {

            int senderID = (int)data[data.Length-1];

            // get caller method name
            _frame = new StackFrame(1);
            _callerMethod = _frame.GetMethod().Name;


            if (Network.isClient && this.IsMine() && senderID != 1) {

                // set senderID for client
                data[data.Length-1] = 0;

                this.lastRPCCalls[_callerMethod] = new RPCCall(_callerMethod, RPCMode.Server, data);
                this.SendRPCCall(this.lastRPCCalls[_callerMethod]);

                if (allowLocalAction) {
                    return true;
                }
            }

            if (Network.isServer) {

                if (broadcastToAll) {

                    UnityEngine.Debug.Log("server broadcasting: " + _callerMethod);

                    // set senderID for server
                    data[data.Length-1] = 1;

                    networkView.RPC(_callerMethod, RPCMode.OthersBuffered, data);
                  
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

    }
}
