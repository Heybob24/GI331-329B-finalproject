using System;
using System.Collections.Generic;
using UnityEngine;

namespace DreamDungeon
{
    public sealed class AchievementManager : MonoBehaviour
    {
        public static AchievementManager Instance { get; private set; }

        [SerializeField] private AchievementDatabase database;

        private readonly Dictionary<string, AchievementProgressEntry> entries = new Dictionary<string, AchievementProgressEntry>(StringComparer.OrdinalIgnoreCase);
        private readonly HashSet<string> claimedRewardIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        private readonly HashSet<string> processedEventIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        private bool isClaiming;
        private bool suppressTracking;
        private bool subscribed;

        public AchievementDatabase Database => database;

        public event Action OnAchievementsUpdated;
        public event Action<AchievementData, AchievementProgressEntry> OnAchievementProgressChanged;
        public event Action<AchievementData> OnAchievementUnlocked;
        public event Action<AchievementData, int, int, int> OnAchievementRewardClaimed;

        public void Configure(AchievementDatabase achievementDb)
        {
            database = achievementDb;
        }

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

            EnsureDatabase();
            InitializeAchievements();
        }

        private void Start()
        {
            SubscribeToGameEvents();
            SyncCumulativeObjectives();
        }

        private void OnEnable()
        {
            SubscribeToGameEvents();
        }

        private void OnDisable()
        {
            UnsubscribeFromGameEvents();
        }

        private void OnDestroy()
        {
            UnsubscribeFromGameEvents();
            if (Instance == this) Instance = null;
        }

        public void EnsureDatabase()
        {
            if (database == null)
            {
                database = Resources.Load<AchievementDatabase>("DreamDungeon/AchievementDatabase");
                if (database == null)
                {
                    database = AchievementDatabase.CreateRuntimeDefaultDatabase();
                }
            }
        }

        public void InitializeAchievements()
        {
            EnsureDatabase();
            if (database == null) return;

            foreach (AchievementData a in database.AllAchievements)
            {
                if (a == null || string.IsNullOrWhiteSpace(a.achievementId)) continue;
                GetOrCreateEntry(a);
            }
        }

        public AchievementProgressEntry GetProgressEntry(string achievementId)
        {
            if (string.IsNullOrWhiteSpace(achievementId)) return null;
            return entries.TryGetValue(achievementId.Trim(), out AchievementProgressEntry entry) ? entry : null;
        }

        public AchievementProgressEntry GetOrCreateEntry(AchievementData achievement)
        {
            if (achievement == null || string.IsNullOrWhiteSpace(achievement.achievementId)) return null;
            string id = achievement.achievementId.Trim();
            if (!entries.TryGetValue(id, out AchievementProgressEntry entry))
            {
                entry = new AchievementProgressEntry
                {
                    achievementId = id,
                    currentProgress = 0,
                    isUnlocked = false,
                    isRewardClaimed = false,
                    rewardClaimId = string.Empty,
                    unlockedTimestamp = 0
                };
                entries[id] = entry;
            }
            return entry;
        }

        public List<AchievementData> GetAchievementsByCategory(AchievementCategory category)
        {
            EnsureDatabase();
            var list = new List<AchievementData>();
            if (database == null) return list;
            foreach (AchievementData a in database.GetByCategory(category))
            {
                if (a != null) list.Add(a);
            }
            return list;
        }

