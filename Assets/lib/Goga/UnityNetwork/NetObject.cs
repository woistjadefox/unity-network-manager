using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using Goga.Tools;

namespace Goga.UnityNetwork {

    [RequireComponent(typeof(NetworkView))]

    public class NetObject : MonoBehaviour {

        [HideInInspector]
        public Manager uNet;
        [HideInInspector]
        public AnimationSynchronizer netAnimator;

        public string playerGuid;
        [HideInInspector]
        public NetworkPrefabs type;

        private StackFrame _frame;
        private string _callerMethod;

        public Dictionary<string, RPCCall> lastRPCCalls = new Dictionary<string, RPCCall>();
        public MaxStack<RPCCall> rpcCallBuffer = new MaxStack<RPCCall>(50);

        void Awake() {
            Network.isMessageQueueRunning = false;
        }

        void Start() {

            // get manager
            this.uNet = FindObjectOfType<Manager>();
            this.uNet.newState += new ChangedCliendState(OnStateChange);

            // check if netanimator is attached
            this.netAnimator = GetComponent<AnimationSynchronizer>();
            if (!this.netAnimator || !this.netAnimator.enabled) {
                this.netAnimator = null;
            }

            Network.isMessageQueueRunning = true;
        }

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

        public void SendBufferedRPCCallsToPlayer(NetworkPlayer player) {

            // send all rpc calls to player
            foreach(RPCCall call in rpcCallBuffer){
                networkView.RPC(call.method, player, call.data);
            }
        }

        public RPCCall GetLastRPCCall() {
            return rpcCallBuffer.Peek();
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


            if (Network.isClient) {

                this.lastRPCCalls[_callerMethod] = new RPCCall(_callerMethod, RPCMode.Server, data);

                if (Network.isClient && this.IsMine() && senderID != 1) {

                    // set senderID for client
                    data[data.Length - 1] = 0;

                    this.SendRPCCall(this.lastRPCCalls[_callerMethod]);

                    if (allowLocalAction) {
                        return true;
                    }
                }

                // save call in buffer when message comes from server
                if (senderID == 1) {
                    this.rpcCallBuffer.Push(new RPCCall(_callerMethod, RPCMode.Server, data));
                }

            }

            if (Network.isServer) {

                if (broadcastToAll) {
                        
                    // set senderID for server
                    data[data.Length-1] = 1;

                    // store last call
                    this.lastRPCCalls[_callerMethod] = new RPCCall(_callerMethod, RPCMode.Others, data);
                    this.SendRPCCall(this.lastRPCCalls[_callerMethod]);

                    // save call in buffer
                    this.rpcCallBuffer.Push(this.lastRPCCalls[_callerMethod]);
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

        void OnStateChange(NetworkPeerType peerType) {

            switch (peerType) {

                case NetworkPeerType.Disconnected:

                    /* since all objects get deleted from disconnected clients it's not needed to clear buffer
                     * 
                    if (!this.uNet.migration || !this.uNet.migration.isNewServer) {
                        this.lastRPCCalls.Clear();
                        this.rpcCallBuffer.Clear();
                    }*/

                    break;
            }

        }

    }
}
