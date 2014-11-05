using UnityEngine;
using System.Collections;
using Goga.UnityNetwork;

public enum PlayerMovementState {
    Idle, Up, Down, Left, Right
}

public class PlayerEngine : MonoBehaviour {

    private UnityNetworkObject uNetObj;
    public float predictionOffset = 0f;
    public float walkSpeed = 3f;
    public Color[] colors;

    private PlayerMovementState movementState;
    private PlayerMovementState lastMovementState;

    internal struct PlayerInput {
        internal double timestamp;
        internal PlayerMovementState state;
    }

    private PlayerInput[] inputBuffer = new PlayerInput[20];

    void SaveNewInput(PlayerMovementState state) {

        // Shift the buffer sideways, deleting state 20
        for (int i = inputBuffer.Length - 1; i >= 1; i--) {
            inputBuffer[i] = inputBuffer[i - 1];
        }

        PlayerInput input;
        input.timestamp = Network.time;
        input.state = state;

        inputBuffer[0] = input;

        this.movementState = state;
    }

    void CheckInterpolation() {

        if (this.inputBuffer[0].timestamp > uNetObj.lastDataTime + predictionOffset) {

            for (int i = 0; i < this.inputBuffer.Length; i++) {

                if (this.inputBuffer[i].timestamp <= uNetObj.lastDataTime + predictionOffset) {


                    this.movementState = this.inputBuffer[i].state;
                    transform.position = Vector3.Lerp(transform.position, uNetObj.lastPos, 5f * Time.fixedDeltaTime);

                    return;
                }
            }
        }
    }

	// Use this for initialization
	void Start () {

        this.uNetObj = this.GetComponent<UnityNetworkObject>();
        renderer.material.color = colors[Random.Range(0, colors.Length)];
	}

    void Update() {

        /*
        if (Network.player == uNetObj.GetOwner() && Network.isClient) {
            CheckInterpolation();
        }
        */
    }

    void FixedUpdate() {

        if (Network.player == uNetObj.GetOwner()) {

            if (Network.peerType == NetworkPeerType.Client) {
                this.CheckInputClient();
            }


            if (Network.peerType == NetworkPeerType.Server) {
                this.CheckInputServer();
            }
        }

        this.MovementStateMachine();
    }

    void CheckInputClient() {

        if (Input.GetKey(KeyCode.UpArrow)) {
            this.SendInput(0);
            this.SaveNewInput(PlayerMovementState.Up);
        }

        if (Input.GetKey(KeyCode.DownArrow)) {
            this.SendInput(1);
            this.SaveNewInput(PlayerMovementState.Down);
        }

        if (Input.GetKey(KeyCode.LeftArrow)) {
            this.SendInput(2);
            this.SaveNewInput(PlayerMovementState.Left);
        }

        if (Input.GetKey(KeyCode.RightArrow)) {
            this.SendInput(3);
            this.SaveNewInput(PlayerMovementState.Right);
        }
    }

    void CheckInputServer() {

        if (Input.GetKey(KeyCode.UpArrow)) {
            this.movementState = PlayerMovementState.Up;
        }

        if (Input.GetKey(KeyCode.DownArrow)) {
            this.movementState = PlayerMovementState.Down;
        }

        if (Input.GetKey(KeyCode.LeftArrow)) {
            this.movementState = PlayerMovementState.Left;
        }

        if (Input.GetKey(KeyCode.RightArrow)) {
            this.movementState = PlayerMovementState.Right;
        }
    }

    void MovementStateMachine() {

        switch (movementState) {

            case PlayerMovementState.Up:

                transform.position = transform.position + (new Vector3(0, 0, 1) * this.walkSpeed * Time.fixedDeltaTime);
                break;

            case PlayerMovementState.Down:

                transform.position = transform.position + (new Vector3(0, 0, -1) * this.walkSpeed * Time.fixedDeltaTime);
                break;

            case PlayerMovementState.Left:

                transform.position = transform.position + (new Vector3(-1, 0, 0) * this.walkSpeed * Time.fixedDeltaTime);
                break;

            case PlayerMovementState.Right:

                transform.position = transform.position + (new Vector3(1, 0, 0) * this.walkSpeed * Time.fixedDeltaTime);
                break;

            default:

                break;
        }

        // always reset movement to idle
        this.movementState = PlayerMovementState.Idle;
    }

    // client send movement input
    void SendInput(float input) {

        networkView.RPC("ReceiveInput", RPCMode.Server, input);
    }

    /*
    public NetworkPlayer GetOwner() {
        return this.owner;
    }
    // client: set the owner of the object
    [RPC]
    public void SetOwner(NetworkPlayer player) {

        if (Network.peerType == NetworkPeerType.Client) {
            Debug.Log("CLIENT: RPC SetOwner came in with playerid:" + player.guid);
        }
        else {
            Debug.Log("SERVER: RPC SetOwner came in with playerid:" + player.guid);
        }

        this.owner = player;

    }
    */

    // server: receive movementState of client
    [RPC]
    void ReceiveInput(float input) {

        if (input==0)
            this.movementState = PlayerMovementState.Up;
        if (input == 1)
            this.movementState = PlayerMovementState.Down;
        if (input == 2)
            this.movementState = PlayerMovementState.Left;
        if (input==3)
            this.movementState = PlayerMovementState.Right;
    
    }
}