        public bool ReportProgress(QuestObjectiveType objective, int amount = 1, string eventId = null, bool isAbsoluteValue = false)
        {
            if (amount <= 0) return false;
            EnsureDatabase();
            if (database == null) return false;

            if (!string.IsNullOrWhiteSpace(eventId))
            {
                if (!processedEventIds.Add(eventId.Trim()))
                    return false;
            }

            bool anyChanged = false;
            foreach (AchievementData ach in database.AllAchievements)
            {
                if (ach == null || ach.objectiveType != objective) continue;
                AchievementProgressEntry entry = GetOrCreateEntry(ach);
                if (entry == null) continue;

                int prev = entry.currentProgress;
                int next = isAbsoluteValue
                    ? Mathf.Clamp(amount, entry.currentProgress, ach.targetAmount)
                    : Mathf.Clamp(entry.currentProgress + amount, 0, ach.targetAmount);

                if (next != prev || (!entry.isUnlocked && next >= ach.targetAmount))
                {
                    entry.currentProgress = next;
                    ach.currentProgress = next;
                    anyChanged = true;

                    if (!entry.isUnlocked && entry.currentProgress >= ach.targetAmount)
                    {
                        entry.currentProgress = ach.targetAmount;
                        entry.isUnlocked = true;
                        entry.unlockedTimestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                        ach.isUnlocked = true;
                        OnAchievementProgressChanged?.Invoke(ach, entry);
                        OnAchievementUnlocked?.Invoke(ach);
                    }
                    else
                    {
                        OnAchievementProgressChanged?.Invoke(ach, entry);
                    }
                }
            }

            if (anyChanged)
            {
                OnAchievementsUpdated?.Invoke();
                SaveManager.Instance?.SaveAchievements();
            }

            return anyChanged;
        }

        public void NotifyBattleResult(string battleResultId, bool victory, int wavesCompleted, int adventurersDefeated, int bossesDefeated)
        {
            string baseId = string.IsNullOrWhiteSpace(battleResultId) ? Guid.NewGuid().ToString("N") : battleResultId.Trim();
            if (!processedEventIds.Add("BATTLE_ACH_" + baseId)) return;

            ReportProgress(QuestObjectiveType.CompleteRaid, 1);
            if (wavesCompleted > 0)
            {
                ReportProgress(QuestObjectiveType.CompleteWave, wavesCompleted);
            }
            if (adventurersDefeated > 0)
            {
                ReportProgress(QuestObjectiveType.DefeatAdventurer, adventurersDefeated);
            }
            if (bossesDefeated > 0 || (victory && HasPlacedBoss()))
            {
                ReportProgress(QuestObjectiveType.DefeatBoss, Mathf.Max(1, bossesDefeated));
            }
        }

        private static bool HasPlacedBoss()
        {
            if (RoomManager.Instance?.Layout?.bosses != null && RoomManager.Instance.Layout.bosses.Count > 0)
                return true;
            if (!string.IsNullOrEmpty(BattleSession.LayoutJson))
            {
                try
                {
                    var layout = DungeonLayoutData.FromJson(BattleSession.LayoutJson);
                    return layout?.bosses != null && layout.bosses.Count > 0;
                }
                catch { }
            }
            return false;
        }

        public bool CanClaimReward(string achievementId, out string error)
        {
            EnsureDatabase();
            AchievementData ach = database != null ? database.FindById(achievementId) : null;
            if (ach == null)
            {
                error = $"Achievement '{achievementId}' does not exist.";
                return false;
            }

            AchievementProgressEntry entry = GetOrCreateEntry(ach);
            if (entry == null || !entry.isUnlocked)
            {
                error = "Achievement is not unlocked yet.";
                return false;
            }
            string claimId = $"ACH_CLAIM_{ach.achievementId}";
            if (entry.isRewardClaimed || claimedRewardIds.Contains(claimId))
            {
                error = "Achievement reward has already been claimed.";
                return false;
            }

            error = string.Empty;
            return true;
        }

