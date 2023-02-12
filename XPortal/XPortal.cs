using BepInEx;
using Jotunn.Managers;
using Jotunn.Utils;
using System.Collections.Generic;
using UnityEngine;

namespace XPortal
{
    [BepInPlugin(PluginGUID, PluginName, PluginVersion)]
    [BepInDependency(Jotunn.Main.ModGuid)]
    [NetworkCompatibility(CompatibilityLevel.EveryoneMustHaveMod, VersionStrictness.Patch)]
    public class XPortal : BaseUnityPlugin
    {
        #region Plugin info
        // This is *the* place to edit plugin details. Everywhere else will be generated based on this info.
        public const string PluginGUID = "yay.spikehimself.xportal";
        public const string PluginName = "XPortal";
        public const string PluginVersion = "1.2.3";
        public const string PluginDescription = "Select portal destination from a list of existing portals. No more tag pairing, and no more portal hubs! XPortal is a complete rewrite of the popular mod AnyPortal.";
        public const string PluginWebsiteUrl = "https://github.com/SpikeHimself/XPortal";
        public const int PluginNexusId = 2239;
        //public const string PluginBepinVersion = ??
        public const string PluginJotunnVersion = Jotunn.Main.Version;
        #endregion

        /// <summary>
        /// Set to true via Game.Start() -> Patches.GameStartPatch.Postfix() -> Patches_OnGameStart()
        /// </summary>
        private bool gameStarted = false;

        #region Determine Environment
        /// <summary>
        /// Are we the Server?
        /// </summary>
        /// <returns>True if ZNet says we are a server</returns>
        public static bool IsServer()
        {
            return ZNet.instance != null && ZNet.instance.IsServer();
        }

        /// <summary>
        /// Are we Headless? (dedicated server)
        /// </summary>
        /// <returns>True if SystemInfo.graphicsDeviceType is not set</returns>
        public static bool IsHeadless()
        {
            return GUIManager.IsHeadless();
        }
        #endregion

        #region Unity Events
        /// <summary>
        /// https://docs.unity3d.com/ScriptReference/MonoBehaviour.Awake.html
        /// </summary>
        private void Awake()
        {
            // Hello, world!
            Jotunn.Logger.LogDebug("I HAVE ARRIVED!");

            // https://www.nexusmods.com/valheim/mods/102
            Config.Bind<int>("General", "NexusID", PluginNexusId, "Nexus mod ID for updates (do not change)");

            // Subscribe to Patches events
            Patches.OnGameStart += Patches_OnGameStart;
            Patches.OnPrePortalHover += Patches_OnPrePortalHover;
            //Patches.OnPostPortalInteract += Patches_OnPostPortalInteract;
            Patches.OnPortalRequestText += Patches_OnPortalRequestText;
            Patches.OnPortalPlaced += Patches_OnPortalPlaced;
            Patches.OnPortalDestroyed += Patches_OnPortalDestroyed;

            if (!IsHeadless())
            {
                // Add buttons
                XPortalUI.Instance.AddInputs();
            }

            // Subscribe to Jotunn's OnVanillaMapDataLoaded event. This is the earliest point where we can update the known portals.
            // (but on dedicated servers this isn't triggered)
            MinimapManager.OnVanillaMapDataLoaded += MinimapManager_OnVanillaMapDataLoaded;

            // Apply the Harmony patches
            Patches.Patch();
        }

        /// <summary>
        /// https://docs.unity3d.com/ScriptReference/MonoBehaviour.Update.html
        /// </summary>
        private void Update()
        {
            if (IsHeadless() || !gameStarted || ZInput.instance == null || !XPortalUI.Instance.IsActive())
            {
                return;
            }

            XPortalUI.Instance.HandleInput();
        }

        /// <summary>
        /// https://docs.unity3d.com/ScriptReference/MonoBehaviour.OnDestroy.html
        /// </summary>
        private void OnDestroy()
        {
            Patches.Unpatch();
            if (!IsHeadless())
            {
                XPortalUI.Instance?.Dispose();
            }
            KnownPortalsManager.Instance?.Dispose();
        }
        #endregion

        #region Jotunn Events
        /// <summary>
        /// https://valheim-modding.github.io/Jotunn/tutorials/events.html
        /// </summary>
        private void MinimapManager_OnVanillaMapDataLoaded()
        {
            // Ask the server to send us the portals
            var myId = ZDOMan.instance.GetMyID();
            RPC.SendSyncRequestToServer($"{myId} has joined the game");
        }
        #endregion

