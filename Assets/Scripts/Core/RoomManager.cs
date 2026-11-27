using System;
using System.Collections.Generic;
using UnityEngine;

namespace DreamDungeon
{
    public sealed class RoomManager : MonoBehaviour
    {
        [SerializeField] private DungeonGrid grid;
        [SerializeField] private List<RoomData> roomCatalog = new List<RoomData>();
        [SerializeField] private RoomDatabase roomDatabase;
        [SerializeField] private CurrencyManager currencyManager;
        [SerializeField] private RoomInventoryManager inventoryManager;
        [SerializeField] private MonsterInventoryManager monsterInventoryManager;
        [SerializeField] private TrapInventoryManager trapInventoryManager;
        [SerializeField] private MonsterDatabase monsterDatabase;
        [SerializeField] private TrapDatabase trapDatabase;
        [SerializeField, Min(0)] private int startingGold = 1000;
        [SerializeField] private int gridWidth = 9;
        [SerializeField] private int gridHeight = 9;

        private readonly Stack<HistoryEntry> undoStack = new Stack<HistoryEntry>();
        private readonly Stack<HistoryEntry> redoStack = new Stack<HistoryEntry>();
        private DungeonLayoutData layout;
        private DungeonPathfinder pathfinder;
        private DungeonValidator validator;
        private int gold;

        public DungeonLayoutData Layout => layout;
        public DungeonGrid Grid => grid;
        public int GridWidth => gridWidth;
        public int GridHeight => gridHeight;
        public int Gold => currencyManager != null ? currencyManager.Gold : gold;
        public CurrencyManager Currency => currencyManager;
        public RoomInventoryManager Inventory => inventoryManager;
        public RoomDatabase RoomsDatabase => roomDatabase;
        public IReadOnlyList<RoomData> RoomCatalog => roomCatalog;
        public MonsterDatabase Monsters => monsterDatabase;
        public TrapDatabase Traps => trapDatabase;
        public bool CanUndo => undoStack.Count > 0;
        public bool CanRedo => redoStack.Count > 0;
        public DungeonPathfinder Pathfinder => pathfinder;
        public static RoomManager Instance { get; private set; }
        public event Action LayoutChanged;
        public event Action<int> GoldChanged;
        public event Action<DungeonRoomInstanceData> RoomPlaced;
        public event Action<MonsterInstanceData> MonsterPlaced;
        public event Action<TrapInstanceData> TrapPlaced;
        public event Action<BossInstanceData> BossPlaced;

        public void Configure(DungeonGrid dungeonGrid, RoomData[] catalog, int width, int height, int initialGold)
        {
            grid = dungeonGrid;
            roomCatalog = catalog != null ? new List<RoomData>(catalog) : new List<RoomData>();
            gridWidth = width;
            gridHeight = height;
            startingGold = Mathf.Max(0, initialGold);
        }

        public void ConfigureRoomSystems(RoomDatabase database, CurrencyManager currency, RoomInventoryManager inventory)
        {
            roomDatabase = database;
            currencyManager = currency;
            inventoryManager = inventory;
        }

        public void ConfigureUnitInventories(MonsterInventoryManager monsters, TrapInventoryManager traps)
        {
            monsterInventoryManager = monsters; trapInventoryManager = traps;
        }

        public void ConfigureUnitDefinitions(MonsterDatabase monsters, TrapDatabase traps)
        {
            monsterDatabase = monsters; trapDatabase = traps;
        }

        private void Awake()
        {
            Instance = this;
            Initialize();
            if (currencyManager != null) currencyManager.CurrencyChanged += OnCurrencyChanged;
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
            if (currencyManager != null) currencyManager.CurrencyChanged -= OnCurrencyChanged;
        }

        public void Initialize()
        {
            if (layout != null) return;
            if (grid == null) grid = GetComponent<DungeonGrid>();
            if (currencyManager == null) currencyManager = GetComponent<CurrencyManager>();
            if (inventoryManager == null) inventoryManager = GetComponent<RoomInventoryManager>();
            if (monsterInventoryManager == null) monsterInventoryManager = GetComponent<MonsterInventoryManager>();
            if (trapInventoryManager == null) trapInventoryManager = GetComponent<TrapInventoryManager>();
            if (grid == null)
            {
                Debug.LogError("RoomManager requires a DungeonGrid component.", this);
                return;
            }

            grid.Initialize();
            layout = new DungeonLayoutData { gridWidth = gridWidth, gridHeight = gridHeight };
            gold = Mathf.Max(0, startingGold);
            if (currencyManager != null && currencyManager.Gold != gold)
                currencyManager.Configure(gold, currencyManager.Soul, currencyManager.Crystal);
            pathfinder = new DungeonPathfinder();
            validator = new DungeonValidator(pathfinder);
            CreateInitialLayout();
            RefreshLayout();
            GoldChanged?.Invoke(gold);
        }

