# XPortal

XPortal is a Valheim mod that lets you select a portal's destination from a list. 

<img src="https://raw.githubusercontent.com/SpikeHimself/XPortal/main/images/controller.gif" height="140" />


# Download and installation instructions (for players)

XPortal is a [BepInEx](https://valheim.thunderstore.io/package/denikson/BepInExPack_Valheim/) plugin. As such, you must have BepInEx installed. Most other Valheim mods are also BepInEx plugins, so chances are you already have this. If you use Vortex, you can't even get rid of BepInEx - it forces you to use it. I recommend using Vortex for many reasons and this is one of them.

XPortal makes use of the [Jötunn](https://www.nexusmods.com/valheim/mods/1138) library, so you must install that before installing XPortal. If you do not install Jötunn, XPortal will simply not be loaded by your game and it will not work.

**If you are new to modding or you're just not sure what the best approach is**, I *very strongly advise* that you install a mod manager such as [Vortex](https://www.nexusmods.com/site/mods/1) or [r2modman](https://valheim.thunderstore.io/package/ebkr/r2modman/). They will take care of everything for you and you don't have to worry about which files go where.

Let me emphasise this point just a little more. If you just want to mess around with the files and download manually because you're learning things, you are probably at a level where *you don't need installation instructions*. The fact you are here and reading this indicates that *you are better off not installing manually, but should instead use one of the mod managers mentioned above*.

With that out of the way, if for whatever unearthly reason you still want to proceed and install manually (**I really don't recommend this, in case that wasn't clear yet**), here is how to do it:

1. Install [BepInEx](https://valheim.thunderstore.io/package/denikson/BepInExPack_Valheim/). Instructions for manual installation can be found [here](https://docs.bepinex.dev/articles/user_guide/installation/index.html).
2. Install [Jötunn](https://www.nexusmods.com/valheim/mods/1138). Instructions can be found [here](https://valheim-modding.github.io/Jotunn/guides/installation.html).
3. Download XPortal from [Nexus Mods](https://www.nexusmods.com/valheim/mods/2239), [Thunderstore](https://valheim.thunderstore.io/package/SpikeHimself/XPortal/) or [GitHub](https://github.com/SpikeHimself/XPortal/releases).
4. Directly inside of the XPortal .zip file you will come across some files related to thunderstore publication. *You do not need these files*. You will also find a directory called `plugins`. You do need the contents of that directory in the following steps.
5. Locate your BepInEx plugins directory. It should be inside the Valheim installation directory. If you run Steam, you will find it here: `[..]\Steam\steamapps\common\Valheim\BepInEx\plugins`.
6. Extract *all* contents of XPortal's `plugins` directory to that same `plugins` directory in your game directory. When you have done this successfully, you should have:
	* `[..]\Steam\steamapps\common\Valheim\BepInEx\plugins\XPortal\XPortal.dll`
	* `[..]\Steam\steamapps\common\Valheim\BepInEx\plugins\XPortal\Translations\` (and its contents)

Just as a side note, do not skip the Translations directory. The English translations inside of it are also required for XPortal to function correctly.

Once again, you do not have to take *any* of these steps if you used a mod manager.


# Bugs, Feature Requests and Translations

First of all, before you report a bug, please make sure that the problem you are experiencing is actually caused by XPortal. If you are running other mods, disable those, and see if the problem goes away. Or the other way around: disable XPortal, and see if that makes the problem go away. If you discover that XPortal is incompatible with another mod, please do report that, because I might be able to create work-arounds for that. If you are not sure, or you are struggling with these steps, then just report the problem, and we'll go from there.

It is important to me that I can make XPortal as bug free as possible, but **please bear in mind that without your `LogOutput.log`, I will not be able to debug your issue at all**. Just showing me a screenshot of an error is not enough for me to discover the cause of that error.

To report a bug, please navigate to the [Issues page](https://github.com/SpikeHimself/XPortal/issues), click [New issue](https://github.com/SpikeHimself/XPortal/issues/new/choose), choose `Bug report`, and fill out the template.

For feature requests, choose `Feature request` on the [New issue](https://github.com/SpikeHimself/XPortal/issues/new/choose) page.

To add a translation to XPortal, choose `Translation` when submitting a [New issue](https://github.com/SpikeHimself/XPortal/issues/new/choose).


# I did more too!

Please have a look at my other mod too! [XStorage](https://www.nexusmods.com/valheim/mods/2290) lets you open multiple chests at once, rename them, and move items/stacks to the most suitable chest.

# Support me
My mods are free and will remain free, for everyone to use, edit or learn from. I lovingly poured many hours of hard work into these projects. If you enjoy my mods and want to support my work, please consider buying me a coffee :)

[<img src="https://cdn.buymeacoffee.com/buttons/v2/default-yellow.png" height="40" align="right" />](https://www.buymeacoffee.com/SpikeHimself)
