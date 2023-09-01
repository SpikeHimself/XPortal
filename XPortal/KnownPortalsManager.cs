using System;
using System.Collections.Generic;
using System.Linq;
using XPortal.RPC;

namespace XPortal
{
    internal sealed class KnownPortalsManager : IDisposable
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
                //Log.Debug($"Adding {portal}");
                knownPortals.Add(portal.Id, portal);
            }
            else
            {
                //Log.Debug($"Updating {portal}");
                knownPortals[portal.Id] = portal;
            }

            return knownPortals[portal.Id];
        }

        public bool Remove(ZDOID id)
        {
            return knownPortals.Remove(id);
        }

        public bool Remove(KnownPortal portal)
        {
            return Remove(portal.Id);
        }

        public void UpdateFromZDOList(List<ZDO> zdoList)
        {
            var portalsWithZdos = new List<KnownPortal>();

            // Create a list of all portals
            foreach (var portalZDO in zdoList)
            {
                var knownPortal = new KnownPortal(portalZDO.m_uid)
                {
                    Name = portalZDO.GetString("tag"),
                    Location = portalZDO.GetPosition(),
                    PreviousId = portalZDO.GetZDOID(XPortal.Key_PreviousId),
                    Target = portalZDO.GetZDOID(XPortal.Key_TargetId),
                };

                portalsWithZdos.Add(knownPortal);
            }

            // Update our known portals
            UpdateFromList(portalsWithZdos);
        }

        public void UpdateFromResyncPackage(ZPackage pkg)
        {
            var count = pkg.ReadInt();

            Log.Debug($"Received {count} portals from server");

            // Unpack all portals 
            var portalsInPackage = new List<KnownPortal>();
            if (count > 0)
            {
                for (int i = 0; i < count; i++)
                {
                    var portalPkg = pkg.ReadPackage();
                    var portal = new KnownPortal(portalPkg);
                    portalsInPackage.Add(portal);
                }
            }

            // Update our known portals
            UpdateFromList(portalsInPackage);
        }

        private void UpdateFromList(List<KnownPortal> updatedPortals)
        {
            // First, update the portals we already know, and add new ones
            Log.Debug($"Updating {updatedPortals.Count} portals");
            foreach (var portal in updatedPortals)
            {
                AddOrUpdate(portal);
            }

            // Second, remove Known Portals that didn't appear in the sync package
            var knownPortals = GetList();
            var deletedPortals = knownPortals.Where(p => !updatedPortals.Contains(p));
            Log.Debug($"Removing {deletedPortals.Count()} portals");
            foreach (var portal in deletedPortals)
            {
                Remove(portal);
            }

            // Third, check if any portals are targeting portals that no longer exist, and ask the server to fix those
            var targetingInvalidPortals = GetList().Where(p => p.Target != ZDOID.None && !ContainsId(p.Target));
            Log.Debug($"Retargeting {targetingInvalidPortals.Count()} portals");
            foreach (var portal in targetingInvalidPortals)
            {
                portal.Target = ZDOID.None;
                SendToServer.AddOrUpdateRequest(portal);
            }

            Log.Info($"Known portals updated. Current total: {Count}");
        }

        public void Reset()
        {
            knownPortals.Clear();
        }

        public void Dispose()
        {
            knownPortals?.Clear();
        }
    }
}
