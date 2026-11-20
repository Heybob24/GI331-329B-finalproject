using System;
using System.Collections.Generic;
using UnityEngine;

namespace DreamDungeon
{
    public sealed class MonsterPlacementManager : MonoBehaviour
    {
        [SerializeField] private DungeonGrid grid;
        [SerializeField] private RoomManager roomManager;
        [SerializeField] private MonsterDatabase database;
        [SerializeField] private MonsterInventoryManager inventory;
        [SerializeField] private RoomPlacement roomPlacement;
        [SerializeField, Min(1)] private int defaultRoomCapacity = 3;
        private string selectedMonsterId;
        private string movingInstanceId;
        private readonly List<GameObject> visuals = new List<GameObject>();
        public event Action<string> StatusChanged;
        public bool HasPendingPlacement => !string.IsNullOrEmpty(selectedMonsterId) || !string.IsNullOrEmpty(movingInstanceId);
        public void Configure(DungeonGrid g, RoomManager rooms, MonsterDatabase db, MonsterInventoryManager inv, RoomPlacement roomPlace = null) { grid = g; roomManager = rooms; database = db; inventory = inv; roomPlacement = roomPlace; }
        private void Awake() { if (grid == null) grid = GetComponent<DungeonGrid>(); if (roomManager == null) roomManager = GetComponent<RoomManager>(); if (inventory == null) inventory = GetComponent<MonsterInventoryManager>(); if (roomPlacement == null) roomPlacement = GetComponent<RoomPlacement>(); if (database == null && inventory != null) database = inventory.Database; }
        private bool subscribed;
        private void OnEnable() => Subscribe();
        private void Start() { Subscribe(); RefreshVisuals(); }
        private void Subscribe() { if (subscribed || grid == null || roomManager == null) return; grid.CellSelected += OnCellSelected; roomManager.LayoutChanged += RefreshVisuals; subscribed = true; }
        private void OnDisable() { if (!subscribed) return; grid.CellSelected -= OnCellSelected; roomManager.LayoutChanged -= RefreshVisuals; subscribed = false; }
        private void OnDestroy() { ClearVisuals(); }
        public bool SelectMonster(string id)
        {
            if (inventory == null) { Report("Monster Inventory Manager is not configured."); return false; }
            if (!inventory.CanPlace(id, out string error)) { Report(error); return false; }
            selectedMonsterId = id; movingInstanceId = null; roomPlacement?.SetExternalUnitPlacementActive(true); Report($"Selected {database.FindById(id).monsterName}. Tap a Monster Room to place it."); return true;
        }
        public void BeginMove(string id) { if (roomManager?.FindMonster(id) == null) { Report("Placed monster was not found."); return; } selectedMonsterId = null; movingInstanceId = id; roomPlacement?.SetExternalUnitPlacementActive(true); Report("Tap a Monster Room to move this monster."); }
        public bool Remove(string id, out string error) { bool result = roomManager.TryRemoveMonster(id, out error); Report(result ? "Monster returned to inventory." : error); return result; }
        public int Capacity(string roomId)
        {
            DungeonRoomInstanceData room = roomManager.FindRoom(roomId); RoomData roomData = room != null ? roomManager.GetRoomData(room.roomType) : null;
            return roomData != null && roomData.maxCapacity > 0 ? Mathf.Min(defaultRoomCapacity, roomData.maxCapacity) : defaultRoomCapacity;
        }
        private void OnCellSelected(DungeonCell cell)
        {
            if (cell == null || (string.IsNullOrEmpty(selectedMonsterId) && string.IsNullOrEmpty(movingInstanceId))) return;
            DungeonRoomInstanceData room = roomManager.GetRoomAt(cell.Position);
            if (room == null || room.roomType != DungeonRoomType.MonsterRoom) { Report("Monsters can only be placed inside a Monster Room."); return; }
            if (!TryGetFreePosition(room.instanceId, movingInstanceId, out Vector2 local, out string error)) { Report(error); return; }
            if (!string.IsNullOrEmpty(movingInstanceId))
            {
                if (roomManager.TryMoveMonster(movingInstanceId, room.instanceId, local, Capacity(room.instanceId), out error)) { movingInstanceId = null; roomPlacement?.SetExternalUnitPlacementActive(false); Report("Monster moved."); } else Report(error);
                return;
            }
            if (inventory == null || !inventory.CanPlace(selectedMonsterId, out error)) { Report("No owned monster is available."); return; }
            var instance = new MonsterInstanceData { instanceId = Guid.NewGuid().ToString("N"), monsterId = selectedMonsterId, roomInstanceId = room.instanceId, localPosition = local, level = 1 };
            if (roomManager.TryPlaceMonster(instance, Capacity(room.instanceId), out error)) { roomPlacement?.SetExternalUnitPlacementActive(false); Report($"Placed {database.FindById(selectedMonsterId).monsterName}."); } else Report(error);
        }
        private bool TryGetFreePosition(string roomId, string ignoreId, out Vector2 position, out string error)
        {
            int capacity = Capacity(roomId); var occupied = new HashSet<Vector2>();
            if (roomManager.Layout.monsters != null) foreach (MonsterInstanceData unit in roomManager.Layout.monsters)
                if (unit.roomInstanceId == roomId && unit.instanceId != ignoreId) occupied.Add(unit.localPosition);
            for (int i = 0; i < capacity; i++) { Vector2 candidate = UnitPlacementSlots.Position(i, capacity); if (!occupied.Contains(candidate)) { position = candidate; error = string.Empty; return true; } }
            position = default; error = "This Monster Room is full."; return false;
        }
        private void RefreshVisuals()
        {
            ClearVisuals(); if (roomManager?.Layout?.monsters == null || grid == null) return;
            foreach (MonsterInstanceData unit in roomManager.Layout.monsters)
            {
                MonsterData data = database != null ? database.FindById(unit.monsterId) : null; DungeonRoomInstanceData room = roomManager.FindRoom(unit.roomInstanceId);
                if (data == null || room == null) continue;
                int width = room.rotation % 180 == 0 ? room.width : room.height; int height = room.rotation % 180 == 0 ? room.height : room.width;
                Vector3 center = grid.CellToWorld(room.position) + new Vector3((width - 1) * grid.CellSize * .5f, (height - 1) * grid.CellSize * .5f, -.25f);
                Vector3 world = center + new Vector3(unit.localPosition.x * width * grid.CellSize, unit.localPosition.y * height * grid.CellSize, 0);
                GameObject visual = data.monsterPrefab != null ? Instantiate(data.monsterPrefab, world, Quaternion.identity, transform) : GameObject.CreatePrimitive(PrimitiveType.Sphere);
                visual.name = $"Monster_{unit.monsterId}_{unit.instanceId}"; visual.transform.SetParent(transform, true); visual.transform.position = world; visual.transform.localScale = Vector3.one * grid.CellSize * .25f;
                foreach (Collider col in visual.GetComponentsInChildren<Collider>()) col.enabled = false;
                foreach (Collider2D col in visual.GetComponentsInChildren<Collider2D>()) col.enabled = false;
                if (visual.GetComponentInChildren<Renderer>() is Renderer renderer) renderer.material.color = data.rarity switch { MonsterRarity.Common => new Color(.35f, .8f, .42f), MonsterRarity.Rare => new Color(.3f, .55f, .95f), MonsterRarity.Epic => new Color(.7f, .35f, .9f), _ => new Color(1f, .65f, .18f) };
                visuals.Add(visual);
            }
        }
        private void ClearVisuals() { foreach (GameObject visual in visuals) if (visual != null) Destroy(visual); visuals.Clear(); }
        private void Report(string message) => StatusChanged?.Invoke(message);
    }

    internal static class UnitPlacementSlots
    {
        public static Vector2 Position(int index, int capacity)
        {
            int columns = Mathf.CeilToInt(Mathf.Sqrt(capacity)); int rows = Mathf.CeilToInt((float)capacity / columns);
            int x = index % columns; int y = index / columns;
            float px = columns == 1 ? 0f : Mathf.Lerp(-.28f, .28f, (float)x / (columns - 1));
            float py = rows == 1 ? 0f : Mathf.Lerp(-.28f, .28f, (float)y / (rows - 1));
            return new Vector2(px, py);
        }
    }
}