        public void ResetToInitialState()
        {
            if (grid == null) grid = GetComponent<DungeonGrid>();
            if (currencyManager == null) currencyManager = GetComponent<CurrencyManager>();
            if (inventoryManager == null) inventoryManager = GetComponent<RoomInventoryManager>();
            if (monsterInventoryManager == null) monsterInventoryManager = GetComponent<MonsterInventoryManager>();
            if (trapInventoryManager == null) trapInventoryManager = GetComponent<TrapInventoryManager>();

            gridWidth = 9;
            gridHeight = 9;
            if (grid != null)
            {
                grid.SelectCell(null);
                grid.ClearPreviews();
                if (grid.Width != gridWidth || grid.Height != gridHeight)
                    grid.RebuildGrid(gridWidth, gridHeight);
                else
                    grid.Initialize();
            }

            undoStack.Clear();
            redoStack.Clear();

            layout = new DungeonLayoutData { gridWidth = gridWidth, gridHeight = gridHeight };
            pathfinder = new DungeonPathfinder();
            validator = new DungeonValidator(pathfinder);
            CreateInitialLayout();

            inventoryManager?.ResetInventory();
            monsterInventoryManager?.ResetInventory();
            trapInventoryManager?.ResetInventory();

            gold = Mathf.Max(0, startingGold);
            if (currencyManager != null)
                currencyManager.ResetToInitial(gold, 0, 0);
            else
                GoldChanged?.Invoke(gold);

            GetComponent<RoomShopManager>()?.ResetShop();
            GetComponent<MonsterShopManager>()?.ResetShop();
            GetComponent<TrapShopManager>()?.ResetShop();
            GetComponent<PlacementPreview>()?.Clear();
            GetComponent<RoomPlacement>()?.ResetPlacement();
            GetComponent<MonsterPlacementManager>()?.ResetPlacement();
            GetComponent<TrapPlacementManager>()?.ResetPlacement();
            GetComponent<BossPlacementManager>()?.ResetPlacement();

            RefreshLayout();
        }

        private void OnCurrencyChanged(int goldValue, int soul, int crystal)
        {
            gold = goldValue;
            GoldChanged?.Invoke(goldValue);
        }

        public RoomData GetRoomData(DungeonRoomType type)
        {
            if (roomDatabase != null)
            {
                IReadOnlyList<RoomData> matches = roomDatabase.GetByType(type);
                if (matches.Count > 0) return matches[0];
            }
            foreach (RoomData data in roomCatalog)
                if (data != null && data.roomType == type) return data;
            return null;
        }

        public DungeonRoomInstanceData GetRoomAt(Vector2Int cell)
        {
            if (layout == null) return null;
            foreach (DungeonRoomInstanceData room in layout.rooms)
                if (DungeonPathfinder.Contains(room, cell)) return room;
            return null;
        }

        public bool CanPlace(RoomData data, Vector2Int position, int rotation, string ignoredInstanceId, out string error)
        {
            if (data == null) { error = "Choose a room first."; return false; }
            if (data.roomType == DungeonRoomType.Path) return CanPlacePath(position, out error);
            if (data.roomType == DungeonRoomType.Entrance || data.roomType == DungeonRoomType.DungeonCore)
            {
                error = "Entrance and Dungeon Core are placed automatically.";
                return false;
            }
            if (inventoryManager == null)
            {
                inventoryManager = GetComponent<RoomInventoryManager>() ?? FindAnyObjectByType<RoomInventoryManager>();
            }
            if (inventoryManager == null)
            {
                error = "Room Inventory Manager is not configured.";
                return false;
            }
            if (!inventoryManager.CanPlace(data.roomId, out error)) return false;

            DungeonRoomInstanceData candidate = CreateInstance(data, position, rotation);
            return validator.ValidatePlacement(layout, candidate, ignoredInstanceId, out error);
        }

        public bool CanMove(string instanceId, Vector2Int destination, out string error)
        {
            DungeonRoomInstanceData existing = FindById(instanceId);
            if (existing == null) { error = "Select a room to move."; return false; }
            DungeonRoomInstanceData moved = Clone(existing);
            moved.position = destination;
            return validator.ValidatePlacement(layout, moved, existing.instanceId, out error);
        }

        public bool CanRotate(string instanceId, out string error)
        {
            DungeonRoomInstanceData existing = FindById(instanceId);
            if (existing == null) { error = "Select a room to rotate."; return false; }
            DungeonRoomInstanceData rotated = Clone(existing);
            rotated.rotation = (rotated.rotation + 90) % 360;
            return validator.ValidatePlacement(layout, rotated, existing.instanceId, out error);
        }

