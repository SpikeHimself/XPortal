# XPortal

XPortal is a Valheim mod that lets you select a portal's destination from a list. 

<img src="https://raw.githubusercontent.com/SpikeHimself/XPortal/main/images/controller.gif" height="180" />


# Download and installation instructions (for players)

XPortal is a [BepInEx](https://valheim.thunderstore.io/package/denikson/BepInExPack_Valheim/) plugin. As such, you must have BepInEx installed. Most other Valheim mods are also BepInEx plugins, so chances are you already have this.

XPortal makes use of the [Jotunn](https://www.nexusmods.com/valheim/mods/1138) library, so you must install that before installing XPortal. If you do not install Jotunn, XPortal will simply not be loaded by your game and it will not work.

I very strongly recommend using a mod manager such as [Vortex](https://www.nexusmods.com/site/mods/1) or [r2modman](https://valheim.thunderstore.io/package/ebkr/r2modman/). They will take care of everything for you and you don't have to worry about which files go where. I recommend against manual installation.
1. Make sure you have [BepInEx](https://valheim.thunderstore.io/package/denikson/BepInExPack_Valheim/) installed.
2. Install [Jotunn](https://www.nexusmods.com/valheim/mods/1138).
3. On [Nexus Mods](https://www.nexusmods.com/valheim/mods/2239) click 'Mod manager download', or on [Thunderstore](https://valheim.thunderstore.io/package/SpikeHimself/XPortal/) click 'Install with Mod Manager'.


To install XPortal on a dedicated server, copy all of the contents of the `plugins\` directory found inside the .zip file download to the  `Valheim\BepInEx\plugins\` directory on your server. 


# Bugs, Feature Requests and Translations

First of all, before you report a bug, please make sure that the problem you are experiencing is actually caused by XPortal. If you are running other mods, disable those, and see if the problem goes away. Or the other way around: disable XPortal, and see if that makes the problem go away. If you discover that XPortal is incompatible with another mod, please do report that, because I might be able to create work-arounds for that. If you are not sure, or you are struggling with these steps, then just report the problem, and we'll go from there.

It is important to me that I can make XPortal as bug free as possible, but **please bear in mind that without your `LogOutput.log`, I will not be able to debug your issue at all**. Just showing me a screenshot of an error is not enough for me to discover the cause of that error.

To report a bug, please navigate to the [Issues page](https://github.com/SpikeHimself/XPortal/issues), click [New issue](https://github.com/SpikeHimself/XPortal/issues/new/choose), choose `Bug report`, and fill out the template.

For feature requests, choose `Feature request` on the [New issue](https://github.com/SpikeHimself/XPortal/issues/new/choose) page.

To add a translation to XPortal, choose `Translation` when submitting a [New issue](https://github.com/SpikeHimself/XPortal/issues/new/choose).


# Installation instructions (for developers)

I will soon write a guide to get XPortal working in your development environment. For now, you can probably figure some stuff out by having a look at the [JotunnModStub](https://github.com/Valheim-Modding/JotunnModStub) project that XPortal inherited from AnyPortal. Please bear in mind that the information there might have changed since XPortal was created, and that XPortal itself may over time have diverted from the steps laid out there. Again, a guide will follow soon!


# I did more too!

Please have a look at my other mod too! [XStorage](https://www.nexusmods.com/valheim/mods/2290) lets you open multiple chests at once, rename them, and move items/stacks to the most suitable chest.


# Support me

My mods are free and will remain free, for everyone to use, edit or learn from. I lovingly poured many hours of hard work into these projects. If you enjoy my mods and want to support my work, please consider buying me a coffee :)

[<img src="https://cdn.buymeacoffee.com/buttons/v2/default-yellow.png" height="40" align="right" />](https://www.buymeacoffee.com/SpikeHimself)
