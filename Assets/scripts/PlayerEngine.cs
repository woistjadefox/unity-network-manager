using UnityEngine;
using System.Collections;


public enum PlayerMovementState {
    Idle, Up, Down, Left, Right
}

[RequireComponent(typeof(NetworkView))]
public class PlayerEngine : MonoBehaviour {

    public float walkSpeed = 3f;
    public float positionErrorThreshold = 0.2f;
    public float positionCorrectionSpeed = 5f;
    public Color[] colors;

    private NetworkPlayer owner;
    public string playerId;

    public Vector3 serverPos;
    public Quaternion serverRot;

    private PlayerMovementState movementState;
    private PlayerMovementState lastMovementState;

	// Use this for initialization
	void Start () {

        renderer.material.color = colors[Random.Range(0, colors.Length)];
	}

    void FixedUpdate() {
        
        this.playerId = this.owner.guid;

        if (Network.player == this.owner) {

            if (Network.peerType == NetworkPeerType.Client) {
                this.CheckInputClient();
                //this.LerpToTarget();
            }


            if (Network.peerType == NetworkPeerType.Server) {
                this.CheckInputServer();
            
            }
        }

        this.MovementStateMachine();
    }

    public void LerpToTarget() {

        // check if server pos is already here
        if (this.serverPos == null || serverRot == null) {
            return;
        }

        float distance = Vector3.Distance(transform.position, serverPos);

        //only correct if the error margin (the distance) is too extreme
        if (distance >= this.positionErrorThreshold) {

            //Debug.Log("position correction working... (distance difference:"+distance+")");

            float lerp = (((1 / distance) * this.positionCorrectionSpeed) * Time.fixedDeltaTime);

            transform.position = Vector3.Lerp(transform.position, serverPos, lerp);
            transform.rotation = Quaternion.Slerp(transform.rotation, serverRot, lerp);
        }
    }

    void CheckInputClient() {

        if (Input.GetKey(KeyCode.UpArrow)) {
            this.SendInput(0);
            this.movementState = PlayerMovementState.Up;
        }

        if (Input.GetKey(KeyCode.DownArrow)) {
            this.SendInput(1);
            this.movementState = PlayerMovementState.Down;
        }

        if (Input.GetKey(KeyCode.LeftArrow)) {
            this.SendInput(2);
            this.movementState = PlayerMovementState.Left;
        }

        if (Input.GetKey(KeyCode.RightArrow)) {
            this.SendInput(3);
            this.movementState = PlayerMovementState.Right;
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