        public DungeonRoomInstanceData FindRoom(string instanceId) => FindById(instanceId);

        public bool TryPlace(RoomData data, Vector2Int position, int rotation, out string error)
        {
            if (data != null && data.roomType == DungeonRoomType.Path) return TryPlacePath(position, out error);
            if (!CanPlace(data, position, rotation, null, out error)) return false;
            DungeonRoomInstanceData instance = CreateInstance(data, position, rotation);
            SaveUndoPoint();
            layout.rooms.Add(instance);
            RefreshLayout();
            RoomPlaced?.Invoke(instance);

            int currentDungeonLevel = DungeonProgressionManager.Instance != null
                ? DungeonProgressionManager.Instance.CurrentLevel
                : 1;
            int currentRoomCount = layout != null && layout.rooms != null ? layout.rooms.Count : 0;
            AnalyticsManager.Instance?.TrackDungeonCreated(
                currentDungeonLevel,
                currentRoomCount,
                AnalyticsEventSource.Gameplay,
                $"room:{instance.instanceId}");

            error = $"Placed {data.roomName}.";
            return true;
        }

        public bool CanPlacePath(Vector2Int position, out string error)
        {
            if (layout == null || validator == null) { error = "Dungeon grid is not ready."; return false; }
            DungeonRoomInstanceData path = CreatePathInstance(position);
            return validator.ValidatePlacement(layout, path, null, out error);
        }

        public bool TryPlacePath(Vector2Int position, out string error)
        {
            if (!CanPlacePath(position, out error)) return false;
            SaveUndoPoint();
            layout.rooms.Add(CreatePathInstance(position));
            RefreshLayout();
            error = "Path tile placed. No Gold or room inventory used.";
            return true;
        }

        public bool TryMove(string instanceId, Vector2Int destination, out string error)
        {
            DungeonRoomInstanceData existing = FindById(instanceId);
            if (existing == null) { error = "Select a room to move."; return false; }
            if (existing.roomType == DungeonRoomType.Entrance || existing.roomType == DungeonRoomType.DungeonCore)
            {
                error = "Entrance and Dungeon Core cannot be moved.";
                return false;
            }

            DungeonRoomInstanceData moved = Clone(existing);
            moved.position = destination;
            if (!validator.ValidatePlacement(layout, moved, existing.instanceId, out error)) return false;
            SaveUndoPoint();
            Replace(existing, moved);
            RefreshLayout();
            error = "Room moved.";
            return true;
        }

        public bool TryRotate(string instanceId, out string error)
        {
            DungeonRoomInstanceData existing = FindById(instanceId);
            if (existing == null) { error = "Select a room to rotate."; return false; }
            if (existing.roomType == DungeonRoomType.Entrance || existing.roomType == DungeonRoomType.DungeonCore)
            {
                error = "Entrance and Dungeon Core cannot be rotated.";
                return false;
            }

            DungeonRoomInstanceData rotated = Clone(existing);
            rotated.rotation = (rotated.rotation + 90) % 360;
            if (!validator.ValidatePlacement(layout, rotated, existing.instanceId, out error)) return false;
            SaveUndoPoint();
            Replace(existing, rotated);
            RefreshLayout();
            error = "Room rotated 90 degrees.";
            return true;
        }

        public bool TryRemove(string instanceId, out string error)
        {
            DungeonRoomInstanceData room = FindById(instanceId);
            if (!validator.ValidateRemoval(layout, room, out error)) return false;
            if (HasUnitInRoom(layout, instanceId)) { error = "Remove the room's monsters and traps before removing this room."; return false; }
            SaveUndoPoint();
            layout.rooms.Remove(room);
            RefreshLayout();
            error = "Room removed.";
            return true;
        }

        public bool TrySetUpgradeLevel(string instanceId, int level, out string error)
        {
            DungeonRoomInstanceData room = FindById(instanceId);
            if (room == null) { error = "Placed room instance was not found."; return false; }
            if (level < 1) { error = "Room level must be at least 1."; return false; }
            RoomData definition = roomDatabase != null ? roomDatabase.FindById(room.roomId) : GetRoomData(room.roomType);
            if (definition == null || level > definition.maxLevel) { error = "Room level exceeds its configured maximum."; return false; }
            room.upgradeLevel = level;
            RefreshLayout();
            error = string.Empty;
            return true;
        }

