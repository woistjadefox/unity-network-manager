using UnityEngine;
using System.Collections;


namespace Goga.UnityNetwork {

    public class UnityNetworkPredictor : MonoBehaviour {

        public bool clientSideInterpolation = false;
        public double m_InterpolationBackTime = 0.1;
        public double m_ExtrapolationLimit = 0.5;

        public float positionCorrectionThreshold = 0.2f;
        public float positionCorrectionSpeed = 1.5f;

        private UnityNetworkObject uNetObj;

        internal struct State {

            internal double timestamp;
            internal Vector3 pos;
            internal Vector3 velocity;
            internal Quaternion rot;
            internal Vector3 angularVelocity;
        }

        private State[] m_BufferedState = new State[30]; // We store twenty states with "playback" information
        private int m_TimestampCount;     // Keep track of what slots are used

        Vector3 pos = Vector3.zero;
        Vector3 velocity = Vector3.zero;
        Quaternion rot = Quaternion.identity;
        Vector3 angularVelocity = Vector3.zero;

        void Start() {
            this.uNetObj = this.GetComponent<UnityNetworkObject>();
        }

        void OnSerializeNetworkView(BitStream stream, NetworkMessageInfo info) {

            // Send data to server
            if (stream.isWriting) {

                if (rigidbody) {

                    pos = rigidbody.position;
                    rot = rigidbody.rotation;
                    velocity = rigidbody.velocity;
                    angularVelocity = rigidbody.angularVelocity;

                } else {
                    pos = transform.position;
                    rot = transform.rotation;
                }

                stream.Serialize(ref pos);
                stream.Serialize(ref rot);

                if (rigidbody) {
                    stream.Serialize(ref velocity);
                    stream.Serialize(ref angularVelocity);
                }

                // Read data from remote client
            } else {

                stream.Serialize(ref pos);
                stream.Serialize(ref rot);

                if (rigidbody) {
                    stream.Serialize(ref velocity);
                    stream.Serialize(ref angularVelocity);
                }

                // Shift the buffer sideways, deleting state 20
                for (int i = m_BufferedState.Length - 1; i >= 1; i--) {
                    m_BufferedState[i] = m_BufferedState[i - 1];
                }

                // Record current state in slot 0
                State state;
                state.timestamp = info.timestamp;
                state.pos = pos;
                state.velocity = velocity;
                state.rot = rot;
                state.angularVelocity = angularVelocity;
                m_BufferedState[0] = state;

                // Update used slot count, however never exceed the buffer size
                // Slots aren't actually freed so this just makes sure the buffer is
                // filled up and that uninitalized slots aren't used.
                m_TimestampCount = Mathf.Min(m_TimestampCount + 1, m_BufferedState.Length);

                // Check if states are in order, if it is inconsistent you could reshuffel or 
                // drop the out-of-order state. Nothing is done here
                for (int i = 0; i < m_TimestampCount - 1; i++) {

                    if (m_BufferedState[i].timestamp < m_BufferedState[i + 1].timestamp)
                        Debug.Log("State inconsistent");
                }
            }
        }

        private void LerpToTarget(Vector3 serverPos, Quaternion serverRot) {

            float distance = Vector3.Distance(transform.position, serverPos);

            //only correct if the error margin (the distance) is too extreme
            if (distance >= this.positionCorrectionThreshold) {

                float lerp = (((1 / distance) * this.positionCorrectionSpeed) * Time.fixedDeltaTime);

                transform.position = Vector3.Lerp(transform.position, serverPos, lerp);
                transform.rotation = Quaternion.Slerp(transform.rotation, serverRot, lerp);
            }
        }

        // We have a window of interpolationBackTime where we basically play 
        // By having interpolationBackTime the average ping, you will usually use interpolation.
        // And only if no more data arrives we will use extra polation
        void Update() {

            // return if you are disconnected or server
            if (Network.peerType == NetworkPeerType.Disconnected || Network.isServer) {
                return;
            }

            
            // if its rigidbody continue, if not, check if you are the owner and lerp to target
            if (this.uNetObj.IsMine() && !rigidbody && Network.isClient) {

                if (!this.clientSideInterpolation) {

                    this.LerpToTarget(pos, rot);
                    return;
                }
            }


            // This is the target playback time of the rigid body
            double interpolationTime = Network.time - m_InterpolationBackTime;

            // Use interpolation if the target playback time is present in the buffer
            if (m_BufferedState[0].timestamp > interpolationTime) {

                // Go through buffer and find correct state to play back
                for (int i = 0; i < m_TimestampCount; i++) {

                    if (m_BufferedState[i].timestamp <= interpolationTime || i == m_TimestampCount - 1) {

                        // The state one slot newer (<100ms) than the best playback state
                        State rhs = m_BufferedState[Mathf.Max(i - 1, 0)];
                        // The best playback state (closest to 100 ms old (default time))
                        State lhs = m_BufferedState[i];

                        // Use the time between the two slots to determine if interpolation is necessary
                        double length = rhs.timestamp - lhs.timestamp;
                        float t = 0.0F;
                        // As the time difference gets closer to 100 ms t gets closer to 1 in 
                        // which case rhs is only used
                        // Example:
                        // Time is 10.000, so sampleTime is 9.900 
                        // lhs.time is 9.910 rhs.time is 9.980 length is 0.070
                        // t is 9.900 - 9.910 / 0.070 = 0.14. So it uses 14% of rhs, 86% of lhs
                        if (length > 0.0001)
                            t = (float)((interpolationTime - lhs.timestamp) / length);

                        // if t=0 => lhs is used directly
                        transform.localPosition = Vector3.Lerp(lhs.pos, rhs.pos, t);
                        transform.localRotation = Quaternion.Slerp(lhs.rot, rhs.rot, t);
                        return;
                    }
                }
            }

            // Use extrapolation
            else {

                State latest = m_BufferedState[0];

                // check if there is a rigidbody
                if (rigidbody == null) {
                    transform.localPosition = Vector3.Lerp(transform.position, latest.pos, 0.5f);
                    transform.localRotation = Quaternion.Slerp(transform.rotation, latest.rot, 0.5f);
                    return;
                }

                float extrapolationLength = (float)(interpolationTime - latest.timestamp);

                // Don't extrapolation for more than 500 ms, you would need to do that carefully
                if (extrapolationLength < m_ExtrapolationLimit) {

                    float axisLength = extrapolationLength * latest.angularVelocity.magnitude * Mathf.Rad2Deg;
                    Quaternion angularRotation = Quaternion.AngleAxis(axisLength, latest.angularVelocity);

                    rigidbody.position = latest.pos + latest.velocity * extrapolationLength;
                    rigidbody.rotation = angularRotation * latest.rot;

                    if (!rigidbody.isKinematic) {
                        rigidbody.velocity = latest.velocity;
                        rigidbody.angularVelocity = latest.angularVelocity;
                    }

                }
            }
        }

    }
}