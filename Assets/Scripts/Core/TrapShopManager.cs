using System;
using System.Collections.Generic;
using UnityEngine;

namespace DreamDungeon
{
    public sealed class TrapShopManager : MonoBehaviour
    {
        [SerializeField] private TrapDatabase database;
        [SerializeField] private TrapInventoryManager inventory;
        [SerializeField] private CurrencyManager currency;
        [SerializeField, Min(1)] private int dungeonLevel = 1;
        private readonly HashSet<string> handledRequests = new HashSet<string>();
        public TrapDatabase Database => database; public TrapInventoryManager Inventory => inventory; public CurrencyManager Currency => currency; public int DungeonLevel => dungeonLevel;
        public event Action<string> StatusChanged;
        public void Configure(TrapDatabase db, TrapInventoryManager inv, CurrencyManager money, int level) { database = db; inventory = inv; currency = money; dungeonLevel = Mathf.Max(1, level); }
        public void SetDungeonLevel(int level) => dungeonLevel = Mathf.Max(1, level);
        public IReadOnlyList<TrapData> GetAll() => database != null ? database.AllTraps : Array.Empty<TrapData>();
        public bool CanPurchase(string id, out string error)
        {
            TrapData data = database != null ? database.FindById(id) : null;
            if (data == null) { error = $"Trap '{id}' is missing."; return false; }
            if (dungeonLevel < data.unlockDungeonLevel) { error = $"Unlocks at Dungeon Level {data.unlockDungeonLevel}."; return false; }
            if (inventory == null) { error = "Trap Inventory Manager is not configured."; return false; }
            if (!inventory.CanAdd(data, 1, out error)) return false;
            if (currency == null) { error = "Currency Manager is not configured."; return false; }
            if (!currency.CanAfford(data.goldCost, 0)) { error = $"Not enough Gold. {data.trapName} costs {data.goldCost}."; return false; }
            error = string.Empty; return true;
        }
        public bool TryPurchase(string id, out string message) => TryPurchase(id, Guid.NewGuid().ToString("N"), out message);
        public bool TryPurchase(string id, string requestId, out string message)
        {
            if (string.IsNullOrWhiteSpace(requestId) || handledRequests.Contains(requestId)) { message = "This purchase request was already processed or has no ID."; return false; }
            if (!CanPurchase(id, out message)) { StatusChanged?.Invoke(message); return false; }
            TrapData data = database.FindById(id);
            if (!currency.TrySpend(data.goldCost, 0, 0, out message)) { StatusChanged?.Invoke(message); return false; }
            if (!inventory.TryAddOwned(data, 1, out message)) { currency.TryAdd(data.goldCost, 0, 0, out _); StatusChanged?.Invoke(message); return false; }
            handledRequests.Add(requestId); message = $"Purchased {data.trapName}. Select it in inventory to place it."; StatusChanged?.Invoke(message); return true;
        }
    }
}