        public bool TryPlaceMonster(MonsterInstanceData instance, int capacity, out string error)
        {
            DungeonRoomInstanceData room = FindById(instance?.roomInstanceId);
            if (room == null || room.roomType != DungeonRoomType.MonsterRoom) { error = "Monsters can only be placed inside Monster Rooms."; return false; }
            if (instance == null || string.IsNullOrWhiteSpace(instance.instanceId) || FindMonster(instance.instanceId) != null) { error = "Monster instance ID is missing or already used."; return false; }
            if (!IsLocalPositionValid(instance.localPosition)) { error = "Monster position must stay within its room."; return false; }
            if (monsterInventoryManager == null) { error = "Monster Inventory Manager is not configured."; return false; }
            if (!monsterInventoryManager.CanPlace(instance.monsterId, out error)) return false;
            if (CountMonstersInRoom(room.instanceId) >= Mathf.Max(1, capacity)) { error = "This Monster Room is full."; return false; }
            if (HasMonsterAt(room.instanceId, instance.localPosition, null)) { error = "Another monster already occupies that position."; return false; }
            SaveUndoPoint(); layout.monsters.Add(instance); RefreshLayout(); MonsterPlaced?.Invoke(instance); error = string.Empty; return true;
        }

        public bool TryRemoveMonster(string instanceId, out string error)
        {
            MonsterInstanceData instance = FindMonster(instanceId);
            if (instance == null) { error = "Placed monster was not found."; return false; }
            SaveUndoPoint(); layout.monsters.Remove(instance); RefreshLayout(); error = string.Empty; return true;
        }

        public bool TryMoveMonster(string instanceId, string roomInstanceId, Vector2 localPosition, int capacity, out string error)
        {
            MonsterInstanceData instance = FindMonster(instanceId);
            DungeonRoomInstanceData room = FindById(roomInstanceId);
            if (instance == null) { error = "Placed monster was not found."; return false; }
            if (room == null || room.roomType != DungeonRoomType.MonsterRoom) { error = "Choose a Monster Room as the destination."; return false; }
            if (!IsLocalPositionValid(localPosition)) { error = "Monster position must stay within its room."; return false; }
            int count = CountMonstersInRoom(roomInstanceId);
            if (instance.roomInstanceId != roomInstanceId && count >= Mathf.Max(1, capacity)) { error = "This Monster Room is full."; return false; }
            if (HasMonsterAt(roomInstanceId, localPosition, instanceId)) { error = "Another monster already occupies that position."; return false; }
            SaveUndoPoint(); instance.roomInstanceId = roomInstanceId; instance.localPosition = localPosition; RefreshLayout(); error = string.Empty; return true;
        }

        public bool TrySetMonsterLevel(string instanceId, int level, int maxLevel, out string error)
            => TrySetMonsterLevel(instanceId, level, maxLevel, 0, 0, out error);

        public bool TrySetMonsterLevel(string instanceId, int level, int maxLevel, int undoGoldDelta, int undoSoulDelta, out string error)
        {
            MonsterInstanceData instance = FindMonster(instanceId);
            if (instance == null) { error = "Placed monster was not found."; return false; }
            if (level < 1 || level > maxLevel) { error = "Monster level exceeds its configured maximum."; return false; }
            SaveUndoPoint(undoGoldDelta, undoSoulDelta); instance.level = level; RefreshLayout(); error = string.Empty; return true;
        }

        public bool TryPlaceTrap(TrapInstanceData instance, int capacity, out string error)
        {
            DungeonRoomInstanceData room = FindById(instance?.roomInstanceId);
            if (room == null || room.roomType != DungeonRoomType.TrapRoom) { error = "Traps can only be placed inside Trap Rooms."; return false; }
            if (instance == null || string.IsNullOrWhiteSpace(instance.instanceId) || FindTrap(instance.instanceId) != null) { error = "Trap instance ID is missing or already used."; return false; }
            if (!IsLocalPositionValid(instance.localPosition)) { error = "Trap position must stay within its room."; return false; }
            if (trapInventoryManager == null) { error = "Trap Inventory Manager is not configured."; return false; }
            if (!trapInventoryManager.CanPlace(instance.trapId, out error)) return false;
            if (CountTrapsInRoom(room.instanceId) >= Mathf.Max(1, capacity)) { error = "This Trap Room is full."; return false; }
            if (HasTrapAt(room.instanceId, instance.localPosition, null)) { error = "Another trap already occupies that position."; return false; }
            SaveUndoPoint(); layout.traps.Add(instance); RefreshLayout(); TrapPlaced?.Invoke(instance); error = string.Empty; return true;
        }

        public bool TryRemoveTrap(string instanceId, out string error)
        {
            TrapInstanceData instance = FindTrap(instanceId);
            if (instance == null) { error = "Placed trap was not found."; return false; }
            SaveUndoPoint(); layout.traps.Remove(instance); RefreshLayout(); error = string.Empty; return true;
        }

