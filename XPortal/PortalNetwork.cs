namespace XPortal
{
    internal static class PortalNetwork
    {
        /// <summary>Dropdown sentinel: "My Private Portals" bucket (not a real network owner id).</summary>
        internal const long DestinationNetworkMyPrivateBucket = -1L;

        /// <summary>Trims and limits length for a stored network owner display name.</summary>
        internal static string SanitizeNetworkOwnerDisplayName(string raw)
        {
            if (string.IsNullOrWhiteSpace(raw))
            {
                return string.Empty;
            }

            var t = raw.Trim();
            const int maxLen = 64;
            if (t.Length > maxLen)
            {
                t = t.Substring(0, maxLen);
            }

            return t;
        }

        internal static string FormatNetworkLabel(long ownerPlayerId)
        {
            if (ownerPlayerId == DestinationNetworkMyPrivateBucket)
            {
                return Localization.instance.Localize("$hud_xportal_network_my_private");
            }

            if (ownerPlayerId == 0L)
            {
                return Localization.instance.Localize("$hud_xportal_network_global");
            }

            if (ownerPlayerId >= CustomNetworks.MinId && ownerPlayerId <= CustomNetworks.MaxId)
            {
                if (CustomNetworks.TryGetDisplayName(ownerPlayerId, out var customName))
                {
                    return customName;
                }

                return ownerPlayerId.ToString();
            }

            var networkWord = Localization.instance.Localize("$hud_xportal_network_suffix");

            // Portal ZDOs cache a display name for the network owner; that string can be stale or wrong
            // (e.g. another character) while NetworkOwnerPlayerId is correct. For the local player's id,
            // always use live resolution so the UI matches the character you are playing.
            if (IsLocalPlayerNetworkId(ownerPlayerId))
            {
                var self = ResolvePlayerDisplayName(ownerPlayerId).Trim();
                return $"{self} {networkWord}";
            }

            var cached = KnownPortalsManager.Instance.GetNetworkOwnerDisplayNameForPlayerId(ownerPlayerId);
            if (!string.IsNullOrEmpty(cached))
            {
                return $"{cached.Trim()} {networkWord}";
            }

            var name = ResolvePlayerDisplayName(ownerPlayerId).Trim();
            if (string.IsNullOrEmpty(name))
            {
                return networkWord;
            }

            return $"{name} {networkWord}";
        }

        private static bool IsLocalPlayerNetworkId(long ownerPlayerId)
        {
            if (Player.m_localPlayer != null)
            {
                return Player.m_localPlayer.GetPlayerID() == ownerPlayerId;
            }

            return Game.instance != null && Game.instance.GetPlayerProfile().GetPlayerID() == ownerPlayerId;
        }

        private static string ResolvePlayerDisplayName(long ownerPlayerId)
        {
            if (Player.m_localPlayer != null && Player.m_localPlayer.GetPlayerID() == ownerPlayerId)
            {
                return Player.m_localPlayer.GetPlayerName();
            }

            var onlinePlayer = Player.GetPlayer(ownerPlayerId);
            if (onlinePlayer != null)
            {
                var fromPlayer = onlinePlayer.GetPlayerName();
                if (!string.IsNullOrWhiteSpace(fromPlayer) && fromPlayer != "...")
                {
                    return fromPlayer;
                }
            }

            var fromWorld = TryResolveByWorldState(ownerPlayerId);
            if (!string.IsNullOrEmpty(fromWorld))
            {
                return fromWorld;
            }

            if (Game.instance != null && Game.instance.GetPlayerProfile().GetPlayerID() == ownerPlayerId)
            {
                return Game.instance.GetPlayerProfile().GetName();
            }

            return ownerPlayerId.ToString();
        }

        /// <summary>
        /// Resolves a display name from character ZDOs, the ESC player list (including server-assigned names), then peers.
        /// </summary>
        internal static string TryResolveByWorldState(long ownerPlayerId)
        {
            if (ownerPlayerId == 0L || ZNet.instance == null || ZDOMan.instance == null)
            {
                return null;
            }

            foreach (var zdo in ZNet.instance.GetAllCharacterZDOS())
            {
                if (zdo == null || zdo.GetLong(ZDOVars.s_playerID) != ownerPlayerId)
                {
                    continue;
                }

                var n = zdo.GetString(ZDOVars.s_playerName, string.Empty);
                if (IsUsablePlayerName(n))
                {
                    return n.Trim();
                }
            }

            foreach (var info in ZNet.instance.GetPlayerList())
            {
                if (info.m_characterID.IsNone())
                {
                    continue;
                }

                var characterZdo = ZDOMan.instance.GetZDO(info.m_characterID);
                if (characterZdo == null || characterZdo.GetLong(ZDOVars.s_playerID) != ownerPlayerId)
                {
                    continue;
                }

                if (!string.IsNullOrEmpty(info.m_serverAssignedDisplayName))
                {
                    return info.m_serverAssignedDisplayName.Trim();
                }

                if (!string.IsNullOrEmpty(info.m_name))
                {
                    return info.m_name.Trim();
                }

                if (!string.IsNullOrEmpty(info.m_userInfo.m_displayName))
                {
                    return info.m_userInfo.m_displayName.Trim();
                }

                var zdoName = characterZdo.GetString(ZDOVars.s_playerName, string.Empty);
                if (IsUsablePlayerName(zdoName))
                {
                    return zdoName.Trim();
                }
            }

            foreach (var peer in ZNet.instance.GetPeers())
            {
                if (peer.m_characterID.IsNone())
                {
                    continue;
                }

                var characterZdo = ZDOMan.instance.GetZDO(peer.m_characterID);
                if (characterZdo == null || characterZdo.GetLong(ZDOVars.s_playerID) != ownerPlayerId)
                {
                    continue;
                }

                if (!string.IsNullOrEmpty(peer.m_playerName))
                {
                    return peer.m_playerName.Trim();
                }
            }

            return null;
        }

        private static bool IsUsablePlayerName(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                return false;
            }

            // Default used by Player.GetPlayerName() when the ZDO has no name yet
            if (name == "...")
            {
                return false;
            }

            return true;
        }
    }
}
