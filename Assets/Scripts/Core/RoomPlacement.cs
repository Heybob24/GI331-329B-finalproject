using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace DreamDungeon
{
    public sealed class RoomPlacement : MonoBehaviour
    {
        [SerializeField] private DungeonGrid grid;
        [SerializeField] private RoomManager roomManager;
        [SerializeField] private PlacementPreview preview;

        private DungeonBuildTool tool = DungeonBuildTool.Room;
        private RoomData selectedRoom;
        private string movingRoomId;
        private int rotation;
        private bool externalUnitPlacementActive;

        public DungeonBuildTool ActiveTool => tool;
        public RoomData SelectedRoom => selectedRoom;
        public event Action<string> StatusChanged;
        public event Action<DungeonBuildTool> ToolChanged;
        public event Action<DungeonRoomInstanceData> RoomFocused;

        public void Configure(DungeonGrid dungeonGrid, RoomManager manager, PlacementPreview placementPreview)
        {
            grid = dungeonGrid;
            roomManager = manager;
            preview = placementPreview;
        }

        private void Awake()
        {
            if (grid == null) grid = GetComponent<DungeonGrid>();
            if (roomManager == null) roomManager = GetComponent<RoomManager>();
            if (preview == null) preview = GetComponent<PlacementPreview>();
            grid.Initialize();
            roomManager.Initialize();
            preview.Initialize(grid);
            grid.CellSelected += OnCellSelected;
            selectedRoom = roomManager.GetRoomData(DungeonRoomType.MonsterRoom);
        }

        private void OnDestroy()
        {
            if (grid != null) grid.CellSelected -= OnCellSelected;
        }

        private void Update()
        {
            if (Mouse.current != null && TryUpdatePreview(Mouse.current.position.ReadValue())) return;
            if (Touchscreen.current != null) TryUpdatePreview(Touchscreen.current.primaryTouch.position.ReadValue());
        }

        public void SelectRoom(RoomData room)
        {
            if (room == null) { Report("Room data is missing from the catalog."); return; }
            if (room.roomType == DungeonRoomType.Path) { SelectPath(); return; }
            selectedRoom = room;
            rotation = 0;
            movingRoomId = null;
            tool = room.roomType == DungeonRoomType.Path ? DungeonBuildTool.Path : DungeonBuildTool.Room;
            ToolChanged?.Invoke(tool);
            preview.Clear();
            Report($"Selected {room.roomName}. Tap a green cell to place.");
        }

        public void SelectRoomTool()
        {
            if (selectedRoom == null || selectedRoom.roomType == DungeonRoomType.Path)
                SelectMonsterRoom();
            else
                SetTool(DungeonBuildTool.Room);
        }

        public void SetTool(DungeonBuildTool newTool)
        {
            tool = newTool;
            ToolChanged?.Invoke(tool);
            movingRoomId = null;
            preview.Clear();
            if (newTool == DungeonBuildTool.Path)
            {
                // Path is a one-cell walkable tile, not a purchasable RoomData entry.
                selectedRoom = null;
                rotation = 0;
                Report("Path tool selected. Tap a green cell next to a walkable tile.");
            }
            else if (newTool == DungeonBuildTool.Move) Report("Move tool: tap a room, then tap a valid destination.");
            else if (newTool == DungeonBuildTool.Remove) Report("Remove tool: tap a room to remove it.");
            else Report("Room tool selected.");
        }

        public void SelectMonsterRoom() => SelectRoom(roomManager.GetRoomData(DungeonRoomType.MonsterRoom));
        public void SelectTreasureRoom() => SelectRoom(roomManager.GetRoomData(DungeonRoomType.TreasureRoom));
        public void SelectPath() => SetTool(DungeonBuildTool.Path);
        public void SelectMove() => SetTool(DungeonBuildTool.Move);
        public void SelectRemove() => SetTool(DungeonBuildTool.Remove);

        public bool BeginMove(string instanceId)
        {
            DungeonRoomInstanceData room = roomManager.FindRoom(instanceId);
            if (room == null) { Report("Select a placed room first."); return false; }
            if (room.roomType == DungeonRoomType.Entrance || room.roomType == DungeonRoomType.DungeonCore)
            { Report("Entrance and Dungeon Core cannot be moved."); return false; }
            movingRoomId = instanceId;
            tool = DungeonBuildTool.Move;
            ToolChanged?.Invoke(tool);
            preview.Clear();
            Report("Move selected room to a valid green cell.");
            return true;
        }

        public void SetExternalUnitPlacementActive(bool active) => externalUnitPlacementActive = active;

        public void Rotate()
        {
            if (!string.IsNullOrEmpty(movingRoomId))
            {
                if (roomManager.TryRotate(movingRoomId, out string message)) Report(message);
                else Report(message);
                return;
            }
            if (selectedRoom == null) { Report("Select a room type first."); return; }
            rotation = (rotation + 90) % 360;
            Report($"Placement preview rotated to {rotation} degrees.");
        }

        private bool TryUpdatePreview(Vector2 position)
        {
            if (PointerOverUi(position)) { preview.Clear(); return true; }
            if (!grid.TryGetCellAtScreen(position, out DungeonCell cell)) { preview.Clear(); return true; }

            if (tool == DungeonBuildTool.Move && !string.IsNullOrEmpty(movingRoomId))
            {
                DungeonRoomInstanceData moving = roomManager.FindRoom(movingRoomId);
                bool validMove = roomManager.CanMove(movingRoomId, cell.Position, out _);
                preview.Show(moving, cell.Position, validMove);
            }
            else if (tool == DungeonBuildTool.Path)
            {
                bool validPath = roomManager.CanPlacePath(cell.Position, out _);
                preview.ShowPath(cell.Position, validPath);
            }
            else if (tool == DungeonBuildTool.Room)
            {
                bool validPlacement = roomManager.CanPlace(selectedRoom, cell.Position, rotation, null, out _);
                preview.Show(selectedRoom, cell.Position, rotation, validPlacement);
            }
            else preview.Clear();
            return true;
        }

        private void OnCellSelected(DungeonCell cell)
        {
            if (cell == null) return;
            DungeonRoomInstanceData room = roomManager.GetRoomAt(cell.Position);

            if (tool == DungeonBuildTool.Room && room != null)
            {
                if (externalUnitPlacementActive || HasPendingUnitPlacement()) return;
                RoomFocused?.Invoke(room);
                Report($"Selected {room.roomType} at ({room.position.x + 1}, {room.position.y + 1}).");
                return;
            }

            switch (tool)
            {
                case DungeonBuildTool.Room:
                    if (roomManager.TryPlace(selectedRoom, cell.Position, rotation, out string placementMessage)) Report(placementMessage);
                    else Report(placementMessage);
                    break;
                case DungeonBuildTool.Path:
                    if (roomManager.TryPlacePath(cell.Position, out string pathMessage)) Report(pathMessage);
                    else Report(pathMessage);
                    break;
                case DungeonBuildTool.Move:
                    HandleMove(room, cell.Position);
                    break;
                case DungeonBuildTool.Remove:
                    if (roomManager.TryRemove(room?.instanceId, out string removeMessage)) Report(removeMessage);
                    else Report(removeMessage);
                    break;
            }
        }

        private static bool HasPendingUnitPlacement()
        {
            MonsterPlacementManager monsters = FindAnyObjectByType<MonsterPlacementManager>();
            TrapPlacementManager traps = FindAnyObjectByType<TrapPlacementManager>();
            return (monsters != null && monsters.HasPendingPlacement) || (traps != null && traps.HasPendingPlacement);
        }

        private void HandleMove(DungeonRoomInstanceData selectedAtCell, Vector2Int destination)
        {
            if (string.IsNullOrEmpty(movingRoomId))
            {
                if (selectedAtCell == null) { Report("Tap an existing room to select it for moving."); return; }
                if (selectedAtCell.roomType == DungeonRoomType.Entrance || selectedAtCell.roomType == DungeonRoomType.DungeonCore)
                {
                    Report("Entrance and Dungeon Core cannot be moved.");
                    return;
                }
                movingRoomId = selectedAtCell.instanceId;
                Report("Room selected. Tap a green destination cell.");
                return;
            }

            if (roomManager.TryMove(movingRoomId, destination, out string message))
            {
                movingRoomId = null;
                Report(message);
            }
            else Report(message);
        }

        private static bool PointerOverUi(Vector2 screenPosition)
        {
            if (EventSystem.current == null) return false;
            if (Mouse.current != null && Mouse.current.position.ReadValue() == screenPosition)
                return EventSystem.current.IsPointerOverGameObject();
            if (Touchscreen.current != null)
                return EventSystem.current.IsPointerOverGameObject(Touchscreen.current.primaryTouch.touchId.ReadValue());
            return false;
        }

        private void Report(string message) => StatusChanged?.Invoke(message);
    }
}
