using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace DreamDungeon
{
    public sealed class DungeonProgressionUI : MonoBehaviour
    {
        [SerializeField] private RectTransform safeAreaRoot;

        private RectTransform overlay;
        private Text levelTitleText;
        private Text expText;
        private Image expBarFill;
        private Text statsText;
        private Transform content;

        public void Configure(RectTransform safeArea)
        {
            safeAreaRoot = safeArea;
        }

        private void Awake()
        {
            if (safeAreaRoot == null)
            {
                GameObject safe = GameObject.Find("SafeAreaRoot");
                if (safe != null) safeAreaRoot = safe.GetComponent<RectTransform>();
            }
            Build();
        }

        public void Open()
        {
            if (overlay == null) Build();
            overlay.gameObject.SetActive(true);
            Refresh();
        }

        public void Close()
        {
            if (overlay != null) overlay.gameObject.SetActive(false);
        }

        private void Build()
        {
            Transform parent = safeAreaRoot != null ? safeAreaRoot : transform;
            overlay = RoomUIFactory.CreateOverlay(parent, "ProgressionOverlay");

            RectTransform window = RoomUIFactory.CreateWindow(overlay, "ProgressionWindow", new Vector2(980, 1600), new Color(0.08f, 0.09f, 0.14f, 0.98f));

            RoomUIFactory.CreateText(window, "Title", "DUNGEON PROGRESSION", 38, new Vector2(0, 720), new Vector2(880, 60), new Color(1f, 0.85f, 0.35f));

            levelTitleText = RoomUIFactory.CreateText(window, "LevelTitle", "DUNGEON LEVEL 1", 46, new Vector2(0, 640), new Vector2(880, 70), Color.white);

            // EXP Bar
            RectTransform barBg = RoomUIFactory.CreatePanel(window, "ExpBarBg", new Color(0.15f, 0.16f, 0.22f), new Vector2(0, 560), new Vector2(860, 44));
            var fillObj = new GameObject("ExpBarFill", typeof(RectTransform), typeof(Image));
            fillObj.transform.SetParent(barBg, false);
            RectTransform fillRt = fillObj.GetComponent<RectTransform>();
            fillRt.anchorMin = Vector2.zero;
            fillRt.anchorMax = Vector2.one;
            fillRt.sizeDelta = Vector2.zero;
            expBarFill = fillObj.GetComponent<Image>();
            expBarFill.type = Image.Type.Filled;
            expBarFill.fillMethod = Image.FillMethod.Horizontal;
            expBarFill.color = new Color(0.35f, 0.75f, 1f);

            expText = RoomUIFactory.CreateText(barBg, "ExpLabel", "0 / 100 EXP", 22, Vector2.zero, new Vector2(860, 44), Color.white);

            // Stats box
            RectTransform statsBox = RoomUIFactory.CreatePanel(window, "StatsBox", new Color(0.12f, 0.13f, 0.18f), new Vector2(0, 410), new Vector2(860, 200));
            statsText = RoomUIFactory.CreateText(statsBox, "StatsText", "Total Raids: 0\nVictories: 0 | Defeats: 0\nBosses Defeated: 0\nHighest Wave: 0", 24, Vector2.zero, new Vector2(800, 170), new Color(0.85f, 0.9f, 0.95f));

            RoomUIFactory.CreateText(window, "UnlocksTitle", "UNLOCKS ROADMAP", 28, new Vector2(0, 260), new Vector2(860, 40), new Color(0.95f, 0.80f, 0.3f), TextAnchor.MiddleLeft);

            content = RoomUIFactory.CreateScrollList(window, "UnlocksScroll", new Vector2(0, -200), new Vector2(880, 840));

            Button closeBtn = RoomUIFactory.CreateButton(window, "CloseButton", "CLOSE", new Vector2(0, -710), new Vector2(400, 80), new Color(0.75f, 0.25f, 0.25f), 28);
            closeBtn.onClick.AddListener(Close);

            overlay.gameObject.SetActive(false);
        }

        public void Refresh()
        {
            var mgr = DungeonProgressionManager.Instance;
            int level = mgr != null ? mgr.CurrentLevel : 1;
            int currentExp = mgr != null ? mgr.CurrentExp : 0;
            int reqExp = mgr != null ? mgr.GetRequiredExpForLevel(level) : 100;

            if (levelTitleText != null)
            {
                levelTitleText.text = level >= DungeonProgressionManager.MaxLevel ? $"DUNGEON LEVEL {level} (MAX)" : $"DUNGEON LEVEL {level}";
            }

            if (expText != null)
            {
                expText.text = reqExp > 0 ? $"{currentExp} / {reqExp} EXP" : "MAX LEVEL REACHED";
            }

            if (expBarFill != null)
            {
                expBarFill.fillAmount = reqExp > 0 ? Mathf.Clamp01((float)currentExp / reqExp) : 1f;
            }

            if (statsText != null && mgr != null)
            {
                statsText.text = $"Total Raids: {mgr.TotalRaids}\nVictories: {mgr.TotalVictories}   |   Defeats: {mgr.TotalDefeats}\nBosses Defeated: {mgr.TotalBossesDefeated}   |   Highest Wave: {mgr.HighestWaveCleared}";
            }

            PopulateRoadmap(level);
        }

        private void PopulateRoadmap(int currentLevel)
        {
            if (content == null) return;

            foreach (Transform child in content)
            {
                Destroy(child.gameObject);
            }

            var unlockMgr = DungeonUnlockManager.Instance;

            for (int lvl = 1; lvl <= DungeonProgressionManager.MaxLevel; lvl++)
            {
                bool isUnlocked = currentLevel >= lvl;
                Color cardColor = isUnlocked ? new Color(0.16f, 0.22f, 0.18f) : new Color(0.14f, 0.14f, 0.18f);

                GameObject card = RoomUIFactory.CreateCard(content, $"LevelCard_{lvl}", 110);
                card.GetComponent<Image>().color = cardColor;

                string statusBadge = isUnlocked ? "<color=#45d865>[UNLOCKED]</color>" : $"<color=#ffaa33>[LV {lvl}]</color>";
                List<string> unlocks = unlockMgr != null ? unlockMgr.GetUnlocksForLevel(lvl) : new List<string>();
                string unlockStr = unlocks.Count > 0 ? string.Join(", ", unlocks) : "Base Content";

                var (gold, soul) = DungeonProgressionManager.Instance != null ? DungeonProgressionManager.Instance.GetRewardForLevel(lvl) : (0, 0);
                string rewardStr = gold > 0 ? $" | Reward: +{gold}G, +{soul}S" : "";

                Text text = RoomUIFactory.CreateText(card.transform, "Info", $"Level {lvl} {statusBadge}\n<color=#cccccc>{unlockStr}{rewardStr}</color>", 22, Vector2.zero, new Vector2(820, 90), Color.white, TextAnchor.MiddleLeft);
            }
        }
    }
}
