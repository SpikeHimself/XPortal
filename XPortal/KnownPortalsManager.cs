using System;
using System.Collections.Generic;
using System.Linq;

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

        public KnownPortal GetKnownPortalById(ZDOID id)
        {
            return knownPortals[id];
        }

        public List<KnownPortal> GetList()
        {
            return knownPortals.Values.ToList();
        }

        /// <summary>
        /// Packs the list of portals into a ZPackage. The package is prepended by an int which indicates how many portals are in the package.
        /// </summary>
        /// <returns></returns>
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

        public KnownPortal AddOrUpdate(KnownPortal portal)
        {
            if (!ContainsId(portal.Id))
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

        public KnownPortal SetName(ZDOID id, string newName)
        {
            knownPortals[id].Name= newName;
            return knownPortals[id];
        }

        public KnownPortal SetTarget(ZDOID id, ZDOID target)
        {
            knownPortals[id].Target = target;
            return knownPortals[id];
        }
        public void Remove(ZDOID id)
        {
            if (ContainsId(id))
            {
                knownPortals.Remove(id);
            }
        }

        public void Remove(KnownPortal portal)
        {
            Remove(portal.Id);
        }

        public void UpdateFromZDOList(List<ZDO> zdoList)
        {
            var portalsWithZdos = new List<KnownPortal>();

            // Create a list of all portals
            foreach (var portalZDO in zdoList)
            {
                var id = portalZDO.m_uid;
                string name = portalZDO.GetString("tag");
                var location = portalZDO.GetPosition();
                var target = portalZDO.GetZDOID("target");

                var knownPortal = new KnownPortal(id, name, location, target);
                portalsWithZdos.Add(knownPortal);
            }

            // Update our known portals
            UpdateFromList(portalsWithZdos);
        }

        public void UpdateFromResyncPackage(ZPackage pkg)
        {
            var count = pkg.ReadInt();

            Jotunn.Logger.LogInfo($"[KnownPortalsManager.UpdateFromResyncPackage] Received {count} portals from server");

            // Unpack all portals 
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

            // Update our known portals
            UpdateFromList(portalsInPackage);
        }

        private void UpdateFromList(List<KnownPortal> updatedPortals)
        {
            // First, update the portals we already know, and add new ones
            Jotunn.Logger.LogInfo($"[KnownPortalsManager.UpdateFromList] Updating {updatedPortals.Count} portals");
            foreach (var portal in updatedPortals)
            {
                AddOrUpdate(portal);
            }

            // Second, remove Known Portals that didn't appear in the sync package
            var knownPortals = GetList();
            var deletedPortals = knownPortals.Where(p => !updatedPortals.Contains(p));
            Jotunn.Logger.LogInfo($"[KnownPortalsManager.UpdateFromList] Removing {deletedPortals.Count()} portals");
            foreach (var portal in deletedPortals)
            {
                Remove(portal);
            }

            // Third, check if any portals are targeting portals that no longer exist, and fix those
            var targetingInvalidPortals = GetList().Where(p => p.Target != ZDOID.None && !ContainsId(p.Target));
            Jotunn.Logger.LogInfo($"[KnownPortalsManager.UpdateFromList] Retargeting {targetingInvalidPortals.Count()} portals");
            foreach (var portal in targetingInvalidPortals)
            {
                RPC.SendTargetChangeRequest(portal.Id, ZDOID.None);
            }

            Jotunn.Logger.LogInfo($"[KnownPortalsManager.UpdateFromList] Known portals: {Count}");
        }

        public void Dispose()
        {
            knownPortals.Clear();
        }
    }
}
