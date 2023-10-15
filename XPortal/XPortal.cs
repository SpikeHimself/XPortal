using BepInEx;
using Jotunn.Managers;
using Jotunn.Utils;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using XPortal.RPC;
using XPortal.UI;
using XPortal.Extension;

namespace XPortal
{
    [BepInPlugin(Mod.Info.GUID, Mod.Info.Name, Mod.Info.Version)]
    [BepInIncompatibility("com.sweetgiorni.anyportal")]
    [BepInDependency(Jotunn.Main.ModGuid)]
    [NetworkCompatibility(CompatibilityLevel.EveryoneMustHaveMod, VersionStrictness.Patch)]
    public class XPortal : BaseUnityPlugin
    {
        public const string Key_TargetId = Mod.Info.Name + "_TargetId";
        public const string Key_PreviousId = Mod.Info.Name + "_PreviousId";
        
        public const string StonePortalPrefabName = "portal";

        private static bool portalRecipeAltered = false;
        private static Dictionary<string, int> portalRecipeOriginal;

        #region Unity Events
        /// <summary>
        /// https://docs.unity3d.com/ScriptReference/MonoBehaviour.Awake.html
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("CodeQuality", "IDE0051:Remove unused private members", Justification = "MonoBehaviour.Awake is called when the script instance is being loaded.")]
        private void Awake()
        {
            // Hello, world!
            Log.Debug("I HAVE ARRIVED!");

            // Load config
            XPortalConfig.Instance.LoadLocalConfig(Config);

            // Subscribe to config events
            XPortalConfig.Instance.OnLocalConfigChanged += OnLocalConfigChanged;
            XPortalConfig.Instance.OnServerConfigChanged += OnServerConfigChanged;

            if (!Environment.IsHeadless)
            {
                // Add buttons
                PortalConfigurationPanel.Instance.AddInputs();
            }

            // Subscribe to Jotunn's OnVanillaMapDataLoaded event. This is the earliest point where we can update the known portals.
            // (but on dedicated servers this isn't triggered)
            MinimapManager.OnVanillaMapDataLoaded += MinimapManager_OnVanillaMapDataLoaded;

            // Apply the Harmony patches
            Patches.Patcher.Patch();
        }


