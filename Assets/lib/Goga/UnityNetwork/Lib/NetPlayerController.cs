using UnityEngine;
using System.Collections.Generic;
using System.Linq;

namespace Goga.UnityNetwork {

    public class NetPlayerController {

        List<NetPlayer> players = new List<NetPlayer>();

        public List<NetPlayer> GetList() {
            return players;
        }

        public void SortList() {
            this.players = this.GetList().OrderBy(o => o.connectingTime).ToList();
        }

        public bool Exists(string guid) {
            return this.players.Exists(o => o.guid == guid);
        }

        public NetPlayer Get(string guid) {
            return players.Find(o => o.guid == guid);
        }

        public void Update(string guid, NetPlayer newPlayer) {
            int i = this.players.FindIndex(o => o.guid == guid);
            this.players[i] = newPlayer;
        }

        public void Add(NetPlayer player) {
            this.players.Add(player);
        }

        public void Remove(string guid) {
            this.players.Remove(players.Find(o => o.guid == guid));
        }
    }
}