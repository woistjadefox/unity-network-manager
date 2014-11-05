using UnityEngine;
using System.Collections;
using Goga.UnityNetwork;

public class UIMenu : MonoBehaviour {

    public UnityNetworkLobby lobby;

    private bool menuActive = true;

	// Use this for initialization
	void Start () {
	
	}

    void OnGUI() {

        GUI.depth = 0;
       
        if (Input.GetKey(KeyCode.Escape)) {

            this.lobby.enabled = false;
            this.menuActive = true;
           
        }

        if (menuActive) {

            GUILayout.Window(99, new Rect(10, 10, 100, 100), (int windowID) => {

                if (GUILayout.Button("Lobby")) {

                    this.lobby.enabled = true;
                    this.menuActive = false;

                }

            }, "Menu");
        }
    }
}
