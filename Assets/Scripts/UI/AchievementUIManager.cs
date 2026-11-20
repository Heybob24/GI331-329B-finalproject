using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace DreamDungeon
{
    public sealed class AchievementUIManager : MonoBehaviour
    {
        [SerializeField] private RectTransform safeAreaRoot;
        [SerializeField] private AchievementManager achievementManager;

        private RectTransform overlay;
        private Transform content;
        private Text statusText;
        private Text summaryText;
        private Text balanceText;

        private Button combatTabBtn;
        private Button dungeonTabBtn;
        private Button progressionTabBtn;
        private Button economyTabBtn;

        private AchievementCategory activeCategory = AchievementCategory.Combat;
        private readonly List<AchievementCardUI> spawnedCards = new List<AchievementCardUI>();

        public AchievementCategory ActiveCategory => activeCategory;
        public IReadOnlyList<AchievementCardUI> SpawnedCards => spawnedCards;

        public void Configure(RectTransform safeArea, AchievementManager manager = null)
        {
            safeAreaRoot = safeArea;
            if (manager != null) achievementManager = manager;
        }

        private void Awake()
        {
            if (safeAreaRoot == null)
            {
                GameObject safe = GameObject.Find("SafeAreaRoot");
                if (safe != null) safeAreaRoot = safe.GetComponent<RectTransform>();
            }
            if (achievementManager == null)
                achievementManager = AchievementManager.Instance ?? FindAnyObjectByType<AchievementManager>();
            Build();
        }

        private void OnEnable()
        {
            if (achievementManager == null)
                achievementManager = AchievementManager.Instance ?? FindAnyObjectByType<AchievementManager>();
            if (achievementManager != null) achievementManager.OnAchievementsUpdated += Refresh;
            if (CurrencyManager.Instance != null) CurrencyManager.Instance.CurrencyChanged += OnCurrencyChanged;
        }

        private void OnDisable()
        {
            if (achievementManager != null) achievementManager.OnAchievementsUpdated -= Refresh;
            if (CurrencyManager.Instance != null) CurrencyManager.Instance.CurrencyChanged -= OnCurrencyChanged;
        }

        public void Open()
        {
            if (achievementManager == null)
                achievementManager = AchievementManager.Instance ?? FindAnyObjectByType<AchievementManager>();
            if (overlay == null) Build();
            if (overlay == null) return;
            overlay.gameObject.SetActive(true);
            SetCategory(activeCategory);
        }

        public void Close()
        {
            if (overlay != null) overlay.gameObject.SetActive(false);
        }

        public void SetCategory(AchievementCategory category)
        {
            activeCategory = category;
            UpdateTabHighlights();
            Refresh();
        }

        private void Build()
        {
            if (overlay != null) return;
            Transform parent = safeAreaRoot != null ? safeAreaRoot : transform;
            overlay = RoomUIFactory.CreateOverlay(parent, "AchievementPanelOverlay");

            RectTransform window = RoomUIFactory.CreateWindow(overlay, "AchievementWindow",
                new Vector2(1000f, 1680f), new Color(0.075f, 0.085f, 0.12f, 0.98f));

            RoomUIFactory.CreateText(window, "AchievementTitle", "ACHIEVEMENTS", 40,
                new Vector2(-40f, 770f), new Vector2(720f, 68f), new Color(0.96f, 0.78f, 0.43f));

            Button closeBtn = RoomUIFactory.CreateButton(window, "CloseAchievementButton", "CLOSE",
                new Vector2(405f, 770f), new Vector2(150f, 68f), new Color(0.34f, 0.19f, 0.19f), 20);
            closeBtn.onClick.AddListener(Close);

            balanceText = RoomUIFactory.CreateText(window, "AchievementBalanceText", "GOLD 1000   SOUL 0", 22,
                new Vector2(-210f, 705f), new Vector2(480f, 44f), Color.white, TextAnchor.MiddleLeft);

            summaryText = RoomUIFactory.CreateText(window, "AchievementSummaryText", "UNLOCKED 0 / 14", 20,
                new Vector2(215f, 705f), new Vector2(450f, 44f), new Color(0.65f, 0.92f, 0.72f), TextAnchor.MiddleRight);

            // 4 Category Buttons: Combat, Dungeon, Progression, Economy
            combatTabBtn = RoomUIFactory.CreateButton(window, "TabCombatButton", "COMBAT",
                new Vector2(-345f, 635f), new Vector2(215f, 64f), new Color(0.38f, 0.18f, 0.18f), 19);
            dungeonTabBtn = RoomUIFactory.CreateButton(window, "TabDungeonButton", "DUNGEON",
                new Vector2(-115f, 635f), new Vector2(215f, 64f), new Color(0.16f, 0.28f, 0.38f), 19);
            progressionTabBtn = RoomUIFactory.CreateButton(window, "TabProgressionButton", "PROGRESSION",
                new Vector2(115f, 635f), new Vector2(215f, 64f), new Color(0.28f, 0.18f, 0.38f), 17);
            economyTabBtn = RoomUIFactory.CreateButton(window, "TabEconomyButton", "ECONOMY",
                new Vector2(345f, 635f), new Vector2(215f, 64f), new Color(0.36f, 0.28f, 0.14f), 19);

            combatTabBtn.onClick.AddListener(() => SetCategory(AchievementCategory.Combat));
            dungeonTabBtn.onClick.AddListener(() => SetCategory(AchievementCategory.Dungeon));
            progressionTabBtn.onClick.AddListener(() => SetCategory(AchievementCategory.Progression));
            economyTabBtn.onClick.AddListener(() => SetCategory(AchievementCategory.Economy));

            statusText = RoomUIFactory.CreateText(window, "AchievementStatusText", "Unlock permanent milestones and claim their rewards.", 19,
                new Vector2(0f, 570f), new Vector2(920f, 48f), new Color(0.82f, 0.86f, 0.92f));

            content = RoomUIFactory.CreateScrollList(window, "AchievementScrollList",
                new Vector2(0f, -110f), new Vector2(940f, 1280f));

            overlay.gameObject.SetActive(false);
        }

        public void Refresh()
        {
            if (content == null) return;
            if (achievementManager == null)
                achievementManager = AchievementManager.Instance ?? FindAnyObjectByType<AchievementManager>();
            if (achievementManager == null) return;

            for (int i = content.childCount - 1; i >= 0; i--)
            {
                Destroy(content.GetChild(i).gameObject);
            }
            spawnedCards.Clear();

            List<AchievementData> list = achievementManager.GetAchievementsByCategory(activeCategory);
            foreach (AchievementData ach in list)
            {
                if (ach == null) continue;
                AchievementProgressEntry entry = achievementManager.GetOrCreateEntry(ach);
                AchievementCardUI card = AchievementCardUI.Create(content, ach, entry, OnClaimAchievementClicked);
                spawnedCards.Add(card);
            }

            RefreshSummary();
            RefreshBalanceText();
            Canvas.ForceUpdateCanvases();
        }

        private void OnClaimAchievementClicked(string achievementId)
        {
            if (achievementManager == null) return;
            if (achievementManager.TryClaimReward(achievementId, out string message))
            {
                SetStatus(message);
            }
            else
            {
                SetStatus(message);
            }
            Refresh();
        }

        private void RefreshSummary()
        {
            if (summaryText == null || achievementManager == null || achievementManager.Database == null) return;
            int total = 0;
            int unlocked = 0;
            foreach (AchievementData a in achievementManager.Database.AllAchievements)
            {
                if (a == null) continue;
                total++;
                AchievementProgressEntry e = achievementManager.GetOrCreateEntry(a);
                if (e != null && e.isUnlocked) unlocked++;
            }
            summaryText.text = $"UNLOCKED {unlocked} / {total}";
        }

        private void UpdateTabHighlights()
        {
            SetButtonTint(combatTabBtn, activeCategory == AchievementCategory.Combat, new Color(0.62f, 0.24f, 0.22f), new Color(0.26f, 0.16f, 0.16f));
            SetButtonTint(dungeonTabBtn, activeCategory == AchievementCategory.Dungeon, new Color(0.22f, 0.48f, 0.66f), new Color(0.14f, 0.22f, 0.28f));
            SetButtonTint(progressionTabBtn, activeCategory == AchievementCategory.Progression, new Color(0.46f, 0.28f, 0.64f), new Color(0.22f, 0.16f, 0.28f));
            SetButtonTint(economyTabBtn, activeCategory == AchievementCategory.Economy, new Color(0.60f, 0.44f, 0.16f), new Color(0.26f, 0.20f, 0.14f));
        }

        private static void SetButtonTint(Button btn, bool active, Color activeCol, Color normalCol)
        {
            if (btn == null) return;
            Image img = btn.GetComponent<Image>();
            if (img != null) img.color = active ? activeCol : normalCol;
        }

        private void RefreshBalanceText()
        {
            CurrencyManager c = CurrencyManager.Instance ?? FindAnyObjectByType<CurrencyManager>();
            if (c != null)
            {
                OnCurrencyChanged(c.Gold, c.Soul, c.Crystal);
            }
        }

        private void OnCurrencyChanged(int gold, int soul, int crystal)
        {
            if (balanceText != null)
            {
                balanceText.text = $"GOLD {gold}    SOUL {soul}";
            }
        }

        private void SetStatus(string message)
        {
            if (statusText != null) statusText.text = message;
        }
    }
}
