using System;
using System.Collections.Generic;
using UnityEngine;

namespace DreamDungeon
{
    public sealed class RoomInventoryManager : MonoBehaviour
    {
        [SerializeField] private RoomDatabase database;
        [SerializeField] private RoomManager roomManager;

        private readonly Dictionary<string, int> owned = new Dictionary<string, int>();
        private bool initialized;

        public event Action InventoryChanged;
        public RoomDatabase Database => database;

        public void Configure(RoomDatabase roomDatabase, RoomManager manager)
        {
            database = roomDatabase;
            roomManager = manager;
        }

        private void Awake() => Initialize();

        private void OnDestroy()
        {
            if (roomManager != null) roomManager.LayoutChanged -= OnLayoutChanged;
        }

        public void Initialize()
        {
            if (initialized) return;
            if (roomManager == null) roomManager = GetComponent<RoomManager>();
            if (roomManager != null)
            {
                roomManager.Initialize();
                roomManager.LayoutChanged += OnLayoutChanged;
            }
            initialized = true;
        }

        public int GetOwned(string roomId) => !string.IsNullOrWhiteSpace(roomId) && owned.TryGetValue(roomId, out int count) ? count : 0;

        public int GetPlaced(string roomId)
        {
            int count = 0;
            if (roomManager?.Layout?.rooms == null) return count;
            foreach (DungeonRoomInstanceData room in roomManager.Layout.rooms)
                if (room != null && room.isPlaced && room.roomId == roomId) count++;
            return count;
        }

        public int GetAvailable(string roomId) => Mathf.Max(0, GetOwned(roomId) - GetPlaced(roomId));
        public int GetAvailable(RoomData data) => data != null ? GetAvailable(data.roomId) : 0;

        public bool CanAdd(RoomData data, int quantity, out string error)
        {
            if (data == null || !data.IsPurchasable) { error = "This room cannot be owned or purchased."; return false; }
            if (quantity <= 0) { error = "Room quantity must be positive."; return false; }
            if (database != null && database.FindById(data.roomId) == null) { error = $"Room '{data.roomId}' is not in the Room Database."; return false; }
            long total = (long)GetOwned(data.roomId) + quantity;
            if (data.maxCapacity > 0 && total > data.maxCapacity) { error = $"Inventory capacity reached for {data.roomName}."; return false; }
            error = string.Empty;
            return true;
        }

        public bool TryAddOwned(RoomData data, int quantity, out string error)
        {
            if (!CanAdd(data, quantity, out error)) return false;
            int baseOwned = Mathf.Max(GetOwned(data.roomId), GetPlaced(data.roomId));
            owned[data.roomId] = baseOwned + quantity;
            InventoryChanged?.Invoke();
            SaveManager.Instance?.SaveGame();
            return true;
        }

        public void EnsureOwnedCoversPlaced()
        {
            if (roomManager?.Layout?.rooms == null) return;
            bool changed = false;
            foreach (DungeonRoomInstanceData room in roomManager.Layout.rooms)
            {
                if (room == null || !room.isPlaced || string.IsNullOrWhiteSpace(room.roomId)) continue;
                int placed = GetPlaced(room.roomId);
                if (GetOwned(room.roomId) < placed)
                {
                    owned[room.roomId] = placed;
                    changed = true;
                }
            }
            if (changed) InventoryChanged?.Invoke();
        }

        public bool CanPlace(string roomId, out string error)
        {
            RoomData data = database != null ? database.FindById(roomId) : null;
            if (data == null) { error = $"Room '{roomId}' is missing from the Room Database."; return false; }
            if (!data.IsPurchasable) { error = "This room is placed automatically and cannot be selected from inventory."; return false; }
            if (GetAvailable(roomId) <= 0) { error = $"No {data.roomName} is available. Purchase one in the Room Shop."; return false; }
            error = string.Empty;
            return true;
        }

        public string ToJson()
        {
            var data = new RoomInventorySaveData();
            foreach (KeyValuePair<string, int> item in owned)
                data.rooms.Add(new RoomOwnershipRecord { roomId = item.Key, ownedQuantity = item.Value });
            return JsonUtility.ToJson(data);
        }

        public bool LoadFromJson(string json, out string error)
        {
            RoomInventorySaveData data;
            try { data = JsonUtility.FromJson<RoomInventorySaveData>(json); }
            catch (Exception exception) { error = $"Room inventory data could not be read: {exception.Message}"; return false; }
            if (data == null) { error = "Room inventory data is empty."; return false; }

            var restored = new Dictionary<string, int>();
            if (data.rooms != null)
            {
                foreach (RoomOwnershipRecord record in data.rooms)
                {
                    if (record == null || string.IsNullOrWhiteSpace(record.roomId)) { error = "Inventory contains an empty roomId."; return false; }
                    if (restored.ContainsKey(record.roomId)) { error = $"Inventory contains duplicate ownership for '{record.roomId}'."; return false; }
                    RoomData definition = database != null ? database.FindById(record.roomId) : null;
                    if (definition == null || !definition.IsPurchasable) { error = $"Inventory room '{record.roomId}' is not a purchasable database entry."; return false; }
                    if (record.ownedQuantity < 0 || (definition.maxCapacity > 0 && record.ownedQuantity > definition.maxCapacity))
                    { error = $"Inventory quantity for '{record.roomId}' is invalid."; return false; }
                    restored.Add(record.roomId, record.ownedQuantity);
                }
            }

            owned.Clear();
            foreach (KeyValuePair<string, int> item in restored) owned.Add(item.Key, item.Value);
            InventoryChanged?.Invoke();
            error = string.Empty;
            return true;
        }

        public void ResetInventory()
        {
            owned.Clear();
            InventoryChanged?.Invoke();
        }

        private void OnLayoutChanged() => InventoryChanged?.Invoke();
    }

    [Serializable]
    public sealed class RoomInventorySaveData
    {
        public List<RoomOwnershipRecord> rooms = new List<RoomOwnershipRecord>();
    }

    [Serializable]
    public sealed class RoomOwnershipRecord
    {
        public string roomId;
        public int ownedQuantity;
    }
}
