/*
 * HamoodMenu  Classes/Menu/MenuTier.cs
 * User-only vs owner-only menu tiers. The user build is the public
 * product: safe helpers, cool local mods, no teeth. The owner build
 * is hers: everything, plus the lobby sniffer that sees user-tier
 * menus and the console grants that make it all work.
 *
 * Tier is resolved at runtime: the user build grants owner tier only to
 * player IDs in the server administrator list.
 *
 * Copyright (C) 2026  HamoodMenu
 * https://github.com/Seralyth/Seralyth-Menu
 */

using Photon.Pun;

namespace Seralyth.Classes.Menu
{
    public enum MenuTier
    {
        User,
        Owner
    }

    public static class MenuTierSystem
    {
        /// <summary>Active tier for this running instance.</summary>
        public static MenuTier CurrentTier
        {
            get
            {
#if OWNER
                return MenuTier.Owner;
#else
                try
                {
                    string userId = null;
                    try { userId = PhotonNetwork.LocalPlayer?.UserId; } catch { }
                    if (!string.IsNullOrEmpty(userId))
                    {
                        if (ServerData.Administrators.ContainsKey(userId))
                            return MenuTier.Owner;
#if !USERONLY
                        if (ServerData.LocalAdmins.ContainsKey(userId))
                            return MenuTier.Owner;
#endif
                    }
                }
                catch { }
                return MenuTier.User;
#endif
            }
        }

        public static bool IsOwner => CurrentTier == MenuTier.Owner;

        public static bool IsUser => CurrentTier == MenuTier.User;

        /// <summary>Short tier tag broadcast in the console handshake.</summary>
        public static string TierTag => IsOwner ? "owner" : "user";

        /// <summary>True if this button should render for the current tier.</summary>
        public static bool VisibleForTier(ButtonInfo button)
        {
            if (button == null) return false;
            // OwnerOnly buttons hide on user builds. Everything else shows.
            if (button.ownerOnly && !IsOwner) return false;
            // UserHidden buttons hide on user builds (owner tooling).
            if (button.userHidden && !IsOwner) return false;
            return true;
        }
    }
}
