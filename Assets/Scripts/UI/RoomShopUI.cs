using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace DreamDungeon
{
    public sealed class RoomShopUI : MonoBehaviour
    {
        [SerializeField] private RoomShopManager shopManager;
        [SerializeField] private RoomDatabase database;
        [SerializeField] private CurrencyManager currency;
        [SerializeField] private RectTransform safeAreaRoot;

        private RectTransform overlay;
        private Transform content;
        private Text balanceText;
        private Text statusText;

        public void Configure(RectTransform safeArea, RoomShopManager manager, RoomDatabase roomDatabase, CurrencyManager economy)
        {
            safeAreaRoot = safeArea;
            shopManager = manager;
            database = roomDatabase;
            currency = economy;
        }

        private void Awake()
        {
            if (shopManager == null) shopManager = FindAnyObjectByType<RoomShopManager>();
            if (database == null && shopManager != null) database = shopManager.Database;
            if (currency == null && shopManager != null) currency = shopManager.Currency;
            if (safeAreaRoot == null)
            {
                GameObject safe = GameObject.Find("SafeAreaRoot");
                if (safe != null) safeAreaRoot = safe.GetComponent<RectTransform>();
            }
            Build();
            if (currency != null) currency.CurrencyChanged += UpdateBalance;
            if (shopManager != null) shopManager.StatusChanged += SetStatus;
            if (shopManager != null && shopManager.Inventory != null) shopManager.Inventory.InventoryChanged += RefreshCards;
            if (currency != null) UpdateBalance(currency.Gold, currency.Soul, currency.Crystal);
        }

        private void OnDestroy()
        {
            if (currency != null) currency.CurrencyChanged -= UpdateBalance;
            if (shopManager != null) shopManager.StatusChanged -= SetStatus;
            if (shopManager != null && shopManager.Inventory != null) shopManager.Inventory.InventoryChanged -= RefreshCards;
        }

        public void Open()
        {
            if (overlay == null) Build();
            overlay.gameObject.SetActive(true);
            RefreshCards();
            UpdateBalance(currency != null ? currency.Gold : 0, currency != null ? currency.Soul : 0, currency != null ? currency.Crystal : 0);
        }

        public void Close()
        {
            if (overlay != null) overlay.gameObject.SetActive(false);
        }

        private void Build()
        {
            if (safeAreaRoot == null || overlay != null) return;
            overlay = RoomUIFactory.CreateOverlay(safeAreaRoot, "RoomShopPanel");
            RectTransform window = RoomUIFactory.CreateWindow(overlay, "RoomShopWindow", new Vector2(1000, 1660), new Color(.075f, .085f, .12f, 1f));
            RoomUIFactory.CreateText(window, "ShopTitle", "ROOM SHOP", 42, new Vector2(-10, 760), new Vector2(720, 70), new Color(.96f, .78f, .43f));
            balanceText = RoomUIFactory.CreateText(window, "ShopBalance", "GOLD 0    SOUL 0", 24, new Vector2(0, 690), new Vector2(700, 52), Color.white);
            statusText = RoomUIFactory.CreateText(window, "ShopStatus", "Buy rooms to add them to your inventory.", 20, new Vector2(0, 620), new Vector2(900, 60), new Color(.82f, .85f, .9f));
            Button close = RoomUIFactory.CreateButton(window, "CloseShopButton", "CLOSE", new Vector2(405, 760), new Vector2(150, 70), new Color(.34f, .19f, .19f), 20);
            close.onClick.AddListener(Close);
            content = RoomUIFactory.CreateScrollList(window, "RoomShopScroll", new Vector2(0, -25), new Vector2(930, 1250));
            overlay.gameObject.SetActive(false);
            RefreshCards();
        }

        private void RefreshCards()
        {
            if (content == null || database == null) return;
            for (int i = content.childCount - 1; i >= 0; i--) Destroy(content.GetChild(i).gameObject);
            IReadOnlyList<RoomData> rooms = database.AllRooms;
            foreach (RoomData data in rooms)
            {
                if (data == null) continue;
                CreateRoomCard(data);
            }
            Canvas.ForceUpdateCanvases();
        }

        private void CreateRoomCard(RoomData data)
        {
            GameObject card = RoomUIFactory.CreateCard(content, "ShopCard_" + data.roomId, 230);
            Transform t = card.transform;
            Color iconColor = data.roomIcon != null ? Color.white : RoomColor(data.roomType);
            RectTransform icon = RoomUIFactory.CreatePanel(t, "RoomIcon", iconColor, new Vector2(-360, 0), new Vector2(110, 110));
            Image iconImage = icon.GetComponent<Image>();
            if (data.roomIcon != null) { iconImage.sprite = data.roomIcon; iconImage.preserveAspect = true; }
            string price = data.IsPurchasable ? $"{data.goldCost} Gold  •  {data.soulCost} Soul" : "Permanent room / build tile";
            int currentDungeonLevel = shopManager != null ? shopManager.DungeonLevel : 1;
            string unlock = data.unlockDungeonLevel > currentDungeonLevel
                ? $"LOCKED — unlocks at Dungeon Level {data.unlockDungeonLevel}"
                : $"Unlocked at Dungeon Level {data.unlockDungeonLevel}";
            RoomUIFactory.CreateText(t, "RoomName", data.roomName, 25, new Vector2(-135, 74), new Vector2(410, 44), Color.white, TextAnchor.MiddleLeft);
            RoomUIFactory.CreateText(t, "RoomDescription", data.description, 17, new Vector2(-125, 25), new Vector2(440, 52), new Color(.78f, .8f, .85f), TextAnchor.MiddleLeft);
            RoomUIFactory.CreateText(t, "RoomFacts", $"{data.width}×{data.height}  •  Max Level {data.maxLevel}  •  Capacity {data.maxCapacity}", 16, new Vector2(-125, -28), new Vector2(440, 38), new Color(.77f, .74f, .64f), TextAnchor.MiddleLeft);
            bool unlocked = shopManager != null && shopManager.IsUnlocked(data);
            bool affordable = currency != null && currency.CanAfford(data.goldCost, data.soulCost);
            string state = !data.IsPurchasable ? "NOT FOR SALE" : !unlocked ? unlock : !affordable ? "INSUFFICIENT CURRENCY" : unlock;
            RoomUIFactory.CreateText(t, "RoomUnlock", state, 15, new Vector2(-125, -78), new Vector2(440, 35), !data.IsPurchasable || unlocked ? new Color(.66f, .8f, .66f) : new Color(1f, .46f, .36f), TextAnchor.MiddleLeft);
            string buttonText = !data.IsPurchasable ? "—" : !unlocked ? "LOCKED" : !affordable ? "NEED GOLD" : "BUY";
            Button buy = RoomUIFactory.CreateButton(t, "BuyRoomButton", buttonText, new Vector2(350, -58), new Vector2(175, 70), new Color(.40f, .27f, .14f), 19);
            buy.interactable = data.IsPurchasable && unlocked && affordable;
            buy.onClick.AddListener(() => Purchase(data.roomId));
        }

        private void Purchase(string roomId)
        {
            if (shopManager == null) { SetStatus("Room Shop Manager is not configured."); return; }
            if (shopManager.TryPurchase(roomId, out string message)) SetStatus(message);
            else SetStatus(message);
            RefreshCards();
        }

        private void UpdateBalance(int gold, int soul, int crystal)
        {
            if (balanceText != null) balanceText.text = $"GOLD {gold}    SOUL {soul}    CRYSTAL {crystal}";
        }

        private void SetStatus(string message)
        {
            if (statusText != null) statusText.text = message;
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
