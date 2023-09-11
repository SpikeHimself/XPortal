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

#### Default destination

A Portal can be marked as the "Default Portal". When a Default Portal has been set, all newly built portals will immediately connect with that portal, without you having to go into the portal configuration panel.

#### Longer names

XPortal completely removes the character length restriction on portal names, so that you can give your portals clear and descriptive titles.

#### Ping a portal location

Forgot where you put your portal? You don't need to teleport to it to find out. Just click the Ping button next to the list, and XPortal will show the selected portal on your map, while also pinging its location to all players on the server.

If you prefer to play without a map, this button can be hidden, either by using the `nomap` global key, or by setting `PingMapDisabled` to `True`.

#### Multiplayer

XPortal has been built with multiplayer support at its core. All players must run the same version of XPortal. If you play on a dedicated server, that too needs to have same version of XPortal installed.

#### Gamepad support

The XPortal UI will respond to gamepad input when configuring your portal. As of v1.2.10 it even shows you the gamepad keyhints!

<img src="https://raw.githubusercontent.com/SpikeHimself/XPortal/main/images/ui-keyhints-small.png" />

The controls are as follows:

* `A` / `Cross` - Submit (i.e. press the OK button)
* `B`/ `Circle` - Cancel
* `Y` / `Triangle` - Ping the selected portal
* `X` / `Square` - Show/hide the contents of the dropdown
* `D-Pad Up` / `D-Pad Down` - Select the previous / next item in the dropdown


#### Mod compatibility and integration

XPortal has been made fully compatible with the following mods:

* [Nexus Update Check](https://valheim.thunderstore.io/package/nexusreupload/aedenthorn_Nexus_Update_Check/) by aedenthorn
* [VHVR - Valheim VR](https://valheim.thunderstore.io/package/Maynard/VHVR/) by Flatscreen to VR Modders
* [Stone Portal](https://valheim.thunderstore.io/package/JereKuusela/Stone_Portal/) by Jere Kuusela
* [Advanced Portals](https://valheim.thunderstore.io/package/RandyKnapp/AdvancedPortals/) by Randy Knapp

Furthermore, XPortal has a configuration option to fully integrate with [Advanced Portals](https://valheim.thunderstore.io/package/RandyKnapp/AdvancedPortals/). If you set `DisplayPortalColour` to `True`, each portal in XPortal's dropdown list will be prepended by a ">>" tag that has the same colour as the light that the portal emits. As of v1.2.10, Stone Portals also get their own colour!

<img src="https://raw.githubusercontent.com/SpikeHimself/XPortal/main/images/advancedportals-small.png" />

XPortal is known to be fully incompatible with:

* [AnyPortal](https://valheim.thunderstore.io/package/sweetgiorni/AnyPortal/) by sweetgiorni


# Configuration

XPortal's config file, which can be found at `Valheim\BepInEx\config\yay.spikehimself.xportal.cfg`, contains the following settings:

`PingMapDisabled`

Disable the Ping Map button completely. For players who wish to play without a map. This setting is enforced (but not overwritten) by the server.

`DisplayPortalColour`

Show a coloured ">>" tag in the list of portals to indicate the portal type (integration with [Advanced Portals](https://valheim.thunderstore.io/package/RandyKnapp/AdvancedPortals/) and [Stone Portal](https://valheim.thunderstore.io/package/JereKuusela/Stone_Portal/)).

`DoublePortalCosts`

Since XPortal is essentially a cheat, in that you only need half the amount of portals now, this setting allows you to compensate for that by doubling portal costs. This setting is enforced (but not overwritten) by the server.

`HidePortalDistance`

If you don't want to see how far away the portals in the list are, you can use this option to remove that. This setting is enforced (but not overwritten) by the server.

`DefaultPortal`

This configuration option exists to save your personal Default Portal. Its value will be set by checking the Default Portal checkbox on the portal configuration panel. This value should not be manually edited in the file.


# Installation instructions

XPortal is a [BepInEx](https://valheim.thunderstore.io/package/denikson/BepInExPack_Valheim/) plugin. As such, you must have BepInEx installed. Most other Valheim mods are also BepInEx plugins, so chances are you already have this.

XPortal makes use of the [Jotunn](https://valheim.thunderstore.io/package/ValheimModding/Jotunn/) library, so you must install that before installing XPortal. If you do not install Jotunn, XPortal will simply not be loaded by your game and it will not work.

I very strongly recommend using a mod manager such as [Vortex](https://www.nexusmods.com/site/mods/1) or [r2modman](https://valheim.thunderstore.io/package/ebkr/r2modman/). They will take care of everything for you and you don't have to worry about which files go where. I recommend against manual installation.
1. Make sure you have [BepInEx](https://valheim.thunderstore.io/package/denikson/BepInExPack_Valheim/) installed.
2. Install [Jotunn](https://valheim.thunderstore.io/package/ValheimModding/Jotunn/).
3. On [Nexus Mods](https://www.nexusmods.com/valheim/mods/2239) click 'Mod manager download', or on [Thunderstore](https://valheim.thunderstore.io/package/SpikeHimself/XPortal/) click 'Install with Mod Manager'.


To install XPortal on a dedicated server, copy all of the contents of the `plugins\` directory found inside the .zip file download to the  `Valheim\BepInEx\plugins\` directory on your server. 


# Bugs, Feature Requests and Translations

First of all, before you report a bug, please make sure that the problem you are experiencing is actually caused by XPortal. If you are running other mods, disable those, and see if the problem goes away. Or the other way around: disable XPortal, and see if that makes the problem go away. If you discover that XPortal is incompatible with another mod, please do report that, because I might be able to create work-arounds for that. If you are not sure, or you are struggling with these steps, then just report the problem, and we'll go from there.

It is important to me that I can make XPortal as bug-free as possible, but **please bear in mind that without your `LogOutput.log`, I will not be able to debug your issue at all**. Just showing me a screenshot of an error is not enough for me to discover the cause of that error.

To report a bug, please navigate to the [Issues page](https://github.com/SpikeHimself/XPortal/issues), click [New issue](https://github.com/SpikeHimself/XPortal/issues/new/choose), choose `Bug report`, and fill out the template.

For feature requests, choose `Feature request` on the [New issue](https://github.com/SpikeHimself/XPortal/issues/new/choose) page.

To add a translation to XPortal, choose `Translation` when submitting a [New issue](https://github.com/SpikeHimself/XPortal/issues/new/choose).


# Credits

* sweetgiorni for creating AnyPortal
* kaiqueknup for translating to Brazillian Portuguese
* makou for translating to French, Spanish
* Smok3y97 for translating to German
* MexExe for translating to Polish, Russian
* hanawa07 for translating to Korean
* bonesbro for adding a colour for Stone Portals (#39)
* VasariRulez for translating to Italian


# I did more too!

Please have a look at my other mod too! [XStorage](https://valheim.thunderstore.io/package/SpikeHimself/XStorage/) lets you open multiple chests at once, rename them, and move items/stacks to the most suitable chest.
