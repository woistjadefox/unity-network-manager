using UnityEngine;
using System;


namespace Goga.UnityNetwork {

    public class LobbyMessage {

        public string guid;
        public string author;
        public string content;
        public DateTime date;

        public LobbyMessage(string content) {
            this.content = content;
            this.date = DateTime.Now;
        }

    }
}
