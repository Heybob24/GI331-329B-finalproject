using System;
using System.Collections.Generic;
using UnityEngine;

namespace DreamDungeon
{
    public sealed class MonsterShopManager : MonoBehaviour
    {
        [SerializeField] private MonsterDatabase database;
        [SerializeField] private MonsterInventoryManager inventory;
        [SerializeField] private CurrencyManager currency;
        [SerializeField, Min(1)] private int dungeonLevel = 1;
        private readonly HashSet<string> handledRequests = new HashSet<string>();
        public MonsterDatabase Database => database; public MonsterInventoryManager Inventory => inventory; public CurrencyManager Currency => currency;
        public int DungeonLevel => DungeonProgressionManager.Instance != null ? Mathf.Max(dungeonLevel, DungeonProgressionManager.Instance.CurrentLevel) : dungeonLevel;
        public event Action<string> StatusChanged;
        public event Action<MonsterData> PurchaseCompleted;
        public void Configure(MonsterDatabase db, MonsterInventoryManager inv, CurrencyManager money, int level) { database = db; inventory = inv; currency = money; dungeonLevel = Mathf.Max(1, level); }
        public void SetDungeonLevel(int level) => dungeonLevel = Mathf.Max(1, level);
        public void ResetShop() { handledRequests.Clear(); dungeonLevel = 1; }
        public IReadOnlyList<MonsterData> GetAll() => database != null ? database.AllMonsters : Array.Empty<MonsterData>();
        public bool CanPurchase(string id, out string error)
        {
            MonsterData data = database != null ? database.FindById(id) : null;
            if (data == null) { error = $"Monster '{id}' is missing."; return false; }
            if (data.goldCost < 0) { error = $"{data.monsterName} has an invalid price."; return false; }
            if (DungeonLevel < data.unlockDungeonLevel) { error = $"Unlocks at Dungeon Level {data.unlockDungeonLevel}."; return false; }
            if (inventory == null) { error = "Monster Inventory Manager is not configured."; return false; }
            if (!inventory.CanAdd(data, 1, out error)) return false;
            if (currency == null) { error = "Currency Manager is not configured."; return false; }
            if (!currency.CanAfford(data.goldCost, 0)) { error = $"Not Enough Gold. {data.monsterName} costs {data.goldCost}."; return false; }
            error = string.Empty; return true;
        }
        public bool TryPurchase(string id, out string message) => TryPurchase(id, Guid.NewGuid().ToString("N"), out message);
        public bool TryPurchase(string id, string requestId, out string message)
        {
            if (string.IsNullOrWhiteSpace(requestId) || handledRequests.Contains(requestId)) { message = "This purchase request was already processed or has no ID."; return false; }
            if (!CanPurchase(id, out message)) { StatusChanged?.Invoke(message); return false; }
            MonsterData data = database.FindById(id);
            if (!currency.TrySpend(data.goldCost, 0, 0, out message)) { StatusChanged?.Invoke(message); return false; }
            if (!inventory.TryAddOwned(data, 1, out message)) { currency.TryAdd(data.goldCost, 0, 0, out _); StatusChanged?.Invoke(message); return false; }
            handledRequests.Add(requestId); message = $"Purchased {data.monsterName}. Select it in inventory to place it."; StatusChanged?.Invoke(message); PurchaseCompleted?.Invoke(data); return true;
        }
    }
}
