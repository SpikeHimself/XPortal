using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using static UnityEngine.GraphicsBuffer;

namespace XPortal.Patches
{
    [HarmonyPatch(typeof(ZDOMan), nameof(ZDOMan.ConnectPortals))]
    static class ZDOMan_ConnectPortals
    {

        static ZDOID FindNewId(List<ZDOID> allPortals, ZDOID oldId)
        {
            // Go over all portal ZDOIDs
            foreach (var newId in allPortals)
            {
                ZDO newZdo = ZDOMan.instance.GetZDO(newId);

                // Skip if the ZDO does not exist
                if (newZdo == null) continue;

                // Get the configured previous ZDOID of this portal
                var previousId = newZdo.GetZDOID(XPortal.Key_PreviousId);

                // If this portal's PreviousId matches the oldId we're looking for, return its new ZDOID
                if (oldId == previousId)
                {
                    var portalName = newZdo.GetString("tag");
                    Log.Debug($"Old ZDOID `{oldId}` is now `{newId}` (`{portalName}`)");
                    return newId;
                }
                
            }

            return oldId;
        }

        static bool Prefix()
        {
            Log.Debug("Restoring Portal connections..");

            // Find all Portals and Targets
            List<ZDOID> connectionIds1 = ZDOExtraData.GetAllConnectionZDOIDs(ZDOExtraData.ConnectionType.Portal );
            List<ZDOID> connectionIds2 = ZDOExtraData.GetAllConnectionZDOIDs(ZDOExtraData.ConnectionType.Portal | ZDOExtraData.ConnectionType.Target);
            
            // Combine the Portals and Targets into one list
            List<ZDOID> allPortalIds = new List<ZDOID>();
            allPortalIds.AddRange(connectionIds1);
            allPortalIds.AddRange(connectionIds2);

            Log.Debug($"Found {allPortalIds.Count} portal(s).");


            // If there are no portals, there is nothing to do
            if (allPortalIds.Count == 0) return false;


            // Go over each portal in the list and try to reconnect it
            foreach (ZDOID portalId in allPortalIds)
            {
                // Skip if this is not a valid ZDOID
                if (portalId == ZDOID.None) continue;

                // Find the ZDO of this portal
                ZDO portalZdo = ZDOMan.instance.GetZDO(portalId);

                // Skip if the ZDO does not exist
                if (portalZdo == null) continue;

                var portalName = portalZdo.GetString("tag");
                Log.Debug($"Checking connection for `{portalId}` (`{portalName}`)");

                // Get the configured target ZDOID of this portal
                var targetId = portalZdo.GetZDOID(XPortal.Key_TargetId);

                // Skip if the target is not a valid ZDOID
                if (targetId == ZDOID.None) continue;

                // Find the ZDO of this target
                var targetZdo = ZDOMan.instance.GetZDO(targetId);

                // If the target ZDO does not exist, or it does not have a PreviousId set, then this is not an existing portal                    
                if (targetZdo == null || string.IsNullOrEmpty(targetZdo.GetString(XPortal.Key_PreviousId)))
                {
                    Log.Debug($"Target `{targetId}` does not exist, finding new ZDOID..");
                    targetId = FindNewId(allPortalIds, targetId);
                    targetZdo = ZDOMan.instance.GetZDO(targetId);
                }

                var targetPortalName = targetZdo.GetString("tag");
                Log.Info($"Connecting: `{portalId}` (`{portalName}`)  ==>  `{targetId}` (`{targetPortalName}`)");

                portalZdo.SetOwner(ZDOMan.GetSessionID());
                portalZdo.SetConnection(ZDOExtraData.ConnectionType.Portal, targetId);
                portalZdo.Set(XPortal.Key_TargetId, targetId);
            }

            // Finish by setting the previousid so that the next session can use that to find the new zdoids
            Log.Debug($"Updating PreviousId for all portals..");
            foreach (ZDOID portalId in allPortalIds)
            {
                ZDO portalZdo = ZDOMan.instance.GetZDO(portalId);
                portalZdo?.Set(XPortal.Key_PreviousId, portalId);
            }

            return false;
        }

    }
}