        public bool TryMoveTrap(string instanceId, string roomInstanceId, Vector2 localPosition, int capacity, out string error)
        {
            TrapInstanceData instance = FindTrap(instanceId);
            DungeonRoomInstanceData room = FindById(roomInstanceId);
            if (instance == null) { error = "Placed trap was not found."; return false; }
            if (room == null || room.roomType != DungeonRoomType.TrapRoom) { error = "Choose a Trap Room as the destination."; return false; }
            if (!IsLocalPositionValid(localPosition)) { error = "Trap position must stay within its room."; return false; }
            int count = CountTrapsInRoom(roomInstanceId);
            if (instance.roomInstanceId != roomInstanceId && count >= Mathf.Max(1, capacity)) { error = "This Trap Room is full."; return false; }
            if (HasTrapAt(roomInstanceId, localPosition, instanceId)) { error = "Another trap already occupies that position."; return false; }
            SaveUndoPoint(); instance.roomInstanceId = roomInstanceId; instance.localPosition = localPosition; RefreshLayout(); error = string.Empty; return true;
        }

        public bool TrySetTrapLevel(string instanceId, int level, int maxLevel, out string error)
            => TrySetTrapLevel(instanceId, level, maxLevel, 0, 0, out error);

        public bool TrySetTrapLevel(string instanceId, int level, int maxLevel, int undoGoldDelta, int undoSoulDelta, out string error)
        {
            TrapInstanceData instance = FindTrap(instanceId);
            if (instance == null) { error = "Placed trap was not found."; return false; }
            if (level < 1 || level > maxLevel) { error = "Trap level exceeds its configured maximum."; return false; }
            SaveUndoPoint(undoGoldDelta, undoSoulDelta); instance.level = level; RefreshLayout(); error = string.Empty; return true;
        }

        public bool SetGridDimensions(int newWidth, int newHeight, out string error)
        {
            if (newWidth < 9 || newHeight < 9)
            {
                error = "Grid dimensions cannot be smaller than 9x9.";
                return false;
            }

            if (layout?.rooms != null)
            {
                foreach (var room in layout.rooms)
                {
                    int w = room.rotation % 180 == 0 ? room.width : room.height;
                    int h = room.rotation % 180 == 0 ? room.height : room.width;
                    if (room.position.x + w > newWidth || room.position.y + h > newHeight)
                    {
                        error = $"Cannot resize grid: Room '{room.roomId}' is outside the new bounds ({newWidth}x{newHeight}).";
                        return false;
                    }
                }
            }

            SaveUndoPoint();
            gridWidth = newWidth;
            gridHeight = newHeight;
            if (layout != null)
            {
                layout.gridWidth = newWidth;
                layout.gridHeight = newHeight;
            }

            if (grid != null)
            {
                grid.RebuildGrid(newWidth, newHeight);
            }

            RefreshLayout();
            error = string.Empty;
            return true;
        }

        public bool TryPlaceBoss(BossInstanceData instance, out string error)
        {
            DungeonRoomInstanceData room = FindById(instance?.roomInstanceId);
            if (room == null || room.roomType != DungeonRoomType.BossRoom) { error = "Bosses can only be placed inside Boss Rooms."; return false; }
            if (instance == null || string.IsNullOrWhiteSpace(instance.instanceId) || FindBoss(instance.instanceId) != null) { error = "Boss instance ID is missing or already used."; return false; }
            if (!IsLocalPositionValid(instance.localPosition)) { error = "Boss position must stay within its room."; return false; }
            if (CountBossesInRoom(room.instanceId) >= 1) { error = "Boss Room can only hold 1 Boss."; return false; }
            SaveUndoPoint();
            if (layout.bosses == null) layout.bosses = new List<BossInstanceData>();
            layout.bosses.Add(instance);
            RefreshLayout();
            BossPlaced?.Invoke(instance);
            error = string.Empty;
            return true;
        }

        public bool TryRemoveBoss(string instanceId, out string error)
        {
            BossInstanceData instance = FindBoss(instanceId);
            if (instance == null) { error = "Placed boss was not found."; return false; }
            SaveUndoPoint();
            layout.bosses.Remove(instance);
            RefreshLayout();
            error = string.Empty;
            return true;
        }

        public bool TryMoveBoss(string instanceId, string roomInstanceId, Vector2 localPosition, out string error)
        {
            BossInstanceData instance = FindBoss(instanceId);
            DungeonRoomInstanceData room = FindById(roomInstanceId);
            if (instance == null) { error = "Placed boss was not found."; return false; }
            if (room == null || room.roomType != DungeonRoomType.BossRoom) { error = "Choose a Boss Room as the destination."; return false; }
            if (!IsLocalPositionValid(localPosition)) { error = "Boss position must stay within its room."; return false; }
            if (instance.roomInstanceId != roomInstanceId && CountBossesInRoom(roomInstanceId) >= 1) { error = "Boss Room can only hold 1 Boss."; return false; }
            SaveUndoPoint();
            instance.roomInstanceId = roomInstanceId;
            instance.localPosition = localPosition;
            RefreshLayout();
            error = string.Empty;
            return true;
        }

