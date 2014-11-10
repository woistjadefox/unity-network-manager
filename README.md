UnityNetworkManager
===================

A simple network manager for the build-in RakNet client in Unity + a simple GUI for the lobby management.

Features: 
- Lobby with chat
- Manager takes care of playername changes and ready state changes
- LAN Autodiscovery for local network games
- Smiple Prediction (Interpolation & Extrapolation)
- Rigidbody support

####Installation:
***
Attach Lobby.cs, Manager.cs & AutoDiscovery.cs to an empty GameObject and set the public parameter "Game Name". 
Example in scene Lobby.unity

####Still in development / Todo:
***
- Protect created host server with a password:
  - pw textfield for host create form
  - pw textfield for join host 
  - setting parameters in UnityNetworkManager.ConnectPeer() & UnityNetworkManager.RegisterGame()

- Scrollbar for all connected players in the actual game window
- Connection retry if NAT punchthrough doesn't work the first time

####Support:
***
- LAN Autodiscovery doesn't work in Webplayer since Webplayer can't send UDP Broadcasts

####Copyright & license:
***
Code released under [the Apache License 2.0](LICENSE).
