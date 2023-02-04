using BepInEx;
using Jotunn.Managers;
using Jotunn.Utils;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace XPortal
{
    [BepInPlugin(PluginGUID, PluginName, PluginVersion)]
    [BepInDependency(Jotunn.Main.ModGuid)]
    [NetworkCompatibility(CompatibilityLevel.EveryoneMustHaveMod, VersionStrictness.Minor)]
    public class XPortal : BaseUnityPlugin
    {
        #region Plugin info
        // This is *the* place to edit plugin details. Everywhere else will be generated based on this info.
        public const string PluginGUID = "yay.spikehimself.xportal";
        public const string PluginName = "XPortal";
        public const string PluginVersion = "1.2.0";
        public const string PluginDescription = "Select portal destination from a list of existing portals. No more tag pairing, and no more portal hubs! XPortal is a complete rewrite of the popular mod AnyPortal.";
        public const string PluginWebsiteUrl = "https://github.com/SpikeHimself/XPortal";
        //public const string PluginBepinVersion = ??
        public const string PluginJotunnVersion = Jotunn.Main.Version;
        #endregion

        #region RPC Names
        // Client to server
        public const string RPC_TARGETCHANGEREQUEST = "XPortal_TargetChangeRequest";
        public const string RPC_NAMECHANGEREQUEST = "XPortal_NameChangeRequest";
        public const string RPC_SYNCREQUEST = "XPortal_SyncRequest";
        public const string RPC_CHECKZDOREQUEST = "XPortal_CheckZDORequest";
        //public const string RPC_REMOVEREQUEST = "XPortal_RemoveRequest";

        // Server to client
        public const string RPC_SYNCPORTAL = "XPortal_SyncPortal";
        public const string RPC_RESYNC = "XPortal_Resync";

        // Client to client
        public const string RPC_CHATMESSAGE = "ChatMessage";
        #endregion

        /// <summary>
        /// Set to true via Game.Start() -> HarmonyPatches.GameStartPatch() -> HarmonyPatches.OnGameStart() -> OnGameStart()
        /// </summary>
        private bool gameStarted = false;

        private bool zdoSyncRequested = false;


        #region Determine Environment
        public bool IsServer()
        {
            return ZNet.instance != null && ZNet.instance.IsServer();
        }

        public bool IsHeadless()
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

            // Subscribe to HarmonyPatches events
            HarmonyPatches.OnGameStart += OnGameStart;
            HarmonyPatches.OnPostCreateSyncList += OnPostCreateSyncList;
            HarmonyPatches.OnPrePortalHover += OnPrePortalHover;
            HarmonyPatches.OnPostPortalInteract += OnPostPortalInteract;

            if (!IsHeadless())
            {
                // Add buttons
                XPortalUI.Instance.AddInputs();
            }

            // Subscribe to Jotunn's OnVanillaMapDataLoaded event. This is the earliest point where we can update the known portals.
            // (but on dedicated servers this isn't triggered)
            //MinimapManager.OnVanillaMapAvailable += OnVanillaMapAvailable;
            MinimapManager.OnVanillaMapDataLoaded += OnVanillaMapDataLoaded;

            // Apply the Harmony patches
            HarmonyPatches.Patch();

            //NetworkManager.Instance.AddRPC()
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
            HarmonyPatches.Unpatch();
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
        private void OnVanillaMapDataLoaded()
        {
            // Ask the server to send us the portals
            RPC.SendSyncRequest();
        }
        #endregion

        #region Process ZDOs
        private List<ZDO> ProcessSyncRequest()
        {
            var allPortalZDOs = GetAllPortalZDOs();
            Jotunn.Logger.LogDebug($"[ProcessSyncRequest] Fetched {allPortalZDOs.Count} portals");

            ForceLocalPortalUpdate(allPortalZDOs);

            RPC.SendResync(KnownPortalsManager.Instance.Pack());

            return allPortalZDOs;
        }

        private List<ZDO> GetAllPortalZDOs()
        {
            var allPortalZDOs = new List<ZDO>();
            ZDOMan.instance.GetAllZDOsWithPrefab(Game.instance.m_portalPrefab.name, allPortalZDOs);
            return allPortalZDOs;
        }
        #endregion

        #region Patch Events
        /// <summary>
        /// OnGameStart() will be called by Patches.GameStartPatch which patches Game.Start.
        /// At this point the world is beginning to load. The portals don't exist yet.
        /// </summary>
        internal void OnGameStart()
        {
            if (!IsHeadless())
            {
                // Remove them in case we joined another world
                XPortalUI.Instance.PortalInfoSubmitted -= OnPortalInfoSubmitted;
                XPortalUI.Instance.PingMapButtonClicked -= OnPingMapButtonClicked;

                XPortalUI.Instance.PortalInfoSubmitted += OnPortalInfoSubmitted;
                XPortalUI.Instance.PingMapButtonClicked += OnPingMapButtonClicked;
            }

            ZRoutedRpc.instance.Register<ZDOID, ZDOID>(RPC_TARGETCHANGEREQUEST, new Action<long, ZDOID, ZDOID>(OnRpcTargetChangeRequest));
            ZRoutedRpc.instance.Register<ZDOID, string>(RPC_NAMECHANGEREQUEST, new Action<long, ZDOID, string>(OnRpcNameChangeRequest));
            //ZRoutedRpc.instance.Register<ZDOID>(RPC_REMOVEREQUEST, new Action<long, ZDOID>(OnRpcRemoveRequest));
            ZRoutedRpc.instance.Register(RPC_SYNCREQUEST, new Action<long>(OnRpcSyncRequest));
            ZRoutedRpc.instance.Register<ZDOID>(RPC_CHECKZDOREQUEST, new Action<long, ZDOID>(OnRpcCheckZDORequest));
            ZRoutedRpc.instance.Register<ZPackage>(RPC_SYNCPORTAL, new Action<long, ZPackage>(OnRpcSyncPortal));
            ZRoutedRpc.instance.Register<ZPackage>(RPC_RESYNC, new Action<long, ZPackage>(OnRpcResync));

            gameStarted = true;
        }

        internal void OnPostCreateSyncList(ref List<ZDO> toSync)
        {
            if (zdoSyncRequested)
            {
                zdoSyncRequested = false;

                var allPortalZDOs = ProcessSyncRequest();

                // Add all portals to the list
                toSync.AddRange(allPortalZDOs);
            }
        }

        private ZDOID lastHoverId = ZDOID.None;
        internal void OnPrePortalHover(out string result, ref ZDO portalZDO)
        {
            if (lastHoverId != portalZDO.m_uid)
            {
                RPC.SendSyncRequest();
                lastHoverId = portalZDO.m_uid;
            }

            // Get information about 'this' portal
            var thisPortalId = portalZDO.m_uid;

            if (!KnownPortalsManager.Instance.ContainsId(thisPortalId))
            {
                Jotunn.Logger.LogDebug($"[OnPrePortalHover] Hovering over unknown portal {thisPortalId}");
                Jotunn.Logger.LogDebug($"[OnPrePortalHover] Sending sync request..");
                RPC.SendSyncRequest();
                result = "Fetching portal info..";
                return;
            }

            var thisKnownPortal = KnownPortalsManager.Instance.GetKnownPortalById(thisPortalId);

            string outputPortalName = thisKnownPortal.Name;
            if (string.IsNullOrEmpty(outputPortalName))
            {
                outputPortalName = "$piece_portal_tag_none"; // "(No Name)"
            }

            // Get information about the target
            string outputPortalDestination = "$piece_portal_target_none"; // "(None)"
            if (thisKnownPortal.HasTarget())
            {
                var targetId = thisKnownPortal.Target;

                if (!KnownPortalsManager.Instance.ContainsId(targetId))
                {
                    Jotunn.Logger.LogDebug($"[OnPrePortalHover] Target portal {targetId} appears to be invalid");
                    RPC.SendCheckZDORequest(targetId);
                    result = "Fetching portal info....";
                    return;
                }

                //var targetPortalZDO = Util.TryGetZDO(targetPortalZDOID);
                //if (targetPortalZDO == null || !targetPortalZDO.IsValid())
                //{
                //    // Target was set but it appears to be invalid. Maybe it got destroyed by a nasty troll on the other side..
                //    // Ask the server for a new portal list
                //    Jotunn.Logger.LogDebug($"[OnPrePortalHover] Invalid ZDO. Send RPC {RPC_SYNCREQUEST}");
                //    RPC.SendSyncRequest();
                //}
                //else
                //{
                outputPortalDestination = KnownPortalsManager.Instance.GetKnownPortalById(targetId).Name;
                if (string.IsNullOrEmpty(outputPortalDestination))
                {
                    outputPortalDestination = "$piece_portal_tag_none"; // "(No Name)"
                }
                //}
            }

            result = Localization.instance.Localize(
                         $"$piece_portal $piece_portal_tag: {outputPortalName}\n"           // "Portal Name: {name}"
                       + $"$piece_portal_target: {outputPortalDestination}\n"               // "Destination: {name}"
                       + $"[<color=yellow><b>$KEY_Use</b></color>] $piece_portal_settag"    // "[E] Configure"
                     );
        }

        internal void OnPostPortalInteract(ZDO portalZDO)
        {
            Jotunn.Logger.LogDebug($"[OnPostPortalInteract] Sending sync request");
            RPC.SendSyncRequest();

            var thisPortalZDOID = portalZDO.m_uid;

            if (!KnownPortalsManager.Instance.ContainsId(thisPortalZDOID))
            {
                return;
            }

            var thisKnownPortal = KnownPortalsManager.Instance.GetKnownPortalById(thisPortalZDOID);

            Jotunn.Logger.LogDebug($"[OnPostPortalInteract] Interacting with: {thisKnownPortal}");
            XPortalUI.Instance.ConfigurePortal(thisKnownPortal);
        }
        #endregion

        #region RPC Events
        /// <summary>
        /// The server sent us all of the portals it knows
        /// </summary>
        /// <param name="sender">The server</param>
        /// <param name="pkg">A ZPackage containing a number followed by a list of packaged portals</param>
        private void OnRpcResync(long sender, ZPackage pkg)
        {
            if (IsServer())
            {
                return;
            }

            KnownPortalsManager.Instance.UpdateFromResyncPackage(pkg);
        }

        private void OnRpcSyncPortal(long sender, ZPackage pkg)
        {
            if (IsServer())
            {
                return;
            }

            var incomingPortal = KnownPortal.Unpack(pkg);

            Jotunn.Logger.LogDebug($"[OnRpcSyncPortal] Received update to portal `{incomingPortal.Name}`");
            KnownPortalsManager.Instance.AddOrUpdate(incomingPortal);
        }

        /// <summary>
        /// A client wishes to receive the portal list
        /// </summary>
        /// <param name="sender"></param>
        private void OnRpcSyncRequest(long sender)
        {
            Jotunn.Logger.LogDebug($"Received sync request from {sender}");

            if (IsHeadless())
            {
                // wait for ZDOMan.CreateSyncList to initiate
                zdoSyncRequested = true;
            }
            else
            {
                // do it immediately
                ProcessSyncRequest();
            }
        }

        private void OnRpcCheckZDORequest(long sender, ZDOID id)
        {
            if (!IsServer())
            {
                return;
            }

            if (id == ZDOID.None)
            {
                return;
            }

            ZDO zdo = Util.TryGetZDO(id);
            if (zdo == null || !zdo.IsValid())
            {
                var portalsWithInvalidTarget = KnownPortalsManager.Instance.GetPortalsWithTarget(id);
                Jotunn.Logger.LogDebug($"[OnRpcCheckZDORequest] {portalsWithInvalidTarget.Count} portals have invalid target `{id}`");

                if (KnownPortalsManager.Instance.ContainsId(id))
                {
                    // TODO Create RPC for removing portal
                    KnownPortalsManager.Instance.Remove(id);
                }

                foreach (var portal in portalsWithInvalidTarget)
                {
                    RPC.SendTargetChangeRequest(portal.Id, ZDOID.None);
                }

            }
        }

        /// <summary>
        /// This RPC message is used when a known portal needs to change target.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="portalId">The ZDOID of portal of which the target needs to change</param>
        /// <param name="targetId">The ZDOID of the new target. Note that this can also be `ZDOID.None`</param>
        private void OnRpcTargetChangeRequest(long sender, ZDOID portalId, ZDOID targetId)
        {
            if (!IsServer())
            {
                Jotunn.Logger.LogDebug($"[OnRpcTargetChangeRequest] {sender} wants `{portalId}` to target `{targetId}`, but I am not the server.");
                return;
            }

            Jotunn.Logger.LogDebug($"[OnRpcTargetChangeRequest] {sender} wants `{portalId}` to target `{targetId}`");

            var updatedPortal = KnownPortalsManager.Instance.SetTarget(portalId, targetId);

            var updatedPortalZDO = Util.TryGetZDO(updatedPortal.Id);

            //updatedPortalZDO.SetOwner(ZDOMan.instance.GetMyID());
            //updatedPortalZDO.SetOwner(sender);
            updatedPortalZDO.Set("target", updatedPortal.Target);
            ZDOMan.instance.ForceSendZDO(updatedPortal.Id);

            RPC.SendSyncPortal(updatedPortal);
            return;
        }

        private void OnRpcNameChangeRequest(long sender, ZDOID portalId, string newName)
        {
            if (!IsServer())
            {
                Jotunn.Logger.LogDebug($"[OnRpcNameChangeRequest] {sender} wants `{portalId}` to be called `{newName}`, but I am not the server.");
                return;
            }

            Jotunn.Logger.LogDebug($"[OnRpcNameChangeRequest] {sender} wants `{portalId}` to be called `{newName}`");

            var updatedPortal = KnownPortalsManager.Instance.SetName(portalId, newName);

            var updatedPortalZDO = Util.TryGetZDO(updatedPortal.Id);

            //updatedPortalZDO.SetOwner(sender);
            updatedPortalZDO.Set("tag", updatedPortal.Name);
            ZDOMan.instance.ForceSendZDO(updatedPortal.Id);

            RPC.SendSyncPortal(updatedPortal);
            return;
        }
        #endregion

        #region UI Events
        private void OnPortalInfoSubmitted(KnownPortal thisPortal, string newName, ZDOID targetZDOID)
        {
            if (!thisPortal.Name.Equals(newName))
            {
                // Ask the server to update the portal's name
                Jotunn.Logger.LogDebug($"[OnPortalInfoSubmitted] Send RPC {RPC_NAMECHANGEREQUEST}");
                RPC.SendNameChangeRequest(thisPortal.Id, newName);
            }

            if (!thisPortal.Targets(targetZDOID))
            {
                // Ask the server to update the portal's target
                Jotunn.Logger.LogDebug($"[OnPortalInfoSubmitted] Send RPC {RPC_TARGETCHANGEREQUEST}");
                RPC.SendTargetChangeRequest(thisPortal.Id, targetZDOID);
            }
        }

        private void OnPingMapButtonClicked(ZDOID targetId)
        {
            var portal = KnownPortalsManager.Instance.GetKnownPortalById(targetId);

            Jotunn.Logger.LogDebug($"[OnPingMapButtonClicked] {portal}");

            // Get selected portal name and position
            string name = portal.Name;
            Vector3 location = portal.Location;

            // Send ping to all players
            RPC.SendPingMap(location, name);

            // Show location on the map
            Minimap.instance.ShowPointOnMap(location);
        }
        #endregion

        #region Update Known Portals
        /// <summary>
        /// This method is responsible for updating our local list of known portals.
        /// </summary>
        internal void ForceLocalPortalUpdate(List<ZDO> allPortals)
        {
            //var allPortals = new List<ZDO>();
            //ZDOMan.instance.GetAllZDOsWithPrefab(Game.instance.m_portalPrefab.name, allPortals);

            KnownPortalsManager.Instance.UpdateFromZDOList(allPortals);

            if (!IsServer())
            {
                Jotunn.Logger.LogDebug("[ForceLocalPortalUpdate] Send Sync Request");
                RPC.SendSyncRequest();
            }
        }
        #endregion
    }
}
