using UnityEngine;
using UnityEngine.UI;

namespace DreamDungeon
{
    public sealed class RoomDetailsUI : MonoBehaviour
    {
        [SerializeField] private RoomDatabase database;
        [SerializeField] private RoomManager roomManager;
        [SerializeField] private RoomPlacement placement;
        [SerializeField] private RoomUpgradeManager upgrades;
        [SerializeField] private RectTransform safeAreaRoot;

        private RectTransform overlay;
        private Image iconImage;
        private Text titleText;
        private Text bodyText;
        private Text statusText;
        private Button upgradeButton;
        private string currentInstanceId;

        public void Configure(RectTransform safeArea, RoomDatabase roomDatabase, RoomManager manager,
            RoomPlacement roomPlacement, RoomUpgradeManager upgradeManager)
        {
            safeAreaRoot = safeArea;
            database = roomDatabase;
            roomManager = manager;
            placement = roomPlacement;
            upgrades = upgradeManager;
        }

        private void Awake()
        {
            if (database == null) database = FindAnyObjectByType<RoomDatabase>();
            if (roomManager == null) roomManager = FindAnyObjectByType<RoomManager>();
            if (placement == null) placement = FindAnyObjectByType<RoomPlacement>();
            if (upgrades == null) upgrades = FindAnyObjectByType<RoomUpgradeManager>();
            if (safeAreaRoot == null)
            {
                GameObject safe = GameObject.Find("SafeAreaRoot");
                if (safe != null) safeAreaRoot = safe.GetComponent<RectTransform>();
            }
            Build();
            if (placement != null) placement.RoomFocused += Show;
            if (roomManager != null) roomManager.LayoutChanged += Refresh;
        }

        private void OnDestroy()
        {
            if (placement != null) placement.RoomFocused -= Show;
            if (roomManager != null) roomManager.LayoutChanged -= Refresh;
        }

        public void Show(DungeonRoomInstanceData instance)
        {
            if (instance == null || overlay == null) return;
            currentInstanceId = instance.instanceId;
            Refresh();
            overlay.gameObject.SetActive(true);
        }

        public void Close()
        {
            currentInstanceId = null;
            if (overlay != null) overlay.gameObject.SetActive(false);
        }

        private void Build()
        {
            if (safeAreaRoot == null || overlay != null) return;
            overlay = RoomUIFactory.CreateOverlay(safeAreaRoot, "RoomDetailsPanel");
            RectTransform window = RoomUIFactory.CreateWindow(overlay, "RoomDetailsWindow", new Vector2(930, 1120), new Color(.075f, .085f, .12f, 1f));
            RectTransform icon = RoomUIFactory.CreatePanel(window, "RoomDetailsIcon", new Color(.28f, .31f, .39f), new Vector2(0, 420), new Vector2(150, 150));
            iconImage = icon.GetComponent<Image>();
            titleText = RoomUIFactory.CreateText(window, "RoomDetailsName", "ROOM", 38, new Vector2(0, 300), new Vector2(780, 60), new Color(.96f, .78f, .43f));
            bodyText = RoomUIFactory.CreateText(window, "RoomDetailsBody", string.Empty, 22, new Vector2(0, 30), new Vector2(820, 450), Color.white);
            statusText = RoomUIFactory.CreateText(window, "RoomDetailsStatus", string.Empty, 18, new Vector2(0, -320), new Vector2(820, 55), new Color(.85f, .84f, .75f));

            upgradeButton = RoomUIFactory.CreateButton(window, "UpgradeRoomButton", "UPGRADE", new Vector2(-260, -410), new Vector2(250, 80), new Color(.42f, .29f, .12f), 20);
            Button move = RoomUIFactory.CreateButton(window, "MoveRoomButton", "MOVE", new Vector2(0, -410), new Vector2(200, 80), new Color(.16f, .30f, .36f), 20);
            Button rotate = RoomUIFactory.CreateButton(window, "RotateRoomButton", "ROTATE", new Vector2(250, -410), new Vector2(230, 80), new Color(.27f, .23f, .38f), 20);
            Button remove = RoomUIFactory.CreateButton(window, "RemoveRoomButton", "REMOVE", new Vector2(-125, -510), new Vector2(240, 72), new Color(.37f, .18f, .21f), 19);
            Button close = RoomUIFactory.CreateButton(window, "CloseRoomDetailsButton", "CLOSE", new Vector2(145, -510), new Vector2(210, 72), new Color(.20f, .22f, .27f), 19);
            upgradeButton.onClick.AddListener(Upgrade);
            move.onClick.AddListener(Move);
            rotate.onClick.AddListener(Rotate);
            remove.onClick.AddListener(Remove);
            close.onClick.AddListener(Close);
            overlay.gameObject.SetActive(false);
        }

