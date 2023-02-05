# XPortal 

 An [AnyPortal](https://valheim.thunderstore.io/package/sweetgiorni/AnyPortal/) revamp.

 <img src="https://raw.githubusercontent.com/SpikeHimself/XPortal/main/images/controller.gif" height="140" />


## Description

XPortal lets you select a portal destination from a list of existing portals. 

No more tag pairing, and no more portal hubs!


## Features

#### Select a destination

When interacting with a portal, instead of entering a tag which has to match another portal's, XPortal lets you choose the portal's destination from a list.

The "edit tag" window has been replaced by XPortal's own configuration window. You can now enter a Name and choose a Destination. Portal connections can now be one-way. In other words, you can go from portal A to portal B, all while portal B might be connected to portal C, or not even connected at all!

For your convenience, the list also shows you how far away the portals are.

#### Longer names

XPortal completely removes the character length restriction on portal names, so that you can give your portals clear and descriptive titles.

#### Ping a portal location

Forgot where you put your portal? You don't need to go through it to find out. Just press the Ping button next to the list, and XPortal will show the selected portal on your map, while also pinging its location to all players on the server.

#### Controller support

As of v1.1.0, XPortal will respond to controller input when configuring your portal. It's not as neat as the rest of the Valheim UI - it won't show you the keyhints on the screen - but it does work somewhat intuitively.

* `A` / `Cross` - Submit (i.e. press the Okay button)

* `B` / `Circle` - Cancel

* `Y` / `Triangle` - Ping the selected portal

* `X` / `Square` - Show the contents of the dropdown

* `D-Pad Up` / `D-Pad Down` - Select the previous / next item in the dropdown


## Multiplayer

XPortal has been built with multiplayer support at its core. This does require all players on the server to run the same version of XPortal. This server itself also needs to have XPortal installed and that too has to be same version.


## Bugs, Issues, Feature Requests

To report problems or provide feedback, please use the project's [GitHub Issues page](https://github.com/SpikeHimself/XPortal/issues).
If you are experiencing a bug, please include the BepInEx output log and provide as many details as possible.


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

* **v1.0.1** (2023-02-01)

	* Improvements for dedicated servers

	* Fix a bug that stopped XPortal showing the UI after destroying a portal

* **v1.1.0** (2023-02-03)

	* Controller support!

	* Translations added for French, Portuguese (BR), German

* **v1.2.0** (2023-02-03)

	* Fix portals disappearing from the dropdown

	* Fix portals sometimes duplicating

* **v1.2.1** (2023-02-05)

	* Translation added for Spanish

	* Added BepInEx dependency, updated Jotunn dependency to 2.10.4

	* Detect portal placement and destruction
	
	* Optimise portal hovering event, UI interaction, and resyncs

	* Update console log messages

	* Some bugfixes


## Credits

* sweetgiorni for creating AnyPortal

* kaiqueknup for translating to Brazillian Portuguese 

* makou for translating to French, Spanish

* Smok3y97 for translating to German


## Support me

XPortal is free and will remain free, for everyone to use, edit or learn from. If you enjoy XPortal and want to support my work, please consider buying me a coffee:

[<img src="https://cdn.buymeacoffee.com/buttons/v2/default-yellow.png" height="40">](https://www.buymeacoffee.com/SpikeHimself)
