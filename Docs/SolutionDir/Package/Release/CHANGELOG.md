#### v1.2.14 (2023-08-26)

* Update Jotun 2.12.6
<details>
<summary>Click to view previous versions</summary>
	
* **v1.2.13** (2023-06-17)

	* v1.2.13 is a hotfix to v1.2.12

	* Remember portal connections between sessions (v1.2.12)

	* Fix a crash related to placing items or using the hoe (v1.2.12)

	* Jotunn update 2.12.1 (v1.2.12)

	* Revert MonoMod dependencies (hotfixed in v1.2.13)

	
* **v1.2.11** (2023-06-13)

	* Add translation to Italian

	* Update Jotun 2.12.0, BepInEx 5.4.2105

	* Fix portals not connecting after game update 0.216.9

* **v1.2.10** (2023-04-02)

	* Improved gamepad support: the XPortal configuration panel now shows gamepad keyhints!

	* Give Stone Portals their own colour (thanks bonesbro!)

	* Fix path separators in XPortal release archive

	* Show portal colour tag on hover

	* Fix crash when destroying a portal that XPortal doesn't know of

	* Jotunn update 2.11.2, BepInEx update 5.4.2102

	* Some portal sync optimisations

	* Improved logging

* **v1.2.9** (2023-03-23)

	* Fix Ping button not working (attempt #2)

* **v1.2.8** (2023-03-22)

	* Add config option `DoublePortalCosts` which doubles the costs of a portal when enabled

	* Fix Ping button not working after a recent Valheim update

	* Add server installation instructions to documentation

	* Add Configuration section to documentation

* **v1.2.7** (2023-03-14)

	* Dependency updates: BepInEx 5.4.21, Jotunn 2.11.0

	* Some fixes towards further [Advanced Portals](https://valheim.thunderstore.io/package/RandyKnapp/AdvancedPortals/) compatibility
 
	* Fix a HarmonyX warning that occurs when loading XPortal

	* Fix error that sometimes appears when logging out or quitting the game

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


