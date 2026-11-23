using System;
using UnityEngine;

namespace DreamDungeon
{
    public enum AuthLoginType
    {
        None = 0,
        Guest = 1,
        Google = 2,
        GoogleLinked = 3
    }

    public enum AccountStatus
    {
        Active = 0,
        Suspended = 1,
        Deleted = 2
    }

    public enum DataSyncState
    {
        Idle = 0,
        Syncing = 1,
        Synced = 2,
        Offline = 3,
        Conflict = 4,
        Error = 5
    }

    public enum AuthErrorCode
    {
        None = 0,
        NoInternet = 1,
        GoogleSignInCancelled = 2,
        GoogleTokenExpired = 3,
        AuthenticationFailed = 4,
        PlayerDataLoadFailed = 5,
        CloudSaveFailed = 6,
        LinkAccountFailed = 7,
        AccountAlreadyLinked = 8,
        PurchaseFailed = 9,
        PurchasePending = 10,
        DuplicateTransaction = 11,
        InvalidProduct = 12,
        StaleRevisionConflict = 13
    }

    [Serializable]
    public sealed class PlayerProfile
    {
        public string playerId = string.Empty;
        public string playerName = "Dungeon Master";
        public AuthLoginType loginType = AuthLoginType.None;
        public string googleAccountEmail = string.Empty;
        public string createdAtUtc = string.Empty;
        public string lastLoginUtc = string.Empty;
        public int playerLevel = 1;
        public int crystalBalance = 0;
        public AccountStatus accountStatus = AccountStatus.Active;

        public bool IsGoogleLinked => loginType == AuthLoginType.Google || loginType == AuthLoginType.GoogleLinked;

        public string GetLoginTypeDisplayName()
        {
            return loginType switch
            {
                AuthLoginType.Guest => "Guest (Anonymous)",
                AuthLoginType.Google => "Google Account",
                AuthLoginType.GoogleLinked => "Google Linked",
                _ => "Not Signed In"
            };
        }

        public static PlayerProfile CreateNew(string ugsPlayerId, AuthLoginType type, string displayName = null)
        {
            string nowIso = DateTime.UtcNow.ToString("o");
            string shortId = !string.IsNullOrEmpty(ugsPlayerId) && ugsPlayerId.Length >= 6
                ? ugsPlayerId.Substring(0, 6).ToUpperInvariant()
                : "GUEST";
            return new PlayerProfile
            {
                playerId = ugsPlayerId ?? string.Empty,
                playerName = string.IsNullOrWhiteSpace(displayName) ? $"Keeper_{shortId}" : displayName.Trim(),
                loginType = type,
                createdAtUtc = nowIso,
                lastLoginUtc = nowIso,
                playerLevel = 1,
                crystalBalance = 0,
                accountStatus = AccountStatus.Active
            };
        }
    }
}
