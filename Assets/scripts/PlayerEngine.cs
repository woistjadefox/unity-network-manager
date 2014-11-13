using UnityEngine;
using System.Collections;
using Goga.UnityNetwork;

public enum PlayerMovementState {
    Idle, Up, Down, Left, Right
}

public enum PlayerColors {
    Red, Blue, Yellow, Green
}

[RequireComponent(typeof(NetObject))]

public class PlayerEngine : MonoBehaviour {

    private NetObject uNetObj;
    public bool allowLocalMovement = true;
    public float walkSpeed = 3f;
    public bool showMoveCount = false;

    private PlayerMovementState movementState;

    private int _moveCount = 0;
  
	// Use this for initialization
	void Start () {
        this.uNetObj = this.GetComponent<NetObject>();
        renderer.material.color = Color.gray;
	}

    void Update() {


        if (this.uNetObj.IsMine()) {
            this.CheckInput();
        }
    }

    void CheckInput() {

        if (Input.GetKey(KeyCode.UpArrow)) {
            this.SendInput((int)PlayerMovementState.Up);
        }


        if (Input.GetKey(KeyCode.DownArrow)) {
            this.SendInput((int)PlayerMovementState.Down);
        }


        if (Input.GetKey(KeyCode.LeftArrow)) {
            this.SendInput((int)PlayerMovementState.Left);
        }


        if (Input.GetKey(KeyCode.RightArrow)) {
            this.SendInput((int)PlayerMovementState.Right);
        }

        if (Input.GetKeyDown("c")) {

            Debug.Log("pressed c");
            this.ChangeColor((int)PlayerColors.Red);
        }

        if (Input.GetKeyDown("m")) {

            if (!this.showMoveCount) {
                this.ShowMoveCount(true);
            } else {
                this.ShowMoveCount(false);
            }
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

    [RPC]
    void SendInput(int state, int senderID = 0) {

        object[] data = { state, senderID };

        if (uNetObj.RoleObserver(data, false, this.allowLocalMovement)) {

            this.movementState = (PlayerMovementState)state;

            this._moveCount++;
            this.MovementStateMachine();
        }
    }

    [RPC]
    void ShowMoveCount(bool state, int senderID = 0) {

        object[] data = { state, senderID };

        if (uNetObj.RoleObserver(data, true, false)) {

            Debug.Log("whoop i show my move count");
            this.showMoveCount = state;
        }
    }

    /*
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
    */

    // change color
    [RPC]
    void ChangeColor(int color, int senderID = 0) {

        object[] data = {color, senderID};

        if (uNetObj.RoleObserver(data, true, false)) {

            Color newColor = Color.gray;

            switch (color) {
                case 0: newColor = Color.red; break;
                case 1: newColor = Color.blue; break;
                case 2: newColor = Color.yellow; break;
                case 3: newColor = Color.green; break;
            }

            renderer.material.color = newColor;

        }        
    }

    void OnGUI() {

        // show player name on top of player
        Vector3 playerScreenPos = Camera.main.WorldToScreenPoint(transform.position);
        playerScreenPos.y = Screen.height - playerScreenPos.y;

        if (this.showMoveCount) {
            GUI.Box(new Rect(playerScreenPos.x, playerScreenPos.y, 100, 40), "moves:" + this._moveCount);
        } else {
            GUI.Box(new Rect(playerScreenPos.x, playerScreenPos.y, 100, 40), this.uNetObj.GetManager().GetNetworkPlayer(this.uNetObj.playerGuid).name);
        }

    }
}
