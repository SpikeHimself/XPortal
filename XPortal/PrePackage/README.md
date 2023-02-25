# XPortal 

 An [AnyPortal](https://valheim.thunderstore.io/package/sweetgiorni/AnyPortal/) revamp.

 <img src="https://raw.githubusercontent.com/SpikeHimself/XPortal/main/images/controller.gif" height="140" />


# Description

XPortal lets you select a portal destination from a list of existing portals. 

No more tag pairing, and no more portal hubs!


# Features

### Select a destination

When interacting with a portal, instead of entering a tag which has to match another portal's, XPortal lets you choose the portal's destination from a list.

The "edit tag" window has been replaced by XPortal's own configuration window. You can now enter a Name and choose a Destination. Portal connections can now be one-way. In other words, you can go from portal A to portal B, all while portal B might be connected to portal C, or not even connected at all!

For your convenience, the list also shows you how far away the portals are.

### Longer names

XPortal completely removes the character length restriction on portal names, so that you can give your portals clear and descriptive titles.

### Ping a portal location

Forgot where you put your portal? You don't need to go through it to find out. Just press the Ping button next to the list, and XPortal will show the selected portal on your map, while also pinging its location to all players on the server.

As of v1.2.4, a configuration option exists which disables this functionality. If you want to play without a map or just don't want to be able to ping  your portals, XPortal now lets you do so. The option can be found in XPortal's config file, and is called `PingMapDisabled`. By default the Ping Map button is *enabled*!  If you play on a server, the server's setting will be leading.

And as of v1.2.5, the Ping Map functionality will be disabled when the "nomap" global key is set (which you can do by typing "nomap" in the console).

### Multiplayer

XPortal has been built with multiplayer support at its core. This does require all players on the server to run the same version of XPortal. If you play on a dedicated server, that too needs to have same version of XPortal installed.

### Controller support

As of v1.1.0, XPortal will respond to controller input when configuring your portal. It's not as neat as the rest of the Valheim UI - it won't show you the keyhints on the screen - but it does work somewhat intuitively.

* `A` / `Cross` - Submit (i.e. press the Okay button)

* `B` / `Circle` - Cancel

* `Y` / `Triangle` - Ping the selected portal

* `X` / `Square` - Show the contents of the dropdown

* `D-Pad Up` / `D-Pad Down` - Select the previous / next item in the dropdown


# Installation instructions

XPortal is a [BepInEx](https://valheim.thunderstore.io/package/denikson/BepInExPack_Valheim/) plugin. As such, you must have BepInEx installed. Most other Valheim mods are also BepInEx plugins, so chances are you already have this. If you use Vortex, you can't even get rid of BepInEx - it forces you to use it. I recommend using Vortex for many reasons and this is one of them.

XPortal makes use of the [Jötunn](https://valheim.thunderstore.io/package/ValheimModding/Jotunn/) library, so you must install that before installing XPortal. If you do not install Jötunn, XPortal will simply not be loaded by your game and it will not work.

**If you are new to modding or you're just not sure what the best approach is**, I *very strongly advise* that you install a mod manager such as [Vortex](https://www.nexusmods.com/site/mods/1) or [r2modman](https://valheim.thunderstore.io/package/ebkr/r2modman/). They will take care of everything for you and you don't have to worry about which files go where.

Let me emphasise this point just a little more. If you just want to mess around with the files and download manually because you're learning things, you are probably at a level where *you don't need installation instructions*. The fact you are here and reading this indicates that *you are better off not installing manually, but should instead use one of the mod managers mentioned above*.

With that out of the way, if for whatever unearthly reason you still want to proceed and install manually (**I really don't recommend this, in case that wasn't clear yet**), here is how to do it:

1. Install [BepInEx](https://valheim.thunderstore.io/package/denikson/BepInExPack_Valheim/). Instructions for manual installation can be found [here](https://docs.bepinex.dev/articles/user_guide/installation/index.html).
2. Install [Jötunn](https://valheim.thunderstore.io/package/ValheimModding/Jotunn/). Instructions can be found [here](https://valheim-modding.github.io/Jotunn/guides/installation.html).
3. Download XPortal from [Nexus Mods](https://www.nexusmods.com/valheim/mods/2239), [Thunderstore](https://valheim.thunderstore.io/package/SpikeHimself/XPortal/) or [GitHub](https://github.com/SpikeHimself/XPortal/releases).
4. Directly inside of the XPortal .zip file you will come across some files related to thunderstore publication. *You do not need these files*. You will also find a directory called `plugins`. You will need the contents of that directory in the following steps.
6. Locate your BepInEx plugins directory. It should be inside the Valheim installation directory. If you run Steam, you will find it here: `[..]\Steam\steamapps\common\Valheim\BepInEx\plugins`.
7. Extract *all* contents of XPortal's `plugins` directory to that same `plugins` directory in your game directory. When you have done this successfully, you should have:
	* `[..]\Steam\steamapps\common\Valheim\BepInEx\plugins\XPortal\XPortal.dll`
	* `[..]\Steam\steamapps\common\Valheim\BepInEx\plugins\XPortal\Translations\` (and its contents)

Just as a side note, do not skip the Translations directory. The English translations inside of it are also required for XPortal to function correctly.

Once again, you do not have take *any* of these steps if you used a mod manager.


# Bugs, Feature Requests and Translations

First of all, before you report a bug, please make sure that the problem you are experiencing is actually caused by XPortal. If you are running other mods, disable those, and see if the problem goes away. Or the other way around: disable XPortal, and see if that makes the problem go away. If you discover that XPortal is incompatible with another mod, please do report that, because I might be able to create work-arounds for that. If you are not sure, or you are struggling with these steps, then just report the problem, and we'll go from there.

It is important to me that I can make XPortal as bug free as possible, but **please bear in mind that without your "LogOutput.log", I will not be able to debug your issue at all**. Just showing me a screenshot of an error is not enough for me to discover the cause of that error.

To report a bug, please navigate to the [Issues page](https://github.com/SpikeHimself/XPortal/issues), click [New issue](https://github.com/SpikeHimself/XPortal/issues/new/choose), choose `Bug report`, and fill out the template.

For feature requests, choose `Feature request` on the [New issue](https://github.com/SpikeHimself/XPortal/issues/new/choose) page.

To add a translation to XPortal, choose `Translation` when submitting a [New issue](https://github.com/SpikeHimself/XPortal/issues/new/choose).


# Improvements over AnyPortal

As mentioned earlier, XPortal is a rewrite of the popular AnyPortal mod. Most players won't necessarily notice much difference, bar a few bugfixes. Under the hood, though, XPortal takes a completely different approach to portal management. Also, the UI was completely overhauled.

Here are the most prominent changes:

* **New UI**:
AnyPortal built its UI incorporating the existing "edit tag" window. XPortal doesn't use the original "edit tag" window at all.

* **Robust network synchronisation**:
AnyPortal updates the information on your screen when you interact with a portal. XPortal updates all portal information at the very moment that any portal is changed, even when someone else does it.

* **Smaller file**:
XPortal uses Jötunn's UI elements, so it no longer needs its own Unity assets.

* **Code optimisation**:
The way AnyPortal dealt with portal tags and targets has been overhauled. XPortal keeps its own list and keeps that updated whenever any portal change happens. Because of this, it will not have to query game data every time you interact with a portal. If you run Valheim as host, you will no longer experience a harsh performance hit while others are in your game.

* **Scalability**:
You won't notice this in your game, but the way XPortal's code is organised should make it much easier to maintain. So when a Valheim update breaks everything, it shouldn't be too hard for me to get things up and running again. XPortal is also ready to deal with any feature requests that you might have. If you are interested in XPortal's code, you are welcome to visit the project's [GitHub page](https://github.com/SpikeHimself/XPortal)!


# Credits

* sweetgiorni for creating AnyPortal

* kaiqueknup for translating to Brazillian Portuguese 

* makou for translating to French, Spanish

* Smok3y97 for translating to German

* MexExe for translating to Polish, Russian

* hanawa07 for translating to Korean


# Changelogs

* **v1.2.5** (2023-02-15)

	* Hide the Ping Map button when the "[nomap](https://valheim.fandom.com/wiki/Global_Keys)" global key is set (you can do this by typing "nomap" in the console).

	* The `PingMapDisabled` config option is now enforced by the server. If the server has this set to `true`, the Ping Map button will be hidden, regardless of your own settings.

	* Add sync support for the stone portal object. This fixes a compatibility issue with the mod [Stone Portal](https://valheim.thunderstore.io/package/JereKuusela/Stone_Portal/)

<details>
<summary>Click to view previous versions</summary>

* **v1.2.4** (2023-02-13)

	* Add translation to Korean

	* Items in the dropdown no longer overlap each other

	* Items in the dropdown are now highlighted when you hover over them

	* Added configuration option `PingMapDisabled` which disables the ability to ping portals in the list. By default the Ping Map button remains *enabled*.

	* Various code optimisations

* **v1.2.3** (2023-02-09)

	* Add support for [Nexus Update Check](https://www.nexusmods.com/valheim/mods/102)

	* Minor UI modifications to accomodate longer translations

	* Added translations for Polish and Russian

	* Updated description/readme

	* Code cleanup and minor bugfixes

* **v1.2.2** (2023-02-06)

	* Fixed "Fetching portal info.." bug

	* Non-placeable world items (such as wild beehives) can now be destroyed again

	* It is no longer possible to configure a portal that is being protected by someone else's ward

* **v1.2.1** (2023-02-05)

	* Translation added for Spanish

	* Added BepInEx dependency, updated Jötunn dependency to 2.10.4

	* Detect portal placement and destruction
	
	* Optimise portal hovering event, UI interaction, and resyncs

	* Update console log messages

	* Some bugfixes

* **v1.2.0** (2023-02-03)

	* Fix portals disappearing from the dropdown

	* Fix portals sometimes duplicating

* **v1.1.0** (2023-02-03)

	* Controller support!

	* Translations added for French, Portuguese (BR), German

* **v1.0.1** (2023-02-01)

	* Improvements for dedicated servers

	* Fix a bug that stopped XPortal showing the UI after destroying a portal

* **v1.0.0** (2023-02-01)
	
	* Initial release
</details>


# Support me

XPortal is free and will remain free, for everyone to use, edit or learn from. If you enjoy XPortal and want to support my work, please consider buying me a coffee:

[<img src="https://cdn.buymeacoffee.com/buttons/v2/default-yellow.png" height="40">](https://www.buymeacoffee.com/SpikeHimself)
