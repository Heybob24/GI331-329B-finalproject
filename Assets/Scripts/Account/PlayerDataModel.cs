using System;
using System.Collections.Generic;

namespace DreamDungeon
{
    public enum PurchaseVerificationStatus
    {
        Pending = 0,
        Verified = 1,
        Fulfilled = 2,
        Failed = 3,
        Duplicate = 4
    }

    [Serializable]
    public sealed class AccountDataRecord
    {
        public string playerId = string.Empty;
        public string playerName = "Dungeon Master";
        public string authenticationProvider = "Anonymous";
        public AuthLoginType loginType = AuthLoginType.Guest;
        public string linkedGoogleSubjectHash = string.Empty;
        public string createdAtUtc = string.Empty;
        public string lastLoginUtc = string.Empty;
        public AccountStatus accountStatus = AccountStatus.Active;
    }

    [Serializable]
    public sealed class CurrencyDataRecord
    {
        public int crystalBalance = 0;
        public int totalCrystalPurchased = 0;
        public int totalCrystalSpent = 0;
        public int gold = 1000;
        public int soul = 200;
    }

    [Serializable]
    public sealed class DungeonDataRecord
    {
        public int dungeonLevel = 1;
        public string dungeonLayoutJson = string.Empty;
        public List<string> unlockedRooms = new List<string>();
        public List<string> placedMonsters = new List<string>();
        public List<string> placedTraps = new List<string>();
        public int dungeonCoreLevel = 1;
        public int gridDimension = 9;
        public int facilityGoldLevel = 1;
        public int facilitySoulLevel = 1;
        public int facilityCapacityLevel = 1;
        public string roomInventoryJson = string.Empty;
        public string monsterInventoryJson = string.Empty;
        public string trapInventoryJson = string.Empty;
    }

    [Serializable]
    public sealed class ProgressionDataRecord
    {
        public int playerLevel = 1;
        public int exp = 0;
        public List<string> unlockedMonsters = new List<string>();
        public List<string> unlockedTraps = new List<string>();
        public List<string> completedCampaignStages = new List<string>();
        public string campaignSaveJson = string.Empty;
        public string questSaveJson = string.Empty;
        public string achievementSaveJson = string.Empty;
        public string endlessChallengeSaveJson = string.Empty;
    }

    [Serializable]
    public sealed class CrystalPurchaseRecord
    {
        public string productId = string.Empty;
        public string transactionId = string.Empty;
        public string store = "GooglePlay";
        public string purchaseDateUtc = string.Empty;
        public int crystalAmount = 0;
        public PurchaseVerificationStatus purchaseStatus = PurchaseVerificationStatus.Pending;
        public string receiptHash = string.Empty;
    }

    [Serializable]
    public sealed class PlayerDataModel
    {
        public int schemaVersion = 1;
        public long revision = 1;
        public long lastModifiedTimestampUtc = 0;
        public string lastDeviceId = string.Empty;

        public AccountDataRecord accountData = new AccountDataRecord();
        public CurrencyDataRecord currencyData = new CurrencyDataRecord();
        public DungeonDataRecord dungeonData = new DungeonDataRecord();
        public ProgressionDataRecord progressionData = new ProgressionDataRecord();
        public List<CrystalPurchaseRecord> purchaseHistory = new List<CrystalPurchaseRecord>();

        public static PlayerDataModel CreateInitial(PlayerProfile profile, string deviceId)
        {
            long now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            var model = new PlayerDataModel
            {
                schemaVersion = 1,
                revision = 1,
                lastModifiedTimestampUtc = now,
                lastDeviceId = deviceId ?? string.Empty
            };

            if (profile != null)
            {
                model.accountData.playerId = profile.playerId;
                model.accountData.playerName = profile.playerName;
                model.accountData.loginType = profile.loginType;
                model.accountData.authenticationProvider = profile.IsGoogleLinked ? "Google" : "Anonymous";
                model.accountData.createdAtUtc = profile.createdAtUtc;
                model.accountData.lastLoginUtc = profile.lastLoginUtc;
                model.accountData.accountStatus = profile.accountStatus;
                model.currencyData.crystalBalance = profile.crystalBalance;
                model.progressionData.playerLevel = profile.playerLevel;
            }

            return model;
        }

        public PlayerProfile ToProfile()
        {
            return new PlayerProfile
            {
                playerId = accountData != null ? accountData.playerId : string.Empty,
                playerName = accountData != null ? accountData.playerName : "Dungeon Master",
                loginType = accountData != null ? accountData.loginType : AuthLoginType.None,
                createdAtUtc = accountData != null ? accountData.createdAtUtc : string.Empty,
                lastLoginUtc = accountData != null ? accountData.lastLoginUtc : string.Empty,
                playerLevel = progressionData != null ? progressionData.playerLevel : 1,
                crystalBalance = currencyData != null ? currencyData.crystalBalance : 0,
                accountStatus = accountData != null ? accountData.accountStatus : AccountStatus.Active
            };
        }
    }
}
