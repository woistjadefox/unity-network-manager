using UnityEngine;

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
}
