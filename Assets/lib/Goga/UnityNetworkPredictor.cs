using UnityEngine;
using System.Collections;

public class NetState {

    public float timestamp; //The time this state occured on the network
    public Vector3 pos; //Position of the attached object at that time
    public Quaternion rot; //Rotation at that time
    
    public NetState() {
        timestamp = 0.0f;
        pos = Vector3.zero;
        rot = Quaternion.identity;
    }
    
    public NetState(float time, Vector3 pos, Quaternion rot) {
        timestamp = time;
        this.pos = pos;
        this.rot = rot;
    }
}

public class UnityNetworkPredictor : MonoBehaviour {

    public Transform observedTransform;
    public PlayerEngine receiver; //Guy who is receiving data
    public float pingMargin = 0.5f; //ping top-margin
    private double lastTimeStamp;
    private int lastDataPing;

    private float clientPing;
    private NetState[] serverStateBuffer = new NetState[30];

    void OnSerializeNetworkView(BitStream stream, NetworkMessageInfo info) {

        Vector3 pos = this.observedTransform.position;
        Quaternion rot = this.observedTransform.rotation;

        if (stream.isWriting) {

            stream.Serialize(ref pos);
            stream.Serialize(ref rot);

        }
        else {

            this.lastDataPing = (int)((info.timestamp - this.lastTimeStamp)*1000);
            //Debug.Log("got last package in:" + this.lastDataPing);

            this.lastTimeStamp = info.timestamp;

            stream.Serialize(ref pos);
            stream.Serialize(ref rot);

            this.receiver.serverPos = pos;
            this.receiver.serverRot = rot;

            if(Network.player == receiver.GetOwner()){
                this.receiver.LerpToTarget();
            } 

            //Take care of data for interpolating remote objects movements
            // Shift up the buffer
            for (int i = serverStateBuffer.Length - 1; i >= 1; i-- ) {

                serverStateBuffer[i] = serverStateBuffer[i-1];
            }
            
            //Override the first element with the latest server info
            this.serverStateBuffer[0] = new NetState((float)info.timestamp, pos, rot);
        }
    }


    void FixedUpdate() {

        if ((Network.player == receiver.GetOwner()) || Network.isServer) {
            return; // This is only for remote peers, get off
        }

        if (Network.peerType == NetworkPeerType.Disconnected) {
            return;
        }

        // client side has !!only the server connected!!
        clientPing = (Network.GetAveragePing(Network.connections[0]) / 100) + this.pingMargin;

        Debug.Log("npc clientping:" + clientPing);

        float interpolationTime = (float)Network.time - clientPing;

        // ensure the buffer has at least one element:
        if (serverStateBuffer[0] == null) {
            serverStateBuffer[0] = new NetState(0, 
                                        transform.position, 
                                        transform.rotation);
        }

        // Try interpolation if possible. 
        // If the latest serverStateBuffer timestamp is smaller than the latency
        // we're not slow enough to really lag out and just extrapolate.
        if (serverStateBuffer[0].timestamp > interpolationTime) {

            for (int i = 0; i < serverStateBuffer.Length; i++) {

                if (serverStateBuffer[i] == null) {
                    continue;
                }

                // Find the state which matches the interp. time or use last state
                if (serverStateBuffer[i].timestamp <= interpolationTime || i == serverStateBuffer.Length - 1) {
                
                    // The state one frame newer than the best playback state
                    NetState bestTarget = serverStateBuffer[Mathf.Max(i-1, 0)];

                    // The best playback state (closest current network time))
                    NetState bestStart  = serverStateBuffer[i];
                
                    float timediff = bestTarget.timestamp - bestStart.timestamp;
                    float lerpTime = 0.0f;

                    // Increase the interpolation amount by growing ping
                    // Reverse that for more smooth but less accurate positioning
                    if (timediff > 0.0001) {
                        lerpTime = ((interpolationTime - bestStart.timestamp) / timediff);
                    }
                
                    transform.position = Vector3.Lerp(	bestStart.pos, 
                                                        bestTarget.pos, 
                                                        lerpTime);

                    transform.rotation = Quaternion.Slerp(	bestStart.rot, 
                                                            bestTarget.rot, 
                                                            lerpTime);

                    //Okay found our way through to lerp the positions, lets return here
                    return;
                }
            }
        }

        //so it appears there is no lag through latency.
        else {

            NetState latest = serverStateBuffer[0];	
            transform.position = Vector3.Lerp(transform.position, latest.pos, 0.5f);
            transform.rotation = Quaternion.Slerp(transform.rotation, latest.rot, 0.5f);
        }
    }
}
