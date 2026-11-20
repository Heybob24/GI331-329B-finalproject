using System;
using System.Collections.Generic;
using UnityEngine;

namespace DreamDungeon
{
    public sealed class BossPlacementManager : MonoBehaviour
    {
        [SerializeField] private DungeonGrid grid;
        [SerializeField] private RoomManager roomManager;
        [SerializeField] private BossDatabase database;
        [SerializeField] private RoomPlacement roomPlacement;

        private string selectedBossId;
        private string movingInstanceId;
        private readonly List<GameObject> visuals = new List<GameObject>();

        public event Action<string> StatusChanged;
        public bool HasPendingPlacement => !string.IsNullOrEmpty(selectedBossId) || !string.IsNullOrEmpty(movingInstanceId);

        public void Configure(DungeonGrid g, RoomManager rooms, BossDatabase db, RoomPlacement roomPlace = null)
        {
            grid = g;
            roomManager = rooms;
            database = db;
            roomPlacement = roomPlace;
        }

        private void Awake()
        {
            if (grid == null) grid = GetComponent<DungeonGrid>();
            if (roomManager == null) roomManager = GetComponent<RoomManager>();
            if (roomPlacement == null) roomPlacement = GetComponent<RoomPlacement>();
            if (database == null) database = Resources.Load<BossDatabase>("DreamDungeon/BossDatabase");
        }

        private bool subscribed;
        private void OnEnable() => Subscribe();
        private void Start() { Subscribe(); RefreshVisuals(); }

        private void Subscribe()
        {
            if (subscribed || grid == null || roomManager == null) return;
            grid.CellSelected += OnCellSelected;
            roomManager.LayoutChanged += RefreshVisuals;
            subscribed = true;
        }

        private void OnDisable()
        {
            if (!subscribed) return;
            grid.CellSelected -= OnCellSelected;
            roomManager.LayoutChanged -= RefreshVisuals;
            subscribed = false;
        }

        private void OnDestroy()
        {
            ClearVisuals();
        }

        public bool SelectBoss(string id)
        {
            if (database == null)
            {
                database = Resources.Load<BossDatabase>("DreamDungeon/BossDatabase");
            }

            BossData boss = database != null ? database.GetBoss(id) : null;
            if (boss == null)
            {
                Report("Boss data was not found.");
                return false;
            }

            int currentLevel = DungeonProgressionManager.Instance != null ? DungeonProgressionManager.Instance.CurrentLevel : 10;
            if (DungeonUnlockManager.Instance != null && !DungeonUnlockManager.Instance.IsBossUnlocked(id, currentLevel))
            {
                Report($"{boss.bossName} unlocks at Dungeon Level {boss.unlockDungeonLevel}.");
                return false;
            }

            selectedBossId = id;
            movingInstanceId = null;
            roomPlacement?.SetExternalUnitPlacementActive(true);
            Report($"Selected {boss.bossName}. Tap a Boss Room to place it.");
            return true;
        }

        public void BeginMove(string id)
        {
            if (roomManager?.FindBoss(id) == null)
            {
                Report("Placed boss was not found.");
                return;
            }
            selectedBossId = null;
            movingInstanceId = id;
            roomPlacement?.SetExternalUnitPlacementActive(true);
            Report("Tap a Boss Room to move this boss.");
        }

        public bool Remove(string id, out string error)
        {
            bool result = roomManager.TryRemoveBoss(id, out error);
            Report(result ? "Boss removed from room." : error);
            return result;
        }

        private void OnCellSelected(DungeonCell cell)
        {
            if (cell == null || (string.IsNullOrEmpty(selectedBossId) && string.IsNullOrEmpty(movingInstanceId))) return;

            DungeonRoomInstanceData room = roomManager.GetRoomAt(cell.Position);
            if (room == null || room.roomType != DungeonRoomType.BossRoom)
            {
                Report("Bosses can only be placed inside a Boss Room.");
                return;
            }

            if (!string.IsNullOrEmpty(movingInstanceId))
            {
                if (roomManager.TryMoveBoss(movingInstanceId, room.instanceId, Vector2.zero, out string moveErr))
                {
                    movingInstanceId = null;
                    roomPlacement?.SetExternalUnitPlacementActive(false);
                    Report("Boss moved.");
                }
                else
                {
                    Report(moveErr);
                }
                return;
            }

            if (roomManager.CountBossesInRoom(room.instanceId) >= 1)
            {
                Report("This Boss Room is already occupied.");
                return;
            }

            var instance = new BossInstanceData
            {
                instanceId = Guid.NewGuid().ToString("N"),
                bossId = selectedBossId,
                roomInstanceId = room.instanceId,
                localPosition = Vector2.zero,
                level = 1
            };

            if (roomManager.TryPlaceBoss(instance, out string placeErr))
            {
                BossData boss = database != null ? database.GetBoss(selectedBossId) : null;
                roomPlacement?.SetExternalUnitPlacementActive(false);
                selectedBossId = null;
                Report($"Placed {boss?.bossName ?? "Boss"}.");
            }
            else
            {
                Report(placeErr);
            }
        }

        private void RefreshVisuals()
        {
            ClearVisuals();
            if (roomManager?.Layout?.bosses == null || grid == null) return;

            if (database == null)
            {
                database = Resources.Load<BossDatabase>("DreamDungeon/BossDatabase");
            }

            foreach (BossInstanceData unit in roomManager.Layout.bosses)
            {
                BossData data = database != null ? database.GetBoss(unit.bossId) : null;
                DungeonRoomInstanceData room = roomManager.FindRoom(unit.roomInstanceId);
                if (room == null) continue;

                int width = room.rotation % 180 == 0 ? room.width : room.height;
                int height = room.rotation % 180 == 0 ? room.height : room.width;
                Vector3 center = grid.CellToWorld(room.position) + new Vector3((width - 1) * grid.CellSize * 0.5f, (height - 1) * grid.CellSize * 0.5f, -0.3f);
                Vector3 world = center + new Vector3(unit.localPosition.x * width * grid.CellSize, unit.localPosition.y * height * grid.CellSize, 0f);

                GameObject visual = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                visual.name = $"Boss_{unit.bossId}_{unit.instanceId}";
                visual.transform.SetParent(transform, true);
                visual.transform.position = world;
                visual.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
                visual.transform.localScale = new Vector3(grid.CellSize * 0.55f, 0.05f, grid.CellSize * 0.55f);

                foreach (Collider col in visual.GetComponentsInChildren<Collider>()) col.enabled = false;
                foreach (Collider2D col in visual.GetComponentsInChildren<Collider2D>()) col.enabled = false;

                if (visual.GetComponentInChildren<Renderer>() is Renderer renderer)
                {
                    renderer.material.color = data != null ? data.bossColor : new Color(0.85f, 0.2f, 0.2f);
                }

                visuals.Add(visual);
            }
        }

        private void ClearVisuals()
        {
            foreach (GameObject visual in visuals)
            {
                if (visual != null) Destroy(visual);
            }
            visuals.Clear();
        }

        private void Report(string message) => StatusChanged?.Invoke(message);
    }
}