        /// <summary>
        /// https://docs.unity3d.com/ScriptReference/MonoBehaviour.Update.html
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("CodeQuality", "IDE0051:Remove unused private members", Justification = "MonoBehaviour.Update is called every frame, if the MonoBehaviour is enabled.")]
        private void Update()
        {
            // QueuedAction allows us to delay things by a certain amount of frames
            // This is used in XPortal's UI to block input, but also in the portal detection patch, see Patches\Piece.cs
            QueuedAction.Update();

            if (Environment.IsHeadless || !Environment.GameStarted || ZInput.instance == null || !PortalConfigurationPanel.Instance.IsActive())
            {
                return;
            }

            PortalConfigurationPanel.Instance.HandleInput();
        }

        /// <summary>
        /// https://docs.unity3d.com/ScriptReference/MonoBehaviour.OnDestroy.html
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("CodeQuality", "IDE0051:Remove unused private members", Justification = "MonoBehaviour.OnDestroy occurs when a Scene or game ends.")]
        private void OnDestroy()
        {
            Log.Debug("Full portal list:");
            KnownPortalsManager.Instance.ReportAllPortals();

            Patches.Patcher.Unpatch();
            if (!Environment.IsHeadless)
            {
                PortalConfigurationPanel.Instance?.Dispose();
            }
            KnownPortalsManager.Instance?.Dispose();
        }
        #endregion

        #region Jotunn Events
        /// <summary>
        /// Event that gets fired once data for a specific Map for a world has been loaded.
        /// https://valheim-modding.github.io/Jotunn/tutorials/events.html
        /// </summary>
        private static void MinimapManager_OnVanillaMapDataLoaded()
        {
            // Ask the server to send us the config
            SendToServer.ConfigRequest();

            // Ask the server to send us the portals
            var myId = ZDOMan.GetSessionID();
            var myName = Game.instance.GetPlayerProfile().GetName();
            SendToServer.SyncRequest($"{myName} ({myId}) has joined the game");
        }
        #endregion

        #region Portal Recipe
        /// <summary>
        /// Update the portal recipe based on the DoublePortalCosts config setting on the server
        /// </summary>
        private static void UpdatePortalRecipe()
        {
            if (!ObjectDB.instance)
            {
                Log.Error("ObjectDB not instantiated");
                return;
            }

            ItemDrop hammer = ObjectDB.instance.GetItemPrefab("Hammer")?.GetComponent<ItemDrop>();
            if (!hammer)
            {
                Log.Error("Could not find Hammer prefab");
                return;
            }

            var portalPiece = hammer.m_itemData.m_shared.m_buildPieces.m_pieces
                .Where(go => go.name.Equals("portal_wood"))
                .Select(go => go.GetComponent<Piece>())
                .FirstOrDefault();

            BackUpPortalRecipe(portalPiece.m_resources);

            foreach (var req in portalPiece.m_resources)
            {
                var itemName = req.m_resItem.name;
                var originalAmount = portalRecipeOriginal[itemName];

                var newAmount = originalAmount;
                if (XPortalConfig.Instance.Server.DoublePortalCosts)
                {
                    newAmount = 2 * originalAmount;
                    Log.Debug($"Doubling amount for requirement {req.m_resItem.name} for item {portalPiece.name} from {originalAmount} to {newAmount}");
                }
                else if (portalRecipeAltered)
                {
                    Log.Debug($"Resetting amount for requirement {req.m_resItem.name} for item {portalPiece.name} to {newAmount}");
                }
                req.m_amount = newAmount;
            }

            portalRecipeAltered = XPortalConfig.Instance.Server.DoublePortalCosts;
        }

        /// <summary>
        /// Create a backup of the portal recipe so we can restore it later
        /// </summary>
        /// <param name="requirements">The Requirement to make a backup of</param>
        private static void BackUpPortalRecipe(Piece.Requirement[] requirements)
        {
            if (portalRecipeOriginal != null && portalRecipeOriginal.Count > 0)
            {
                return;
            }

            Log.Debug($"Copying original requirements for portal recipe");
            portalRecipeOriginal = new Dictionary<string, int>();
            foreach (var req in requirements)
            {
                portalRecipeOriginal.Add(req.m_resItem.name, req.m_amount);
            }
        }
        #endregion

        #region Config events
        internal static void OnLocalConfigChanged()
        {
            // Honestly, nobody cares
        }

        internal static void OnServerConfigChanged()
        {
            UpdatePortalRecipe();
        }
        #endregion

        #region Patch Events
        /// <summary>
        /// Called by a patch on Game.Start.
        /// At this point the world is beginning to load. The portals don't exist yet.
        /// </summary>
        internal static void GameStarted()
        {
            KnownPortalsManager.Instance.Reset();
            RPCManager.Register();
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
                Log.Debug($"Hovering over new portal `{portalId}`");

                // Sometimes XPortal does not detect placement of new portals.
                // Assuming that that is the case here, let's just create a new dummy portal for now.
                // We'll tell the server about it *after* interacting with it (when updating the name/target in the UI).
                // Everything will be fine. Promise.
                var dummyPortal = new KnownPortal(portalId, location);
                KnownPortalsManager.Instance.AddOrUpdate(dummyPortal);
            }

            // Get information about the portal being hovered over
            var portal = KnownPortalsManager.Instance.GetKnownPortalById(portalId);
            var outputPortalName = portal.GetFriendlyName();
            var outputPortalDestination = portal.GetFriendlyTargetName();
            var colourTag = string.Empty;

            if (portal.HasTarget())
            {
                // Get information about the portal's destination
                var targetId = portal.Target;
                if (!KnownPortalsManager.Instance.ContainsId(targetId))
                {
                    Log.Error($"Target portal {targetId} appears to be invalid");
                    SendToServer.SyncRequest($"Hovering over portal `{outputPortalName}` which has invalid target `{targetId}`");
                    result = "Fetching portal info...";
                    return;
                }

                if (XPortalConfig.Instance.Local.DisplayPortalColour)
                {
                    var targetPortal = KnownPortalsManager.Instance.GetKnownPortalById(portal.Target);
                    colourTag = $"<color={targetPortal.Colour}>>> </color>";
                }
            }

            result = Localization.instance.Localize(
                         $"$piece_portal_tag: {outputPortalName}\n"                         // "Name: {name}"
                       + $"$piece_portal_target: {colourTag}{outputPortalDestination}\n"    // "Destination: {name}"
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
                Log.Error("Interacting with an unknown portal");
                return;
            }

            var portal = KnownPortalsManager.Instance.GetKnownPortalById(portalId);
            Log.Debug($"Interacting with: {portal}");
            PortalConfigurationPanel.Instance.ConfigurePortal(portal);
        }

        /// <summary>
        /// The player has placed a portal
        /// </summary>
        /// <param name="portalId">The ZDOID of the portal being placed</param>
        /// <param name="location">The location in the world where the portal was placed</param>
        internal static void OnPortalPlaced(ZDOID portalId, Vector3 location)
        {
            Log.Debug($"Portal `{portalId}` was placed");

            ZDOMan.instance.ForceSendZDO(portalId);

            var portal = new KnownPortal(portalId, location);
            SendToServer.AddOrUpdateRequest(portal);
        }

        /// <summary>
        /// A portal was destroyed by damage or by a hammer
        /// </summary>
        /// <param name="portalId">The ZDOID of the portal being destroyed</param>
        internal static void OnPortalDestroyed(ZDOID portalId)
        {
            if (!KnownPortalsManager.Instance.ContainsId(portalId))
            {
                Log.Error($"Portal `{portalId}` is being destroyed, but XPortal does not know it");
                return;
            }
            var portalName = KnownPortalsManager.Instance.GetKnownPortalById(portalId).Name;
            Log.Debug($"Portal `{portalName}` is being destroyed");
            SendToServer.RemoveRequest(portalId);
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
            Log.Debug($"Fetched {allPortalZDOs.Count} portals");

            ForceLocalPortalUpdate(allPortalZDOs);

            SendToClient.Resync(KnownPortalsManager.Instance.Pack(), reason);

            return allPortalZDOs;
        }

        /// <summary>
        /// Queries ZDOMan for all ZDOs that were instantiated using the Portal prefab (Game.m_portalPrefab)
        /// </summary>
        /// <returns>A list of portal ZDOs</returns>
        private static List<ZDO> GetAllPortalZDOs()
        {
            return ZDOMan.instance.GetPortals();
        }

        /// <summary>
        /// Update our KnownPortals from a list of ZDOs.
        /// If we are not the server then also send the server a Sync Request.
        /// </summary>
        private static void ForceLocalPortalUpdate(List<ZDO> allPortals)
        {
            KnownPortalsManager.Instance.UpdateFromZDOList(allPortals);

            if (!Environment.IsServer)
            {
                var syncRequestReason = "Local portal list was updated";
                Log.Debug($"Send Sync Request, because: {syncRequestReason}");
                SendToServer.SyncRequest(syncRequestReason);
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
        internal static void PortalInfoSubmitted(KnownPortal portal, string newName, ZDOID newTarget, bool defaultPortal)
        {
            if (defaultPortal)
            {
                // The "Default Portal" checkbox is checked: make this the default portal
                XPortalConfig.Instance.Local.DefaultPortal.Value = portal.Location.Round();
            }
            else
            {
                // The "Default Portal" checkbox is not checked
                if (portal.IsDefaultPortal)
                {
                    // This portal was the default portal: unset it
                    XPortalConfig.Instance.Local.DefaultPortal.Value = Vector3.zero;
                }
            }

            if (!portal.Name.Equals(newName) || !portal.Targets(newTarget))
            {
                portal.Name = newName;
                portal.Target = newTarget;

                // Ask the server to update the portal
                Log.Debug($"Updating portal `{portal.Name}`");
                SendToServer.AddOrUpdateRequest(portal);
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

            Log.Debug($"Pinging portal: {portal}");

            // Get selected portal name and position
            string name = portal.GetFriendlyName();
            Vector3 location = portal.Location;

            // Send ping to all players
            SendToClient.PingMap(location, name);

            // Show location on the map
            Minimap.instance.ShowPointOnMap(location);
        }
        #endregion
    }
}
