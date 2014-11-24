UnityNetworkManager
===================

A simple network framework for the build-in RakNet client in Unity 4.x. 

Features: 
- Authoritative Server Structure
- Lobby with Chat
- Manager takes care of playername changes and ready state changes
- RPC function observer (Observer takes care about Server/Client code)
- Simple Prediction (Interpolation & Extrapolation)
- Rigidbody support
- Mecanim Animation Synchronizer 
- LAN Autodiscovery for local network games
- Host Migration (beta state)
- Own RPC Buffer System (Framework does not work with Network.Instantiate and Unity RPCBuffers)

Requirements: 
- Unity 4.x Free or Pro
- LAN Autodiscovery on iOS & Android requires Unity iOS / Android Pro License (.NET Socket is used for this feature). 
- LAN Autodiscovery doesn't work in Webplayer since Webplayer can't send UDP Broadcasts (Sandbox Security Issue)
- For real time games on iOS & Android & punch through function a Wi-Fi connection on iOS & Android is highly recommended

####Installation:
***
Check the example in the scene Assets/workspace/demo/scenes/Lobby.unity

####Still in development / Todo:
***
- Protect created host server with a password:
  - pw textfield for host create form
  - pw textfield for join host 
  - setting parameters in UnityNetworkManager.ConnectPeer() & UnityNetworkManager.RegisterGame()

- Scrollbar for all connected players in the actual game window
- Connection retry if NAT punchthrough doesn't work the first time

####Copyright & license:
***
Code released under [the Apache License 2.0](LICENSE).
