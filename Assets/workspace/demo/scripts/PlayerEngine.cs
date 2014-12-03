using UnityEngine;
using System.Collections;
using Goga.UnityNetwork;


public enum PlayerMovementState {
    Idle, Up, Down, Left, Right, Jump
}

[RequireComponent(typeof(NetObject))]
public class PlayerEngine : MonoBehaviour {

    private NetObject uNetObj;

    public Animator animator;
    public bool allowLocalMovement = true;
    public float walkSpeed = 3f;
    public bool showMoveCount = false;

    private PlayerMovementState movementState;
    private PlayerMovementState lastSendMove;
    private int _moveCount = 0;
  
	void Start () {

        this.uNetObj = this.GetComponent<NetObject>();
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

        if (Input.GetKeyDown(KeyCode.Space)) {
            this.SendInput((int)PlayerMovementState.Jump);
        }

        if (Input.GetKeyDown("m")) {

            if (!this.showMoveCount) {
                this.ShowMoveCount(true);
            } else {
                this.ShowMoveCount(false);
            }
        }

        if (!Input.anyKey) {

            // send idle input only once after a action
            if (this.lastSendMove != PlayerMovementState.Idle) {
                this.SendInput((int)PlayerMovementState.Idle);
            }
        }

    }

    void MovementStateMachine() {

        switch (movementState) {

            case PlayerMovementState.Up:
                this.animator.SetBool("walk", true);
                transform.position = transform.position + (new Vector3(0, 0, 1) * this.walkSpeed * Time.fixedDeltaTime);
                break;

            case PlayerMovementState.Down:
                this.animator.SetBool("walk", true);
                transform.position = transform.position + (new Vector3(0, 0, -1) * this.walkSpeed * Time.fixedDeltaTime);
                break;

            case PlayerMovementState.Left:
                this.animator.SetBool("walk", true);
                transform.position = transform.position + (new Vector3(-1, 0, 0) * this.walkSpeed * Time.fixedDeltaTime);
                break;

            case PlayerMovementState.Right:
                this.animator.SetBool("walk", true);
                transform.position = transform.position + (new Vector3(1, 0, 0) * this.walkSpeed * Time.fixedDeltaTime);
                break;

            case PlayerMovementState.Jump:
                this.animator.SetBool("jump", true);
                break;

            case PlayerMovementState.Idle:
                this.animator.SetBool("walk", false);
                this.animator.SetBool("jump", false);
                break;
        }

    }

    [RPC]
    void SendInput(int state, int senderID = 0) {

        object[] data = { state, senderID };   

        if (uNetObj.RoleObserver(data, false, this.allowLocalMovement)) {

            this.movementState = (PlayerMovementState)state;
            this.MovementStateMachine();

            this.lastSendMove = (PlayerMovementState)state;
            this._moveCount++;
        }

    }

    [RPC]
    void ShowMoveCount(bool state, int senderID = 0) {

        object[] data = { state, senderID };

        if (uNetObj.RoleObserver(data, true, false)) {

            // show move count
            this.showMoveCount = state;
        }
    }

    void OnGUI() {

        // show player name on top of player
        Vector3 playerScreenPos = Camera.main.WorldToScreenPoint(transform.position);
        playerScreenPos.y = Screen.height - playerScreenPos.y;

        if (this.showMoveCount) {
            GUI.Box(new Rect(playerScreenPos.x, playerScreenPos.y, 100, 40), "moves:" + this._moveCount);
        } else {
            GUI.Box(new Rect(playerScreenPos.x, playerScreenPos.y, 100, 40), this.uNetObj.uNet.GetNetworkPlayer(this.uNetObj.playerGuid).name);
        }

    }
}
