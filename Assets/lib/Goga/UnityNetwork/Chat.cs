using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;


namespace Goga.UnityNetwork {

    public class Chat : MonoBehaviour {

        private Manager uNet;
        public List<LobbyMessage> lobbyChat = new List<LobbyMessage>();

        void Start() {
            this.uNet = GetComponent<Manager>();
        }

        public void ClearMessages() {
            this.lobbyChat.Clear();
        }


        public void SendLobbyChatMessage(string message) {

            if (message != "") {

                LobbyMessage msg = new LobbyMessage(message);
                msg.author = this.uNet.playerName;

                string _msg = this.uNet.jWriter.Write(msg);

                networkView.RPC("GetLobbyChatMessage", RPCMode.All, _msg);
            }
        }

        [RPC]
        void GetLobbyChatMessage(string message) {

            LobbyMessage _msg = this.uNet.jReader.Read<LobbyMessage>(message);
            lobbyChat.Add(_msg);
        }

        void OnPlayerConnected(NetworkPlayer player) {

            if (!enabled) {
                return;
            }

            if (!this.uNet.netPlayers.Exists(player.guid)) {

                // send player all chat messages
                foreach (LobbyMessage msg in lobbyChat) {

                    string _msg = this.uNet.jWriter.Write(msg);
                    networkView.RPC("GetLobbyChatMessage", player, _msg);
                }

            }

        }

    }
}