        public MonsterInstanceData FindMonster(string id) => FindByInstanceId(layout?.monsters, id);
        public TrapInstanceData FindTrap(string id) => FindByInstanceId(layout?.traps, id);
        public BossInstanceData FindBoss(string id) => FindByInstanceId(layout?.bosses, id);
        public int CountMonstersInRoom(string roomId) => CountInRoom(layout?.monsters, roomId);
        public int CountTrapsInRoom(string roomId) => CountInRoom(layout?.traps, roomId);
        public int CountBossesInRoom(string roomId) => CountInRoom(layout?.bosses, roomId);
        public BossInstanceData GetBossAtRoom(string roomInstanceId)
        {
            if (layout?.bosses == null || string.IsNullOrEmpty(roomInstanceId)) return null;
            return layout.bosses.Find(b => b.roomInstanceId == roomInstanceId);
        }

        public string LayoutToJson() => layout != null ? layout.ToJson() : string.Empty;

        public bool LoadLayoutJson(string json, out string error)
        {
            DungeonLayoutData restored;
            try { restored = DungeonLayoutData.FromJson(json); }
            catch (Exception exception) { error = $"Dungeon layout could not be read: {exception.Message}"; return false; }
            if (restored.gridWidth >= 9 && restored.gridHeight >= 9 && (restored.gridWidth != gridWidth || restored.gridHeight != gridHeight))
            {
                gridWidth = restored.gridWidth;
                gridHeight = restored.gridHeight;
                if (grid != null) grid.RebuildGrid(gridWidth, gridHeight);
            }
            if (restored.gridWidth != gridWidth || restored.gridHeight != gridHeight)
            { error = "Saved dungeon dimensions do not match the current grid."; return false; }

            var instanceIds = new HashSet<string>();
            var occupiedCells = new HashSet<Vector2Int>();
            foreach (DungeonRoomInstanceData room in restored.rooms)
            {
                if (room == null || string.IsNullOrWhiteSpace(room.instanceId) || !instanceIds.Add(room.instanceId))
                { error = "Saved layout contains a missing or duplicate room instance ID."; return false; }
                RoomData definition = roomDatabase != null ? roomDatabase.FindById(room.roomId) : null;
                if (definition == null || definition.roomType != room.roomType)
                { error = $"Saved room definition '{room.roomId}' is missing or has changed type."; return false; }
                if (room.upgradeLevel < 1 || room.upgradeLevel > definition.maxLevel)
                { error = $"Saved upgrade level for '{room.roomId}' is invalid."; return false; }
                int width = room.rotation % 180 == 0 ? room.width : room.height;
                int height = room.rotation % 180 == 0 ? room.height : room.width;
                if (room.width < 1 || room.height < 1 || room.position.x < 0 || room.position.y < 0 ||
                    room.position.x + width > gridWidth || room.position.y + height > gridHeight)
                { error = $"Saved room '{room.roomId}' is outside the dungeon grid."; return false; }
                for (int y = room.position.y; y < room.position.y + height; y++)
                    for (int x = room.position.x; x < room.position.x + width; x++)
                        if (!occupiedCells.Add(new Vector2Int(x, y)))
                        { error = "Saved layout contains overlapping room instances."; return false; }
                room.isPlaced = true;
            }

            var unitIds = new HashSet<string>();
            var monsterPositions = new HashSet<string>();
            var trapPositions = new HashSet<string>();
            var monsterCounts = new Dictionary<string, int>();
            var trapCounts = new Dictionary<string, int>();
            foreach (MonsterInstanceData unit in restored.monsters)
            {
                DungeonRoomInstanceData room = unit != null ? restored.rooms.Find(r => r.instanceId == unit.roomInstanceId) : null;
                MonsterData definition = unit != null && monsterDatabase != null ? monsterDatabase.FindById(unit.monsterId) : null;
                if (unit == null || string.IsNullOrWhiteSpace(unit.instanceId) || !unitIds.Add(unit.instanceId) || string.IsNullOrWhiteSpace(unit.monsterId) || unit.level < 1 ||
                    definition == null || unit.level > definition.maxLevel || room == null || room.roomType != DungeonRoomType.MonsterRoom || !IsLocalPositionValid(unit.localPosition))
                { error = "Saved layout contains an invalid monster instance or room reference."; return false; }
                if (!monsterPositions.Add(PositionKey(unit.roomInstanceId, unit.localPosition))) { error = "Saved layout contains overlapping monster positions."; return false; }
                monsterCounts.TryGetValue(room.instanceId, out int count); monsterCounts[room.instanceId] = ++count;
                if (count > UnitRoomCapacity(room)) { error = "Saved Monster Room exceeds its capacity."; return false; }
            }
            foreach (TrapInstanceData unit in restored.traps)
            {
                DungeonRoomInstanceData room = unit != null ? restored.rooms.Find(r => r.instanceId == unit.roomInstanceId) : null;
                TrapData definition = unit != null && trapDatabase != null ? trapDatabase.FindById(unit.trapId) : null;
                if (unit == null || string.IsNullOrWhiteSpace(unit.instanceId) || !unitIds.Add(unit.instanceId) || definition == null || unit.level < 1 || unit.level > definition.maxLevel ||
                    room == null || room.roomType != DungeonRoomType.TrapRoom || !IsLocalPositionValid(unit.localPosition))
                { error = "Saved layout contains an invalid trap instance or room reference."; return false; }
                if (!trapPositions.Add(PositionKey(unit.roomInstanceId, unit.localPosition))) { error = "Saved layout contains overlapping trap positions."; return false; }
                trapCounts.TryGetValue(room.instanceId, out int count); trapCounts[room.instanceId] = ++count;
                if (count > UnitRoomCapacity(room)) { error = "Saved Trap Room exceeds its capacity."; return false; }
            }

            if (!HasExactlyOne(restored, DungeonRoomType.Entrance) || !HasExactlyOne(restored, DungeonRoomType.DungeonCore))
            { error = "Saved layout must contain exactly one Entrance and one Dungeon Core."; return false; }
            if (!pathfinder.TryFindRoute(restored, out _))
            { error = "Saved layout does not connect Entrance to Dungeon Core."; return false; }

            layout = restored;
            undoStack.Clear();
            redoStack.Clear();
            RefreshLayout();
            error = string.Empty;
            return true;
        }

