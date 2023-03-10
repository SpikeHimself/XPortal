# XPortal

An [AnyPortal](https://valheim.thunderstore.io/package/sweetgiorni/AnyPortal/) revamp.

<img src="https://raw.githubusercontent.com/SpikeHimself/XPortal/main/images/controller.gif" height="180" />


# Description

XPortal lets you select a portal destination from a list of existing portals. 

No more tag pairing, and no more portal hubs!


# Features

#### Select a destination

When interacting with a portal, instead of entering a tag which has to match another portal's, XPortal lets you choose the portal's destination from a list.
For your convenience, this list also shows you how far away the portals are.

#### Longer names

XPortal completely removes the character length restriction on portal names, so that you can give your portals clear and descriptive titles.

#### Ping a portal location

Forgot where you put your portal? You don't need to teleport to it to find out. Just click the Ping button next to the list, and XPortal will show the selected portal on your map, while also pinging its location to all players on the server.

If you prefer to play without a map, this button can be hidden, either by using the `nomap` global key, or by setting `PingMapDisabled` to `True`.

#### Multiplayer

XPortal has been built with multiplayer support at its core. All players must run the same version of XPortal. If you play on a dedicated server, that too needs to have same version of XPortal installed.

#### Gamepad support

The XPortal UI will respond to gamepad input when configuring your portal. It's not as neat as the rest of the Valheim UI - it won't show you the keyhints on the screen - but it does work somewhat intuitively:
* `A` / `Cross` - Submit (i.e. press the Okay button)
* `B`/ `Circle` - Cancel
* `Y` / `Triangle` - Ping the selected portal
* `X` / `Square` - Show the contents of the dropdown
* `D-Pad Up` / `D-Pad Down` - Select the previous / next item in the dropdown


#### Mod compatibility and integration

XPortal has been made fully compatible with the following mods:
* [Nexus Update Check](https://valheim.thunderstore.io/package/nexusreupload/aedenthorn_Nexus_Update_Check/) by aedenthorn
* [VHVR - Valheim VR](https://valheim.thunderstore.io/package/Maynard/VHVR/) by Flatscreen to VR Modders
* [Stone Portal](https://valheim.thunderstore.io/package/JereKuusela/Stone_Portal/) by Jere Kuusela
* [Advanced Portals](https://valheim.thunderstore.io/package/RandyKnapp/AdvancedPortals/) by Randy Knapp

Furthermore, XPortal has a configuration option to fully integrate with [Advanced Portals](https://valheim.thunderstore.io/package/RandyKnapp/AdvancedPortals/). If you set `DisplayPortalColour` to `True`, each portal in XPortal's dropdown list will be prepended by a ">>" tag that has the same colour as the light that the portal emits:

<img src="https://raw.githubusercontent.com/SpikeHimself/XPortal/main/images/advancedportals.png" height="280" />

XPortal is known to be incompatible with:
* [AnyPortal](https://valheim.thunderstore.io/package/sweetgiorni/AnyPortal/) by sweetgiorni


# Installation instructions

XPortal is a [BepInEx](https://valheim.thunderstore.io/package/denikson/BepInExPack_Valheim/) plugin. As such, you must have BepInEx installed. Most other Valheim mods are also BepInEx plugins, so chances are you already have this.

XPortal makes use of the [Jotunn](https://valheim.thunderstore.io/package/ValheimModding/Jotunn/) library, so you must install that before installing XPortal. If you do not install Jotunn, XPortal will simply not be loaded by your game and it will not work.

I very strongly recommend using a mod manager such as [Vortex](https://www.nexusmods.com/site/mods/1) or [r2modman](https://valheim.thunderstore.io/package/ebkr/r2modman/). They will take care of everything for you and you don't have to worry about which files go where. I recommend against manual installation.
1. Make sure you have [BepInEx](https://valheim.thunderstore.io/package/denikson/BepInExPack_Valheim/) installed.
2. Install [Jotunn](https://valheim.thunderstore.io/package/ValheimModding/Jotunn/).
3. On [Nexus Mods](https://www.nexusmods.com/valheim/mods/2239) click 'Mod manager download', or on [Thunderstore](https://valheim.thunderstore.io/package/SpikeHimself/XPortal/) click 'Install with Mod Manager'.



# Bugs, Feature Requests and Translations

First of all, before you report a bug, please make sure that the problem you are experiencing is actually caused by XPortal. If you are running other mods, disable those, and see if the problem goes away. Or the other way around: disable XPortal, and see if that makes the problem go away. If you discover that XPortal is incompatible with another mod, please do report that, because I might be able to create work-arounds for that. If you are not sure, or you are struggling with these steps, then just report the problem, and we'll go from there.

It is important to me that I can make XPortal as bug free as possible, but **please bear in mind that without your `LogOutput.log`, I will not be able to debug your issue at all**. Just showing me a screenshot of an error is not enough for me to discover the cause of that error.

To report a bug, please navigate to the [Issues page](https://github.com/SpikeHimself/XPortal/issues), click [New issue](https://github.com/SpikeHimself/XPortal/issues/new/choose), choose `Bug report`, and fill out the template.

For feature requests, choose `Feature request` on the [New issue](https://github.com/SpikeHimself/XPortal/issues/new/choose) page.

To add a translation to XPortal, choose `Translation` when submitting a [New issue](https://github.com/SpikeHimself/XPortal/issues/new/choose).


# Changelogs

* **v1.2.7** (2023-03-14)

	* Dependency updates: BepInEx 5.4.21, Jotunn 2.11.0

	* Some fixes towards further [Advanced Portals](https://valheim.thunderstore.io/package/RandyKnapp/AdvancedPortals/) compatibility
 
	* Fix a HarmonyX warning that occurs when loading XPortal

	* Fix error that sometimes appears when logging out or quitting the game

<details>
<summary>Click to view previous versions</summary>

* **v1.2.6** (2023-03-05)

	* XPortal is now marked as incompatible with [AnyPortal](https://valheim.thunderstore.io/package/sweetgiorni/AnyPortal/): if you have AnyPortal installed, XPortal will not work
	
	* Config option added: DisplayPortalColour. Enabling this will display a coloured tag in portal list (integration with [Advanced Portals](https://valheim.thunderstore.io/package/RandyKnapp/AdvancedPortals/) by Randy Knapp)
	
	* Documentation overhaul (readmes are now fully modular -- if you're a mod author, check this out on GitHub!)
	
	* Improved logging

* **v1.2.5** (2023-02-15)

	* Hide the Ping Map button when the [nomap](https://valheim.fandom.com/wiki/Global_Keys) global key is set (you can do this by typing `nomap` in the console).

	* The `PingMapDisabled` config option is now enforced by the server. If the server has this set to `true`, the Ping Map button will be hidden, regardless of your own settings.

	* Add sync support for the stone portal object. This fixes a compatibility issue with the mod [Stone Portal](https://valheim.thunderstore.io/package/JereKuusela/Stone_Portal/).

* **v1.2.4** (2023-02-13)

	* Add translation to Korean.

	* Items in the dropdown no longer overlap each other.

	* Items in the dropdown are now highlighted when you hover over them.

	* Added configuration option `PingMapDisabled` which disables the ability to ping portals in the list. By default the Ping Map button remains *enabled*.

	* Various code optimisations.

* **v1.2.3** (2023-02-09)

	* Add support for [Nexus Update Check](https://www.nexusmods.com/valheim/mods/102)

	* Minor UI modifications to accomodate longer translations.

	* Added translations for Polish and Russian.

	* Updated description/readme.

	* Code cleanup and minor bugfixes.

* **v1.2.2** (2023-02-06)

	* Fixed "Fetching portal info.." bug.

	* Non-placeable world items (such as wild beehives) can now be destroyed again.

	* It is no longer possible to configure a portal that is being protected by someone else's ward.

* **v1.2.1** (2023-02-05)

	* Translation added for Spanish

	* Added BepInEx dependency, updated Jotunn dependency to 2.10.4.

	* Detect portal placement and destruction.
	
	* Optimise portal hovering event, UI interaction, and resyncs.

	* Update console log messages.

	* Some bugfixes

* **v1.2.0** (2023-02-03)

	* Fix portals disappearing from the dropdown.

	* Fix portals sometimes duplicating.

* **v1.1.0** (2023-02-03)

	* Controller support!

	* Translations added for French, Portuguese (BR), German.

* **v1.0.1** (2023-02-01)

	* Improvements for dedicated servers.

	* Fix a bug that stopped XPortal showing the UI after destroying a portal.

* **v1.0.0** (2023-02-01)
	
	* Initial release.

</details>


# Credits

* sweetgiorni for creating AnyPortal
* kaiqueknup for translating to Brazillian Portuguese
* makou for translating to French, Spanish
* Smok3y97 for translating to German
* MexExe for translating to Polish, Russian
* hanawa07 for translating to Korean


# I did more too!

Please have a look at my other mod too! [XStorage](https://valheim.thunderstore.io/package/SpikeHimself/XStorage/) lets you open multiple chests at once, rename them, and move items/stacks to the most suitable chest.


# Support me

My mods are free and will remain free, for everyone to use, edit or learn from. I lovingly poured many hours of hard work into these projects. If you enjoy my mods and want to support my work, don't forget to click the Like button, and please consider buying me a coffee :)

[<img src="https://cdn.buymeacoffee.com/buttons/v2/default-yellow.png" height="40" align="right" />](https://www.buymeacoffee.com/SpikeHimself)
