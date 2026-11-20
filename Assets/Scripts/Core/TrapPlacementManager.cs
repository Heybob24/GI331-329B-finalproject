using System;
using System.Collections.Generic;
using UnityEngine;

namespace DreamDungeon
{
    public sealed class TrapPlacementManager : MonoBehaviour
    {
        [SerializeField] private DungeonGrid grid;
        [SerializeField] private RoomManager roomManager;
        [SerializeField] private TrapDatabase database;
        [SerializeField] private TrapInventoryManager inventory;
        [SerializeField] private RoomPlacement roomPlacement;
        [SerializeField, Min(1)] private int defaultRoomCapacity = 3;
        private string selectedTrapId;
        private string movingInstanceId;
        private readonly List<GameObject> visuals = new List<GameObject>();
        public event Action<string> StatusChanged;
        public bool HasPendingPlacement => !string.IsNullOrEmpty(selectedTrapId) || !string.IsNullOrEmpty(movingInstanceId);
        public void Configure(DungeonGrid g, RoomManager rooms, TrapDatabase db, TrapInventoryManager inv, RoomPlacement roomPlace = null) { grid = g; roomManager = rooms; database = db; inventory = inv; roomPlacement = roomPlace; }
        private void Awake() { if (grid == null) grid = GetComponent<DungeonGrid>(); if (roomManager == null) roomManager = GetComponent<RoomManager>(); if (inventory == null) inventory = GetComponent<TrapInventoryManager>(); if (roomPlacement == null) roomPlacement = GetComponent<RoomPlacement>(); if (database == null && inventory != null) database = inventory.Database; }
        private bool subscribed;
        private void OnEnable() => Subscribe();
        private void Start() { Subscribe(); RefreshVisuals(); }
        private void Subscribe() { if (subscribed || grid == null || roomManager == null) return; grid.CellSelected += OnCellSelected; roomManager.LayoutChanged += RefreshVisuals; subscribed = true; }
        private void OnDisable() { if (!subscribed) return; grid.CellSelected -= OnCellSelected; roomManager.LayoutChanged -= RefreshVisuals; subscribed = false; }
        private void OnDestroy() { ClearVisuals(); }
        public bool SelectTrap(string id)
        {
            if (inventory == null) { Report("Trap Inventory Manager is not configured."); return false; }
            if (!inventory.CanPlace(id, out string error)) { Report(error); return false; }
            selectedTrapId = id; movingInstanceId = null; roomPlacement?.SetExternalUnitPlacementActive(true); Report($"Selected {database.FindById(id).trapName}. Tap a Trap Room to place it."); return true;
        }
        public void BeginMove(string id) { if (roomManager?.FindTrap(id) == null) { Report("Placed trap was not found."); return; } selectedTrapId = null; movingInstanceId = id; roomPlacement?.SetExternalUnitPlacementActive(true); Report("Tap a Trap Room to move this trap."); }
        public bool Remove(string id, out string error) { bool result = roomManager.TryRemoveTrap(id, out error); Report(result ? "Trap returned to inventory." : error); return result; }
        public int Capacity(string roomId)
        {
            DungeonRoomInstanceData room = roomManager.FindRoom(roomId); RoomData roomData = room != null ? roomManager.GetRoomData(room.roomType) : null;
            return roomData != null && roomData.maxCapacity > 0 ? Mathf.Min(defaultRoomCapacity, roomData.maxCapacity) : defaultRoomCapacity;
        }
        private void OnCellSelected(DungeonCell cell)
        {
            if (cell == null || (string.IsNullOrEmpty(selectedTrapId) && string.IsNullOrEmpty(movingInstanceId))) return;
            DungeonRoomInstanceData room = roomManager.GetRoomAt(cell.Position);
            if (room == null || room.roomType != DungeonRoomType.TrapRoom) { Report("Traps can only be placed inside a Trap Room."); return; }
            if (!TryGetFreePosition(room.instanceId, movingInstanceId, out Vector2 local, out string error)) { Report(error); return; }
            if (!string.IsNullOrEmpty(movingInstanceId))
            {
                if (roomManager.TryMoveTrap(movingInstanceId, room.instanceId, local, Capacity(room.instanceId), out error)) { movingInstanceId = null; roomPlacement?.SetExternalUnitPlacementActive(false); Report("Trap moved."); } else Report(error);
                return;
            }
            if (inventory == null || !inventory.CanPlace(selectedTrapId, out error)) { Report("No owned trap is available."); return; }
            var instance = new TrapInstanceData { instanceId = Guid.NewGuid().ToString("N"), trapId = selectedTrapId, roomInstanceId = room.instanceId, localPosition = local, level = 1 };
            if (roomManager.TryPlaceTrap(instance, Capacity(room.instanceId), out error)) { roomPlacement?.SetExternalUnitPlacementActive(false); Report($"Placed {database.FindById(selectedTrapId).trapName}."); } else Report(error);
        }
        private bool TryGetFreePosition(string roomId, string ignoreId, out Vector2 position, out string error)
        {
            int capacity = Capacity(roomId); var occupied = new HashSet<Vector2>();
            if (roomManager.Layout.traps != null) foreach (TrapInstanceData unit in roomManager.Layout.traps)
                if (unit.roomInstanceId == roomId && unit.instanceId != ignoreId) occupied.Add(unit.localPosition);
            for (int i = 0; i < capacity; i++) { Vector2 candidate = UnitPlacementSlots.Position(i, capacity); if (!occupied.Contains(candidate)) { position = candidate; error = string.Empty; return true; } }
            position = default; error = "This Trap Room is full."; return false;
        }
        private void RefreshVisuals()
        {
            ClearVisuals(); if (roomManager?.Layout?.traps == null || grid == null) return;
            foreach (TrapInstanceData unit in roomManager.Layout.traps)
            {
                TrapData data = database != null ? database.FindById(unit.trapId) : null; DungeonRoomInstanceData room = roomManager.FindRoom(unit.roomInstanceId);
                if (data == null || room == null) continue;
                int width = room.rotation % 180 == 0 ? room.width : room.height; int height = room.rotation % 180 == 0 ? room.height : room.width;
                Vector3 center = grid.CellToWorld(room.position) + new Vector3((width - 1) * grid.CellSize * .5f, (height - 1) * grid.CellSize * .5f, -.3f);
                Vector3 world = center + new Vector3(unit.localPosition.x * width * grid.CellSize, unit.localPosition.y * height * grid.CellSize, 0);
                GameObject visual = data.trapPrefab != null ? Instantiate(data.trapPrefab, world, Quaternion.identity, transform) : GameObject.CreatePrimitive(PrimitiveType.Cube);
                visual.name = $"Trap_{unit.trapId}_{unit.instanceId}"; visual.transform.SetParent(transform, true); visual.transform.position = world; visual.transform.localScale = Vector3.one * grid.CellSize * .22f;
                foreach (Collider col in visual.GetComponentsInChildren<Collider>()) col.enabled = false;
                foreach (Collider2D col in visual.GetComponentsInChildren<Collider2D>()) col.enabled = false;
                if (visual.GetComponentInChildren<Renderer>() is Renderer renderer) renderer.material.color = data.trapType switch { TrapType.Spike => new Color(.72f, .72f, .74f), TrapType.Fire => new Color(1f, .3f, .08f), TrapType.Poison => new Color(.35f, .8f, .2f), TrapType.Slow => new Color(.25f, .72f, .95f), _ => new Color(.7f, .35f, 1f) };
                visuals.Add(visual);
            }
        }
        private void ClearVisuals() { foreach (GameObject visual in visuals) if (visual != null) Destroy(visual); visuals.Clear(); }
        private void Report(string message) => StatusChanged?.Invoke(message);
    }
}
