using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace DreamDungeon
{
    public sealed class StageDetailsUI : MonoBehaviour
    {
        [SerializeField] private RectTransform safeAreaRoot;
        [SerializeField] private CampaignManager campaignManager;
        [SerializeField] private DifficultyManager difficultyManager;
        [SerializeField] private CampaignSelectionUI campaignSelectionUI;
        [SerializeField] private RoomManager roomManager;

        private RectTransform overlay;
        private Text stageTitleText;
        private Text stageDescText;
        private Text factionInfoText;
        private Text enemyPreviewText;
        private Text bossInfoText;
        private Text difficultyInfoText;
        private Text rewardPreviewText;
        private Text statusText;
        private Button startRaidButton;

        private readonly Dictionary<StageDifficulty, Button> difficultyButtons = new Dictionary<StageDifficulty, Button>();
        private CampaignStageData currentStage;
        private StageDifficulty selectedDifficulty = StageDifficulty.Easy;

        public bool IsOpen => overlay != null && overlay.gameObject.activeSelf;
        public CampaignStageData CurrentStage => currentStage;
        public StageDifficulty SelectedDifficulty => selectedDifficulty;

        public void Configure(RectTransform safeArea, CampaignManager campaign = null, DifficultyManager difficulty = null,
            CampaignSelectionUI selectionUI = null, RoomManager rooms = null)
        {
            safeAreaRoot = safeArea;
            if (campaign != null) campaignManager = campaign;
            if (difficulty != null) difficultyManager = difficulty;
            if (selectionUI != null) campaignSelectionUI = selectionUI;
            if (rooms != null) roomManager = rooms;
        }

        private void Awake()
        {
            if (safeAreaRoot == null)
            {
                GameObject safe = GameObject.Find("SafeAreaRoot");
                if (safe != null) safeAreaRoot = safe.GetComponent<RectTransform>();
            }
            EnsureReferences();
            Build();
        }

        private void EnsureReferences()
        {
            if (campaignManager == null)
                campaignManager = CampaignManager.Instance ?? FindAnyObjectByType<CampaignManager>();
            if (difficultyManager == null)
                difficultyManager = DifficultyManager.Instance ?? FindAnyObjectByType<DifficultyManager>();
            if (campaignSelectionUI == null)
                campaignSelectionUI = FindAnyObjectByType<CampaignSelectionUI>();
            if (roomManager == null)
                roomManager = RoomManager.Instance ?? FindAnyObjectByType<RoomManager>();
        }

        public void OpenForStage(CampaignStageData stage)
        {
            EnsureReferences();
            if (stage == null) return;
            currentStage = stage;

            // Default difficulty to the stage's base difficulty if unlocked, otherwise Easy
            if (difficultyManager != null && difficultyManager.IsDifficultyUnlocked(stage.difficulty, out _))
            {
                selectedDifficulty = stage.difficulty;
                difficultyManager.TrySelectDifficulty(selectedDifficulty, out _);
            }
            else
            {
                selectedDifficulty = StageDifficulty.Easy;
                difficultyManager?.TrySelectDifficulty(StageDifficulty.Easy, out _);
            }

            if (overlay == null) Build();
            if (overlay == null) return;
            overlay.gameObject.SetActive(true);
            Refresh();
        }

        public void Close()
        {
            if (overlay != null) overlay.gameObject.SetActive(false);
        }

        private void BackToStageList()
        {
            string regionId = currentStage != null ? currentStage.regionId : "forgotten_forest";
            Close();
            campaignSelectionUI?.OpenForRegion(regionId);
        }

        private void Build()
        {
            if (overlay != null) return;
            Transform parent = safeAreaRoot != null ? safeAreaRoot : transform;
            overlay = RoomUIFactory.CreateOverlay(parent, "StageDetailsOverlay");

            RectTransform window = RoomUIFactory.CreateWindow(overlay, "StageDetailsWindow",
                new Vector2(1000f, 1680f), new Color(0.075f, 0.085f, 0.12f, 0.98f));

            Button backBtn = RoomUIFactory.CreateButton(window, "BackToStagesButton", "< STAGES",
                new Vector2(-395f, 770f), new Vector2(170f, 68f), new Color(0.20f, 0.26f, 0.36f), 19);
            backBtn.onClick.AddListener(BackToStageList);

            stageTitleText = RoomUIFactory.CreateText(window, "StageDetailsTitle", "STAGE DETAILS", 35,
                new Vector2(0f, 770f), new Vector2(600f, 68f), new Color(0.96f, 0.78f, 0.43f));

            Button closeBtn = RoomUIFactory.CreateButton(window, "CloseStageDetailsButton", "CLOSE",
                new Vector2(405f, 770f), new Vector2(150f, 68f), new Color(0.34f, 0.19f, 0.19f), 20);
            closeBtn.onClick.AddListener(Close);

            stageDescText = RoomUIFactory.CreateText(window, "StageDetailsDesc", "", 20,
                new Vector2(0f, 690f), new Vector2(900f, 60f), new Color(0.84f, 0.88f, 0.94f));

            // Faction & Enemy Preview Card
            RectTransform intelBox = RoomUIFactory.CreatePanel(window, "EnemyIntelBox",
                new Color(0.12f, 0.14f, 0.19f, 1f), new Vector2(0f, 435f), new Vector2(920f, 410f));

            RoomUIFactory.CreateText(intelBox, "IntelHeader", "ENEMY FACTION & WAVE INTEL", 23,
                new Vector2(0f, 168f), new Vector2(880f, 38f), new Color(0.98f, 0.80f, 0.38f));

            factionInfoText = RoomUIFactory.CreateText(intelBox, "FactionInfoText", "", 19,
                new Vector2(0f, 95f), new Vector2(860f, 95f), Color.white, TextAnchor.MiddleLeft);

            enemyPreviewText = RoomUIFactory.CreateText(intelBox, "EnemyPreviewText", "", 18,
                new Vector2(0f, -15f), new Vector2(860f, 110f), new Color(0.78f, 0.88f, 0.96f), TextAnchor.MiddleLeft);

            bossInfoText = RoomUIFactory.CreateText(intelBox, "BossInfoText", "", 19,
                new Vector2(0f, -135f), new Vector2(860f, 80f), new Color(1.0f, 0.60f, 0.55f), TextAnchor.MiddleLeft);

            // Difficulty Selection Box
            RectTransform diffBox = RoomUIFactory.CreatePanel(window, "DifficultyBox",
                new Color(0.12f, 0.13f, 0.18f, 1f), new Vector2(0f, 65f), new Vector2(920f, 270f));

            RoomUIFactory.CreateText(diffBox, "DiffHeader", "SELECT DIFFICULTY", 23,
                new Vector2(0f, 98f), new Vector2(880f, 36f), new Color(0.98f, 0.80f, 0.38f));

            difficultyButtons.Clear();
            StageDifficulty[] diffLevels = (StageDifficulty[])Enum.GetValues(typeof(StageDifficulty));
            float startX = -352f;
            float stepX = 176f;

            for (int i = 0; i < diffLevels.Length; i++)
            {
                StageDifficulty level = diffLevels[i];
                string label = level == StageDifficulty.VeryHard ? "V.HARD" : level.ToString().ToUpperInvariant();
                Button btn = RoomUIFactory.CreateButton(diffBox, $"DiffBtn_{level}", label,
                    new Vector2(startX + i * stepX, 25f), new Vector2(162f, 68f), new Color(0.22f, 0.26f, 0.34f), 17);
                StageDifficulty captured = level;
                btn.onClick.AddListener(() => OnDifficultyClicked(captured));
                difficultyButtons[level] = btn;
            }

            difficultyInfoText = RoomUIFactory.CreateText(diffBox, "DifficultyInfoText", "", 18,
                new Vector2(0f, -68f), new Vector2(860f, 80f), new Color(0.84f, 0.88f, 0.95f));

            // Reward Preview Box
            RectTransform rewardBox = RoomUIFactory.CreatePanel(window, "RewardPreviewBox",
                new Color(0.12f, 0.16f, 0.15f, 1f), new Vector2(0f, -295f), new Vector2(920f, 380f));

            RoomUIFactory.CreateText(rewardBox, "RewardHeader", "EXPECTED VICTORY REWARDS", 23,
                new Vector2(0f, 145f), new Vector2(880f, 38f), new Color(0.55f, 0.96f, 0.68f));

            rewardPreviewText = RoomUIFactory.CreateText(rewardBox, "RewardPreviewText", "", 20,
                new Vector2(0f, -15f), new Vector2(860f, 260f), Color.white, TextAnchor.MiddleLeft);

            statusText = RoomUIFactory.CreateText(window, "StageDetailsStatus",
                "Review enemy faction and difficulty, then press START RAID.", 19,
                new Vector2(0f, -555f), new Vector2(900f, 52f), new Color(0.86f, 0.88f, 0.94f));

            startRaidButton = RoomUIFactory.CreateButton(window, "CampaignStartRaidButton", "START CAMPAIGN RAID",
                new Vector2(0f, -685f), new Vector2(540f, 92f), new Color(0.55f, 0.22f, 0.14f), 25);
            startRaidButton.onClick.AddListener(OnStartRaidClicked);

            overlay.gameObject.SetActive(false);
        }

        private void OnDifficultyClicked(StageDifficulty level)
        {
            EnsureReferences();
            if (difficultyManager != null && !difficultyManager.TrySelectDifficulty(level, out string error))
            {
                SetStatus(error);
                return;
            }
            selectedDifficulty = level;
            SetStatus($"Selected {level} difficulty.");
            Refresh();
        }

        public void Refresh()
        {
            EnsureReferences();
            if (currentStage == null) return;

            if (stageTitleText != null)
            {
                stageTitleText.text = $"STAGE {currentStage.stageNumber}: {currentStage.stageName.ToUpperInvariant()}";
            }
            if (stageDescText != null)
            {
                stageDescText.text = $"{currentStage.description}  (Recommended Dungeon Lv.{currentStage.recommendedDungeonLevel})";
            }

            AdventurerFactionData faction = campaignManager != null
                ? campaignManager.GetPrimaryFactionForStage(currentStage)
                : AdventurerFactionData.FindFaction("adventurer_guild");

            DifficultyData diffData = difficultyManager != null
                ? difficultyManager.GetDifficultyData(selectedDifficulty)
                : null;

            if (factionInfoText != null && faction != null)
            {
                string units = faction.adventurerTypes != null ? string.Join(", ", faction.adventurerTypes) : "Swordsman, Archer";
                string passive = faction.factionPassiveBonus != null ? faction.factionPassiveBonus.passiveDescription : "Balanced";
                factionInfoText.text = $"FACTION: {faction.factionName}   |   UNITS: {units}\n" +
                                       $"PASSIVE: {passive}\n{faction.description}";
            }

            if (enemyPreviewText != null)
            {
                int waveCount = currentStage.waveConfiguration != null ? currentStage.waveConfiguration.Count : 0;
                int totalRaiders = 0;
                float countMult = diffData != null ? diffData.enemyCountMultiplier : 1f;
                if (currentStage.waveConfiguration != null)
                {
                    foreach (WaveConfiguration w in currentStage.waveConfiguration)
                    {
                        if (w?.spawnGroups == null) continue;
                        foreach (WaveSpawnGroup g in w.spawnGroups)
                        {
                            if (g != null) totalRaiders += Mathf.Max(1, Mathf.RoundToInt(g.count * countMult));
                        }
                    }
                }
                enemyPreviewText.text = $"Total Waves: {waveCount}   •   Estimated Invaders ({selectedDifficulty}): ~{totalRaiders} Adventurers";
            }

            if (bossInfoText != null)
            {
                if (!string.IsNullOrWhiteSpace(currentStage.bossId))
                {
                    bossInfoText.text = $"BOSS ENCOUNTER: {currentStage.bossId.ToUpperInvariant()} appears on the final wave!";
                }
                else
                {
                    bossInfoText.text = "BOSS ENCOUNTER: None (Standard wave defense)";
                }
            }

            // Refresh Difficulty Buttons
            foreach (var kvp in difficultyButtons)
            {
                StageDifficulty lvl = kvp.Key;
                Button btn = kvp.Value;
                if (btn == null) continue;

                bool unlocked = difficultyManager == null || difficultyManager.IsDifficultyUnlocked(lvl, out _);
                bool isSelected = lvl == selectedDifficulty;
                Image img = btn.GetComponent<Image>();
                if (img != null)
                {
                    img.color = isSelected
                        ? new Color(0.65f, 0.42f, 0.14f)
                        : (unlocked ? new Color(0.22f, 0.28f, 0.38f) : new Color(0.16f, 0.16f, 0.20f));
                }
            }

            if (difficultyInfoText != null && diffData != null)
            {
                difficultyInfoText.text =
                    $"{diffData.difficultyName.ToUpperInvariant()}: Enemy HP x{diffData.enemyHealthMultiplier:0.00}  •  ATK x{diffData.enemyDamageMultiplier:0.00}  •  DEF x{diffData.enemyDefenseMultiplier:0.00}  •  Count x{diffData.enemyCountMultiplier:0.00}\n" +
                    $"Reward Multipliers: Gold x{diffData.rewardGoldMultiplier:0.00}  •  Soul x{diffData.rewardSoulMultiplier:0.00}  •  EXP x{diffData.rewardEXPMultiplier:0.00}";
            }

            if (rewardPreviewText != null)
            {
                bool fcAvailable = campaignManager == null || !campaignManager.IsFirstClearClaimed(currentStage.stageId);
                CampaignRewardBreakdown preview = CampaignRewardManager.PreviewStageReward(
                    currentStage, diffData, faction, fcAvailable, !string.IsNullOrWhiteSpace(currentStage.bossId));

                string fcLine = fcAvailable
                    ? $"First Clear Bonus: +{preview.FirstClearGold} Gold, +{preview.FirstClearSoul} Soul, +{preview.FirstClearEXP} EXP (AVAILABLE)"
                    : "First Clear Bonus: ALREADY CLAIMED";
                string bossLine = preview.BossBonusGold > 0
                    ? $"Boss Victory Bonus: +{preview.BossBonusGold} Gold, +{preview.BossBonusSoul} Soul, +{preview.BossBonusEXP} EXP"
                    : "Boss Victory Bonus: —";

                rewardPreviewText.text =
                    $"Stage Clear Reward ({selectedDifficulty}): +{preview.BaseGold} Gold, +{preview.BaseSoul} Soul, +{preview.BaseEXP} EXP\n" +
                    $"{fcLine}\n" +
                    $"{bossLine}\n\n" +
                    $"TOTAL EXPECTED REWARD: +{preview.TotalGold} GOLD   +{preview.TotalSoul} SOUL   +{preview.TotalEXP} EXP";
            }
        }

        private void OnStartRaidClicked()
        {
            EnsureReferences();
            if (campaignManager == null || currentStage == null) return;

            if (!campaignManager.TryStartCampaignRaid(currentStage.stageId, selectedDifficulty, roomManager, out string error))
            {
                SetStatus(error);
            }
        }

        private void SetStatus(string message)
        {
            if (statusText != null) statusText.text = message;
        }
    }
}
