using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace DreamDungeon
{
    public sealed class QuestUIManager : MonoBehaviour
    {
        [SerializeField] private RectTransform safeAreaRoot;
        [SerializeField] private QuestManager questManager;

        private RectTransform overlay;
        private Transform content;
        private Text statusText;
        private Text timerText;
        private Text balanceText;

        private Button mainTabBtn;
        private Button dailyTabBtn;
        private Button weeklyTabBtn;
        private Button specialTabBtn;

        private QuestType activeTab = QuestType.Main;
        private readonly List<QuestCardUI> spawnedCards = new List<QuestCardUI>();
        private float nextTimerRefresh;

        public QuestType ActiveTab => activeTab;
        public IReadOnlyList<QuestCardUI> SpawnedCards => spawnedCards;

        public void Configure(RectTransform safeArea, QuestManager manager = null)
        {
            safeAreaRoot = safeArea;
            if (manager != null) questManager = manager;
        }

        private void Awake()
        {
            if (safeAreaRoot == null)
            {
                GameObject safe = GameObject.Find("SafeAreaRoot");
                if (safe != null) safeAreaRoot = safe.GetComponent<RectTransform>();
            }
            if (questManager == null) questManager = QuestManager.Instance ?? FindAnyObjectByType<QuestManager>();
            Build();
        }

        private void OnEnable()
        {
            if (questManager == null) questManager = QuestManager.Instance ?? FindAnyObjectByType<QuestManager>();
            if (questManager != null) questManager.OnQuestsUpdated += Refresh;
            if (CurrencyManager.Instance != null) CurrencyManager.Instance.CurrencyChanged += OnCurrencyChanged;
        }

        private void OnDisable()
        {
            if (questManager != null) questManager.OnQuestsUpdated -= Refresh;
            if (CurrencyManager.Instance != null) CurrencyManager.Instance.CurrencyChanged -= OnCurrencyChanged;
        }

        private void Update()
        {
            if (overlay == null || !overlay.gameObject.activeSelf) return;
            if (Time.unscaledTime >= nextTimerRefresh)
            {
                nextTimerRefresh = Time.unscaledTime + 1f;
                RefreshTimerText();
            }
        }

        public void Open()
        {
            if (questManager == null) questManager = QuestManager.Instance ?? FindAnyObjectByType<QuestManager>();
            if (overlay == null) Build();
            if (overlay == null) return;
            overlay.gameObject.SetActive(true);
            SetTab(activeTab);
        }

        public void Close()
        {
            if (overlay != null) overlay.gameObject.SetActive(false);
        }

        public void SetTab(QuestType tab)
        {
            activeTab = tab;
            UpdateTabHighlights();
            Refresh();
        }

        private void Build()
        {
            if (overlay != null) return;
            Transform parent = safeAreaRoot != null ? safeAreaRoot : transform;
            overlay = RoomUIFactory.CreateOverlay(parent, "QuestPanelOverlay");

            RectTransform window = RoomUIFactory.CreateWindow(overlay, "QuestWindow",
                new Vector2(1000f, 1680f), new Color(0.075f, 0.085f, 0.12f, 0.98f));

            RoomUIFactory.CreateText(window, "QuestTitle", "DUNGEON QUESTS", 40,
                new Vector2(-40f, 770f), new Vector2(720f, 68f), new Color(0.96f, 0.78f, 0.43f));

            Button closeBtn = RoomUIFactory.CreateButton(window, "CloseQuestButton", "CLOSE",
                new Vector2(405f, 770f), new Vector2(150f, 68f), new Color(0.34f, 0.19f, 0.19f), 20);
            closeBtn.onClick.AddListener(Close);

            balanceText = RoomUIFactory.CreateText(window, "QuestBalanceText", "GOLD 1000   SOUL 0", 22,
                new Vector2(-210f, 705f), new Vector2(480f, 44f), Color.white, TextAnchor.MiddleLeft);

            timerText = RoomUIFactory.CreateText(window, "QuestTimerText", "", 20,
                new Vector2(215f, 705f), new Vector2(450f, 44f), new Color(0.65f, 0.85f, 1.0f), TextAnchor.MiddleRight);

            // 4 Tab Buttons: Main, Daily, Weekly, Special
            mainTabBtn = RoomUIFactory.CreateButton(window, "TabMainButton", "MAIN",
                new Vector2(-345f, 635f), new Vector2(215f, 64f), new Color(0.32f, 0.24f, 0.14f), 20);
            dailyTabBtn = RoomUIFactory.CreateButton(window, "TabDailyButton", "DAILY",
                new Vector2(-115f, 635f), new Vector2(215f, 64f), new Color(0.16f, 0.28f, 0.38f), 20);
            weeklyTabBtn = RoomUIFactory.CreateButton(window, "TabWeeklyButton", "WEEKLY",
                new Vector2(115f, 635f), new Vector2(215f, 64f), new Color(0.26f, 0.18f, 0.36f), 20);
            specialTabBtn = RoomUIFactory.CreateButton(window, "TabSpecialButton", "SPECIAL",
                new Vector2(345f, 635f), new Vector2(215f, 64f), new Color(0.16f, 0.32f, 0.24f), 20);

            mainTabBtn.onClick.AddListener(() => SetTab(QuestType.Main));
            dailyTabBtn.onClick.AddListener(() => SetTab(QuestType.Daily));
            weeklyTabBtn.onClick.AddListener(() => SetTab(QuestType.Weekly));
            specialTabBtn.onClick.AddListener(() => SetTab(QuestType.Special));

            statusText = RoomUIFactory.CreateText(window, "QuestStatusText", "Complete dungeon objectives to earn Gold, Soul, and EXP.", 19,
                new Vector2(0f, 570f), new Vector2(920f, 48f), new Color(0.82f, 0.86f, 0.92f));

            content = RoomUIFactory.CreateScrollList(window, "QuestScrollList",
                new Vector2(0f, -110f), new Vector2(940f, 1280f));

            overlay.gameObject.SetActive(false);
        }

        public void Refresh()
        {
            if (content == null) return;
            if (questManager == null) questManager = QuestManager.Instance ?? FindAnyObjectByType<QuestManager>();
            if (questManager == null) return;

            for (int i = content.childCount - 1; i >= 0; i--)
            {
                Destroy(content.GetChild(i).gameObject);
            }
            spawnedCards.Clear();

            List<QuestData> quests = questManager.GetQuestsForTab(activeTab);
            foreach (QuestData q in quests)
            {
                if (q == null) continue;
                QuestProgressEntry entry = questManager.GetOrCreateEntry(q);
                QuestCardUI card = QuestCardUI.Create(content, q, entry, OnClaimQuestClicked);
                spawnedCards.Add(card);
            }

            RefreshBalanceText();
            RefreshTimerText();
            Canvas.ForceUpdateCanvases();
        }

        private void OnClaimQuestClicked(string questId)
        {
            if (questManager == null) return;
            if (questManager.TryClaimReward(questId, out string message))
            {
                SetStatus(message);
            }
            else
            {
                SetStatus(message);
            }
            Refresh();
        }

        private void UpdateTabHighlights()
        {
            SetButtonTint(mainTabBtn, activeTab == QuestType.Main, new Color(0.55f, 0.40f, 0.16f), new Color(0.24f, 0.20f, 0.14f));
            SetButtonTint(dailyTabBtn, activeTab == QuestType.Daily, new Color(0.20f, 0.46f, 0.64f), new Color(0.14f, 0.22f, 0.28f));
            SetButtonTint(weeklyTabBtn, activeTab == QuestType.Weekly, new Color(0.44f, 0.26f, 0.62f), new Color(0.22f, 0.16f, 0.28f));
            SetButtonTint(specialTabBtn, activeTab == QuestType.Special, new Color(0.20f, 0.50f, 0.36f), new Color(0.14f, 0.24f, 0.18f));
        }

        private static void SetButtonTint(Button btn, bool active, Color activeCol, Color normalCol)
        {
            if (btn == null) return;
            Image img = btn.GetComponent<Image>();
            if (img != null) img.color = active ? activeCol : normalCol;
        }

        private void RefreshTimerText()
        {
            if (timerText == null || questManager == null) return;
            if (activeTab == QuestType.Daily && questManager.DailyQuests != null)
            {
                timerText.text = $"DAILY RESET IN: {questManager.DailyQuests.GetFormattedCountdown()}";
            }
            else if (activeTab == QuestType.Weekly && questManager.WeeklyQuests != null)
            {
                timerText.text = $"WEEKLY RESET IN: {questManager.WeeklyQuests.GetFormattedCountdown()}";
            }
            else if (activeTab == QuestType.Main)
            {
                int lvl = DungeonProgressionManager.Instance != null ? DungeonProgressionManager.Instance.CurrentLevel : 1;
                timerText.text = $"DUNGEON LEVEL {lvl}";
            }
            else
            {
                timerText.text = "SPECIAL MISSIONS";
            }
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