        public bool TryUndo(out string message)
        {
            if (undoStack.Count == 0) { message = "Nothing to undo."; return false; }
            HistoryEntry target = undoStack.Peek();
            if (currencyManager != null && !currencyManager.TryApplyDelta(target.undoGoldDelta, target.undoSoulDelta, 0, out message)) return false;
            undoStack.Pop();
            redoStack.Push(Capture(-target.undoGoldDelta, -target.undoSoulDelta));
            Restore(target);
            message = "Build action undone.";
            return true;
        }

        public bool TryRedo(out string message)
        {
            if (redoStack.Count == 0) { message = "Nothing to redo."; return false; }
            HistoryEntry target = redoStack.Peek();
            if (currencyManager != null && !currencyManager.TryApplyDelta(target.undoGoldDelta, target.undoSoulDelta, 0, out message)) return false;
            redoStack.Pop();
            undoStack.Push(Capture(-target.undoGoldDelta, -target.undoSoulDelta));
            Restore(target);
            message = "Build action restored.";
            return true;
        }

        private void CreateInitialLayout()
        {
            AddInitial(DungeonRoomType.Entrance, "entrance", new Vector2Int(1, 4), 1, 1);
            AddInitial(DungeonRoomType.DungeonCore, "dungeon_core", new Vector2Int(7, 4), 1, 1);
            for (int x = 2; x <= 6; x++) AddInitial(DungeonRoomType.Path, "path", new Vector2Int(x, 4), 1, 1);
        }

        private void AddInitial(DungeonRoomType type, string roomId, Vector2Int position, int width, int height)
        {
            layout.rooms.Add(new DungeonRoomInstanceData
            {
                instanceId = Guid.NewGuid().ToString("N"), roomId = roomId, roomType = type,
                position = position, width = width, height = height, rotation = 0
            });
        }

        private static DungeonRoomInstanceData CreateInstance(RoomData data, Vector2Int position, int rotation) => new DungeonRoomInstanceData
        {
            instanceId = Guid.NewGuid().ToString("N"), roomId = data.roomId, roomType = data.roomType,
            position = position, width = Mathf.Max(1, data.width), height = Mathf.Max(1, data.height), rotation = ((rotation % 360) + 360) % 360,
            upgradeLevel = 1, isPlaced = true
        };

        private static DungeonRoomInstanceData CreatePathInstance(Vector2Int position) => new DungeonRoomInstanceData
        {
            instanceId = Guid.NewGuid().ToString("N"), roomId = "path", roomType = DungeonRoomType.Path,
            position = position, width = 1, height = 1, rotation = 0
        };

        private void RefreshLayout()
        {
            if (layout.monsters == null) layout.monsters = new List<MonsterInstanceData>();
            if (layout.traps == null) layout.traps = new List<TrapInstanceData>();
            if (layout.bosses == null) layout.bosses = new List<BossInstanceData>();
            grid.RefreshCellStates(layout);
            LayoutChanged?.Invoke();
        }

