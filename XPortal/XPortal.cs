using BepInEx;
using Jotunn.Managers;
using Jotunn.Utils;
using System.Collections.Generic;
using UnityEngine;

namespace XPortal
{
    [BepInPlugin(PluginGUID, PluginName, PluginVersion)]
    [BepInIncompatibility("com.sweetgiorni.anyportal")]
    [BepInDependency(Jotunn.Main.ModGuid)]
    [NetworkCompatibility(CompatibilityLevel.EveryoneMustHaveMod, VersionStrictness.Patch)]
    public class XPortal : BaseUnityPlugin
    {
        #region Plugin info
        // This is *the* place to edit plugin details. Everywhere else will be generated based on this info.
        public const string PluginGUID = "yay.spikehimself.xportal";
        public const string PluginName = "XPortal";
        public const string PluginVersion = "1.2.5";
        public const string PluginDescription = "Select portal destination from a list of existing portals. No more tag pairing, and no more portal hubs! XPortal is a complete rewrite of the popular mod AnyPortal.";
        public const string PluginWebsiteUrl = "https://github.com/SpikeHimself/XPortal";
        public const int PluginNexusId = 2239;
        //public const string PluginBepinVersion = ??
        public const string PluginJotunnVersion = Jotunn.Main.Version;
        #endregion

        public const string StonePortalPrefabName = "portal";

        /// <summary>
        /// Set to true via a patch on Game.Start()
        /// </summary>
        private static bool gameStarted = false;

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
        [System.Diagnostics.CodeAnalysis.SuppressMessage("CodeQuality", "IDE0051:Remove unused private members", Justification = "MonoBehaviour.Awake is called when the script instance is being loaded.")]
        private void Awake()
        {
            // Hello, world!
            Jotunn.Logger.LogDebug("I HAVE ARRIVED!");

            // Load config
            XPortalConfig.Instance.LoadConfigFile(Config);

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
        [System.Diagnostics.CodeAnalysis.SuppressMessage("CodeQuality", "IDE0051:Remove unused private members", Justification = "MonoBehaviour.Update is called every frame, if the MonoBehaviour is enabled.")]
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
        [System.Diagnostics.CodeAnalysis.SuppressMessage("CodeQuality", "IDE0051:Remove unused private members", Justification = "MonoBehaviour.OnDestroy occurs when a Scene or game ends.")]
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

            // Ask the server to send us the config
            RPC.SendConfigRequestToServer();
        }
        #endregion

        #region Patch Events
        /// <summary>
        /// OnGameStart will be called by a patch on Game.Start.
        /// At this point the world is beginning to load. The portals don't exist yet.
        /// </summary>
        internal static void OnGameStart()
        {
            RPC.RegisterRPCs();

            gameStarted = true;
        }

        /// <summary>
        /// Render the string that is displayed on the screen when the player hovers over a portal
        /// </summary>
        /// <param name="result">The string output that the player gets to see</param>
        /// <param name="portalId">The ZDOID of the portal the player is hovering over</param>
        internal static void OnPrePortalHover(out string result, ZDOID portalId, Vector3 location)
        {
            if (!KnownPortalsManager.Instance.ContainsId(portalId))
            {
                // This appears to be a new portal
                Jotunn.Logger.LogDebug($"[OnPrePortalHover] Hovering over new portal {portalId}");

                // Sometimes XPortal does not detect placement of new portals.
                // Assuming that that is the case here, let's just create a new dummy portal for now.
                // We'll tell the server about it *after* interacting with it (when updating the name/target in the UI).
                // Everything will be fine. Promise.
                var dummyPortal = new KnownPortal(portalId);
                KnownPortalsManager.Instance.AddOrUpdate(dummyPortal);
            }

            // Get information about the portal being hovered over
            var portal = KnownPortalsManager.Instance.GetKnownPortalById(portalId);
            string outputPortalName = portal.GetFriendlyName();
            string outputPortalDestination = portal.GetFriendlyTargetName();

            if (portal.HasTarget())
            {
                // Get information about the portal's destination
                var targetId = portal.Target;
                if (!KnownPortalsManager.Instance.ContainsId(targetId))
                {
                    Jotunn.Logger.LogError($"[OnPrePortalHover] Target portal {targetId} appears to be invalid");
                    RPC.SendSyncRequestToServer($"Hovering over portal `{outputPortalName}` which has invalid target `{targetId}`");
                    result = "Fetching portal info...";
                    return;
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
        /// <param name="portalId"></param>
        internal static void OnPortalRequestText(ZDOID portalId)
        {
            if (!KnownPortalsManager.Instance.ContainsId(portalId))
            {
                // TODO: show a friendly message on the screen
                Jotunn.Logger.LogError("Interacting with an unknown portal");
                return;
            }

            var portal = KnownPortalsManager.Instance.GetKnownPortalById(portalId);
            Jotunn.Logger.LogDebug($"[OnPortalRequestText] Interacting with: {portal}");
            XPortalUI.Instance.ConfigurePortal(portal);
        }

        /// <summary>
        /// The player has placed a portal
        /// </summary>
        /// <param name="portalId">The ZDOID of the portal being placed</param>
        /// <param name="location">The location in the world where the portal was placed</param>
        internal static void OnPortalPlaced(ZDOID portalId, Vector3 location)
        {
            Jotunn.Logger.LogDebug($"[OnPortalPlaced] Portal `{portalId}` was placed");

            var portal = new KnownPortal(portalId, location);
            RPC.SendAddOrUpdateRequestToServer(portal);
        }

        /// <summary>
        /// A portal was destroyed by damage or by a hammer
        /// </summary>
        /// <param name="portalId">The ZDOID of the portal being destroyed</param>
        internal static void OnPortalDestroyed(ZDOID portalId)
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
            ZDOMan.instance.GetAllZDOsWithPrefab(StonePortalPrefabName, allPortalZDOs);
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
        internal static void PortalInfoSubmitted(KnownPortal portal, string newName, ZDOID newTarget)
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
        internal static void PingMapButtonClicked(ZDOID targetId)
        {
            if (XPortalConfig.Instance.Server.PingMapDisabled)
            {
                return;
            }

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