        #region Patch Events
        /// <summary>
        /// OnGameStart will be called by Patch.GameStartPatch which patches Game.Start.
        /// At this point the world is beginning to load. The portals don't exist yet.
        /// </summary>
        internal void Patches_OnGameStart()
        {
            if (!IsHeadless())
            {
                XPortalUI.Instance.PortalInfoSubmitted -= XPortalUI_OnPortalInfoSubmitted;
                XPortalUI.Instance.PortalInfoSubmitted += XPortalUI_OnPortalInfoSubmitted;

                XPortalUI.Instance.PingMapButtonClicked -= XPortalUI_OnPingMapButtonClicked;
                XPortalUI.Instance.PingMapButtonClicked += XPortalUI_OnPingMapButtonClicked;
            }

            RPC.RegisterRPCs();

            gameStarted = true;
        }

        /// <summary>
        /// Render the string that is displayed on the screen when the player hovers over a portal
        /// </summary>
        /// <param name="result">The string output that the player gets to see</param>
        /// <param name="portalId">The ZDOID of the portal the player is hovering over</param>
        internal void Patches_OnPrePortalHover(out string result, ZDO portalZDO, ZDOID portalId)
        {
            if (!KnownPortalsManager.Instance.ContainsId(portalId))
            {
                // This appears to be a new portal
                Jotunn.Logger.LogDebug($"[OnPrePortalHover] Hovering over new portal {portalId}");

                // Sometimes XPortal does not detect placement of new portals.
                // Assuming that that is the case here, let's just create a new dummy portal for now.
                // We'll tell the server about it *after* interacting with it (when updating the name/target in the UI).
                // Everything will be fine. Promise.
                var dummyPortal = new KnownPortal(portalId, string.Empty, portalZDO.GetPosition(), ZDOID.None);
                KnownPortalsManager.Instance.AddOrUpdate(dummyPortal);
            }

            // Get information about the portal being hovered over
            var portal = KnownPortalsManager.Instance.GetKnownPortalById(portalId);
            string outputPortalName = portal.Name;
            if (string.IsNullOrEmpty(outputPortalName))
            {
                outputPortalName = "$piece_portal_tag_none"; // "(No Name)"
            }

            string outputPortalDestination = "$piece_portal_target_none"; // "(None)"
            if (portal.HasTarget())
            {
                // Get information about the portal's destination
                var targetId = portal.Target;
                if (!KnownPortalsManager.Instance.ContainsId(targetId))
                {
                    Jotunn.Logger.LogDebug($"[OnPrePortalHover] Target portal {targetId} appears to be invalid");
                    RPC.SendSyncRequestToServer("Hovering over portal with invalid target");
                    result = "Fetching portal info....";
                    return;
                }

                outputPortalDestination = KnownPortalsManager.Instance.GetKnownPortalById(targetId).Name;
                if (string.IsNullOrEmpty(outputPortalDestination))
                {
                    outputPortalDestination = "$piece_portal_tag_none"; // "(No Name)"
                }
            }

            result = Localization.instance.Localize(
                         $"$piece_portal_tag: {outputPortalName}\n"                         // "Name: {name}"
                       + $"$piece_portal_target: {outputPortalDestination}\n"               // "Destination: {name}"
                       + $"[<color=yellow><b>$KEY_Use</b></color>] $piece_portal_settag"    // "[E] Configure"
                     );
        }

        /// <summary>
        /// When interacting with a portal, we want to show the XPortal UI
        /// </summary>
        /// <param name="portalZDO"></param>
        internal void Patches_OnPortalRequestText(ZDOID portalId)
        {
            if (!KnownPortalsManager.Instance.ContainsId(portalId))
            {
                // TODO: show a friendly message on the screen
                Jotunn.Logger.LogError("Interacting with an unknown portal");
                return;
            }

            var portal = KnownPortalsManager.Instance.GetKnownPortalById(portalId);
            Jotunn.Logger.LogDebug($"[OnPostPortalInteract] Interacting with: {portal}");
            XPortalUI.Instance.ConfigurePortal(portal);
        }

