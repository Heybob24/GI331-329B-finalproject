using System;
using UnityEngine;

namespace DreamDungeon
{
    public sealed class RoomUpgradeManager : MonoBehaviour
    {
        [SerializeField] private RoomDatabase database;
        [SerializeField] private CurrencyManager currency;
        [SerializeField] private RoomManager roomManager;

        public event Action<DungeonRoomInstanceData> RoomUpgraded;

        public void Configure(RoomDatabase roomDatabase, CurrencyManager economy, RoomManager manager)
        {
            database = roomDatabase;
            currency = economy;
            roomManager = manager;
        }

        public bool TryGetUpgradeCost(DungeonRoomInstanceData instance, out int goldCost, out int soulCost, out string error)
        {
            goldCost = 0;
            soulCost = 0;
            if (instance == null) { error = "Select a placed room first."; return false; }
            RoomData definition = database != null ? database.FindById(instance.roomId) : null;
            if (definition == null) { error = $"Room data '{instance.roomId}' is missing."; return false; }
            if (!definition.IsPurchasable) { error = "This permanent room cannot be upgraded."; return false; }
            if (!instance.isPlaced) { error = "Only placed rooms can be upgraded."; return false; }
            if (instance.upgradeLevel >= definition.maxLevel) { error = $"{definition.roomName} is already at maximum level {definition.maxLevel}."; return false; }

            long gold = (long)definition.baseUpgradeGoldCost * Mathf.Max(1, instance.upgradeLevel);
            long soul = (long)definition.baseUpgradeSoulCost * Mathf.Max(1, instance.upgradeLevel);
            if (gold > int.MaxValue || soul > int.MaxValue) { error = "Upgrade cost exceeds the supported currency range."; return false; }
            goldCost = (int)gold;
            soulCost = (int)soul;
            error = string.Empty;
            return true;
        }

        public bool CanUpgrade(string instanceId, out int goldCost, out int soulCost, out string error)
        {
            goldCost = 0;
            soulCost = 0;
            if (roomManager == null || currency == null) { error = "Room upgrade services are not configured."; return false; }
            DungeonRoomInstanceData instance = roomManager.FindRoom(instanceId);
            if (!TryGetUpgradeCost(instance, out goldCost, out soulCost, out error)) return false;
            if (!currency.CanAfford(goldCost, soulCost)) { error = $"Not enough currency. Upgrade costs {goldCost} Gold and {soulCost} Soul."; return false; }
            return true;
        }

        public bool TryUpgrade(string instanceId, out string message)
        {
            if (!CanUpgrade(instanceId, out int goldCost, out int soulCost, out message)) return false;
            DungeonRoomInstanceData instance = roomManager.FindRoom(instanceId);
            int nextLevel = instance.upgradeLevel + 1;
            if (!currency.TrySpend(goldCost, soulCost, 0, out message)) return false;
            if (!roomManager.TrySetUpgradeLevel(instanceId, nextLevel, out message))
            {
                currency.TryAdd(goldCost, soulCost, 0, out _);
                return false;
            }

            DungeonRoomInstanceData upgraded = roomManager.FindRoom(instanceId);
            message = $"{database.FindById(upgraded.roomId).roomName} upgraded to Level {upgraded.upgradeLevel}.";
            RoomUpgraded?.Invoke(upgraded);
            return true;
        }
    }
}