        private void Refresh()
        {
            if (string.IsNullOrEmpty(currentInstanceId) || bodyText == null) return;
            DungeonRoomInstanceData instance = roomManager.FindRoom(currentInstanceId);
            if (instance == null) { Close(); return; }
            RoomData data = database != null ? database.FindById(instance.roomId) : null;
            if (data == null) { titleText.text = "UNKNOWN ROOM"; bodyText.text = instance.roomId; return; }
            titleText.text = data.roomName;
            iconImage.sprite = data.roomIcon;
            iconImage.color = data.roomIcon != null ? Color.white : RoomColor(data.roomType);
            int rotatedWidth = instance.rotation % 180 == 0 ? instance.width : instance.height;
            int rotatedHeight = instance.rotation % 180 == 0 ? instance.height : instance.width;
            int goldCost = 0;
            int soulCost = 0;
            string upgradeError = string.Empty;
            bool canUpgrade = upgrades != null && upgrades.CanUpgrade(instance.instanceId, out goldCost, out soulCost, out upgradeError);
            string upgradeInfo = canUpgrade
                ? $"Next upgrade: {goldCost} Gold, {soulCost} Soul"
                : (!string.IsNullOrEmpty(upgradeError) ? upgradeError : "This room cannot be upgraded.");
            bodyText.text = $"{data.description}\n\nLevel: {instance.upgradeLevel} / {data.maxLevel}\nGrid: ({instance.position.x + 1}, {instance.position.y + 1})\nSize: {rotatedWidth}×{rotatedHeight}\nCapacity: {data.maxCapacity}\n{upgradeInfo}";
            upgradeButton.interactable = canUpgrade;
            statusText.text = canUpgrade || data.maxLevel > instance.upgradeLevel ? string.Empty : "Maximum room level reached.";
        }

        private void Upgrade()
        {
            if (upgrades == null) { statusText.text = "Room Upgrade Manager is not configured."; return; }
            bool upgraded = upgrades.TryUpgrade(currentInstanceId, out string message);
            statusText.text = message;
            if (upgraded) Refresh();
        }

        private void Move()
        {
            if (placement != null && placement.BeginMove(currentInstanceId)) Close();
            else if (placement == null) statusText.text = "Room Placement is not configured.";
        }

        private void Rotate()
        {
            if (roomManager == null) { statusText.text = "Room Manager is not configured."; return; }
            roomManager.TryRotate(currentInstanceId, out string message);
            statusText.text = message;
            Refresh();
        }

        private void Remove()
        {
            if (roomManager == null) { statusText.text = "Room Manager is not configured."; return; }
            bool removed = roomManager.TryRemove(currentInstanceId, out string message);
            statusText.text = message;
            if (removed)
            {
                Close();
            }
        }

        private static Color RoomColor(DungeonRoomType type) => type switch
        {
            DungeonRoomType.MonsterRoom => new Color(.72f, .25f, .21f),
            DungeonRoomType.TreasureRoom => new Color(.88f, .71f, .19f),
            DungeonRoomType.TrapRoom => new Color(.35f, .45f, .33f),
            DungeonRoomType.MagicRoom => new Color(.38f, .28f, .75f),
            DungeonRoomType.BossRoom => new Color(.55f, .16f, .4f),
            DungeonRoomType.Entrance => new Color(.91f, .66f, .26f),
            DungeonRoomType.DungeonCore => new Color(.66f, .31f, .82f),
            _ => new Color(.48f, .43f, .38f)
        };
    }
}