        private static T FindByInstanceId<T>(List<T> instances, string id) where T : class
        {
            if (instances == null || string.IsNullOrEmpty(id)) return null;
            foreach (T item in instances)
            {
                if (item is MonsterInstanceData monster && monster.instanceId == id) return item;
                if (item is TrapInstanceData trap && trap.instanceId == id) return item;
                if (item is BossInstanceData boss && boss.instanceId == id) return item;
            }
            return null;
        }

        private static int CountInRoom<T>(List<T> instances, string roomId)
        {
            int count = 0; if (instances == null) return count;
            foreach (T item in instances)
            {
                if (item is MonsterInstanceData monster && monster.roomInstanceId == roomId) count++;
                if (item is TrapInstanceData trap && trap.roomInstanceId == roomId) count++;
                if (item is BossInstanceData boss && boss.roomInstanceId == roomId) count++;
            }
            return count;
        }

        private bool HasMonsterAt(string roomId, Vector2 position, string ignoredId)
        {
            if (layout.monsters == null) return false;
            foreach (MonsterInstanceData item in layout.monsters)
                if (item.roomInstanceId == roomId && item.instanceId != ignoredId && Vector2.Distance(item.localPosition, position) < .001f) return true;
            return false;
        }
        private bool HasTrapAt(string roomId, Vector2 position, string ignoredId)
        {
            if (layout.traps == null) return false;
            foreach (TrapInstanceData item in layout.traps)
                if (item.roomInstanceId == roomId && item.instanceId != ignoredId && Vector2.Distance(item.localPosition, position) < .001f) return true;
            return false;
        }
        private static bool IsLocalPositionValid(Vector2 p) => p.x >= -.5f && p.x <= .5f && p.y >= -.5f && p.y <= .5f;
        private static string PositionKey(string roomId, Vector2 position) => $"{roomId}:{position.x:R}:{position.y:R}";
        private int UnitRoomCapacity(DungeonRoomInstanceData room)
        {
            RoomData data = room != null ? (roomDatabase != null ? roomDatabase.FindById(room.roomId) : GetRoomData(room.roomType)) : null;
            return data != null && data.maxCapacity > 0 ? Mathf.Min(3, data.maxCapacity) : 3;
        }
        private static bool HasUnitInRoom(DungeonLayoutData data, string roomId)
        {
            if (data.monsters != null) foreach (MonsterInstanceData unit in data.monsters) if (unit.roomInstanceId == roomId) return true;
            if (data.traps != null) foreach (TrapInstanceData unit in data.traps) if (unit.roomInstanceId == roomId) return true;
            if (data.bosses != null) foreach (BossInstanceData unit in data.bosses) if (unit.roomInstanceId == roomId) return true;
            return false;
        }

        private void SaveUndoPoint(int undoGoldDelta = 0, int undoSoulDelta = 0)
        {
            undoStack.Push(Capture(undoGoldDelta, undoSoulDelta));
            redoStack.Clear();
        }

        private HistoryEntry Capture(int undoGoldDelta = 0, int undoSoulDelta = 0) => new HistoryEntry { json = layout.ToJson(), undoGoldDelta = undoGoldDelta, undoSoulDelta = undoSoulDelta };

        private void Restore(HistoryEntry entry)
        {
            layout = DungeonLayoutData.FromJson(entry.json);
            RefreshLayout();
        }

        private DungeonRoomInstanceData FindById(string id)
        {
            if (string.IsNullOrEmpty(id)) return null;
            foreach (DungeonRoomInstanceData room in layout.rooms)
                if (room.instanceId == id) return room;
            return null;
        }

        private void Replace(DungeonRoomInstanceData oldValue, DungeonRoomInstanceData newValue)
        {
            int index = layout.rooms.IndexOf(oldValue);
            layout.rooms[index] = newValue;
        }

        private static bool HasExactlyOne(DungeonLayoutData source, DungeonRoomType type)
        {
            int matches = 0;
            foreach (DungeonRoomInstanceData room in source.rooms)
                if (room.roomType == type && ++matches > 1) return false;
            return matches == 1;
        }

        private static DungeonRoomInstanceData Clone(DungeonRoomInstanceData room) => new DungeonRoomInstanceData
        {
            instanceId = room.instanceId, roomId = room.roomId, roomType = room.roomType,
            position = room.position, rotation = room.rotation, width = room.width, height = room.height,
            upgradeLevel = room.upgradeLevel, isPlaced = room.isPlaced
        };

        [Serializable]
        private sealed class HistoryEntry
        {
            public string json;
            public int undoGoldDelta;
            public int undoSoulDelta;
        }
    }
}
