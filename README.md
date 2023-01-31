﻿# XPortal 

 An [AnyPortal](https://www.nexusmods.com/valheim/mods/170) revamp.


## Description

XPortal lets you select a portal destination from a list of existing portals. 

No more tag pairing, and no more portal hubs!


## Features

#### Select a destination

When interacting with a portal, instead of entering a tag which has to match another portal's, XPortal lets you choose the portal's destination from a list.

The "edit tag" window has been replaced by XPortal's own configuration window. You can now enter a Name and choose a Destination. Portal connections can now be one-way. In other words, you can go from portal A to portal B, all while portal B might be connected to portal C, or not even connected at all!

For your convenience, the list also shows you how far away the portals are.

#### Ping a portal location
Forgot where you put your portal? You don't need to go through it to find out. Just press the Ping button next to the list, and XPortal will show the selected portal on your map, while also pinging its location to all players on the server.


## Multiplayer

XPortal has been built with multiplayer support at its core. This does require all players on the server to run the same version of XPortal. This server itself also needs to have XPortal installed and that too has to be same version.


## Bugs, Issues, Feature Requests

To report problems or provide feedback, please use the project's [GitHub Issues page](https://github.com/SpikeHimself/XPortal/issues).
If you are experiencing a bug, please include the BepinEx output log and provide as many details as possible.


## Improvements over AnyPortal

As mentioned earlier, XPortal is a rewrite of the popular AnyPortal mod. Most players won't necessarily notice much difference, bar a few bugfixes. Under the hood, though, XPortal takes a completely different approach to portal updates. Also, the UI was completely overhauled.
Here are the most prominent changes:


* **New UI**:
AnyPortal built its UI incorporating the existing "edit tag" window. XPortal doesn't use the original "edit tag" window at all.

* **Robust network synchronisation**:
AnyPortal updates the information on your screen when you interact with a portal. XPortal updates all portal information at the very moment that any portal is changed, even when someone else does it.

* **Smaller file**:
XPortal uses Jötunn's UI elements, so it no longer needs its own Unity assets.

* **Code optimisation**:
The way AnyPortal dealt with portal tags and targets has been overhauled. XPortal keeps its own list and keeps that updated when any portal changes name or destination. Because of this, it will not have to query game data every time you interact with a portal.

* **Scalability**:
You won't notice this in your game, but the way XPortal's code is organised should make it much easier to maintain. So when a Valheim update breaks everything, it shouldn't be too hard for me to get things up and running again. XPortal is also ready to deal with any feature requests that you might have. If you are interested in XPortal's code, you are welcome to visit the project's [GitHub page](https://github.com/SpikeHimself/XPortal)!



## Changelog


* **v1.0.0** (2023-02-01)
	
 * Initial release



## Credits


* [sweetgiorni](https://www.nexusmods.com/valheim/users/6246023) for creating [AnyPortal](https://www.nexusmods.com/valheim/mods/170)

---

[<img src="
https://cdn.buymeacoffee.com/buttons/v2/default-yellow.png" height="60" width="217" alt="Buy me a coffee!">](https://www.buymeacoffee.com/SpikeHimself)
