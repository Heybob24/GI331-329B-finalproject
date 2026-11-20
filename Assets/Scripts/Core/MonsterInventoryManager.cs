using System;
using System.Collections.Generic;
using UnityEngine;

namespace DreamDungeon
{
    public sealed class MonsterInventoryManager : MonoBehaviour
    {
        [SerializeField] private MonsterDatabase database;
        [SerializeField] private RoomManager roomManager;
        [SerializeField, Min(1)] private int maxOwnedPerType = 99;
        private readonly Dictionary<string, int> owned = new Dictionary<string, int>();
        public event Action InventoryChanged;
        public MonsterDatabase Database => database;
        public void Configure(MonsterDatabase db, RoomManager manager) { database = db; roomManager = manager; if (roomManager != null) roomManager.LayoutChanged += NotifyChanged; }
        private void Awake() { if (roomManager == null) roomManager = GetComponent<RoomManager>(); if (roomManager != null) roomManager.LayoutChanged += NotifyChanged; }
        private void OnDestroy() { if (roomManager != null) roomManager.LayoutChanged -= NotifyChanged; }
        public int GetOwned(string id) => !string.IsNullOrWhiteSpace(id) && owned.TryGetValue(id, out int value) ? value : 0;
        public int GetPlaced(string id) { int count = 0; if (roomManager?.Layout?.monsters != null) foreach (MonsterInstanceData i in roomManager.Layout.monsters) if (i.monsterId == id) count++; return count; }
        public int GetAvailable(string id) => Mathf.Max(0, GetOwned(id) - GetPlaced(id));
        public bool CanAdd(MonsterData data, int quantity, out string error)
        {
            if (data == null || database == null || database.FindById(data.monsterId) != data) { error = "Monster is missing from the Monster Database."; return false; }
            if (quantity <= 0 || (long)GetOwned(data.monsterId) + quantity > maxOwnedPerType) { error = "Monster inventory capacity reached."; return false; }
            error = string.Empty; return true;
        }
        public bool TryAddOwned(MonsterData data, int quantity, out string error)
        {
            if (!CanAdd(data, quantity, out error)) return false;
            owned[data.monsterId] = GetOwned(data.monsterId) + quantity; InventoryChanged?.Invoke(); return true;
        }
        public bool CanPlace(string id, out string error)
        {
            if (database == null || database.FindById(id) == null) { error = $"Monster '{id}' is not in the database."; return false; }
            if (GetAvailable(id) <= 0) { error = "No unplaced monster of this type is available."; return false; }
            error = string.Empty; return true;
        }
        public string ToJson()
        {
            var save = new MonsterInventorySaveData(); foreach (var pair in owned) save.entries.Add(new MonsterOwnershipRecord { monsterId = pair.Key, ownedQuantity = pair.Value });
            return JsonUtility.ToJson(save);
        }
        public bool LoadFromJson(string json, out string error)
        {
            MonsterInventorySaveData save; try { save = JsonUtility.FromJson<MonsterInventorySaveData>(json); } catch (Exception e) { error = e.Message; return false; }
            if (save == null) { error = "Monster inventory data is empty."; return false; }
            var next = new Dictionary<string, int>();
            foreach (MonsterOwnershipRecord record in save.entries ?? new List<MonsterOwnershipRecord>())
            {
                if (record == null || string.IsNullOrWhiteSpace(record.monsterId) || next.ContainsKey(record.monsterId)) { error = "Monster inventory has a missing or duplicate ID."; return false; }
                if (database == null || database.FindById(record.monsterId) == null || record.ownedQuantity < 0 || record.ownedQuantity > maxOwnedPerType) { error = $"Invalid ownership record '{record.monsterId}'."; return false; }
                next.Add(record.monsterId, record.ownedQuantity);
            }
            foreach (var pair in next) if (pair.Value < GetPlaced(pair.Key)) { error = $"Ownership for '{pair.Key}' is below its placed quantity."; return false; }
            owned.Clear(); foreach (var pair in next) owned.Add(pair.Key, pair.Value); InventoryChanged?.Invoke(); error = string.Empty; return true;
        }
        private void NotifyChanged() => InventoryChanged?.Invoke();
    }
    [Serializable] public sealed class MonsterInventorySaveData { public List<MonsterOwnershipRecord> entries = new List<MonsterOwnershipRecord>(); }
    [Serializable] public sealed class MonsterOwnershipRecord { public string monsterId; public int ownedQuantity; }
}