        public bool TryClaimReward(string achievementId, out string message)
        {
            if (isClaiming)
            {
                message = "A reward claim is already in progress.";
                return false;
            }

            isClaiming = true;
            try
            {
                if (!CanClaimReward(achievementId, out message))
                {
                    return false;
                }

                AchievementData ach = database.FindById(achievementId);
                AchievementProgressEntry entry = GetOrCreateEntry(ach);
                string claimId = $"ACH_CLAIM_{ach.achievementId}";

                entry.isRewardClaimed = true;
                entry.rewardClaimId = claimId;
                ach.isRewardClaimed = true;
                claimedRewardIds.Add(claimId);

                CurrencyManager currency = CurrencyManager.Instance ?? FindAnyObjectByType<CurrencyManager>();
                if (currency != null && (ach.rewardGold > 0 || ach.rewardSoul > 0))
                {
                    currency.TryAdd(ach.rewardGold, ach.rewardSoul, 0, out _);
                }

                DungeonProgressionManager progression = DungeonProgressionManager.Instance ?? FindAnyObjectByType<DungeonProgressionManager>();
                if (progression != null && ach.rewardDungeonEXP > 0)
                {
                    progression.AddExp(ach.rewardDungeonEXP);
                }

                message = $"Claimed '{ach.achievementName}' rewards: +{ach.rewardGold} Gold, +{ach.rewardSoul} Soul, +{ach.rewardDungeonEXP} EXP!";
                OnAchievementRewardClaimed?.Invoke(ach, ach.rewardGold, ach.rewardSoul, ach.rewardDungeonEXP);
                OnAchievementsUpdated?.Invoke();
                SaveManager.Instance?.SaveAchievements();
                SaveManager.Instance?.SaveGame();
                return true;
            }
            finally
            {
                isClaiming = false;
            }
        }

        public void SyncCumulativeObjectives()
        {
            int currentLevel = DungeonProgressionManager.Instance != null ? DungeonProgressionManager.Instance.CurrentLevel : 1;
            if (currentLevel > 1)
            {
                ReportProgress(QuestObjectiveType.ReachDungeonLevel, currentLevel, isAbsoluteValue: true);
            }
            int coreLevel = DungeonCoreUpgradeManager.Instance != null ? DungeonCoreUpgradeManager.Instance.CoreLevel : 1;
            if (coreLevel > 1)
            {
                ReportProgress(QuestObjectiveType.UpgradeCore, coreLevel, isAbsoluteValue: true);
            }
            if (DungeonFacilityManager.Instance != null)
            {
                var fac = DungeonFacilityManager.Instance;
                if (fac.GoldFacilityLevel >= DungeonFacilityManager.MaxFacilityLevel ||
                    fac.SoulFacilityLevel >= DungeonFacilityManager.MaxFacilityLevel ||
                    fac.CapacityFacilityLevel >= DungeonFacilityManager.MaxCapacityLevel)
                {
                    ReportProgress(QuestObjectiveType.MaximizeFacility, 1, isAbsoluteValue: true);
                }
            }
        }

        public AchievementSaveState ExportSaveState()
        {
            EnsureDatabase();
            var save = new AchievementSaveState { saveVersion = 1 };
            foreach (var kvp in entries)
            {
                if (kvp.Value != null)
                {
                    save.entries.Add(new AchievementProgressEntry
                    {
                        achievementId = kvp.Value.achievementId,
                        currentProgress = kvp.Value.currentProgress,
                        isUnlocked = kvp.Value.isUnlocked,
                        isRewardClaimed = kvp.Value.isRewardClaimed,
                        rewardClaimId = kvp.Value.rewardClaimId,
                        unlockedTimestamp = kvp.Value.unlockedTimestamp
                    });
                }
            }
            save.claimedRewardIds.AddRange(claimedRewardIds);
            save.processedEventIds.AddRange(processedEventIds);
            return save;
        }

