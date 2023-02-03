using BepInEx;
using Jotunn.Managers;
using Jotunn.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace XPortal
{
    [BepInPlugin(PluginGUID, PluginName, PluginVersion)]
    [BepInDependency(Jotunn.Main.ModGuid)]
    [NetworkCompatibility(CompatibilityLevel.EveryoneMustHaveMod, VersionStrictness.Minor)]
    public class XPortal : BaseUnityPlugin
    {
        #region Plugin info
        // This is the place to edit plugin details. Everywhere else will be generated based on this info.
        public const string PluginGUID = "yay.spikehimself.xportal";
        public const string PluginName = "XPortal";
        public const string PluginVersion = "1.0.1";
        public const string PluginDescription = "Select portal destination from a list of existing portals. No more tag pairing, and no more portal hubs! XPortal is a complete rewrite of the popular mod AnyPortal.";
        public const string PluginWebsiteUrl = "https://github.com/SpikeHimself/XPortal";
        public const string PluginJotunnVersion = Jotunn.Main.Version;
        #endregion

        #region RPC Names
        public const string RPC_TARGETCHANGEREQUEST = "XPortal_TargetChangeRequest";
        public const string RPC_NAMECHANGEREQUEST = "XPortal_NameChangeRequest";
        public const string RPC_REMOVEREQUEST = "XPortal_RemoveRequest";
        private const string RPC_CHATMESSAGE = "ChatMessage";
        #endregion

        /// <summary>
        /// Set to true via Game.Start() -> HarmonyPatches.GameStartPatch() -> HarmonyPatches.OnGameStart() -> OnGameStart()
        /// </summary>
        private bool gameStarted = false;

        /// <summary>
        /// Pressing Enter in the "Portal Name" textfield should close the UI, but if you do that immediately, the keypress
        /// is forwarded by the game and the Chat window will be opened. So we must delay this by one frame. Update() sets this 
        /// to true after an Enter keypress is detected. Then on the next Update(), the keypress will be handled and this is set 
        /// to false again.
        /// </summary>
        private bool submitUIOnNextFrame = false;

        /// <summary>
        /// This is the core of XPortal. This is the list of known portals, indexed by ZDOID.
        /// Each value is a KnownPortal, which holds the ZDOID, Name, and the portal's target ZDOID.
        /// </summary>
        private Dictionary<ZDOID, KnownPortal> knownPortals = new Dictionary<ZDOID, KnownPortal>();


        #region Unity Events
        /// <summary>
        /// https://docs.unity3d.com/ScriptReference/MonoBehaviour.Awake.html
        /// </summary>
        private void Awake()
        {
            // Hello, world!
            Jotunn.Logger.LogDebug("I HAVE ARRIVED!");

            // Subscribe to HarmonyPatches events
            HarmonyPatches.OnGameStart += OnGameStart;
            //HarmonyPatches.OnPostCreateSyncList += OnPostCreateSyncList;
            HarmonyPatches.OnPrePortalHover += OnPrePortalHover;
            HarmonyPatches.OnPostPortalInteract += OnPostPortalInteract;

            // Subscribe to Jotunn's OnVanillaMapAvailable event. This is the earliest point where we can update the known portals.
            // (but on dedicated servers this isn't triggered)
            MinimapManager.OnVanillaMapAvailable += OnVanillaMapAvailable;

            // Apply the Harmony patches
            HarmonyPatches.Patch();
        }

        /// <summary>
        /// https://docs.unity3d.com/ScriptReference/MonoBehaviour.Update.html
        /// </summary>
        private void Update()
        {
            if (GUIManager.IsHeadless() || !gameStarted || !XPortalUI.Instance.IsActive())
            {
                return;
            }

            if (submitUIOnNextFrame)
            {
                // This weird work-around stops the chatbox from opening when you press enter in the portal UI
                submitUIOnNextFrame = false;
                XPortalUI.Instance.Submit();
            }

            if (Input.GetKeyDown(KeyCode.Escape))
            {
                XPortalUI.Instance.Hide();
                return;
            }

            if (Input.GetKeyDown(KeyCode.KeypadEnter) ||
                Input.GetKeyDown(KeyCode.Return))
            {
                submitUIOnNextFrame = true;
                return;
            }
        }

        /// <summary>
        /// https://docs.unity3d.com/ScriptReference/MonoBehaviour.OnDestroy.html
        /// </summary>
        private void OnDestroy()
        {
            HarmonyPatches.Unpatch();
            if (!GUIManager.IsHeadless())
            {
                XPortalUI.Instance?.Dispose();
            }
            knownPortals?.Clear();
        }
        #endregion

        #region Jotunn Events
        /// <summary>
        /// https://valheim-modding.github.io/Jotunn/tutorials/events.html
        /// </summary>
        private void OnVanillaMapAvailable()
        {
            Jotunn.Logger.LogDebug("OnVanillaMapAvailable");
            UpdateKnownPortals(forceRefresh: true);
        }
        #endregion

        #region Patch Events
        /// <summary>
        /// OnGameStart() will be called by Patches.GameStartPatch which patches Game.Start.
        /// At this point the world is beginning to load. The portals don't exist yet.
        /// </summary>
        internal void OnGameStart()
        {
            if (!GUIManager.IsHeadless())
            {
                XPortalUI.Instance.PortalInfoSubmitted += OnPortalInfoSubmitted;
                XPortalUI.Instance.PingMapButtonClicked += OnPingMapButtonClicked;
            }

            ZRoutedRpc.instance.Register<ZDOID, ZDOID>(RPC_TARGETCHANGEREQUEST, new Action<long, ZDOID, ZDOID>(OnRpcTargetChangeRequest));
            ZRoutedRpc.instance.Register<ZDOID, string>(RPC_NAMECHANGEREQUEST, new Action<long, ZDOID, string>(OnRpcNameChangeRequest));
            ZRoutedRpc.instance.Register<ZDOID>(RPC_REMOVEREQUEST, new Action<long, ZDOID>(OnRpcRemoveRequest));

            gameStarted = true;
        }

        //internal void OnPostCreateSyncList(ref List<ZDO> toSync)
        //{
        //    ZDOMan.instance.GetAllZDOsWithPrefab(Game.instance.m_portalPrefab.name, toSync);
        //}

        internal void OnPrePortalHover(out string result, ref ZDO portalZDO)
        {
            UpdateKnownPortals();

            // Get information about 'this' portal
            var thisPortalZDOID = portalZDO.m_uid;
            var thisKnownPortal = knownPortals[thisPortalZDOID];
            string thisPortalName = thisKnownPortal.Name;
            if (string.IsNullOrEmpty(thisPortalName))
            {
                thisPortalName = "$piece_portal_tag_none"; // "(No Name)"
            }

            // Get information about the target
            string thisPortalDestination = "$piece_portal_target_none"; // "(None)"
            if (thisKnownPortal.HasTarget())
            {
                var targetPortalZDOID = thisKnownPortal.Target;
                var targetPortalZDO = Util.TryGetZDO(targetPortalZDOID);
                if (targetPortalZDO == null || !targetPortalZDO.IsValid())
                {
                    // Target was set but it appears to be invalid. Maybe it got destroyed by a nasty troll on the other side..
                    // Let everyone know that the portal was removed
                    Jotunn.Logger.LogDebug($"[OnPrePortalHover] Send RPC {RPC_REMOVEREQUEST}");
                    SendRpcRemoveRequest(targetPortalZDOID);
                }
                else
                {
                    thisPortalDestination = knownPortals[targetPortalZDOID].Name;
                    if (string.IsNullOrEmpty(thisPortalDestination))
                    {
                        thisPortalDestination = "$piece_portal_tag_none"; // "(No Name)"
                    }
                }
            }

            result = Localization.instance.Localize(
                         $"$piece_portal $piece_portal_tag: {thisPortalName}\n"
                       + $"$piece_portal_target: {thisPortalDestination}\n"
                       + $"[<color=yellow><b>$KEY_Use</b></color>] $piece_portal_settag"
                     );
        }

        internal void OnPostPortalInteract(ZDO portalZDO)
        {
            var thisPortalZDOID = portalZDO.m_uid;
            var thisKnownPortal = knownPortals[thisPortalZDOID];

            Jotunn.Logger.LogDebug($"[OnPostPortalInteract] Interacting with: {thisKnownPortal}");
            XPortalUI.Instance.ConfigurePortal(thisKnownPortal, ref knownPortals);
        }
        #endregion

        #region RPC Shorthands
        public void SendRpcTargetChangeRequest(ZDOID portalZDOID, ZDOID targetZDOID)
        {
            ZRoutedRpc.instance.InvokeRoutedRPC(ZRoutedRpc.Everybody, RPC_TARGETCHANGEREQUEST, portalZDOID, targetZDOID);
        }

        public void SendRpcNameChangeRequest(ZDOID portalZDOID, string newName)
        {
            ZRoutedRpc.instance.InvokeRoutedRPC(ZRoutedRpc.Everybody, RPC_NAMECHANGEREQUEST, portalZDOID, newName);
        }

        public void SendRpcRemoveRequest(ZDOID portalZDOID)
        {
            ZRoutedRpc.instance.InvokeRoutedRPC(ZRoutedRpc.Everybody, RPC_REMOVEREQUEST, portalZDOID);
        }

        public void SendRpcPingMap(Vector3 location, string text)
        {
            // I have no idea what the numbers 3 and 1 mean in this context, but it works..
            ZRoutedRpc.instance.InvokeRoutedRPC(ZRoutedRpc.Everybody, RPC_CHATMESSAGE, location, 3, text, string.Empty, 1);
        }
        #endregion

        #region RPC Events
        /// <summary>
        /// This RPC message is used when a known portal needs to change target.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="thisPortalZDOID">The ZDOID of portal of which the target needs to change</param>
        /// <param name="targetPortalZDOID">The ZDOID of the new target. Note that this can also be `ZDOID.None`</param>
        private void OnRpcTargetChangeRequest(long sender, ZDOID thisPortalZDOID, ZDOID targetPortalZDOID)
        {
            if (GUIManager.IsHeadless() && knownPortals.Count == 0)
            {
                // On dedicated servers the list might still be empty 
                // TODO: find an event that triggers after portals are available but before players join 
                UpdateKnownPortals();
            }

            // According to an incoming RPC message, a known portal should change target
            var knownPortal = knownPortals[thisPortalZDOID];

            if (targetPortalZDOID == ZDOID.None || targetPortalZDOID.IsNone())
            {
                Jotunn.Logger.LogDebug($"[OnRpcTargetChangeRequest] Portal `{knownPortal.Name}` is disconnecting");
            }
            else
            {
                var targetName = knownPortals[targetPortalZDOID].Name;
                Jotunn.Logger.LogDebug($"[OnRpcTargetChangeRequest] Portal `{knownPortal.Name}` is connecting to `{targetName}`");
            }

            knownPortals[thisPortalZDOID].Target = targetPortalZDOID;

            var thisPortalZDO = Util.TryGetZDO(thisPortalZDOID);
            if (thisPortalZDO == null || !thisPortalZDO.IsValid())
            {
                Jotunn.Logger.LogError($"[OnRpcTargetChangeRequest] Invalid portal ZDO for `{thisPortalZDOID}`");
                UpdateKnownPortals(forceRefresh: true);
                return;
            }

            thisPortalZDO.SetOwner(ZDOMan.instance.GetMyID());
            thisPortalZDO.Set("target", targetPortalZDOID);
            ZDOMan.instance.ForceSendZDO(thisPortalZDOID);

            Jotunn.Logger.LogInfo($"[OnRpcTargetChangeRequest] Updated portal: {knownPortals[thisPortalZDOID]}");
        }

        private void OnRpcNameChangeRequest(long sender, ZDOID thisPortalZDOID, string newName)
        {
            if (GUIManager.IsHeadless() && knownPortals.Count == 0)
            {
                // On dedicated servers the list might still be empty 
                // TODO: find an event that triggers after portals are available but before players join 
                UpdateKnownPortals();
            }

            if (!knownPortals.ContainsKey(thisPortalZDOID))
            {
                UpdateKnownPortals(forceRefresh: true);
                return;
            }

            // According to an incoming RPC message, a known portal should change name
            string oldName = knownPortals[thisPortalZDOID].Name;
            Jotunn.Logger.LogDebug($"[OnRpcNameChangeRequest] Portal `{oldName}` is renaming to `{newName}`");

            knownPortals[thisPortalZDOID].Name = newName;

            var thisPortalZDO = Util.TryGetZDO(thisPortalZDOID);
            if (thisPortalZDO == null || !thisPortalZDO.IsValid())
            {
                Jotunn.Logger.LogError($"[OnRpcNameChangeRequest] Invalid portal ZDO for `{thisPortalZDOID}`");
                UpdateKnownPortals(forceRefresh: true);
                return;
            }

            thisPortalZDO.SetOwner(ZDOMan.instance.GetMyID());
            thisPortalZDO.Set("tag", newName);
            ZDOMan.instance.ForceSendZDO(thisPortalZDOID);

            Jotunn.Logger.LogInfo($"[OnRpcNameChangeRequest] Renamed portal `{oldName}`: {knownPortals[thisPortalZDOID]} ");
        }

        private void OnRpcRemoveRequest(long sender, ZDOID removedZDOID)
        {
            if (GUIManager.IsHeadless() && knownPortals.Count == 0)
            {
                // On dedicated servers the list might still be empty 
                // TODO: find an event that triggers after portals are available but before players join 
                UpdateKnownPortals();
            }

            // According to an incoming RPC message, a known portal needs to be removed
            Jotunn.Logger.LogDebug($"[OnRpcRemoveRequest] Portal `{removedZDOID}` is being removed (it no longer exists)");

            knownPortals.Remove(removedZDOID);
            Jotunn.Logger.LogInfo($"[OnRpcRemoveRequest] Removed Portal: {removedZDOID}");

            // This may cause some other portals to lose connection, so let's send around some updates about those
            var disconnectedPortals = knownPortals.Where(portal => portal.Value.Targets(removedZDOID)).ToList();
            foreach (var disconnectedPortalKVP in disconnectedPortals)
            {
                var disconnectedPortalZDOID = disconnectedPortalKVP.Key;
                var disconnectedKnownPortal = disconnectedPortalKVP.Value;
                Jotunn.Logger.LogDebug($"[OnRpcRemoveRequest] Send RPC {RPC_TARGETCHANGEREQUEST} for disconnected portal `{disconnectedKnownPortal.Name}`");
                SendRpcTargetChangeRequest(disconnectedPortalZDOID, ZDOID.None);
            }
        }
        #endregion

        #region UI Events
        private void OnPortalInfoSubmitted(ZDOID thisPortalZDOID, string newName, ZDOID targetZDOID)
        {
            KnownPortal knownPortal = knownPortals[thisPortalZDOID];

            // If this is not a valid ZDO, have it removed, and don't bother updating
            if (!knownPortal.IsZDOValid())
            {
                SendRpcRemoveRequest(targetZDOID);
                return;
            }

            if (!knownPortal.Name.Equals(newName))
            {
                // Send a message around so everyone can keep their stuff updated
                Jotunn.Logger.LogDebug($"[OnPortalInfoSubmitted] Send RPC {RPC_NAMECHANGEREQUEST}");
                SendRpcNameChangeRequest(thisPortalZDOID, newName);
            }

            if (!knownPortal.Targets(targetZDOID))
            {
                // Spam all clients, err, I mean, tell everyone to update the target!
                Jotunn.Logger.LogDebug($"[OnPortalInfoSubmitted] Send RPC {RPC_TARGETCHANGEREQUEST}");
                SendRpcTargetChangeRequest(thisPortalZDOID, targetZDOID);
            }
        }

        private void OnPingMapButtonClicked(ZDOID targetZDOID)
        {
            Jotunn.Logger.LogDebug($"[OnPingMapButtonClicked] {knownPortals[targetZDOID]}");
            var knownPortal = knownPortals[targetZDOID];

            // If this is not a valid ZDO, have it removed, and don't bother pinging
            if (!knownPortal.IsZDOValid())
            {
                SendRpcRemoveRequest(targetZDOID);
                return;
            }

            // Get selected portal name and position
            string targetName = knownPortal.Name;
            Vector3 targetPos = Util.GetPosition(knownPortal.ZDOID);

            // Send ping to all players
            SendRpcPingMap(targetPos, targetName);

            // Show location on the map
            Minimap.instance.ShowPointOnMap(targetPos);
        }
        #endregion

        #region Update Known Portals
        /// <summary>
        /// This method is responsible for updating our local list of known portals.
        /// We are keeping this list as a cache of sorts, so that we don't have to query game data every time we want to know something about our portals.
        /// </summary>
        /// <param name="forceRefresh">If true, clear the list first</param>
        internal void UpdateKnownPortals(bool forceRefresh = false)
        {
            if (forceRefresh)
            {
                knownPortals.Clear();
            }

            var allPortals = new List<ZDO>();
            ZDOMan.instance.GetAllZDOsWithPrefab(Game.instance.m_portalPrefab.name, allPortals);

            if (allPortals.Count == 0)
            {
                Jotunn.Logger.LogDebug("No portals found :(");
                return;
            }

            // Sort the list by tag
            allPortals = allPortals.OrderBy(zdo => zdo.GetString("tag")).ToList();

            // Update existing portals and update changed ones
            foreach (var portalZDO in allPortals)
            {
                string portalName = portalZDO.GetString("tag");
                var portalZDOID = portalZDO.m_uid;
                var targetZDOID = portalZDO.GetZDOID("target");

                UpdateKnownPortal(portalZDOID, portalName, targetZDOID);
            }

            // Remove portals that no longer exist
            foreach (var portalZDOID in knownPortals.Keys.ToList())
            {
                var portalZDO = Util.TryGetZDO(portalZDOID);
                if (portalZDO == null || !portalZDO.IsValid())
                {
                    Jotunn.Logger.LogDebug($"[UpdateKnownPortals] Forced removal of Portal `{portalZDOID}`.");
                    knownPortals.Remove(portalZDOID);
                }
                else if (!allPortals.Contains(portalZDO))
                {
                    Jotunn.Logger.LogDebug($"[UpdateKnownPortals] Uknown Portal `{knownPortals[portalZDOID]}`, removing..");
                    SendRpcRemoveRequest(portalZDOID);
                }
            }
        }

        private void UpdateKnownPortal(ZDOID portalZDOID, string updatedName, ZDOID updatedTargetZDOID)
        {

            var updatedKnownPortal = new KnownPortal(portalZDOID, updatedName, updatedTargetZDOID);

            if (!knownPortals.ContainsKey(updatedKnownPortal.ZDOID))
            {
                Jotunn.Logger.LogDebug($"[UpdateKnownPortal] Adding portal: `{updatedKnownPortal}`");
                knownPortals.Add(updatedKnownPortal.ZDOID, updatedKnownPortal);
                return;
            }

            var currentKnownPortal = knownPortals[portalZDOID];
            if (!currentKnownPortal.Targets(updatedKnownPortal.Target))
            {
                if (!updatedKnownPortal.HasTarget())
                {
                    Jotunn.Logger.LogDebug($"[UpdateKnownPortal] Portal `{updatedKnownPortal.Name}` has lost its target");
                }
                else
                {
                    Jotunn.Logger.LogDebug($"[UpdateKnownPortal] Portal `{updatedKnownPortal.Name}` now targets `{knownPortals[updatedKnownPortal.Target].Name}`");
                }
                knownPortals[updatedKnownPortal.ZDOID] = updatedKnownPortal;
            }
        }
        #endregion

    }

}

