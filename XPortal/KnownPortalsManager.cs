using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.PlayerLoop;
using static MeleeWeaponTrail;

namespace XPortal
{
    internal class KnownPortalsManager : IDisposable
    {
        ////////////////////////////
        //// Singleton instance ////
        private static readonly Lazy<KnownPortalsManager> lazy = new Lazy<KnownPortalsManager>(() => new KnownPortalsManager());
        public static KnownPortalsManager Instance { get { return lazy.Value; } }
        ////////////////////////////

        /// <summary>
        /// This is the core of XPortal. This is the list of known portals, indexed by ZDOID.
        /// Each value is a KnownPortal, which holds the ZDOID, Name, Location and the portal's target ZDOID.
        /// </summary>
        private readonly Dictionary<ZDOID, KnownPortal> knownPortals = new Dictionary<ZDOID, KnownPortal>();
        public int Count
        {
            get
            {
                return knownPortals.Count;
            }
        }

        private KnownPortalsManager() { }

        public bool ContainsId(ZDOID id)
        {
            return knownPortals.ContainsKey(id);
        }

        public KnownPortal GetKnownPortal(ZDOID id)
        {
            return knownPortals[id];
        }

        public List<KnownPortal> GetList()
        {
            return knownPortals.Values.ToList();
        }

        public ZPackage Pack()
        {
            var allPortals = GetList();

            var pkg = new ZPackage();
            pkg.Write(allPortals.Count);

            foreach (var knownPortal in allPortals)
            {
                pkg.Write(knownPortal.Pack());
            }

            return pkg;
        }

        public List<KnownPortal> GetSortedList()
        {
            var list = GetList();
            list.Sort((valueA, valueB) => valueA.Name.CompareTo(valueB.Name));
            return list;
        }
        
        public List<KnownPortal> GetPortalsWithTarget(ZDOID target)
        {
            return knownPortals.Values.Where(p => p.Target == target).ToList();
        }

        public void Remove(KnownPortal portal)
        {
            if (ContainsId(portal.Id))
            {
                knownPortals.Remove(portal.Id);
            }
        }

        public KnownPortal Update(KnownPortal portal)
        {
            if(!ContainsId(portal.Id))
            {
                //Jotunn.Logger.LogDebug($"[KnownPortalsManager.Update] Adding {portal}");
                knownPortals.Add(portal.Id, portal);
            }
            else
            {
                //Jotunn.Logger.LogDebug($"[KnownPortalsManager.Update] Updating {portal}");
                knownPortals[portal.Id] = portal;
            }
            return knownPortals[portal.Id];
        }

        public KnownPortal Update(ZDOID id, string name, Vector3 location, ZDOID target)
        {
            return Update(new KnownPortal(id, name, location, target));
        }
        
        public KnownPortal UpdateName(ZDOID id, string newName)
        {
            knownPortals[id].Name= newName;
            return knownPortals[id];
        }

        public KnownPortal UpdateTarget(ZDOID id, ZDOID target)
        {
            knownPortals[id].Target = target;
            return knownPortals[id];
        }

        public void UpdateFromZDOList(List<ZDO> zdoList)
        {
            knownPortals.Clear();

            if (zdoList.Count == 0)
            {
                Jotunn.Logger.LogDebug("[UpdateFromZDOList] No portals to update");
                return;
            }

            // Sort the list by tag
            zdoList = zdoList.OrderBy(zdo => zdo.GetString("tag")).ToList();

            // Update existing portals and update changed ones
            foreach (var portalZDO in zdoList)
            {
                string portalName = portalZDO.GetString("tag");
                var portalZDOID = portalZDO.m_uid;
                var location = portalZDO.GetPosition();
                var targetZDOID = portalZDO.GetZDOID("target");

                Update(portalZDOID, portalName, location, targetZDOID);
            }
        }

        public void UpdateFromResyncPackage(ZPackage pkg)
        {
            var count = pkg.ReadInt();

            Jotunn.Logger.LogInfo($"[KnownPortalsManager.UpdateFromResyncPackage] Received {count} portals from server");

            // First, unpack all portals 
            var portalsInPackage = new List<KnownPortal>();
            if (count>0)
            {
                for (int i = 0; i<count; i++)
                {
                    var portalPkg = pkg.ReadPackage();
                    var portal = KnownPortal.Unpack(portalPkg);
                    portalsInPackage.Add(portal);
                }
            }

            // Second, update them in our Known Portals
            Jotunn.Logger.LogInfo($"[KnownPortalsManager.UpdateFromResyncPackage] Updating {portalsInPackage.Count} portals");
            foreach (var portal in portalsInPackage)
            {
                KnownPortalsManager.Instance.Update(portal);
            }

            // Third, remove Known Portals that didn't appear in the sync package
            var knownPortals = GetList();
            var deletedPortals = knownPortals.Where(p => !portalsInPackage.Contains(p));
            Jotunn.Logger.LogInfo($"[KnownPortalsManager.UpdateFromResyncPackage] Removing {deletedPortals.Count()} portals");
            foreach (var deletedPortal in deletedPortals)
            {
                Remove(deletedPortal);
            }

            // Fourth, check if any portals are targeting portals that no longer exist, and fix those
            var targetingInvalidPortals = GetList().Where(p => p.Target != ZDOID.None && !ContainsId(p.Target));
            Jotunn.Logger.LogInfo($"[KnownPortalsManager.UpdateFromResyncPackage] Retargeting {targetingInvalidPortals.Count()} portals");
            foreach (var targetingInvalidPortal in targetingInvalidPortals)
            {
                RPC.SendTargetChangeRequest(targetingInvalidPortal.Id, ZDOID.None);
            }

            Jotunn.Logger.LogInfo($"[KnownPortalsManager.UpdateFromResyncPackage] Known portals: {Count}");
        }

        public void Dispose()
        {
            knownPortals.Clear();
        }
    }
}