        public void ImportSaveState(AchievementSaveState save)
        {
            EnsureDatabase();
            if (save == null) return;

            suppressTracking = true;
            try
            {
                if (save.claimedRewardIds != null)
                {
                    foreach (string id in save.claimedRewardIds)
                        if (!string.IsNullOrWhiteSpace(id)) claimedRewardIds.Add(id.Trim());
                }

                if (save.processedEventIds != null)
                {
                    foreach (string id in save.processedEventIds)
                        if (!string.IsNullOrWhiteSpace(id)) processedEventIds.Add(id.Trim());
                }

                if (save.entries != null)
                {
                    foreach (AchievementProgressEntry incoming in save.entries)
                    {
                        if (incoming == null || string.IsNullOrWhiteSpace(incoming.achievementId)) continue;
                        string id = incoming.achievementId.Trim();
                        AchievementData def = database != null ? database.FindById(id) : null;
                        int target = def != null ? def.targetAmount : Mathf.Max(1, incoming.currentProgress);

                        // Never downgrade permanent achievement progress
                        entries.TryGetValue(id, out AchievementProgressEntry existing);
                        int mergedProgress = Mathf.Clamp(
                            Mathf.Max(existing != null ? existing.currentProgress : 0, incoming.currentProgress),
                            0, target);
                        bool mergedUnlocked = (existing != null && existing.isUnlocked) || incoming.isUnlocked || mergedProgress >= target;
                        bool mergedClaimed = (existing != null && existing.isRewardClaimed) || incoming.isRewardClaimed || claimedRewardIds.Contains($"ACH_CLAIM_{id}");

                        var merged = new AchievementProgressEntry
                        {
                            achievementId = id,
                            currentProgress = mergedProgress,
                            isUnlocked = mergedUnlocked,
                            isRewardClaimed = mergedClaimed,
                            rewardClaimId = !string.IsNullOrEmpty(incoming.rewardClaimId) ? incoming.rewardClaimId : (existing?.rewardClaimId ?? string.Empty),
                            unlockedTimestamp = incoming.unlockedTimestamp > 0 ? incoming.unlockedTimestamp : (existing?.unlockedTimestamp ?? 0)
                        };
                        entries[id] = merged;

                        if (def != null)
                        {
                            def.currentProgress = merged.currentProgress;
                            def.isUnlocked = merged.isUnlocked;
                            def.isRewardClaimed = merged.isRewardClaimed;
                        }
                    }
                }

                if (database != null)
                {
                    foreach (AchievementData a in database.AllAchievements)
                    {
                        if (a != null) GetOrCreateEntry(a);
                    }
                }

                OnAchievementsUpdated?.Invoke();
            }
            finally
            {
                suppressTracking = false;
            }
        }

        private void SubscribeToGameEvents()
        {
            if (subscribed) return;
            subscribed = true;

            if (CurrencyManager.Instance != null)
            {
                CurrencyManager.Instance.CurrencyAdded -= OnCurrencyAdded;
                CurrencyManager.Instance.CurrencyAdded += OnCurrencyAdded;
            }

            if (DungeonProgressionManager.Instance != null)
            {
                DungeonProgressionManager.Instance.OnLevelUp -= OnDungeonLevelUp;
                DungeonProgressionManager.Instance.OnLevelUp += OnDungeonLevelUp;
            }

            if (DungeonCoreUpgradeManager.Instance != null)
            {
                DungeonCoreUpgradeManager.Instance.OnCoreUpgraded -= OnCoreUpgraded;
                DungeonCoreUpgradeManager.Instance.OnCoreUpgraded += OnCoreUpgraded;
            }

            if (DungeonExpansionManager.Instance != null)
            {
                DungeonExpansionManager.Instance.OnGridExpanded -= OnGridExpanded;
                DungeonExpansionManager.Instance.OnGridExpanded += OnGridExpanded;
            }

            if (DungeonFacilityManager.Instance != null)
            {
                DungeonFacilityManager.Instance.OnFacilityUpgraded -= OnFacilityUpgraded;
                DungeonFacilityManager.Instance.OnFacilityUpgraded += OnFacilityUpgraded;
            }

            if (RoomManager.Instance != null)
            {
                RoomManager.Instance.RoomPlaced -= OnRoomPlaced;
                RoomManager.Instance.RoomPlaced += OnRoomPlaced;
                RoomManager.Instance.MonsterPlaced -= OnMonsterPlaced;
                RoomManager.Instance.MonsterPlaced += OnMonsterPlaced;
                RoomManager.Instance.TrapPlaced -= OnTrapPlaced;
                RoomManager.Instance.TrapPlaced += OnTrapPlaced;
            }
        }

