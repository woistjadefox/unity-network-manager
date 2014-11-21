using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using JsonFx.Json;

namespace Goga.UnityNetwork {

    public class HostDataLAN {

        public string guid;
        public string gameType;
        public string gameName;
        public string comment;
        public int connectedPlayers;
        public int playerLimit;
        public string ip;
        public int port;
        public bool passwordProtected = false;
        public DateTime timestamp;

        public HostDataLAN(string guid) {
            this.guid = guid;
        }
    }

    public class AutoDiscovery : MonoBehaviour {

        private Manager uNet;
        public int remotePort = 19784;
        public float sendRate = 3f;
        public int timeoutThreshold = 5;

        private UdpClient sender;
        private UdpClient receiver;
        private IPEndPoint receiveIPGroup;

        private string uniqueServerId;
        private HostDataLAN serverHost;
        private HostDataLAN _receivedHost;
        public List<HostDataLAN> lobbyListLAN = new List<HostDataLAN>();
        private static readonly object listLock = new object();

        private JsonReader jReader = new JsonReader();
        private JsonWriter jWriter = new JsonWriter();

        void Start() {

            this.uNet = GetComponent<Manager>();
            this.uNet.newState += new ChangedCliendState(OnStateChange);
            this.StartReceivingIp();
        }

        void FixedUpdate() {

            // clean up lobby list every second
            if (Time.frameCount % 60 == 0) {
                this.CleanUpLobbyList();
                //Debug.Log("lan entries:" + this.lobbyListLAN.Count);
            }
        }

        public HostDataLAN GetServerHost() {
            return this.serverHost;
        }

        void OnStateChange(NetworkPeerType peerType) {

            switch (peerType) {

                case NetworkPeerType.Server:

                    //this.StopReceivingIp();
                    if (uNet.isLanOnly) {
                        this.StartBroadcastIp();
                    }

                    break;

                case NetworkPeerType.Client:

                    //this.StopReceivingIp();
                    break;


                case NetworkPeerType.Connecting:
                    break;

                case NetworkPeerType.Disconnected:

                    // stop sending ip if peer is no server anymore
                    this.StopBroadcastIp();

                    // receving data from server
                    this.StartReceivingIp();

                    break;
            }
        }

        #region server sending signal
        public void StartBroadcastIp() {

            sender = new UdpClient();
            IPEndPoint groupEP = new IPEndPoint(IPAddress.Broadcast, remotePort);
            sender.Connect(groupEP);

            // create unique id
            this.uniqueServerId = this.CreateUniqueId();

            // create server hostdata for LAN
            this.serverHost = new HostDataLAN(this.uniqueServerId);

            // start to broadcast message
            InvokeRepeating("SendBroadcast", 0, this.sendRate);

            Debug.Log("UnityNetworkDiscovery:start start sending ip from this host..");
        }

        private void StopBroadcastIp() {

            if (sender != null) {
                CancelInvoke("SendBroadcast");
                this.sender.Close();
                this.sender = null;
            }

        }

        private void SendBroadcast() {

            // pack new data into the server HostDataLAN
            this.serverHost.gameType = uNet.gameName;
            this.serverHost.gameName = uNet.GetActualHostLAN().gameName;
            this.serverHost.comment = uNet.GetActualHostLAN().comment;
            this.serverHost.connectedPlayers = uNet.netPlayers.GetList().Count;
            this.serverHost.playerLimit = uNet.GetActualHostLAN().playerLimit;
            this.serverHost.ip = this.GetLocalIPAddress();
            this.serverHost.port = uNet.serverPort;
            this.serverHost.timestamp = DateTime.Now;

            // serialize data
            string hostJsonMessage = jWriter.Write(this.serverHost);

            // broadcast data over the socket
            if (hostJsonMessage != "") {
                sender.Send(Encoding.ASCII.GetBytes(hostJsonMessage), hostJsonMessage.Length);
            }

        }
        #endregion 
        
