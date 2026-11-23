using System;
using System.Collections.Generic;
using UnityEngine;

namespace DreamDungeon
{
    public sealed class CampaignManager : MonoBehaviour
    {
        public static CampaignManager Instance { get; private set; }

        [SerializeField] private List<CampaignData> campaigns = new List<CampaignData>();
        [SerializeField] private List<CampaignStageData> stages = new List<CampaignStageData>();
        [SerializeField] private StageUnlockManager stageUnlockManager;
        [SerializeField] private CampaignRewardManager rewardManager;

        private readonly Dictionary<string, CampaignData> campaignsById = new Dictionary<string, CampaignData>(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, CampaignStageData> stagesById = new Dictionary<string, CampaignStageData>(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, StageProgressRecord> progressByStageId = new Dictionary<string, StageProgressRecord>(StringComparer.OrdinalIgnoreCase);

        private readonly HashSet<string> unlockedStageIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        private readonly HashSet<string> completedStageIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        private readonly HashSet<string> firstClearStageIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        private readonly HashSet<string> perfectClearStageIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        private readonly HashSet<string> defeatedBossIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        private readonly HashSet<string> claimedBattleResultIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        private int totalAttempts;
        private int totalVictories;
        private int totalDefeats;
        private string selectedCampaignId = "campaign_forgotten_forest";
        private string selectedStageId = "forest_stage_1";
        private bool isStartingCampaignRaid;
        private bool isInitializing;

        public IReadOnlyList<CampaignData> AllCampaigns
        {
            get { EnsureInitialized(); return campaigns; }
        }

        public IReadOnlyList<CampaignStageData> AllStages
        {
            get { EnsureInitialized(); return stages; }
        }

        public StageUnlockManager UnlockManager => stageUnlockManager;
        public CampaignRewardManager RewardManager => rewardManager;
        public string SelectedCampaignId => selectedCampaignId;
        public string SelectedStageId => selectedStageId;
        public int TotalAttempts => totalAttempts;
        public int TotalVictories => totalVictories;
        public int TotalDefeats => totalDefeats;

        public event Action OnCampaignUpdated;
        public event Action<CampaignStageData, CampaignRewardBreakdown> OnStageCompleted;
        public event Action<CampaignStageData> OnStageDefeated;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                if (transform.parent == null && gameObject.GetComponents<Component>().Length <= 2)
                    Destroy(gameObject);
                else
                    Destroy(this);
                return;
            }
            Instance = this;
            if (transform.parent == null && gameObject.GetComponents<Component>().Length <= 2)
            {
                DontDestroyOnLoad(gameObject);
            }

            EnsureInitialized();
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        public void EnsureInitialized()
        {
            if (isInitializing) return;
            if (campaigns != null && campaigns.Count > 0 && stages != null && stages.Count > 0 &&
                stageUnlockManager != null && rewardManager != null && campaignsById.Count > 0 && stagesById.Count > 0)
            {
                return;
            }

            isInitializing = true;
            try
            {
                if (campaigns == null || campaigns.Count == 0 || stages == null || stages.Count == 0)
                {
                    BuildDefaultCampaignsAndStages();
                }

                if (stageUnlockManager == null)
                    stageUnlockManager = GetComponent<StageUnlockManager>() ?? gameObject.AddComponent<StageUnlockManager>();
                if (rewardManager == null)
                    rewardManager = GetComponent<CampaignRewardManager>() ?? gameObject.AddComponent<CampaignRewardManager>();

                WorldMapManager worldMap = WorldMapManager.Instance ?? FindAnyObjectByType<WorldMapManager>();
                if (worldMap == null)
                {
                    worldMap = gameObject.GetComponent<WorldMapManager>() ?? gameObject.AddComponent<WorldMapManager>();
                }
                worldMap.EnsureInitialized();
                worldMap.UnlockManager?.Initialize(worldMap, this);
                stageUnlockManager.Initialize(this, worldMap);

                RebuildLookupCaches();
                LoadSavedCampaignStateIfPresent();
                stageUnlockManager.EvaluateAllStages(notifyNewUnlocks: false);
                worldMap.UnlockManager?.EvaluateAllRegions(notifyNewUnlocks: false);
            }
            finally
            {
                isInitializing = false;
            }
        }

        private void LoadSavedCampaignStateIfPresent()
        {
            if (!PlayerPrefs.HasKey("DreamDungeon_CampaignData")) return;
            try
            {
                string cJson = PlayerPrefs.GetString("DreamDungeon_CampaignData", string.Empty);
                if (string.IsNullOrEmpty(cJson)) return;
                var cState = JsonUtility.FromJson<CampaignSaveState>(cJson);
                if (cState != null)
                {
                    ImportSaveState(cState);
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[CampaignManager] Could not auto-load saved campaign progress: {ex.Message}");
            }
        }

        private void RebuildLookupCaches()
        {
            campaignsById.Clear();
            stagesById.Clear();

            foreach (CampaignData c in campaigns)
            {
                if (c == null || string.IsNullOrWhiteSpace(c.campaignId)) continue;
                campaignsById[c.campaignId.Trim()] = c;
            }

            foreach (CampaignStageData s in stages)
            {
                if (s == null || string.IsNullOrWhiteSpace(s.stageId)) continue;
                string id = s.stageId.Trim();
                stagesById[id] = s;
                GetOrCreateRecord(s);
            }

            // Ensure first stage of starting region is unlocked
            unlockedStageIds.Add("forest_stage_1");
            if (progressByStageId.TryGetValue("forest_stage_1", out StageProgressRecord firstRec) &&
                firstRec.status == CampaignStageStatus.Locked)
            {
                firstRec.status = CampaignStageStatus.Available;
            }
        }

        public CampaignData GetCampaign(string campaignId)
        {
            EnsureInitialized();
            if (string.IsNullOrWhiteSpace(campaignId)) return null;
            return campaignsById.TryGetValue(campaignId.Trim(), out CampaignData c) ? c : null;
        }

        public CampaignData GetCampaignForRegion(string regionId)
        {
            EnsureInitialized();
            if (string.IsNullOrWhiteSpace(regionId)) return null;
            foreach (CampaignData c in campaigns)
            {
                if (c != null && string.Equals(c.regionId, regionId.Trim(), StringComparison.OrdinalIgnoreCase))
                    return c;
            }
            return null;
        }

        public CampaignStageData GetStage(string stageId)
        {
            EnsureInitialized();
            if (string.IsNullOrWhiteSpace(stageId)) return null;
            return stagesById.TryGetValue(stageId.Trim(), out CampaignStageData s) ? s : null;
        }

        public List<CampaignStageData> GetStagesForRegion(string regionId)
        {
            EnsureInitialized();
            var list = new List<CampaignStageData>();
            if (string.IsNullOrWhiteSpace(regionId)) return list;
            foreach (CampaignStageData s in stages)
            {
                if (s != null && string.Equals(s.regionId, regionId.Trim(), StringComparison.OrdinalIgnoreCase))
                    list.Add(s);
            }
            list.Sort((a, b) => a.stageNumber.CompareTo(b.stageNumber));
            return list;
        }

        public CampaignStageData GetNextStage(string currentStageId)
        {
            CampaignStageData current = GetStage(currentStageId);
            if (current == null) return null;

            List<CampaignStageData> regionStages = GetStagesForRegion(current.regionId);
            for (int i = 0; i < regionStages.Count; i++)
            {
                if (string.Equals(regionStages[i].stageId, current.stageId, StringComparison.OrdinalIgnoreCase) && i + 1 < regionStages.Count)
                {
                    return regionStages[i + 1];
                }
            }

            // Or first stage of next region
            WorldMapManager worldMap = WorldMapManager.Instance ?? FindAnyObjectByType<WorldMapManager>();
            if (worldMap != null)
            {
                WorldRegionData curRegion = worldMap.GetRegion(current.regionId);
                if (curRegion != null)
                {
                    foreach (WorldRegionData r in worldMap.GetAllRegions())
                    {
                        if (r != null && r.regionOrder == curRegion.regionOrder + 1 && worldMap.IsRegionUnlocked(r.regionId))
                        {
                            List<CampaignStageData> nextRegionStages = GetStagesForRegion(r.regionId);
                            if (nextRegionStages.Count > 0) return nextRegionStages[0];
                        }
                    }
                }
            }
            return null;
        }

        public StageProgressRecord GetOrCreateRecord(CampaignStageData stage)
        {
            if (stage == null || string.IsNullOrWhiteSpace(stage.stageId)) return null;
            string id = stage.stageId.Trim();
            if (!progressByStageId.TryGetValue(id, out StageProgressRecord rec))
            {
                bool isFirst = string.Equals(id, "forest_stage_1", StringComparison.OrdinalIgnoreCase);
                rec = new StageProgressRecord
                {
                    stageId = id,
                    status = isFirst ? CampaignStageStatus.Available : CampaignStageStatus.Locked,
                    bestDifficulty = stage.difficulty,
                    bestWave = 0,
                    totalAttempts = 0,
                    totalVictories = 0,
                    totalDefeats = 0,
                    firstClearClaimed = false,
                    perfectClearAchieved = false
                };
                progressByStageId[id] = rec;
                if (isFirst) unlockedStageIds.Add(id);
            }
            return rec;
        }

        public StageProgressRecord GetStageRecord(string stageId)
        {
            EnsureInitialized();
            if (string.IsNullOrWhiteSpace(stageId)) return null;
            CampaignStageData s = GetStage(stageId);
            return s != null ? GetOrCreateRecord(s) : null;
        }

        public bool IsStageUnlocked(string stageId)
        {
            EnsureInitialized();
            if (string.IsNullOrWhiteSpace(stageId)) return false;
            return unlockedStageIds.Contains(stageId.Trim());
        }

        public bool IsStageCompleted(string stageId)
        {
            EnsureInitialized();
            if (string.IsNullOrWhiteSpace(stageId)) return false;
            return completedStageIds.Contains(stageId.Trim());
        }

        public bool IsFirstClearClaimed(string stageId)
        {
            EnsureInitialized();
            if (string.IsNullOrWhiteSpace(stageId)) return false;
            return firstClearStageIds.Contains(stageId.Trim());
        }

        public bool IsPerfectClear(string stageId)
        {
            EnsureInitialized();
            if (string.IsNullOrWhiteSpace(stageId)) return false;
            return perfectClearStageIds.Contains(stageId.Trim());
        }

        public bool HasDefeatedBoss(string bossId)
        {
            EnsureInitialized();
            if (string.IsNullOrWhiteSpace(bossId)) return true;
            return defeatedBossIds.Contains(bossId.Trim());
        }

        public bool IsCampaignCompleted(string campaignId)
        {
            CampaignData campaign = GetCampaign(campaignId);
            if (campaign == null || campaign.stageIds == null || campaign.stageIds.Count == 0) return false;
            foreach (string sId in campaign.stageIds)
            {
                if (!IsStageCompleted(sId)) return false;
            }
            return true;
        }

        public void UnlockStageInternal(string stageId)
        {
            if (string.IsNullOrWhiteSpace(stageId)) return;
            string id = stageId.Trim();
            unlockedStageIds.Add(id);
            if (progressByStageId.TryGetValue(id, out StageProgressRecord rec) && rec.status == CampaignStageStatus.Locked)
            {
                rec.status = CampaignStageStatus.Available;
            }
        }

        public bool TrySelectStage(string stageId, out string error)
        {
            error = string.Empty;
            EnsureInitialized();
            CampaignStageData stage = GetStage(stageId);
            if (stage == null)
            {
                error = $"Stage '{stageId}' does not exist.";
                return false;
            }

            if (!IsStageUnlocked(stage.stageId))
            {
                if (stageUnlockManager != null && stageUnlockManager.CheckStageUnlockRequirements(stage, out error))
                {
                    UnlockStageInternal(stage.stageId);
                }
                else
                {
                    if (string.IsNullOrEmpty(error)) error = $"Stage '{stage.stageName}' is locked.";
                    return false;
                }
            }

            if (IsStageCompleted(stage.stageId) && !stage.replayable)
            {
                error = $"Stage '{stage.stageName}' has already been completed and is not replayable.";
                return false;
            }

            selectedStageId = stage.stageId;
            CampaignData campaign = GetCampaignForRegion(stage.regionId);
            if (campaign != null) selectedCampaignId = campaign.campaignId;
            error = string.Empty;
            OnCampaignUpdated?.Invoke();
            return true;
        }

        public AdventurerFactionData GetPrimaryFactionForStage(CampaignStageData stage)
        {
            if (stage != null && stage.adventurerGroupIds != null && stage.adventurerGroupIds.Count > 0)
            {
                return AdventurerFactionData.FindFaction(stage.adventurerGroupIds[0]);
            }
            WorldMapManager wm = WorldMapManager.Instance ?? FindAnyObjectByType<WorldMapManager>();
            WorldRegionData region = stage != null && wm != null ? wm.GetRegion(stage.regionId) : null;
            return AdventurerFactionData.FindFaction(region != null ? region.primaryFactionId : "adventurer_guild");
        }

        public bool TryStartCampaignRaid(string stageId, StageDifficulty difficulty, RoomManager roomManager, out string error)
        {
            if (isStartingCampaignRaid)
            {
                error = "A campaign raid is already starting.";
                return false;
            }

            EnsureInitialized();
            if (!TrySelectStage(stageId, out error))
            {
                return false;
            }

            DifficultyManager diffMgr = DifficultyManager.Instance ?? FindAnyObjectByType<DifficultyManager>() ?? gameObject.AddComponent<DifficultyManager>();
            if (!diffMgr.TrySelectDifficulty(difficulty, out error))
            {
                return false;
            }

            CampaignStageData stage = GetStage(stageId);
            StageProgressRecord rec = GetOrCreateRecord(stage);

            // Validate current Dungeon Layout before starting raid (never deletes player layout)
            if (roomManager == null) roomManager = RoomManager.Instance ?? FindAnyObjectByType<RoomManager>();
            if (roomManager == null)
            {
                error = "Dungeon Room Manager is unavailable.";
                return false;
            }
            roomManager.Initialize();
            DungeonLayoutData layout = roomManager.Layout;
            if (layout == null || layout.rooms == null)
            {
                error = "Dungeon Layout is missing.";
                return false;
            }
            if (layout.rooms.FindAll(r => r != null && r.roomType == DungeonRoomType.Entrance).Count != 1)
            {
                error = "Raid cannot start: place exactly one Entrance.";
                return false;
            }
            if (layout.rooms.FindAll(r => r != null && r.roomType == DungeonRoomType.DungeonCore).Count != 1)
            {
                error = "Raid cannot start: place exactly one Dungeon Core.";
                return false;
            }
            if (roomManager.Pathfinder == null || !roomManager.Pathfinder.TryFindRoute(layout, out List<Vector2Int> route) || route == null || route.Count < 2)
            {
                error = "Raid cannot start: no walkable Path connects Entrance to Dungeon Core.";
                return false;
            }

            isStartingCampaignRaid = true;
            try
            {
                if (rec != null && rec.status == CampaignStageStatus.Available)
                {
                    rec.status = CampaignStageStatus.InProgress;
                }

                AdventurerFactionData faction = GetPrimaryFactionForStage(stage);
                CurrencyManager currency = roomManager.Currency ?? CurrencyManager.Instance;
                int gold = currency != null ? currency.Gold : roomManager.Gold;
                int soul = currency != null ? currency.Soul : 0;
                int crystal = currency != null ? currency.Crystal : 0;

                BattleSession.BeginCampaign(
                    roomManager.LayoutToJson(),
                    "DungeonEditor",
                    gold,
                    soul,
                    crystal,
                    stage.regionId,
                    selectedCampaignId,
                    stage.stageId,
                    difficulty,
                    faction != null ? faction.factionId : "adventurer_guild",
                    stage.bossId);

                if (!SceneLoader.Load("Battle"))
                {
                    error = "Raid cannot start: Battle scene is not in Build Settings.";
                    return false;
                }

                error = string.Empty;
                return true;
            }
            finally
            {
                isStartingCampaignRaid = false;
            }
        }

        public CampaignRewardBreakdown RecordCampaignBattleOutcome(
            string battleResultId,
            string stageId,
            StageDifficulty difficultyLevel,
            bool victory,
            int wavesCleared,
            int totalWaves,
            int adventurersDefeated,
            int bossesDefeated,
            int coreHpRemaining,
            int coreMaxHp,
            bool grantImmediateCurrency = false)
        {
            EnsureInitialized();
            CampaignStageData stage = GetStage(stageId);
            if (stage == null) return default;

            string resultKey = string.IsNullOrWhiteSpace(battleResultId)
                ? $"CAMP_RES_{stageId}_{Guid.NewGuid():N}"
                : $"CAMP_RES_{battleResultId.Trim()}";

            StageProgressRecord rec = GetOrCreateRecord(stage);
            DifficultyManager diffMgr = DifficultyManager.Instance ?? FindAnyObjectByType<DifficultyManager>() ?? gameObject.AddComponent<DifficultyManager>();
            DifficultyData diffData = diffMgr.GetDifficultyData(difficultyLevel);
            AdventurerFactionData faction = GetPrimaryFactionForStage(stage);
            WorldMapManager worldMap = WorldMapManager.Instance ?? FindAnyObjectByType<WorldMapManager>();
            WorldRegionData region = worldMap != null ? worldMap.GetRegion(stage.regionId) : null;

            // Prevent duplicate reward processing for the same battleResultId
            if (!claimedBattleResultIds.Add(resultKey))
            {
                return CampaignRewardManager.CalculateBattleOutcome(
                    stage, diffData, faction, victory, wavesCleared, totalWaves,
                    adventurersDefeated, bossesDefeated, coreHpRemaining, coreMaxHp,
                    isFirstClearAvailable: false, isRegionCompletedNow: false, regionData: region);
            }

            totalAttempts++;
            rec.totalAttempts++;
            if (wavesCleared > rec.bestWave) rec.bestWave = wavesCleared;

            if (!victory)
            {
                totalDefeats++;
                rec.totalDefeats++;
                if (rec.status == CampaignStageStatus.InProgress)
                {
                    rec.status = completedStageIds.Contains(stage.stageId)
                        ? (perfectClearStageIds.Contains(stage.stageId) ? CampaignStageStatus.PerfectClear : CampaignStageStatus.Completed)
                        : CampaignStageStatus.Available;
                }

                CampaignRewardBreakdown defeatBreakdown = CampaignRewardManager.CalculateBattleOutcome(
                    stage, diffData, faction, false, wavesCleared, totalWaves,
                    adventurersDefeated, bossesDefeated, coreHpRemaining, coreMaxHp,
                    false, false, region);

                OnStageDefeated?.Invoke(stage);
                OnCampaignUpdated?.Invoke();
                PersistCampaignProgressImmediately();
                return defeatBreakdown;
            }

            // Victory handling
            totalVictories++;
            rec.totalVictories++;
            if (difficultyLevel > rec.bestDifficulty) rec.bestDifficulty = difficultyLevel;

            bool isFirstClear = !firstClearStageIds.Contains(stage.stageId) && !rec.firstClearClaimed;
            completedStageIds.Add(stage.stageId);
            firstClearStageIds.Add(stage.stageId);
            rec.firstClearClaimed = true;

            if (!string.IsNullOrWhiteSpace(stage.bossId) || bossesDefeated > 0)
            {
                if (!string.IsNullOrWhiteSpace(stage.bossId))
                    defeatedBossIds.Add(stage.bossId.Trim());
            }

            // Check if this victory completes the region for the first time
            bool wasRegionClaimed = worldMap != null && worldMap.IsRegionRewardClaimed(stage.regionId);
            bool allRegionStagesDone = true;
            if (region != null && region.availableStageIds != null)
            {
                foreach (string sId in region.availableStageIds)
                {
                    if (!completedStageIds.Contains(sId))
                    {
                        allRegionStagesDone = false;
                        break;
                    }
                }
            }
            bool isRegionCompletedNow = allRegionStagesDone && !wasRegionClaimed;

            CampaignRewardBreakdown breakdown = CampaignRewardManager.CalculateBattleOutcome(
                stage, diffData, faction, true, wavesCleared, totalWaves,
                adventurersDefeated, bossesDefeated, coreHpRemaining, coreMaxHp,
                isFirstClear, isRegionCompletedNow, region);

            if (breakdown.IsPerfectClear)
            {
                perfectClearStageIds.Add(stage.stageId);
                rec.perfectClearAchieved = true;
                rec.status = CampaignStageStatus.PerfectClear;
            }
            else if (rec.status != CampaignStageStatus.PerfectClear)
            {
                rec.status = CampaignStageStatus.Completed;
            }

            if (allRegionStagesDone && worldMap != null)
            {
                worldMap.MarkRegionCompleted(stage.regionId);
                if (isRegionCompletedNow)
                {
                    worldMap.MarkRegionRewardClaimed(stage.regionId);
                }
            }

            // Evaluate Stage & Region Unlocks
            stageUnlockManager?.EvaluateAllStages(notifyNewUnlocks: true);
            worldMap?.UnlockManager?.EvaluateAllRegions(notifyNewUnlocks: true);

            // Grant rewards
            rewardManager?.GrantBreakdownRewards(breakdown, grantImmediateCurrency);

            // Notify Quest & Achievement systems
            int bossCount = (!string.IsNullOrWhiteSpace(stage.bossId) || bossesDefeated > 0) ? Mathf.Max(1, bossesDefeated) : 0;
            QuestManager.Instance?.NotifyBattleResult(resultKey, true, wavesCleared, adventurersDefeated, bossCount);
            AchievementManager.Instance?.NotifyBattleResult(resultKey, true, wavesCleared, adventurersDefeated, bossCount);

            OnStageCompleted?.Invoke(stage, breakdown);
            OnCampaignUpdated?.Invoke();
            PersistCampaignProgressImmediately();
            return breakdown;
        }

        private void PersistCampaignProgressImmediately()
        {
            try
            {
                // Always persist directly to PlayerPrefs even when in Battle scene where SaveManager.Instance may be null
                var cState = ExportSaveState();
                PlayerPrefs.SetString("DreamDungeon_CampaignData", JsonUtility.ToJson(cState, false));
                PlayerPrefs.Save();
            }
            catch (Exception ex)
            {
                Debug.LogError($"[CampaignManager] Failed to persist campaign progress: {ex.Message}");
            }

            if (SaveManager.Instance != null)
            {
                SaveManager.Instance.SaveAchievements();
                SaveManager.Instance.SaveGame();
            }
        }

        public CampaignSaveState ExportSaveState()
        {
            EnsureInitialized();
            var save = new CampaignSaveState
            {
                saveVersion = 1,
                totalAttempts = totalAttempts,
                totalVictories = totalVictories,
                totalDefeats = totalDefeats
            };

            WorldMapManager worldMap = WorldMapManager.Instance ?? FindAnyObjectByType<WorldMapManager>();
            worldMap?.ExportToSave(save);

            save.unlockedStageIds.AddRange(unlockedStageIds);
            save.completedStageIds.AddRange(completedStageIds);
            save.firstClearStageIds.AddRange(firstClearStageIds);
            save.perfectClearStageIds.AddRange(perfectClearStageIds);
            save.defeatedBossIds.AddRange(defeatedBossIds);
            save.claimedBattleResultIds.AddRange(claimedBattleResultIds);

            foreach (var kvp in progressByStageId)
            {
                if (kvp.Value == null) continue;
                save.stageRecords.Add(new StageProgressRecord
                {
                    stageId = kvp.Value.stageId,
                    status = kvp.Value.status,
                    bestDifficulty = kvp.Value.bestDifficulty,
                    bestWave = kvp.Value.bestWave,
                    totalAttempts = kvp.Value.totalAttempts,
                    totalVictories = kvp.Value.totalVictories,
                    totalDefeats = kvp.Value.totalDefeats,
                    firstClearClaimed = kvp.Value.firstClearClaimed,
                    perfectClearAchieved = kvp.Value.perfectClearAchieved
                });
            }

            return save;
        }

        public void ImportSaveState(CampaignSaveState save)
        {
            EnsureInitialized();
            if (save == null) return;

            totalAttempts = Mathf.Max(totalAttempts, save.totalAttempts);
            totalVictories = Mathf.Max(totalVictories, save.totalVictories);
            totalDefeats = Mathf.Max(totalDefeats, save.totalDefeats);

            if (save.unlockedStageIds != null)
            {
                foreach (string id in save.unlockedStageIds)
                    if (!string.IsNullOrWhiteSpace(id)) unlockedStageIds.Add(id.Trim());
            }
            if (save.completedStageIds != null)
            {
                foreach (string id in save.completedStageIds)
                {
                    if (!string.IsNullOrWhiteSpace(id))
                    {
                        completedStageIds.Add(id.Trim());
                        unlockedStageIds.Add(id.Trim());
                    }
                }
            }
            if (save.firstClearStageIds != null)
            {
                foreach (string id in save.firstClearStageIds)
                    if (!string.IsNullOrWhiteSpace(id)) firstClearStageIds.Add(id.Trim());
            }
            if (save.perfectClearStageIds != null)
            {
                foreach (string id in save.perfectClearStageIds)
                    if (!string.IsNullOrWhiteSpace(id)) perfectClearStageIds.Add(id.Trim());
            }
            if (save.defeatedBossIds != null)
            {
                foreach (string id in save.defeatedBossIds)
                    if (!string.IsNullOrWhiteSpace(id)) defeatedBossIds.Add(id.Trim());
            }
            if (save.claimedBattleResultIds != null)
            {
                foreach (string id in save.claimedBattleResultIds)
                    if (!string.IsNullOrWhiteSpace(id)) claimedBattleResultIds.Add(id.Trim());
            }

            if (save.stageRecords != null)
            {
                foreach (StageProgressRecord incoming in save.stageRecords)
                {
                    if (incoming == null || string.IsNullOrWhiteSpace(incoming.stageId)) continue;
                    string id = incoming.stageId.Trim();
                    CampaignStageStatus mergedStatus = incoming.status;
                    if (perfectClearStageIds.Contains(id) || incoming.perfectClearAchieved)
                        mergedStatus = CampaignStageStatus.PerfectClear;
                    else if (completedStageIds.Contains(id))
                        mergedStatus = CampaignStageStatus.Completed;
                    else if (unlockedStageIds.Contains(id) && mergedStatus == CampaignStageStatus.Locked)
                        mergedStatus = CampaignStageStatus.Available;

                    progressByStageId[id] = new StageProgressRecord
                    {
                        stageId = id,
                        status = mergedStatus,
                        bestDifficulty = incoming.bestDifficulty,
                        bestWave = incoming.bestWave,
                        totalAttempts = incoming.totalAttempts,
                        totalVictories = incoming.totalVictories,
                        totalDefeats = incoming.totalDefeats,
                        firstClearClaimed = incoming.firstClearClaimed || firstClearStageIds.Contains(id),
                        perfectClearAchieved = incoming.perfectClearAchieved || perfectClearStageIds.Contains(id)
                    };
                }
            }

            WorldMapManager worldMap = WorldMapManager.Instance ?? FindAnyObjectByType<WorldMapManager>();
            worldMap?.ImportFromSave(save);

            stageUnlockManager?.EvaluateAllStages(notifyNewUnlocks: false);
            worldMap?.UnlockManager?.EvaluateAllRegions(notifyNewUnlocks: false);
            OnCampaignUpdated?.Invoke();
        }

        private void BuildDefaultCampaignsAndStages()
        {
            campaigns = new List<CampaignData>();
            stages = new List<CampaignStageData>();

            // Region 1: Forgotten Forest (Easy — tuned to be clearable with 1,000 starting Gold)
            AddRegionCampaign("campaign_forgotten_forest", "Forgotten Forest Campaign",
                "Repel the Adventurer Guild expeditions probing the woodland dungeon entrance.",
                "forgotten_forest", 1, "", 450, 90, 160,
                new[]
                {
                    MakeStage("forest_stage_1", "Forest Border", "Scout party of rookie swordsmen.", "forgotten_forest", 1,
                        StageDifficulty.Easy, 1, "adventurer_guild", "", "", 250, 40, 80, 200, 30, 60,
                        MakeWaves("forest_s1", 2, new[] { "swordsman", "archer" }, 1, false, "")),
                    MakeStage("forest_stage_2", "Mossy Ruins", "Mixed swordsmen and archers advance deeper.", "forgotten_forest", 2,
                        StageDifficulty.Easy, 1, "adventurer_guild", "forest_stage_1", "", 300, 50, 100, 220, 35, 70,
                        MakeWaves("forest_s2", 3, new[] { "swordsman", "archer" }, 1, false, "")),
                    MakeStage("forest_stage_3", "Ancient Grove Champion", "Guild captain leads a full raid force.", "forgotten_forest", 3,
                        StageDifficulty.Easy, 2, "adventurer_guild", "forest_stage_2", "cave_dragon", 380, 75, 140, 280, 55, 110,
                        MakeWaves("forest_s3", 3, new[] { "swordsman", "archer", "knight" }, 2, true, "cave_dragon"))
                });

            // Region 2: Cursed Cave (Normal, Holy Order)
            AddRegionCampaign("campaign_cursed_cave", "Cursed Cave Campaign",
                "Defend the underground caverns against the Holy Order's purifying crusade.",
                "cursed_cave", 2, "campaign_forgotten_forest", 700, 150, 250,
                new[]
                {
                    MakeStage("cave_stage_1", "Echoing Tunnel", "Holy Order vanguard with regenerative wards.", "cursed_cave", 1,
                        StageDifficulty.Normal, 2, "holy_order", "forest_stage_3", "", 340, 75, 145, 220, 45, 90,
                        MakeWaves("cave_s1", 3, new[] { "knight", "swordsman" }, 2, false, "")),
                    MakeStage("cave_stage_2", "Crypt Crossroads", "Paladins and Priests march in formation.", "cursed_cave", 2,
                        StageDifficulty.Normal, 3, "holy_order", "cave_stage_1", "", 400, 90, 175, 260, 55, 110,
                        MakeWaves("cave_s2", 3, new[] { "knight", "swordsman", "mage" }, 2, false, "")),
                    MakeStage("cave_stage_3", "Sanctified Cavern", "High Inquisitor assault on the cavern core.", "cursed_cave", 3,
                        StageDifficulty.Normal, 3, "holy_order", "cave_stage_2", "cave_dragon", 480, 115, 225, 340, 75, 150,
                        MakeWaves("cave_s3", 4, new[] { "knight", "mage", "swordsman" }, 2, true, "cave_dragon"))
                });

            // Region 3: Volcanic Land (Hard, Shadow Assassins)
            AddRegionCampaign("campaign_volcanic_land", "Volcanic Land Campaign",
                "Outmaneuver the lightning-fast Shadow Assassins striking across lava bridges.",
                "volcanic_land", 3, "campaign_cursed_cave", 1100, 250, 400,
                new[]
                {
                    MakeStage("volcano_stage_1", "Ashfall Ridge", "Rapid rogue ambush teams rush the path.", "volcanic_land", 1,
                        StageDifficulty.Hard, 4, "shadow_assassins", "cave_stage_3", "", 500, 120, 240, 300, 70, 130,
                        MakeWaves("volc_s1", 4, new[] { "swordsman", "archer" }, 4, false, "")),
                    MakeStage("volcano_stage_2", "Magma Vent", "Coordinated assassin strike squads.", "volcanic_land", 2,
                        StageDifficulty.Hard, 4, "shadow_assassins", "volcano_stage_1", "", 580, 140, 280, 360, 85, 160,
                        MakeWaves("volc_s2", 5, new[] { "swordsman", "archer" }, 4, false, "")),
                    MakeStage("volcano_stage_3", "Caldera Citadel", "Master Assassin and elite raiders.", "volcanic_land", 3,
                        StageDifficulty.Hard, 5, "shadow_assassins", "volcano_stage_2", "dark_golem", 700, 175, 350, 450, 110, 220,
                        MakeWaves("volc_s3", 5, new[] { "swordsman", "archer", "knight" }, 5, true, "dark_golem"))
                });

            // Region 4: Frozen Wasteland (Hard, Arcane Council)
            AddRegionCampaign("campaign_frozen_wasteland", "Frozen Wasteland Campaign",
                "Weather the long-range spell barrages of the Arcane Council in subzero ruins.",
                "frozen_wasteland", 4, "campaign_volcanic_land", 1500, 350, 550,
                new[]
                {
                    MakeStage("frozen_stage_1", "Frostbite Pass", "Arcane apprentices and ranged support.", "frozen_wasteland", 1,
                        StageDifficulty.Hard, 5, "arcane_council", "volcano_stage_3", "", 720, 180, 360, 420, 100, 200,
                        MakeWaves("froz_s1", 4, new[] { "mage", "archer" }, 4, false, "")),
                    MakeStage("frozen_stage_2", "Glacier Spire", "Heavy spellcaster formation advances.", "frozen_wasteland", 2,
                        StageDifficulty.Hard, 6, "arcane_council", "frozen_stage_1", "", 840, 210, 420, 500, 120, 240,
                        MakeWaves("froz_s2", 5, new[] { "mage", "archer", "knight" }, 4, false, "")),
                    MakeStage("frozen_stage_3", "Archmage Conclave", "Grand Archmage siege on the frozen core.", "frozen_wasteland", 3,
                        StageDifficulty.Hard, 6, "arcane_council", "frozen_stage_2", "dark_golem", 980, 250, 500, 620, 150, 300,
                        MakeWaves("froz_s3", 5, new[] { "mage", "archer", "knight" }, 5, true, "dark_golem"))
                });

            // Region 5: Demon Territory (Very Hard, Dragon Hunters)
            AddRegionCampaign("campaign_demon_territory", "Demon Territory Campaign",
                "Hold the line against veteran Dragon Hunters equipped to slay dungeon bosses.",
                "demon_territory", 5, "campaign_frozen_wasteland", 2200, 500, 800,
                new[]
                {
                    MakeStage("demon_stage_1", "Obsidian Gate", "Dragon Hunter vanguard breaches the gate.", "demon_territory", 1,
                        StageDifficulty.VeryHard, 7, "dragon_hunters", "frozen_stage_3", "", 1100, 280, 550, 650, 160, 320,
                        MakeWaves("dem_s1", 5, new[] { "knight", "archer", "swordsman" }, 4, false, "")),
                    MakeStage("demon_stage_2", "Hellfire Causeway", "Elite rangers and slayers push forward.", "demon_territory", 2,
                        StageDifficulty.VeryHard, 8, "dragon_hunters", "demon_stage_1", "", 1300, 330, 650, 780, 190, 380,
                        MakeWaves("dem_s2", 5, new[] { "knight", "archer", "swordsman" }, 5, false, "")),
                    MakeStage("demon_stage_3", "Dragon Slayer Bastion", "Grand Dragon Slayer commander assault.", "demon_territory", 3,
                        StageDifficulty.VeryHard, 8, "dragon_hunters", "demon_stage_2", "demon_lord", 1550, 400, 800, 950, 240, 480,
                        MakeWaves("dem_s3", 6, new[] { "knight", "archer", "swordsman", "mage" }, 5, true, "demon_lord"))
                });

            // Region 6: Abyssal Realm (Nightmare, Demon Hunters)
            AddRegionCampaign("campaign_abyssal_realm", "Abyssal Realm Campaign",
                "The final nightmare siege against legendary Demon Hunters in the Abyssal Realm.",
                "abyssal_realm", 6, "campaign_demon_territory", 3500, 900, 1400,
                new[]
                {
                    MakeStage("abyss_stage_1", "Void Rift", "Demon Hunter crusade enters the Abyss.", "abyssal_realm", 1,
                        StageDifficulty.Nightmare, 9, "demon_hunters", "demon_stage_3", "", 1800, 450, 900, 1100, 280, 550,
                        MakeWaves("aby_s1", 5, new[] { "knight", "mage", "archer", "swordsman" }, 5, false, "")),
                    MakeStage("abyss_stage_2", "Eclipse Sanctum", "Relentless waves of legendary hunters.", "abyssal_realm", 2,
                        StageDifficulty.Nightmare, 9, "demon_hunters", "abyss_stage_1", "", 2100, 520, 1050, 1300, 340, 650,
                        MakeWaves("aby_s2", 6, new[] { "knight", "mage", "archer", "swordsman" }, 5, false, "")),
                    MakeStage("abyss_stage_3", "Throne of the Abyss", "Ultimate showdown for the heart of the dungeon.", "abyssal_realm", 3,
                        StageDifficulty.Nightmare, 10, "demon_hunters", "abyss_stage_2", "demon_lord", 2600, 680, 1350, 1800, 450, 900,
                        MakeWaves("aby_s3", 6, new[] { "knight", "mage", "archer", "swordsman" }, 6, true, "demon_lord"))
                });
        }

        private void AddRegionCampaign(string campaignId, string name, string desc, string regionId, int order,
            string unlockReq, int compGold, int compSoul, int compExp, CampaignStageData[] regionStages)
        {
            CampaignData c = ScriptableObject.CreateInstance<CampaignData>();
            c.name = campaignId;
            c.campaignId = campaignId;
            c.campaignName = name;
            c.description = desc;
            c.regionId = regionId;
            c.campaignOrder = order;
            c.unlockRequirements = unlockReq;
            c.completionReward = new StageRewardInfo { gold = compGold, soul = compSoul, exp = compExp };
            c.stageIds = new List<string>();

            foreach (CampaignStageData s in regionStages)
            {
                stages.Add(s);
                c.stageIds.Add(s.stageId);
            }
            campaigns.Add(c);
        }

        private static CampaignStageData MakeStage(string id, string title, string desc, string regionId, int num,
            StageDifficulty diff, int recLvl, string factionId, string unlockReq, string bossId,
            int gold, int soul, int exp, int fcGold, int fcSoul, int fcExp, List<WaveConfiguration> waveList)
        {
            CampaignStageData s = ScriptableObject.CreateInstance<CampaignStageData>();
            s.name = id;
            s.stageId = id;
            s.stageName = title;
            s.description = desc;
            s.regionId = regionId;
            s.stageNumber = num;
            s.difficulty = diff;
            s.recommendedDungeonLevel = recLvl;
            s.adventurerGroupIds = new List<string> { factionId };
            s.unlockRequirements = unlockReq;
            s.bossId = bossId;
            s.rewardGold = gold;
            s.rewardSoul = soul;
            s.rewardDungeonEXP = exp;
            s.firstClearReward = new StageRewardInfo { gold = fcGold, soul = fcSoul, exp = fcExp };
            s.waveConfiguration = waveList;
            s.replayable = true;
            s.perfectClearCoreHpRatio = 0.70f;
            return s;
        }

        private static List<WaveConfiguration> MakeWaves(string prefix, int waveCount, string[] unitPool, int baseUnitsPerGroup, bool hasBossOnFinalWave, string bossId)
        {
            var list = new List<WaveConfiguration>();
            for (int w = 1; w <= waveCount; w++)
            {
                WaveConfiguration wc = ScriptableObject.CreateInstance<WaveConfiguration>();
                wc.name = $"{prefix}_w{w}";
                wc.waveId = $"{prefix}_w{w}";
                wc.waveNumber = w;
                wc.spawnInterval = Mathf.Max(0.45f, 0.95f - w * 0.06f);
                wc.spawnDelay = 0.25f;
                wc.maxAdventurers = 30;
                wc.bossWave = hasBossOnFinalWave && w == waveCount;
                wc.bossId = wc.bossWave ? bossId : string.Empty;
                wc.spawnGroups = new List<WaveSpawnGroup>();

                for (int i = 0; i < unitPool.Length; i++)
                {
                    if (w == 1 && i > 1) continue;
                    wc.spawnGroups.Add(new WaveSpawnGroup
                    {
                        adventurerType = unitPool[i],
                        count = Mathf.Max(1, baseUnitsPerGroup + (w - 1) - i),
                        spawnInterval = wc.spawnInterval,
                        spawnOrder = i
                    });
                }
                list.Add(wc);
            }
            return list;
        }
    }
}
