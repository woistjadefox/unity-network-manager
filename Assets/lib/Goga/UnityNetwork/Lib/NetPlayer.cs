using UnityEngine;

namespace Goga.UnityNetwork {

    public class NetPlayer {
        public string guid;
        public string name;
        public bool ready = false;
        public double connectingTime;

        public NetPlayer() {
            connectingTime = Network.time;
        }
    }
}