        #region client receiving signal
        public void StartReceivingIp() {
            try {
                if (receiver == null) {
                    receiver = new UdpClient(remotePort);
                    receiver.BeginReceive(new AsyncCallback(ReceiveBroadcast), null);

                    Debug.Log("UnityNetworkDiscovery:start receivingIp from servers..");
                }
            }
            catch (SocketException e) {
                Debug.Log(e.Message);
            }
        }

        private void StopReceivingIp() {

            if (receiver != null) {
                this.receiver.Close();
                this.receiver = null;
            }

        }

        private void ReceiveBroadcast(IAsyncResult result) {
            
            IPEndPoint receiveIPGroup = new IPEndPoint(IPAddress.Any, remotePort);
            byte[] received;
            if (receiver != null) {
                received = receiver.EndReceive(result, ref receiveIPGroup);
            }
            else {
                return;
            }
            receiver.BeginReceive(new AsyncCallback(ReceiveBroadcast), null);
            string receivedString = Encoding.ASCII.GetString(received);

            this.ConvertBroadcastDataToHost(receivedString);
            
        }
        #endregion

        // transform json string to HostDataLAN obj
        private void ConvertBroadcastDataToHost(string data) {

            // deserialize json string to obj
            _receivedHost = jReader.Read<HostDataLAN>(data);

            // return out of the function if received gametype isn't correct
            if (_receivedHost.gameType != uNet.gameName) {
                return;
            }

            lock (listLock) {

                // search host in lobby list
                int _result = this.lobbyListLAN.FindIndex(d => d.guid == _receivedHost.guid);

                if (_result != -1) {

                    // update host in list
                    this.lobbyListLAN[_result] = _receivedHost;

                } else {

                    // add host to list
                    Debug.Log("new host:" + _receivedHost.gameName);
                    this.lobbyListLAN.Add(_receivedHost);

                }
            }
              
        }

        // cleans up old entries
        public void CleanUpLobbyList() {

            lock (listLock) 
            {

                for (int i = 0; i < this.lobbyListLAN.Count; i++ ){

                    if ((DateTime.Now - this.lobbyListLAN[i].timestamp).Seconds > this.timeoutThreshold) {

                        this.lobbyListLAN.Remove(this.lobbyListLAN[i]);
                    }
                }

            }
           
        }

        // get back all lan hosts
        public HostDataLAN[] GetLanHostData(){

            HostDataLAN[] list = this.lobbyListLAN.ToArray();
            return list;
        }

        public string GetLocalIPAddress() {

            IPHostEntry host;
            string localIP = "";
            host = Dns.GetHostEntry(Dns.GetHostName());
            foreach (IPAddress ip in host.AddressList) {
                if (ip.AddressFamily == AddressFamily.InterNetwork) {
                    localIP = ip.ToString();
                    break;
                }
            }
            return localIP;
        }

        private string CreateUniqueId() {

            string characters = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            string ticks = DateTime.UtcNow.Ticks.ToString();
            var code = "";
            for (var i = 0; i < characters.Length; i += 2) {
                if ((i + 2) <= ticks.Length) {
                    var number = int.Parse(ticks.Substring(i, 2));
                    if (number > characters.Length - 1) {
                        var one = double.Parse(number.ToString().Substring(0, 1));
                        var two = double.Parse(number.ToString().Substring(1, 1));
                        code += characters[Convert.ToInt32(one)];
                        code += characters[Convert.ToInt32(two)];
                    }
                    else
                        code += characters[number];
                }
            }
            return code;
        }

        void CloseConnections() {

            if (this.receiver != null) {
                this.receiver.Close();
                this.receiver = null;
            }

            if (this.sender != null) {
                this.sender.Close();
                this.receiver = null;
            }

        }

        void OnDisable() {
            this.CloseConnections();
        }

        void OnApplicationQuit() {
            this.CloseConnections();
        }

    }
}

