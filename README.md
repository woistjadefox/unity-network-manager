UnityNetworkManager
===================

A simple network manager for the build-in RakNet client in Unity + a simple GUI for a lobby.
Added playernames and ready state to the basic lobby and a chat window. 

####Installation:
***
Just attach UILobby.cs & UnityNetworkManager.cs to an empty GameObject and set the public parameter "Game Name". 
Example in scene Lobby.unity

####Todo / Next Steps:
***
- Protect created host server with a password:
  - pw textfield for host create form
  - pw textfield for join host 
  - setting parameters in UnityNetworkManager.ConnectPeer() & UnityNetworkManager.RegisterGame()

- Scrollbar for all connected players in the actual game window

- LAN Autodiscovery for local network games
