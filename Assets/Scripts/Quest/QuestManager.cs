using System;
using System.Collections.Generic;
using UnityEngine;

namespace DreamDungeon
{
    public sealed class QuestManager : MonoBehaviour
    {
        public static QuestManager Instance { get; private set; }

        [SerializeField] private QuestDatabase database;
        [SerializeField] private MainQuestManager mainQuestManager;
        [SerializeField] private DailyQuestManager dailyQuestManager;
        [SerializeField] private WeeklyQuestManager weeklyQuestManager;

        private readonly Dictionary<string, QuestProgressEntry> entries = new Dictionary<string, QuestProgressEntry>(StringComparer.OrdinalIgnoreCase);
        private readonly HashSet<string> unlockedMainQuestIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        private readonly HashSet<string> claimedRewardIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        private readonly HashSet<string> processedEventIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        private ITimeProvider timeProvider = new SystemTimeProvider();
        private bool isClaiming;
        private bool suppressEarnTracking;
        private bool subscribed;

        public QuestDatabase Database => database;
        public MainQuestManager MainQuests => mainQuestManager;
        public DailyQuestManager DailyQuests => dailyQuestManager;
        public WeeklyQuestManager WeeklyQuests => weeklyQuestManager;
        public DateTime UtcNow => timeProvider != null ? timeProvider.UtcNow : DateTime.UtcNow;

        public event Action OnQuestsUpdated;
        public event Action<QuestData, QuestProgressEntry> OnQuestProgressChanged;
        public event Action<QuestData> OnQuestCompleted;
        public event Action<QuestData, int, int, int> OnQuestRewardClaimed;
        public event Action OnDailyQuestsRefreshed;
        public event Action OnWeeklyQuestsRefreshed;

        public void Configure(QuestDatabase questDb, MainQuestManager mainMgr = null, DailyQuestManager dailyMgr = null, WeeklyQuestManager weeklyMgr = null)
        {
            database = questDb;
            if (mainMgr != null) mainQuestManager = mainMgr;
            if (dailyMgr != null) dailyQuestManager = dailyMgr;
            if (weeklyMgr != null) weeklyQuestManager = weeklyMgr;
        }

