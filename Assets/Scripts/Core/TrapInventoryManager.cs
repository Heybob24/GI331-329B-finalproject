using System;
using System.Collections.Generic;
using UnityEngine;

namespace DreamDungeon
{
    public sealed class TrapInventoryManager : MonoBehaviour
    {
        [SerializeField] private TrapDatabase database;
        [SerializeField] private RoomManager roomManager;
        [SerializeField, Min(1)] private int maxOwnedPerType = 99;
        private readonly Dictionary<string, int> owned = new Dictionary<string, int>();
        public event Action InventoryChanged;
        public TrapDatabase Database => database;
        public void Configure(TrapDatabase db, RoomManager manager) { database = db; roomManager = manager; if (roomManager != null) roomManager.LayoutChanged += NotifyChanged; }
        private void Awake() { if (roomManager == null) roomManager = GetComponent<RoomManager>(); if (roomManager != null) roomManager.LayoutChanged += NotifyChanged; }
        private void OnDestroy() { if (roomManager != null) roomManager.LayoutChanged -= NotifyChanged; }
        public int GetOwned(string id) => !string.IsNullOrWhiteSpace(id) && owned.TryGetValue(id, out int value) ? value : 0;
        public int GetPlaced(string id) { int count = 0; if (roomManager?.Layout?.traps != null) foreach (TrapInstanceData i in roomManager.Layout.traps) if (i.trapId == id) count++; return count; }
        public int GetAvailable(string id) => Mathf.Max(0, GetOwned(id) - GetPlaced(id));
        public bool CanAdd(TrapData data, int quantity, out string error)
        {
            if (data == null || database == null || database.FindById(data.trapId) != data) { error = "Trap is missing from the Trap Database."; return false; }
            if (quantity <= 0 || (long)GetOwned(data.trapId) + quantity > maxOwnedPerType) { error = "Trap inventory capacity reached."; return false; }
            error = string.Empty; return true;
        }
        public bool TryAddOwned(TrapData data, int quantity, out string error)
        {
            if (!CanAdd(data, quantity, out error)) return false;
            owned[data.trapId] = GetOwned(data.trapId) + quantity; InventoryChanged?.Invoke(); return true;
        }
        public bool CanPlace(string id, out string error)
        {
            if (database == null || database.FindById(id) == null) { error = $"Trap '{id}' is not in the database."; return false; }
            if (GetAvailable(id) <= 0) { error = "No unplaced trap of this type is available."; return false; }
            error = string.Empty; return true;
        }
        public string ToJson()
        {
            var save = new TrapInventorySaveData(); foreach (var pair in owned) save.entries.Add(new TrapOwnershipRecord { trapId = pair.Key, ownedQuantity = pair.Value });
            return JsonUtility.ToJson(save);
        }
        public bool LoadFromJson(string json, out string error)
        {
            TrapInventorySaveData save; try { save = JsonUtility.FromJson<TrapInventorySaveData>(json); } catch (Exception e) { error = e.Message; return false; }
            if (save == null) { error = "Trap inventory data is empty."; return false; }
            var next = new Dictionary<string, int>();
            foreach (TrapOwnershipRecord record in save.entries ?? new List<TrapOwnershipRecord>())
            {
                if (record == null || string.IsNullOrWhiteSpace(record.trapId) || next.ContainsKey(record.trapId)) { error = "Trap inventory has a missing or duplicate ID."; return false; }
                if (database == null || database.FindById(record.trapId) == null || record.ownedQuantity < 0 || record.ownedQuantity > maxOwnedPerType) { error = $"Invalid ownership record '{record.trapId}'."; return false; }
                next.Add(record.trapId, record.ownedQuantity);
            }
            foreach (var pair in next) if (pair.Value < GetPlaced(pair.Key)) { error = $"Ownership for '{pair.Key}' is below its placed quantity."; return false; }
            owned.Clear(); foreach (var pair in next) owned.Add(pair.Key, pair.Value); InventoryChanged?.Invoke(); error = string.Empty; return true;
        }
        private void NotifyChanged() => InventoryChanged?.Invoke();
    }
    [Serializable] public sealed class TrapInventorySaveData { public List<TrapOwnershipRecord> entries = new List<TrapOwnershipRecord>(); }
    [Serializable] public sealed class TrapOwnershipRecord { public string trapId; public int ownedQuantity; }
}
