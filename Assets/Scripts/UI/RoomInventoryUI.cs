using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace DreamDungeon
{
    public sealed class RoomInventoryUI : MonoBehaviour
    {
        [SerializeField] private RoomInventoryManager inventory;
        [SerializeField] private RoomDatabase database;
        [SerializeField] private RoomPlacement placement;
        [SerializeField] private RectTransform safeAreaRoot;

        private RectTransform overlay;
        private Transform content;
        private Text statusText;

        public void Configure(RectTransform safeArea, RoomInventoryManager manager, RoomDatabase roomDatabase, RoomPlacement roomPlacement)
        {
            safeAreaRoot = safeArea;
            inventory = manager;
            database = roomDatabase;
            placement = roomPlacement;
        }

        private void Awake()
        {
            if (inventory == null) inventory = FindAnyObjectByType<RoomInventoryManager>();
            if (database == null && inventory != null) database = inventory.Database;
            if (placement == null) placement = FindAnyObjectByType<RoomPlacement>();
            if (safeAreaRoot == null)
            {
                GameObject safe = GameObject.Find("SafeAreaRoot");
                if (safe != null) safeAreaRoot = safe.GetComponent<RectTransform>();
            }
            Build();
            if (inventory != null) inventory.InventoryChanged += RefreshCards;
            if (placement != null) placement.StatusChanged += SetStatus;
        }

        private void OnDestroy()
        {
            if (inventory != null) inventory.InventoryChanged -= RefreshCards;
            if (placement != null) placement.StatusChanged -= SetStatus;
        }

        public void Open()
        {
            if (overlay == null) Build();
            overlay.gameObject.SetActive(true);
            RefreshCards();
        }

        public void Close()
        {
            if (overlay != null) overlay.gameObject.SetActive(false);
        }

        private void Build()
        {
            if (safeAreaRoot == null || overlay != null) return;
            overlay = RoomUIFactory.CreateOverlay(safeAreaRoot, "RoomInventoryPanel");
            RectTransform window = RoomUIFactory.CreateWindow(overlay, "RoomInventoryWindow", new Vector2(1000, 1660), new Color(.075f, .085f, .12f, 1f));
            RoomUIFactory.CreateText(window, "InventoryTitle", "ROOM INVENTORY", 40, new Vector2(-10, 760), new Vector2(750, 70), new Color(.96f, .78f, .43f));
            statusText = RoomUIFactory.CreateText(window, "InventoryStatus", "Available = owned rooms minus placed rooms.", 19, new Vector2(0, 680), new Vector2(900, 55), new Color(.82f, .85f, .9f));
            Button close = RoomUIFactory.CreateButton(window, "CloseInventoryButton", "CLOSE", new Vector2(405, 760), new Vector2(150, 70), new Color(.34f, .19f, .19f), 20);
            close.onClick.AddListener(Close);
            content = RoomUIFactory.CreateScrollList(window, "RoomInventoryScroll", new Vector2(0, -10), new Vector2(930, 1320));
            overlay.gameObject.SetActive(false);
            RefreshCards();
        }

        private void RefreshCards()
        {
            if (content == null || database == null || inventory == null) return;
            for (int i = content.childCount - 1; i >= 0; i--) Destroy(content.GetChild(i).gameObject);
            IReadOnlyList<RoomData> rooms = database.AllRooms;
            foreach (RoomData data in rooms)
            {
                if (data == null || !data.IsPurchasable || data.roomType == DungeonRoomType.TrapRoom) continue;
                CreateRoomCard(data);
            }
            Canvas.ForceUpdateCanvases();
        }

        private void CreateRoomCard(RoomData data)
        {
            GameObject card = RoomUIFactory.CreateCard(content, "InventoryCard_" + data.roomId, 190);
            Transform t = card.transform;
            Color tint = data.roomType == DungeonRoomType.MonsterRoom ? new Color(.72f, .25f, .21f) : new Color(.3f, .35f, .42f);
            RectTransform icon = RoomUIFactory.CreatePanel(t, "RoomIcon", tint, new Vector2(-360, 0), new Vector2(95, 95));
            if (data.roomIcon != null) { Image image = icon.GetComponent<Image>(); image.sprite = data.roomIcon; image.preserveAspect = true; }
            int owned = inventory.GetOwned(data.roomId);
            int placed = inventory.GetPlaced(data.roomId);
            int available = inventory.GetAvailable(data.roomId);
            RoomUIFactory.CreateText(t, "RoomName", data.roomName, 25, new Vector2(-150, 57), new Vector2(450, 42), Color.white, TextAnchor.MiddleLeft);
            RoomUIFactory.CreateText(t, "RoomCounts", $"Owned {owned}     Placed {placed}     Available {available}", 18,
                new Vector2(-140, -3), new Vector2(460, 44), new Color(.84f, .85f, .9f), TextAnchor.MiddleLeft);
            RoomUIFactory.CreateText(t, "RoomSize", $"{data.width}×{data.height}  •  Level 1–{data.maxLevel}", 16,
                new Vector2(-140, -48), new Vector2(460, 38), new Color(.76f, .74f, .67f), TextAnchor.MiddleLeft);
            Button select = RoomUIFactory.CreateButton(t, "SelectRoomButton", available > 0 ? "SELECT" : "NONE AVAILABLE",
                new Vector2(345, -48), new Vector2(190, 68), new Color(.18f, .34f, .30f), 17);
            select.interactable = available > 0;
            select.onClick.AddListener(() => SelectRoom(data));
        }

        private void SelectRoom(RoomData data)
        {
            if (inventory == null)
            {
                SetStatus("Room Inventory Manager is not configured.");
                return;
            }
            if (!inventory.CanPlace(data.roomId, out string error)) { SetStatus(error); return; }
            if (placement != null) placement.SelectRoom(data);
            Close();
        }

        private void SetStatus(string message)
        {
            if (statusText != null) statusText.text = message;
        }
    }
}