        public void SetTimeProvider(ITimeProvider provider)
        {
            timeProvider = provider ?? new SystemTimeProvider();
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

            EnsureDatabaseAndSubManagers();
            InitializeQuests();
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

        public void EnsureDatabaseAndSubManagers()
        {
            if (database == null)
            {
                database = Resources.Load<QuestDatabase>("DreamDungeon/QuestDatabase");
                if (database == null)
                {
                    database = QuestDatabase.CreateRuntimeDefaultDatabase();
                }
            }

            if (mainQuestManager == null)
                mainQuestManager = GetComponent<MainQuestManager>() ?? gameObject.AddComponent<MainQuestManager>();
            if (dailyQuestManager == null)
                dailyQuestManager = GetComponent<DailyQuestManager>() ?? gameObject.AddComponent<DailyQuestManager>();
            if (weeklyQuestManager == null)
                weeklyQuestManager = GetComponent<WeeklyQuestManager>() ?? gameObject.AddComponent<WeeklyQuestManager>();
        }

        public void InitializeQuests()
        {
            EnsureDatabaseAndSubManagers();
            if (database == null) return;

            foreach (QuestData q in database.AllQuests)
            {
                if (q == null || string.IsNullOrWhiteSpace(q.questId)) continue;
                GetOrCreateEntry(q);
            }

            mainQuestManager.Initialize(this);
            dailyQuestManager.Initialize(this, null, string.Empty);
            weeklyQuestManager.Initialize(this, null, string.Empty);
            SyncCumulativeObjectives();
        }

        public QuestProgressEntry GetProgressEntry(string questId)
        {
            if (string.IsNullOrWhiteSpace(questId)) return null;
            return entries.TryGetValue(questId.Trim(), out QuestProgressEntry entry) ? entry : null;
        }

        public QuestProgressEntry GetOrCreateEntry(QuestData quest)
        {
            if (quest == null || string.IsNullOrWhiteSpace(quest.questId)) return null;
            string id = quest.questId.Trim();
            if (!entries.TryGetValue(id, out QuestProgressEntry entry))
            {
                bool locked = quest.questType == QuestType.Main && !string.IsNullOrWhiteSpace(quest.unlockRequirement) && !unlockedMainQuestIds.Contains(id);
                entry = new QuestProgressEntry
                {
                    questId = id,
                    currentProgress = 0,
                    status = locked ? QuestStatus.Locked : QuestStatus.InProgress,
                    isRewardClaimed = false,
                    rewardClaimId = string.Empty,
                    lastUpdatedTimestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
                };
                entries[id] = entry;
            }
            return entry;
        }

        public void MarkMainQuestUnlocked(string questId)
        {
            if (!string.IsNullOrWhiteSpace(questId))
                unlockedMainQuestIds.Add(questId.Trim());
        }

        public void ResetQuestEntryForCycle(string questId, string cycleKey)
        {
            if (string.IsNullOrWhiteSpace(questId)) return;
            QuestData quest = database != null ? database.FindById(questId) : null;
            QuestProgressEntry entry = quest != null ? GetOrCreateEntry(quest) : GetProgressEntry(questId);
            if (entry == null) return;

            if (!string.IsNullOrEmpty(entry.rewardClaimId))
            {
                claimedRewardIds.Remove(entry.rewardClaimId);
            }

            entry.currentProgress = 0;
            entry.status = QuestStatus.InProgress;
            entry.isRewardClaimed = false;
            entry.rewardClaimId = string.Empty;
            entry.lastUpdatedTimestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        }

        public void NotifyDailyQuestsRefreshed()
        {
            OnDailyQuestsRefreshed?.Invoke();
            OnQuestsUpdated?.Invoke();
            SaveManager.Instance?.SaveGame();
        }

        public void NotifyWeeklyQuestsRefreshed()
        {
            OnWeeklyQuestsRefreshed?.Invoke();
            OnQuestsUpdated?.Invoke();
            SaveManager.Instance?.SaveGame();
        }

        public void CheckTimeBasedResets()
        {
            EnsureDatabaseAndSubManagers();
            bool dailyChanged = dailyQuestManager != null && dailyQuestManager.CheckAndResetIfNeeded(true);
            bool weeklyChanged = weeklyQuestManager != null && weeklyQuestManager.CheckAndResetIfNeeded(true);
            if (dailyChanged || weeklyChanged)
            {
                OnQuestsUpdated?.Invoke();
                SaveManager.Instance?.SaveGame();
            }
        }

        public List<QuestData> GetQuestsForTab(QuestType tabType)
        {
            EnsureDatabaseAndSubManagers();
            CheckTimeBasedResets();

            if (tabType == QuestType.Daily)
            {
                return dailyQuestManager != null ? dailyQuestManager.GetActiveDailyQuests() : new List<QuestData>();
            }
            if (tabType == QuestType.Weekly)
            {
                return weeklyQuestManager != null ? weeklyQuestManager.GetActiveWeeklyQuests() : new List<QuestData>();
            }

            var list = new List<QuestData>();
            if (database == null) return list;
            foreach (QuestData q in database.GetByType(tabType))
            {
                if (q != null && q.isActive) list.Add(q);
            }
            return list;
        }

        public bool ReportObjectiveProgress(QuestObjectiveType objective, int amount = 1, string eventId = null, string targetFilterId = null, bool isAbsoluteValue = false)
        {
            if (amount <= 0) return false;
            EnsureDatabaseAndSubManagers();

            if (!string.IsNullOrWhiteSpace(eventId))
            {
                if (!processedEventIds.Add(eventId.Trim()))
                {
                    return false;
                }
            }

            CheckTimeBasedResets();

            bool anyChanged = false;
            var candidateQuests = new List<QuestData>();

            // Gather active quests across all 4 categories
            candidateQuests.AddRange(database.GetByType(QuestType.Main));
            candidateQuests.AddRange(dailyQuestManager != null ? dailyQuestManager.GetActiveDailyQuests() : (IEnumerable<QuestData>)Array.Empty<QuestData>());
            candidateQuests.AddRange(weeklyQuestManager != null ? weeklyQuestManager.GetActiveWeeklyQuests() : (IEnumerable<QuestData>)Array.Empty<QuestData>());
            candidateQuests.AddRange(database.GetByType(QuestType.Special));

            foreach (QuestData quest in candidateQuests)
            {
                if (quest == null || !quest.isActive || quest.objectiveType != objective) continue;

                if (!string.IsNullOrWhiteSpace(quest.targetIdFilter) &&
                    !string.IsNullOrWhiteSpace(targetFilterId) &&
                    !string.Equals(quest.targetIdFilter.Trim(), targetFilterId.Trim(), StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                QuestProgressEntry entry = GetOrCreateEntry(quest);
                if (entry == null) continue;

                // Locked, Completed, or Claimed quests do not advance
                if (entry.status == QuestStatus.Locked || entry.status == QuestStatus.Completed || entry.status == QuestStatus.Claimed)
                    continue;

                int previousProgress = entry.currentProgress;
                int nextProgress = isAbsoluteValue
                    ? Mathf.Clamp(amount, entry.currentProgress, quest.targetAmount)
                    : Mathf.Clamp(entry.currentProgress + amount, 0, quest.targetAmount);

                if (nextProgress != previousProgress)
                {
                    entry.currentProgress = nextProgress;
                    entry.lastUpdatedTimestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                    anyChanged = true;

                    if (entry.currentProgress >= quest.targetAmount)
                    {
                        entry.currentProgress = quest.targetAmount;
                        entry.status = QuestStatus.Completed;
                        OnQuestProgressChanged?.Invoke(quest, entry);
                        OnQuestCompleted?.Invoke(quest);

                        if (quest.questType == QuestType.Main && mainQuestManager != null)
                        {
                            mainQuestManager.EvaluateMainQuestSequence(notifyNewUnlocks: true);
                        }
                    }
                    else
                    {
                        OnQuestProgressChanged?.Invoke(quest, entry);
                    }
                }
            }

            if (anyChanged)
            {
                OnQuestsUpdated?.Invoke();
                SaveManager.Instance?.SaveGame();
            }

            return anyChanged;
        }

        public void NotifyBattleResult(string battleResultId, bool victory, int wavesCompleted, int adventurersDefeated, int bossesDefeated)
        {
            string baseId = string.IsNullOrWhiteSpace(battleResultId) ? Guid.NewGuid().ToString("N") : battleResultId.Trim();
            if (!processedEventIds.Add("BATTLE_QUEST_" + baseId)) return;

            ReportObjectiveProgress(QuestObjectiveType.CompleteRaid, 1);
            if (victory)
            {
                ReportObjectiveProgress(QuestObjectiveType.WinRaid, 1);
            }
            if (wavesCompleted > 0)
            {
                ReportObjectiveProgress(QuestObjectiveType.CompleteWave, wavesCompleted);
            }
            if (adventurersDefeated > 0)
            {
                ReportObjectiveProgress(QuestObjectiveType.DefeatAdventurer, adventurersDefeated);
            }
            if (bossesDefeated > 0 || (victory && HasPlacedBossInCurrentLayout()))
            {
                ReportObjectiveProgress(QuestObjectiveType.DefeatBoss, Mathf.Max(1, bossesDefeated));
            }
        }

        private static bool HasPlacedBossInCurrentLayout()
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

        public bool CanClaimReward(string questId, out string error)
        {
            EnsureDatabaseAndSubManagers();
            QuestData quest = database != null ? database.FindById(questId) : null;
            if (quest == null)
            {
                error = $"Quest '{questId}' does not exist.";
                return false;
            }

            QuestProgressEntry entry = GetOrCreateEntry(quest);
            if (entry == null || entry.status == QuestStatus.Locked)
            {
                error = "This quest is still locked.";
                return false;
            }
            if (entry.isRewardClaimed || entry.status == QuestStatus.Claimed)
            {
                error = "Quest reward has already been claimed.";
                return false;
            }
            if (entry.status != QuestStatus.Completed || entry.currentProgress < quest.targetAmount)
            {
                error = "Quest objective is not yet completed.";
                return false;
            }

            string stableClaimId = GetStableClaimId(quest);
            if (claimedRewardIds.Contains(stableClaimId))
            {
                error = "Quest reward has already been claimed.";
                return false;
            }

            error = string.Empty;
            return true;
        }

        public bool TryClaimReward(string questId, out string message)
        {
            if (isClaiming)
            {
                message = "A reward claim is already in progress.";
                return false;
            }

            isClaiming = true;
            try
            {
                if (!CanClaimReward(questId, out message))
                {
                    return false;
                }

                QuestData quest = database.FindById(questId);
                QuestProgressEntry entry = GetOrCreateEntry(quest);
                string stableClaimId = GetStableClaimId(quest);

                entry.isRewardClaimed = true;
                entry.status = QuestStatus.Claimed;
                entry.rewardClaimId = stableClaimId;
                entry.lastUpdatedTimestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                claimedRewardIds.Add(stableClaimId);

                // Grant Gold & Soul via CurrencyManager
                CurrencyManager currency = CurrencyManager.Instance ?? FindAnyObjectByType<CurrencyManager>();
                if (currency != null && (quest.rewardGold > 0 || quest.rewardSoul > 0))
                {
                    currency.TryAdd(quest.rewardGold, quest.rewardSoul, 0, out _);
                }

                // Grant Dungeon EXP via DungeonProgressionManager
                DungeonProgressionManager progression = DungeonProgressionManager.Instance ?? FindAnyObjectByType<DungeonProgressionManager>();
                if (progression != null && quest.rewardDungeonEXP > 0)
                {
                    progression.AddExp(quest.rewardDungeonEXP);
                }

                // Grant optional item rewards via InventoryManagers
                GrantItemRewards(quest.rewardItems);

                if (quest.questType == QuestType.Main && mainQuestManager != null)
                {
                    mainQuestManager.EvaluateMainQuestSequence(notifyNewUnlocks: true);
                }

                message = $"Claimed '{quest.questName}' rewards: +{quest.rewardGold} Gold, +{quest.rewardSoul} Soul, +{quest.rewardDungeonEXP} EXP!";
                OnQuestRewardClaimed?.Invoke(quest, quest.rewardGold, quest.rewardSoul, quest.rewardDungeonEXP);
                OnQuestsUpdated?.Invoke();
                SaveManager.Instance?.SaveGame();
                return true;
            }
            finally
            {
                isClaiming = false;
            }
        }

        private string GetStableClaimId(QuestData quest)
        {
            if (quest == null) return string.Empty;
            if (quest.questType == QuestType.Daily && dailyQuestManager != null)
            {
                return $"CLAIM_DAILY_{quest.questId}_{dailyQuestManager.LastResetDateKey}";
            }
            if (quest.questType == QuestType.Weekly && weeklyQuestManager != null)
            {
                return $"CLAIM_WEEKLY_{quest.questId}_{weeklyQuestManager.LastResetWeekKey}";
            }
            return $"CLAIM_{quest.questId}";
        }

        private static void GrantItemRewards(List<QuestRewardItem> items)
        {
            if (items == null || items.Count == 0) return;

            var roomInv = FindAnyObjectByType<RoomInventoryManager>();
            var monsterInv = FindAnyObjectByType<MonsterInventoryManager>();
            var trapInv = FindAnyObjectByType<TrapInventoryManager>();

            foreach (QuestRewardItem item in items)
            {
                if (item == null || string.IsNullOrWhiteSpace(item.itemId) || item.quantity <= 0) continue;
                switch (item.itemType)
                {
                    case QuestItemType.Room:
                        if (roomInv?.Database != null)
                        {
                            RoomData r = roomInv.Database.FindById(item.itemId);
                            if (r != null) roomInv.TryAddOwned(r, item.quantity, out _);
                        }
                        break;
                    case QuestItemType.Monster:
                        if (monsterInv?.Database != null)
                        {
                            MonsterData m = monsterInv.Database.FindById(item.itemId);
                            if (m != null) monsterInv.TryAddOwned(m, item.quantity, out _);
                        }
                        break;
                    case QuestItemType.Trap:
                        if (trapInv?.Database != null)
                        {
                            TrapData t = trapInv.Database.FindById(item.itemId);
                            if (t != null) trapInv.TryAddOwned(t, item.quantity, out _);
                        }
                        break;
                }
            }
        }

        public void SyncCumulativeObjectives()
        {
            int currentLevel = DungeonProgressionManager.Instance != null ? DungeonProgressionManager.Instance.CurrentLevel : 1;
            if (currentLevel > 1)
            {
                ReportObjectiveProgress(QuestObjectiveType.ReachDungeonLevel, currentLevel, isAbsoluteValue: true);
            }
            if (DungeonUnlockManager.Instance != null && DungeonUnlockManager.Instance.IsRoomUnlocked("boss_room", currentLevel))
            {
                ReportObjectiveProgress(QuestObjectiveType.UnlockRoom, 1, targetFilterId: "boss_room", isAbsoluteValue: true);
            }
        }

        public QuestSaveState ExportSaveState()
        {
            EnsureDatabaseAndSubManagers();
            var save = new QuestSaveState
            {
                lastDailyResetDate = dailyQuestManager != null ? dailyQuestManager.LastResetDateKey : string.Empty,
                lastWeeklyResetDate = weeklyQuestManager != null ? weeklyQuestManager.LastResetWeekKey : string.Empty
            };

            foreach (var kvp in entries)
            {
                if (kvp.Value != null)
                {
                    save.entries.Add(new QuestProgressEntry
                    {
                        questId = kvp.Value.questId,
                        currentProgress = kvp.Value.currentProgress,
                        status = kvp.Value.status,
                        isRewardClaimed = kvp.Value.isRewardClaimed,
                        rewardClaimId = kvp.Value.rewardClaimId,
                        lastUpdatedTimestamp = kvp.Value.lastUpdatedTimestamp
                    });
                }
            }

            save.unlockedMainQuestIds.AddRange(unlockedMainQuestIds);
            save.claimedRewardIds.AddRange(claimedRewardIds);
            if (dailyQuestManager != null) save.activeDailyQuestIds.AddRange(dailyQuestManager.ActiveDailyQuestIds);
            if (weeklyQuestManager != null) save.activeWeeklyQuestIds.AddRange(weeklyQuestManager.ActiveWeeklyQuestIds);
            save.processedEventIds.AddRange(processedEventIds);
            return save;
        }

        public void ImportSaveState(QuestSaveState save)
        {
            EnsureDatabaseAndSubManagers();
            if (save == null) return;

            suppressEarnTracking = true;
            try
            {
                entries.Clear();
                unlockedMainQuestIds.Clear();
                claimedRewardIds.Clear();
                processedEventIds.Clear();

                if (save.unlockedMainQuestIds != null)
                {
                    foreach (string id in save.unlockedMainQuestIds)
                        if (!string.IsNullOrWhiteSpace(id)) unlockedMainQuestIds.Add(id.Trim());
                }

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
                    foreach (QuestProgressEntry entry in save.entries)
                    {
                        if (entry == null || string.IsNullOrWhiteSpace(entry.questId)) continue;
                        QuestData def = database != null ? database.FindById(entry.questId) : null;
                        int target = def != null ? def.targetAmount : Mathf.Max(1, entry.currentProgress);
                        int clamped = Mathf.Clamp(entry.currentProgress, 0, target);

                        QuestStatus status = entry.status;
                        if (entry.isRewardClaimed) status = QuestStatus.Claimed;
                        else if (clamped >= target && status != QuestStatus.Locked) status = QuestStatus.Completed;

                        entries[entry.questId.Trim()] = new QuestProgressEntry
                        {
                            questId = entry.questId.Trim(),
                            currentProgress = clamped,
                            status = status,
                            isRewardClaimed = entry.isRewardClaimed,
                            rewardClaimId = entry.rewardClaimId ?? string.Empty,
                            lastUpdatedTimestamp = entry.lastUpdatedTimestamp
                        };
                    }
                }

                // Ensure all database quests exist in entries
                if (database != null)
                {
                    foreach (QuestData q in database.AllQuests)
                    {
                        if (q != null) GetOrCreateEntry(q);
                    }
                }

                mainQuestManager?.EvaluateMainQuestSequence(notifyNewUnlocks: false);
                dailyQuestManager?.Initialize(this, save.activeDailyQuestIds, save.lastDailyResetDate);
                weeklyQuestManager?.Initialize(this, save.activeWeeklyQuestIds, save.lastWeeklyResetDate);

                OnQuestsUpdated?.Invoke();
            }
            finally
            {
                suppressEarnTracking = false;
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
                RoomManager.Instance.BossPlaced -= OnBossPlaced;
                RoomManager.Instance.BossPlaced += OnBossPlaced;
            }

            var roomShop = FindAnyObjectByType<RoomShopManager>();
            if (roomShop != null)
            {
                roomShop.PurchaseCompleted -= OnRoomPurchased;
                roomShop.PurchaseCompleted += OnRoomPurchased;
            }

            var monsterShop = FindAnyObjectByType<MonsterShopManager>();
            if (monsterShop != null)
            {
                monsterShop.PurchaseCompleted -= OnMonsterPurchased;
                monsterShop.PurchaseCompleted += OnMonsterPurchased;
            }

            var trapShop = FindAnyObjectByType<TrapShopManager>();
            if (trapShop != null)
            {
                trapShop.PurchaseCompleted -= OnTrapPurchased;
                trapShop.PurchaseCompleted += OnTrapPurchased;
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
                RoomManager.Instance.BossPlaced -= OnBossPlaced;
            }

            var roomShop = FindAnyObjectByType<RoomShopManager>();
            if (roomShop != null) roomShop.PurchaseCompleted -= OnRoomPurchased;
            var monsterShop = FindAnyObjectByType<MonsterShopManager>();
            if (monsterShop != null) monsterShop.PurchaseCompleted -= OnMonsterPurchased;
            var trapShop = FindAnyObjectByType<TrapShopManager>();
            if (trapShop != null) trapShop.PurchaseCompleted -= OnTrapPurchased;
        }

        private void OnCurrencyAdded(int goldAmount, int soulAmount, int crystalAmount)
        {
            if (suppressEarnTracking) return;
            if (goldAmount > 0) ReportObjectiveProgress(QuestObjectiveType.EarnGold, goldAmount);
            if (soulAmount > 0) ReportObjectiveProgress(QuestObjectiveType.EarnSoul, soulAmount);
        }

        private void OnDungeonLevelUp(int newLevel)
        {
            ReportObjectiveProgress(QuestObjectiveType.ReachDungeonLevel, newLevel, isAbsoluteValue: true);
            if (DungeonUnlockManager.Instance != null && DungeonUnlockManager.Instance.IsRoomUnlocked("boss_room", newLevel))
            {
                ReportObjectiveProgress(QuestObjectiveType.UnlockRoom, 1, targetFilterId: "boss_room", isAbsoluteValue: true);
            }
        }

        private void OnCoreUpgraded(int newLevel, int maxHp, int defense)
        {
            if (suppressEarnTracking) return;
            if (newLevel > 1)
            {
                ReportObjectiveProgress(QuestObjectiveType.UpgradeCore, 1);
            }
        }

        private void OnGridExpanded(int width, int height)
        {
            if (suppressEarnTracking) return;
            if (width > 9 || height > 9)
            {
                ReportObjectiveProgress(QuestObjectiveType.ExpandDungeon, 1);
            }
        }

        private void OnFacilityUpgraded(string facilityName, int newLevel)
        {
            if (suppressEarnTracking) return;
            if (newLevel > 1)
            {
                ReportObjectiveProgress(QuestObjectiveType.UpgradeFacility, 1);
            }
        }

        private void OnRoomPlaced(DungeonRoomInstanceData room)
        {
            if (room == null) return;
            ReportObjectiveProgress(QuestObjectiveType.PlaceRoom, 1, eventId: "PLACE_ROOM_" + room.instanceId, targetFilterId: room.roomId);
            if (room.roomType == DungeonRoomType.BossRoom)
            {
                ReportObjectiveProgress(QuestObjectiveType.UnlockRoom, 1, targetFilterId: "boss_room", isAbsoluteValue: true);
            }
        }

        private void OnMonsterPlaced(MonsterInstanceData monster)
        {
            if (monster == null) return;
            ReportObjectiveProgress(QuestObjectiveType.PlaceMonster, 1, eventId: "PLACE_MONSTER_" + monster.instanceId, targetFilterId: monster.monsterId);
        }

        private void OnTrapPlaced(TrapInstanceData trap)
        {
            if (trap == null) return;
            ReportObjectiveProgress(QuestObjectiveType.PlaceTrap, 1, eventId: "PLACE_TRAP_" + trap.instanceId, targetFilterId: trap.trapId);
        }

        private void OnBossPlaced(BossInstanceData boss)
        {
            if (boss == null) return;
            ReportObjectiveProgress(QuestObjectiveType.UnlockRoom, 1, targetFilterId: "boss_room", isAbsoluteValue: true);
        }

        private void OnRoomPurchased(RoomData room)
        {
            if (room == null) return;
            ReportObjectiveProgress(QuestObjectiveType.UnlockRoom, 1, targetFilterId: room.roomId);
        }

        private void OnMonsterPurchased(MonsterData monster)
        {
            if (monster == null) return;
            ReportObjectiveProgress(QuestObjectiveType.UnlockMonster, 1, targetFilterId: monster.monsterId);
        }

        private void OnTrapPurchased(TrapData trap)
        {
            if (trap == null) return;
            ReportObjectiveProgress(QuestObjectiveType.UnlockTrap, 1, targetFilterId: trap.trapId);
        }
    }
}
