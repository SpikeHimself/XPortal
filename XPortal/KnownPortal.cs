using System.IO;
using UnityEngine;
using XPortal.Extension;

namespace XPortal
{
    public class KnownPortal
    {
        public ZDOID Id { get; set; }
        public string Name { get; set; }
        public ZDOID PreviousId { get; set; }
        public ZDOID Target { get; set; }
        public Vector3 Location { get; set; }
        public string Colour { get; set; }

        /// <summary>0 = Global, 1–15 = custom globals, else personal network id.</summary>
        public long NetworkOwnerPlayerId { get; set; }

        /// <summary>Network owner label stored on the portal ZDO.</summary>
        public string NetworkOwnerDisplayName { get; set; }

        /// <summary>Private portals are owner-only in lists and behave as personal network.</summary>
        public bool IsPrivate { get; set; }

        public bool IsDefaultPortal
        {
            get
            {
                return Location.Round().Equals(XPortalConfig.Instance.Local.DefaultPortal.Value.Round());
            }
        }

        public KnownPortal(ZDOID id)
        {
            Id = id;
            Name = string.Empty;
            Location = Vector3.zero;
            PreviousId = ZDOID.None;
            Target = KnownPortalsManager.Instance.FindDefaultPortal();
            Colour = PortalColour.GetPortalColour(id);
            NetworkOwnerPlayerId = 0L;
            NetworkOwnerDisplayName = string.Empty;
            IsPrivate = false;
        }

        /// <summary>Used when a portal is first placed (or hover placeholder). Privacy default comes from config (<see cref="XPortalConfig.ConfigSettings.DefaultPrivatePortal"/>).</summary>
        public KnownPortal(ZDOID id, Vector3 location) : this(id)
        {
            Location = location;
            IsPrivate = XPortalConfig.Instance.Local.DefaultPrivatePortal.Value;
        }

        public KnownPortal(ZPackage pkg)
        {
            Id = pkg.ReadZDOID();
            Name = pkg.ReadString();
            Location = pkg.ReadVector3();
            PreviousId = pkg.ReadZDOID();
            Target = pkg.ReadZDOID();
            Colour = pkg.ReadString();
            NetworkOwnerPlayerId = pkg.ReadLong();
            NetworkOwnerDisplayName = ReadOptionalString(pkg);
            IsPrivate = ReadOptionalBool(pkg);
        }

        /// <summary>Reads the packed network owner display name, or empty if the package has no more data.</summary>
        private static string ReadOptionalString(ZPackage pkg)
        {
            try
            {
                return pkg.ReadString();
            }
            catch (EndOfStreamException)
            {
                return string.Empty;
            }
        }

        private static bool ReadOptionalBool(ZPackage pkg)
        {
            try
            {
                return pkg.ReadBool();
            }
            catch (EndOfStreamException)
            {
                return false;
            }
        }

        public string GetFriendlyName()
        {
            var portalName = Name;
            if (string.IsNullOrEmpty(portalName))
            {
                return Localization.instance.Localize("$piece_portal_tag_none");  // "(No Name)"
            }
            else
            {
                return portalName;
            }
        }

        public string GetFriendlyTargetName()
        {
            if (!HasTarget())
            {
                return Localization.instance.Localize("$piece_portal_target_none");   // "(None)"
            }

            if (!KnownPortalsManager.Instance.ContainsId(Target))
            {
                return $"{Target} (invalid)";
            }

            return KnownPortalsManager.Instance.GetKnownPortalById(Target).GetFriendlyName();
        }

        public bool HasTarget()
        {
            return Target != null && Target != ZDOID.None && !Target.IsNone();
        }

        public ZPackage Pack()
        {
            var pkg = new ZPackage();
            pkg.Write(Id);
            pkg.Write(Name);
            pkg.Write(Location);
            pkg.Write(PreviousId);
            pkg.Write(Target);
            pkg.Write(Colour);
            pkg.Write(NetworkOwnerPlayerId);
            pkg.Write(NetworkOwnerDisplayName ?? string.Empty);
            pkg.Write(IsPrivate);
            return pkg;
        }

        public bool Targets(ZDOID target)
        {
            return Target == target;
        }

        public override string ToString()
        {
            return $"{{ Id: `{Id}`, Name; `{GetFriendlyName()}`, Location: `{Location}`, NetworkOwner: `{NetworkOwnerPlayerId}` (`{NetworkOwnerDisplayName}`), Private: `{IsPrivate}`, Target: `{Target}` (`{GetFriendlyTargetName()}`), Colour: `{Colour}` }}";
        }

        public bool IsGlobalNetwork()
        {
            return NetworkOwnerPlayerId == 0L;
        }
    }
}
