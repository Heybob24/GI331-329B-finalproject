using System;
using System.Collections.Generic;
using UnityEngine;

namespace DreamDungeon
{
    public sealed class RoomShopManager : MonoBehaviour
    {
        [SerializeField] private RoomDatabase database;
        [SerializeField] private CurrencyManager currency;
        [SerializeField] private RoomInventoryManager inventory;
        [SerializeField, Min(1)] private int dungeonLevel = 1;

        private readonly HashSet<string> completedRequests = new HashSet<string>();

        public int DungeonLevel => DungeonProgressionManager.Instance != null ? Mathf.Max(dungeonLevel, DungeonProgressionManager.Instance.CurrentLevel) : dungeonLevel;
        public RoomDatabase Database => database;
        public CurrencyManager Currency => currency;
        public RoomInventoryManager Inventory => inventory;
        public event Action<string> StatusChanged;
        public event Action<RoomData> PurchaseCompleted;

        public void Configure(RoomDatabase roomDatabase, CurrencyManager economy, RoomInventoryManager roomInventory, int level)
        {
            database = roomDatabase;
            currency = economy;
            inventory = roomInventory;
            dungeonLevel = Mathf.Max(1, level);
        }

        public void SetDungeonLevel(int level) => dungeonLevel = Mathf.Max(1, level);

        public void ResetShop()
        {
            completedRequests.Clear();
            dungeonLevel = 1;
        }

        public bool IsUnlocked(RoomData data) => data != null && DungeonLevel >= data.unlockDungeonLevel;

        public bool CanPurchase(string roomId, out string error)
        {
            RoomData data = database != null ? database.FindById(roomId) : null;
            if (data == null) { error = $"Room '{roomId}' is missing from the Room Database."; return false; }
            if (!data.IsPurchasable) { error = $"{data.roomName} cannot be purchased."; return false; }
            if (data.goldCost < 0 || data.soulCost < 0) { error = $"{data.roomName} has an invalid price."; return false; }
            if (!IsUnlocked(data)) { error = $"{data.roomName} unlocks at Dungeon Level {data.unlockDungeonLevel}."; return false; }
            if (inventory == null) { error = "Room Inventory Manager is not configured."; return false; }
            if (!inventory.CanAdd(data, 1, out error)) return false;
            if (currency == null) { error = "Currency Manager is not configured."; return false; }
            if (!currency.CanAfford(data.goldCost, data.soulCost))
            {
                if (currency.Gold < data.goldCost && data.soulCost == 0)
                    error = $"Not Enough Gold. {data.roomName} costs {data.goldCost} Gold.";
                else if (currency.Gold < data.goldCost)
                    error = $"Not Enough Gold. {data.roomName} costs {data.goldCost} Gold and {data.soulCost} Soul.";
                else
                    error = $"Not enough currency. {data.roomName} costs {data.goldCost} Gold and {data.soulCost} Soul.";
                return false;
            }
            error = string.Empty;
            return true;
        }

        public bool TryPurchase(string roomId, out string message) => TryPurchase(roomId, Guid.NewGuid().ToString("N"), out message);

        public bool TryPurchase(string roomId, string requestId, out string message)
        {
            if (string.IsNullOrWhiteSpace(requestId)) { message = "Purchase request ID is missing."; return false; }
            if (completedRequests.Contains(requestId)) { message = "This purchase request was already processed."; return false; }
            if (!CanPurchase(roomId, out message))
            {
                StatusChanged?.Invoke(message);
                return false;
            }

            RoomData data = database.FindById(roomId);
            if (!currency.TrySpend(data.goldCost, data.soulCost, 0, out message))
            {
                StatusChanged?.Invoke(message);
                return false;
            }
            if (!inventory.TryAddOwned(data, 1, out message))
            {
                currency.TryAdd(data.goldCost, data.soulCost, 0, out _);
                StatusChanged?.Invoke(message);
                return false;
            }

            completedRequests.Add(requestId);
            message = $"Purchased {data.roomName}. It is now available in Room Inventory.";
            StatusChanged?.Invoke(message);
            PurchaseCompleted?.Invoke(data);
            return true;
        }

        public IReadOnlyList<RoomData> GetAllRooms() => database != null ? database.AllRooms : Array.Empty<RoomData>();
    }
}
