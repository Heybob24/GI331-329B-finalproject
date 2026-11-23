using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace DreamDungeon
{
    public sealed class BossSelectionUI : MonoBehaviour
    {
        [SerializeField] private RectTransform safeAreaRoot;
        [SerializeField] private BossPlacementManager placementManager;
        [SerializeField] private BossDatabase database;

        private RectTransform overlay;
        private Transform content;
        private Text statusText;

        public void Configure(RectTransform safeArea, BossPlacementManager bossPlacement, BossDatabase bossDb)
        {
            safeAreaRoot = safeArea;
            placementManager = bossPlacement;
            database = bossDb;
        }

        private void Awake()
        {
            if (safeAreaRoot == null)
            {
                GameObject safe = GameObject.Find("SafeAreaRoot");
                if (safe != null) safeAreaRoot = safe.GetComponent<RectTransform>();
            }
            if (placementManager == null) placementManager = FindAnyObjectByType<BossPlacementManager>();
            if (database == null) database = Resources.Load<BossDatabase>("DreamDungeon/BossDatabase");
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
            overlay = RoomUIFactory.CreateOverlay(parent, "BossSelectionOverlay");

            RectTransform window = RoomUIFactory.CreateWindow(overlay, "BossSelectionWindow", new Vector2(980, 1600), new Color(0.09f, 0.08f, 0.12f, 0.98f));

            RoomUIFactory.CreateText(window, "Title", "DUNGEON BOSSES", 40, new Vector2(0, 720), new Vector2(880, 60), new Color(1f, 0.35f, 0.35f));

            statusText = RoomUIFactory.CreateText(window, "StatusText", "Select a Boss to place in a Boss Room (Capacity: 1 Boss)", 22, new Vector2(0, 650), new Vector2(880, 50), new Color(0.85f, 0.85f, 0.9f));

            content = RoomUIFactory.CreateScrollList(window, "BossScroll", new Vector2(0, -30), new Vector2(880, 1200));

            Button closeBtn = RoomUIFactory.CreateButton(window, "CloseButton", "CLOSE", new Vector2(0, -710), new Vector2(400, 80), new Color(0.75f, 0.25f, 0.25f), 28);
            closeBtn.onClick.AddListener(Close);

            overlay.gameObject.SetActive(false);
        }

        public void Refresh()
        {
            if (content == null) return;

            foreach (Transform child in content)
            {
                Destroy(child.gameObject);
            }

            if (database == null)
            {
                database = Resources.Load<BossDatabase>("DreamDungeon/BossDatabase");
            }

            if (database == null)
            {
                if (statusText != null) statusText.text = "Boss database is not loaded.";
                return;
            }

            int currentLevel = DungeonProgressionManager.Instance != null ? DungeonProgressionManager.Instance.CurrentLevel : 1;
            var unlockMgr = DungeonUnlockManager.Instance;

            foreach (BossData boss in database.GetAllBosses())
            {
                if (boss == null) continue;

                bool isUnlocked = unlockMgr != null ? unlockMgr.IsBossUnlocked(boss.bossId, currentLevel) : currentLevel >= boss.unlockDungeonLevel;

                GameObject card = RoomUIFactory.CreateCard(content, $"BossCard_{boss.bossId}", 320);
                card.GetComponent<Image>().color = isUnlocked ? new Color(0.18f, 0.15f, 0.20f) : new Color(0.12f, 0.11f, 0.14f);

                var iconPanel = RoomUIFactory.CreatePanel(card.transform, "Icon", boss.bossIcon != null ? Color.white : boss.bossColor, new Vector2(-345f, 0f), new Vector2(125f, 125f));
                if (boss.bossIcon != null)
                {
                    Image iconImg = iconPanel.GetComponent<Image>();
                    iconImg.sprite = boss.bossIcon;
                    iconImg.preserveAspect = true;
                }

                string statusColor = isUnlocked ? "#55dd66" : "#ff7744";
                string statusTextStr = isUnlocked ? "UNLOCKED" : $"UNLOCKS AT LV {boss.unlockDungeonLevel}";

                string skillsDesc = "";
                if (boss.skills != null && boss.skills.Count > 0)
                {
                    skillsDesc = $"\nSkill: {boss.skills[0].skillName} ({boss.skills[0].description})";
                }

                string info = $"<size=28><b>{boss.bossName}</b></size>   <color={statusColor}>[{statusTextStr}]</color>\n" +
                              $"HP: <color=#ff6666>{boss.maxHP}</color>   ATK: <color=#ffaa44>{boss.attack}</color>   DEF: <color=#66aaff>{boss.defense}</color>   SPD: {boss.attackSpeed:0.0}/s\n" +
                              $"<color=#aaaaaa>{boss.description}{skillsDesc}</color>";

                RoomUIFactory.CreateText(card.transform, "Details", info, 20, new Vector2(-45, 10), new Vector2(450, 260), Color.white, TextAnchor.MiddleLeft);

                string bossIdCopy = boss.bossId;
                Color btnColor = isUnlocked ? new Color(0.25f, 0.65f, 0.35f) : new Color(0.35f, 0.35f, 0.35f);
                string btnLabel = isUnlocked ? "PLACE BOSS" : "LOCKED";

                Button placeBtn = RoomUIFactory.CreateButton(card.transform, "PlaceBtn", btnLabel, new Vector2(300, 0), new Vector2(220, 75), btnColor, 22);
                placeBtn.interactable = isUnlocked;

                if (isUnlocked)
                {
                    placeBtn.onClick.AddListener(() =>
                    {
                        if (placementManager != null)
                        {
                            placementManager.SelectBoss(bossIdCopy);
                        }
                        Close();
                    });
                }
            }
        }
    }
}
