using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace DreamDungeon
{
    public sealed class EndlessModeUI : MonoBehaviour
    {
        private RectTransform safeAreaRoot;
        private EndlessChallengeManager manager;
        private RoomManager roomManager;

        private GameObject rootOverlay;
        private Text recordsSummaryText;
        private Text preparationPreviewText;
        private Text validationWarningText;
        private Button startEndlessButton;

        public bool IsOpen => rootOverlay != null && rootOverlay.activeSelf;

        public void Configure(RectTransform safeRoot, EndlessChallengeManager ecMgr = null, RoomManager rMgr = null)
        {
            safeAreaRoot = safeRoot;
            if (ecMgr != null) manager = ecMgr;
            if (rMgr != null) roomManager = rMgr;
        }

        private void EnsureConfigured()
        {
            if (safeAreaRoot == null)
            {
                GameObject safeObj = GameObject.Find("SafeAreaRoot");
                if (safeObj != null) safeAreaRoot = safeObj.GetComponent<RectTransform>();
            }
            if (manager == null)
            {
                manager = EndlessChallengeManager.Instance ?? FindAnyObjectByType<EndlessChallengeManager>();
                if (manager == null)
                {
                    manager = gameObject.GetComponent<EndlessChallengeManager>() ?? gameObject.AddComponent<EndlessChallengeManager>();
                }
            }
            if (roomManager == null)
            {
                roomManager = RoomManager.Instance ?? FindAnyObjectByType<RoomManager>();
            }
        }

        public void Open()
        {
            EnsureConfigured();
            if (rootOverlay == null)
            {
                BuildUI();
            }
            if (rootOverlay != null)
            {
                rootOverlay.SetActive(true);
                Refresh();
            }
        }

        public void Close()
        {
            if (rootOverlay != null)
            {
                rootOverlay.SetActive(false);
            }
        }

        public void Refresh()
        {
            EnsureConfigured();
            if (manager == null) return;

            if (recordsSummaryText != null)
            {
                recordsSummaryText.text =
                    $"HIGHEST WAVE:  <color=#FFD966>{manager.HighestEndlessWave}</color>     •     " +
                    $"HIGHEST SCORE:  <color=#70F096>{manager.HighestEndlessScore}</color>\n" +
                    $"Total Runs: {manager.TotalEndlessRuns}   •   " +
                    $"Enemies Slain: {manager.TotalEndlessEnemiesDefeated}   •   " +
                    $"Elites: {manager.TotalEndlessElitesDefeated}   •   " +
                    $"Bosses: {manager.TotalEndlessBossesDefeated}";
            }

            if (preparationPreviewText != null)
            {
                int roomCount = roomManager?.Layout?.rooms != null ? roomManager.Layout.rooms.Count : 0;
                int monsterCount = roomManager?.Layout?.monsters != null ? roomManager.Layout.monsters.Count : 0;
                int trapCount = roomManager?.Layout?.traps != null ? roomManager.Layout.traps.Count : 0;
                int bossCount = roomManager?.Layout?.bosses != null ? roomManager.Layout.bosses.Count : 0;
                int coreHp = DungeonCoreUpgradeManager.Instance != null ? DungeonCoreUpgradeManager.Instance.MaxHP : 500;
                int coreDef = DungeonCoreUpgradeManager.Instance != null ? DungeonCoreUpgradeManager.Instance.Defense : 1;

                var cfg = manager.EndlessConfig;
                preparationPreviewText.text =
                    "<b>CURRENT DUNGEON PREPARATION</b>\n" +
                    $"• Rooms Placed: {roomCount}   •   Monsters Deployed: {monsterCount}   •   Traps Installed: {trapCount}   •   Bosses: {bossCount}\n" +
                    $"• Dungeon Core Integrity: {coreHp} Max HP   (Defense: {coreDef})\n\n" +
                    "<b>ENDLESS SCALING & SCHEDULE</b>\n" +
                    $"• Starting Wave: Wave {cfg.startingWave}   ({cfg.baseEnemiesPerWave} base enemies, scales dynamically)\n" +
                    $"• Elite Champions: Every {cfg.eliteWaveInterval} waves (Elite Knight, Archer, Mage, Assassin, Paladin)\n" +
                    $"• Endless Bosses: Every {cfg.bossWaveInterval} waves (Mini-Boss W5, Major Boss W10, Elite Boss W15, Endless Boss W30)";
            }

            bool isValid = manager.ValidateDungeonForRun(roomManager, null, out string error);
            if (validationWarningText != null)
            {
                validationWarningText.text = isValid
                    ? "<color=#66F088>✓ Dungeon Layout Validated — Ready for Endless Run!</color>"
                    : $"<color=#FF6666>⚠ {error}</color>";
            }

            if (startEndlessButton != null)
            {
                startEndlessButton.interactable = isValid;
            }
        }

        private void OnStartEndlessClicked()
        {
            EnsureConfigured();
            if (manager == null) return;

            if (!manager.TryStartEndlessRun(roomManager, out string error))
            {
                if (validationWarningText != null)
                {
                    validationWarningText.text = $"<color=#FF6666>⚠ {error}</color>";
                }
            }
        }

        private void BuildUI()
        {
            if (safeAreaRoot == null) return;

            RectTransform overlay = RoomUIFactory.CreateOverlay(safeAreaRoot, "EndlessModeOverlay");
            rootOverlay = overlay.gameObject;

            RectTransform card = RoomUIFactory.CreatePanel(overlay, "EndlessModeCard",
                new Color(0.08f, 0.09f, 0.14f, 0.98f), Vector2.zero, new Vector2(960f, 1340f));

            RoomUIFactory.CreateText(card, "Title", "ENDLESS DUNGEON", 42,
                new Vector2(0f, 595f), new Vector2(880f, 70f), new Color(0.96f, 0.78f, 0.43f));
            RoomUIFactory.CreateText(card, "Subtitle", "SURVIVE RELENTLESS WAVES  •  ELITES & ENDLESS BOSSES", 20,
                new Vector2(0f, 540f), new Vector2(880f, 40f), new Color(0.74f, 0.78f, 0.90f));

            RectTransform recordBox = RoomUIFactory.CreatePanel(card, "RecordsPanel",
                new Color(0.13f, 0.15f, 0.22f, 1f), new Vector2(0f, 405f), new Vector2(880f, 170f));
            recordsSummaryText = RoomUIFactory.CreateText(recordBox, "RecordsSummaryText", "", 23,
                Vector2.zero, new Vector2(840f, 150f), Color.white);

            RectTransform prepBox = RoomUIFactory.CreatePanel(card, "PreparationPanel",
                new Color(0.11f, 0.13f, 0.18f, 1f), new Vector2(0f, 55f), new Vector2(880f, 470f));
            preparationPreviewText = RoomUIFactory.CreateText(prepBox, "PreparationPreviewText", "", 21,
                Vector2.zero, new Vector2(830f, 430f), new Color(0.90f, 0.92f, 0.96f), TextAnchor.MiddleLeft);

            validationWarningText = RoomUIFactory.CreateText(card, "ValidationWarningText", "", 22,
                new Vector2(0f, -245f), new Vector2(880f, 70f), Color.white);

            startEndlessButton = RoomUIFactory.CreateButton(card, "StartEndlessRunButton", "START ENDLESS RUN",
                new Vector2(0f, -370f), new Vector2(560f, 96f), new Color(0.52f, 0.22f, 0.14f, 1f), 28);
            startEndlessButton.onClick.AddListener(OnStartEndlessClicked);

            Button closeBtn = RoomUIFactory.CreateButton(card, "CloseEndlessButton", "BACK",
                new Vector2(0f, -515f), new Vector2(360f, 82f), new Color(0.22f, 0.24f, 0.32f, 1f), 24);
            closeBtn.onClick.AddListener(Close);
        }
    }
}
