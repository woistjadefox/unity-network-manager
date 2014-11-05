using UnityEngine;
using System.Collections;
using Goga.UnityNetwork;

public enum PlayerMovementState {
    Idle, Up, Down, Left, Right
}

[RequireComponent(typeof(UnityNetworkObject))]

public class PlayerEngine : MonoBehaviour {

    private UnityNetworkManager uNet;
    private UnityNetworkObject uNetObj;
    public bool allowLocalMovement = true;
    public float walkSpeed = 3f;
    public bool showMoveCount = false;
    public Color[] colors;

    private PlayerMovementState movementState;

    private int _moveCount = 0;
  
	// Use this for initialization
	void Start () {

        this.uNet = FindObjectOfType <UnityNetworkManager>();
        this.uNetObj = this.GetComponent<UnityNetworkObject>();
        renderer.material.color = colors[Random.Range(0, colors.Length)];
	}

    void FixedUpdate() {


        if (this.uNetObj.IsMine()) {
            this.CheckInput();
        }
    }

    void CheckInput() {

        if (Input.GetKey(KeyCode.UpArrow)) {
            this.SendInput(PlayerMovementState.Up);
        }


        if (Input.GetKey(KeyCode.DownArrow)) {
            this.SendInput(PlayerMovementState.Down);
        }


        if (Input.GetKey(KeyCode.LeftArrow)) {
            this.SendInput(PlayerMovementState.Left);
        }


        if (Input.GetKey(KeyCode.RightArrow)) {
            this.SendInput(PlayerMovementState.Right);
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
    void SendInput(PlayerMovementState state) {

        if (Network.isClient) {

            networkView.RPC("ReceiveInput", RPCMode.Server, (int)state);

            if (this.allowLocalMovement) {
                this.ReceiveInput((int)state);
            }

        }

        if (Network.isServer) {
            this.ReceiveInput((int)state);
        }

    }

    // server: receive movementState of client
    [RPC]
    void ReceiveInput(int state) {

        this.movementState = (PlayerMovementState)state;

        this._moveCount++;
        this.MovementStateMachine();

    }

    void OnGUI() {

        // show player name on top of player
        Vector3 playerScreenPos = Camera.main.WorldToScreenPoint(transform.position);
        playerScreenPos.y = Screen.height - playerScreenPos.y;

        if (this.showMoveCount) {
            GUI.Box(new Rect(playerScreenPos.x, playerScreenPos.y, 100, 40), "moves:" + this._moveCount);
        } else {
            GUI.Box(new Rect(playerScreenPos.x, playerScreenPos.y, 100, 40), this.uNet.GetNetworkPlayer(uNetObj.playerGuid).name);
        }

    }
}