        private void UnsubscribeFromGameEvents()
        {
            if (!subscribed) return;
            subscribed = false;

            if (CurrencyManager.Instance != null)
                CurrencyManager.Instance.CurrencyAdded -= OnCurrencyAdded;
            if (DungeonProgressionManager.Instance != null)
                DungeonProgressionManager.Instance.OnLevelUp -= OnDungeonLevelUp;
            if (DungeonCoreUpgradeManager.Instance != null)
                DungeonCoreUpgradeManager.Instance.OnCoreUpgraded -= OnCoreUpgraded;
            if (DungeonExpansionManager.Instance != null)
                DungeonExpansionManager.Instance.OnGridExpanded -= OnGridExpanded;
            if (DungeonFacilityManager.Instance != null)
                DungeonFacilityManager.Instance.OnFacilityUpgraded -= OnFacilityUpgraded;
            if (RoomManager.Instance != null)
            {
                RoomManager.Instance.RoomPlaced -= OnRoomPlaced;
                RoomManager.Instance.MonsterPlaced -= OnMonsterPlaced;
                RoomManager.Instance.TrapPlaced -= OnTrapPlaced;
            }
        }

        private void OnCurrencyAdded(int goldAmount, int soulAmount, int crystalAmount)
        {
            if (suppressTracking) return;
            if (goldAmount > 0) ReportProgress(QuestObjectiveType.EarnGold, goldAmount);
            if (soulAmount > 0) ReportProgress(QuestObjectiveType.EarnSoul, soulAmount);
        }

        private void OnDungeonLevelUp(int newLevel)
        {
            if (suppressTracking) return;
            ReportProgress(QuestObjectiveType.ReachDungeonLevel, newLevel, isAbsoluteValue: true);
        }

        private void OnCoreUpgraded(int newLevel, int maxHp, int defense)
        {
            if (suppressTracking) return;
            if (newLevel > 1)
            {
                ReportProgress(QuestObjectiveType.UpgradeCore, newLevel, isAbsoluteValue: true);
            }
        }

        private void OnGridExpanded(int width, int height)
        {
            if (suppressTracking) return;
            if (width > 9 || height > 9)
            {
                ReportProgress(QuestObjectiveType.ExpandDungeon, 1);
            }
        }

        private void OnFacilityUpgraded(string facilityName, int newLevel)
        {
            if (suppressTracking) return;
            bool isMax = false;
            if (DungeonFacilityManager.Instance != null)
            {
                var fac = DungeonFacilityManager.Instance;
                isMax = fac.GoldFacilityLevel >= DungeonFacilityManager.MaxFacilityLevel ||
                        fac.SoulFacilityLevel >= DungeonFacilityManager.MaxFacilityLevel ||
                        fac.CapacityFacilityLevel >= DungeonFacilityManager.MaxCapacityLevel;
            }
            else
            {
                isMax = newLevel >= DungeonFacilityManager.MaxFacilityLevel;
            }

            if (isMax)
            {
                ReportProgress(QuestObjectiveType.MaximizeFacility, 1, isAbsoluteValue: true);
            }
        }

        private void OnRoomPlaced(DungeonRoomInstanceData room)
        {
            if (suppressTracking || room == null) return;
            ReportProgress(QuestObjectiveType.PlaceRoom, 1, eventId: "ACH_PLACE_ROOM_" + room.instanceId);
        }

        private void OnMonsterPlaced(MonsterInstanceData monster)
        {
            if (suppressTracking || monster == null) return;
            ReportProgress(QuestObjectiveType.PlaceMonster, 1, eventId: "ACH_PLACE_MONSTER_" + monster.instanceId);
        }

        private void OnTrapPlaced(TrapInstanceData trap)
        {
            if (suppressTracking || trap == null) return;
            ReportProgress(QuestObjectiveType.PlaceTrap, 1, eventId: "ACH_PLACE_TRAP_" + trap.instanceId);
        }
    }
}