        /// <summary>
        /// The player has placed a portal
        /// </summary>
        /// <param name="portalId">The ZDOID of the portal being placed</param>
        /// <param name="location">The location in the world where the portal was placed</param>
        internal void Patches_OnPortalPlaced(ZDOID portalId, Vector3 location)
        {
            Jotunn.Logger.LogDebug($"[OnPortalPlaced] Portal `{portalId}` was placed");

            var portal = new KnownPortal(portalId, string.Empty, location, ZDOID.None);
            RPC.SendAddOrUpdateRequestToServer(portal);
        }

        /// <summary>
        /// A portal was destroyed by damage or by a hammer
        /// </summary>
        /// <param name="portalId">The ZDOID of the portal being destroyed</param>
        internal void Patches_OnPortalDestroyed(ZDOID portalId)
        {
            var portalName = KnownPortalsManager.Instance.GetKnownPortalById(portalId).Name;
            Jotunn.Logger.LogDebug($"[OnPortalDestroyed] Portal `{portalName}` is being destroyed");
            RPC.SendRemoveRequestToServer(portalId);
        }
        #endregion

        #region Process ZDOs
        /// <summary>
        /// We have received a request to update our KnownPortals using the actual ZDOs that our game knows about
        /// </summary>
        /// <param name="reason">The reason why this request is being made</param>
        /// <returns>A list of portal ZDOs</returns>
        internal static List<ZDO> ProcessSyncRequest(string reason)
        {
            var allPortalZDOs = GetAllPortalZDOs();
            Jotunn.Logger.LogDebug($"[ProcessSyncRequest] Fetched {allPortalZDOs.Count} portals");

            ForceLocalPortalUpdate(allPortalZDOs);

            RPC.SendResyncToClients(KnownPortalsManager.Instance.Pack(), reason);

            return allPortalZDOs;
        }

        /// <summary>
        /// Queries ZDOMan for all ZDOs that were instantiated using the Portal prefab (Game.m_portalPrefab)
        /// </summary>
        /// <returns>A list of portal ZDOs</returns>
        private static List<ZDO> GetAllPortalZDOs()
        {
            var allPortalZDOs = new List<ZDO>();
            ZDOMan.instance.GetAllZDOsWithPrefab(Game.instance.m_portalPrefab.name, allPortalZDOs);
            return allPortalZDOs;
        }

        /// <summary>
        /// Update our KnownPortals from a list of ZDOs.
        /// If we are not the server then also send the server a Sync Request.
        /// </summary>
        private static void ForceLocalPortalUpdate(List<ZDO> allPortals)
        {
            KnownPortalsManager.Instance.UpdateFromZDOList(allPortals);

            if (!IsServer())
            {
                Jotunn.Logger.LogDebug("[ForceLocalPortalUpdate] Send Sync Request");
                RPC.SendSyncRequestToServer("Local portal list was updated");
            }
        }
        #endregion

        #region UI Events
        /// <summary>
        /// The Okay button was clicked in the XPortal UI
        /// </summary>
        /// <param name="portal">The KnownPortal that was being configured</param>
        /// <param name="newName">The new name for the portal</param>
        /// <param name="newTarget">The new target for the portal</param>
        private void XPortalUI_OnPortalInfoSubmitted(KnownPortal portal, string newName, ZDOID newTarget)
        {
            if (!portal.Name.Equals(newName) || !portal.Targets(newTarget))
            {
                portal.Name = newName;
                portal.Target = newTarget;

                // Ask the server to update the portal
                Jotunn.Logger.LogDebug($"[OnPortalInfoSubmitted] Updating portal `{portal.Name}`");
                RPC.SendAddOrUpdateRequestToServer(portal);
            }
        }

        /// <summary>
        /// The Ping Map button was clicked in the XPortal UI
        /// </summary>
        /// <param name="targetId">The ZDOID of the portal to ping</param>
        private void XPortalUI_OnPingMapButtonClicked(ZDOID targetId)
        {
            var portal = KnownPortalsManager.Instance.GetKnownPortalById(targetId);

            Jotunn.Logger.LogDebug($"[OnPingMapButtonClicked] {portal}");

            // Get selected portal name and position
            string name = portal.Name;
            Vector3 location = portal.Location;

            // Send ping to all players
            RPC.SendPingMapToEverybody(location, name);

            // Show location on the map
            Minimap.instance.ShowPointOnMap(location);
        }
        #endregion

    }
}
