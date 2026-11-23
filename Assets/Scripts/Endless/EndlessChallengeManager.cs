using System;
using System.Collections.Generic;
using UnityEngine;

namespace DreamDungeon
{
    public struct EndlessRunOutcome
    {
        public string RunResultId;
        public int FinalWaveReached;
        public int WavesCompleted;
        public int NormalEnemiesDefeated;
        public int EliteEnemiesDefeated;
        public int TotalEnemiesDefeated;
        public int BossesDefeated;
        public int CoreHpRemaining;
        public int CoreMaxHp;
        public float RunDurationSeconds;
        public RunScoreBreakdown ScoreBreakdown;
        public int RewardGold;
        public int RewardSoul;
        public int RewardExp;
        public bool IsNewWaveRecord;
        public bool IsNewScoreRecord;
    }

    public struct ChallengeRunOutcome
    {
        public string RunResultId;
        public string ChallengeId;
        public string ChallengeName;
        public bool Completed;
        public bool IsFirstClear;
        public bool IsNewScoreRecord;
        public bool IsNewTimeRecord;
        public int WavesCleared;
        public int TotalWaves;
        public int NormalEnemiesDefeated;
        public int EliteEnemiesDefeated;
        public int BossesDefeated;
        public int CoreHpRemaining;
        public int CoreMaxHp;
        public float RunDurationSeconds;
        public RunScoreBreakdown ScoreBreakdown;
        public int RewardGold;
        public int RewardSoul;
        public int RewardExp;
        public string ObjectivesSummary;
        public string ActiveModifiersSummary;
    }

    public sealed class EndlessChallengeManager : MonoBehaviour
    {
        public static EndlessChallengeManager Instance { get; private set; }

        [SerializeField] private EndlessModeConfig endlessConfig;
        [SerializeField] private ChallengeDatabase challengeDatabase;
        [SerializeField] private ScoreCalculationConfig scoreConfig = new ScoreCalculationConfig();

        // Permanent Endless Records
        private int highestEndlessWave;
        private int highestEndlessScore;
        private int totalEndlessRuns;
        private int totalEndlessEnemiesDefeated;
        private int totalEndlessElitesDefeated;
        private int totalEndlessBossesDefeated;

        // Permanent Challenge Records & Anti-Duplicate Ledger
        private readonly Dictionary<string, ChallengeRecordEntry> challengeRecords = new Dictionary<string, ChallengeRecordEntry>(StringComparer.OrdinalIgnoreCase);
        private readonly HashSet<string> claimedRunResultIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        // Temporary Active Run State (Cleaned up when run starts/ends)
        private readonly ChallengeRuntimeModifierState activeModifierState = new ChallengeRuntimeModifierState();
        private string selectedChallengeId = "challenge_monster_survival";
        private bool isStartingRun;

        public EndlessModeConfig EndlessConfig
        {
            get { EnsureInitialized(); return endlessConfig; }
        }

        public ChallengeDatabase Challenges
        {
            get { EnsureInitialized(); return challengeDatabase; }
        }

        public ScoreCalculationConfig ScoreConfig => scoreConfig;
        public ChallengeRuntimeModifierState ActiveModifiers => activeModifierState;

        public int HighestEndlessWave => highestEndlessWave;
        public int HighestEndlessScore => highestEndlessScore;
        public int TotalEndlessRuns => totalEndlessRuns;
        public int TotalEndlessEnemiesDefeated => totalEndlessEnemiesDefeated;
        public int TotalEndlessElitesDefeated => totalEndlessElitesDefeated;
        public int TotalEndlessBossesDefeated => totalEndlessBossesDefeated;
        public string SelectedChallengeId => selectedChallengeId;

        // Leaderboard & UI Events
        public event Action OnRecordsUpdated;
        public event Action<string, int, int> OnLeaderboardScoreSubmitted; // modeOrChallengeId, score, waveReached
        public event Action<EndlessRunOutcome> OnEndlessRunCompleted;
        public event Action<ChallengeRunOutcome> OnChallengeRunCompleted;

        public void Configure(EndlessModeConfig endlessCfg, ChallengeDatabase challengeDb, ScoreCalculationConfig scoreCfg = null)
        {
            if (endlessCfg != null) endlessConfig = endlessCfg;
            if (challengeDb != null) challengeDatabase = challengeDb;
            if (scoreCfg != null) scoreConfig = scoreCfg;
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

            EnsureInitialized();
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        public void EnsureInitialized()
        {
            if (endlessConfig == null)
            {
                endlessConfig = Resources.Load<EndlessModeConfig>("DreamDungeon/EndlessModeConfig");
                if (endlessConfig == null)
                {
                    endlessConfig = EndlessModeConfig.CreateRuntimeDefaultConfig();
                }
            }
            endlessConfig.EnsureDefaultsIfEmpty();

            if (challengeDatabase == null)
            {
                challengeDatabase = Resources.Load<ChallengeDatabase>("DreamDungeon/ChallengeDatabase");
                if (challengeDatabase == null)
                {
                    challengeDatabase = ChallengeDatabase.CreateRuntimeDefaultDatabase();
                }
            }

            if (scoreConfig == null)
            {
                scoreConfig = new ScoreCalculationConfig();
            }

            foreach (ChallengeData c in challengeDatabase.AllChallenges)
            {
                if (c != null && !string.IsNullOrWhiteSpace(c.challengeId))
                {
                    GetOrCreateChallengeRecord(c.challengeId);
                }
            }
        }

        public ChallengeData GetChallenge(string challengeId)
        {
            EnsureInitialized();
            return challengeDatabase != null ? challengeDatabase.FindById(challengeId) : null;
        }

        public ChallengeRecordEntry GetOrCreateChallengeRecord(string challengeId)
        {
            if (string.IsNullOrWhiteSpace(challengeId)) return null;
            string id = challengeId.Trim();
            if (!challengeRecords.TryGetValue(id, out ChallengeRecordEntry rec))
            {
                rec = new ChallengeRecordEntry
                {
                    challengeId = id,
                    isCompleted = false,
                    firstClearClaimed = false,
                    bestScore = 0,
                    bestClearTimeSeconds = 0f,
                    totalAttempts = 0,
                    totalClears = 0
                };
                challengeRecords[id] = rec;
            }
            return rec;
        }

        public bool IsChallengeCompleted(string challengeId)
        {
            EnsureInitialized();
            if (string.IsNullOrWhiteSpace(challengeId)) return false;
            return challengeRecords.TryGetValue(challengeId.Trim(), out var rec) && rec.isCompleted;
        }

        public bool IsChallengeFirstClearClaimed(string challengeId)
        {
            EnsureInitialized();
            if (string.IsNullOrWhiteSpace(challengeId)) return false;
            return challengeRecords.TryGetValue(challengeId.Trim(), out var rec) && rec.firstClearClaimed;
        }

        public bool IsChallengeUnlocked(string challengeId, out string unmetReason)
        {
            EnsureInitialized();
            ChallengeData challenge = GetChallenge(challengeId);
            if (challenge == null)
            {
                unmetReason = $"Challenge '{challengeId}' does not exist.";
                return false;
            }

            int currentLevel = DungeonProgressionManager.Instance != null ? DungeonProgressionManager.Instance.CurrentLevel : 1;
            if (currentLevel < challenge.requiredDungeonLevel)
            {
                unmetReason = $"Requires Dungeon Level {challenge.requiredDungeonLevel} (Current: Level {currentLevel}).";
                return false;
            }

            if (!string.IsNullOrWhiteSpace(challenge.requiredPreviousChallengeId))
            {
                if (!IsChallengeCompleted(challenge.requiredPreviousChallengeId))
                {
                    ChallengeData prev = GetChallenge(challenge.requiredPreviousChallengeId);
                    string prevName = prev != null ? prev.challengeName : challenge.requiredPreviousChallengeId;
                    unmetReason = $"Requires completing '{prevName}'.";
                    return false;
                }
            }

            if (!string.IsNullOrWhiteSpace(challenge.requiredCampaignStageId) && CampaignManager.Instance != null)
            {
                if (!CampaignManager.Instance.IsStageCompleted(challenge.requiredCampaignStageId))
                {
                    unmetReason = $"Requires completing Campaign Stage '{challenge.requiredCampaignStageId}'.";
                    return false;
                }
            }

            unmetReason = string.Empty;
            return true;
        }

        public bool TrySelectChallenge(string challengeId, out string error)
        {
            if (!IsChallengeUnlocked(challengeId, out error))
            {
                return false;
            }
            selectedChallengeId = challengeId.Trim();
            return true;
        }

        public bool ValidateDungeonForRun(RoomManager roomManager, ChallengeData optionalChallenge, out string error)
        {
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
                error = "Dungeon is invalid: place exactly one Entrance.";
                return false;
            }
            if (layout.rooms.FindAll(r => r != null && r.roomType == DungeonRoomType.DungeonCore).Count != 1)
            {
                error = "Dungeon is invalid: place exactly one Dungeon Core.";
                return false;
            }
            if (roomManager.Pathfinder == null || !roomManager.Pathfinder.TryFindRoute(layout, out List<Vector2Int> route) || route == null || route.Count < 2)
            {
                error = "Dungeon is invalid: no walkable Path connects Entrance to Dungeon Core.";
                return false;
            }

            // If a challenge is selected, validate challenge modifier placement limits or auto-trim in runtime layout copy
            if (optionalChallenge != null && optionalChallenge.modifiers != null)
            {
                // All valid dungeons with Entrance -> Core path can start; BattleManager enforces runtime monster/trap caps non-destructively.
            }

            error = string.Empty;
            return true;
        }

        public bool TryStartEndlessRun(RoomManager roomManager, out string error)
        {
            if (isStartingRun)
            {
                error = "An Endless Run is already starting.";
                return false;
            }

            EnsureInitialized();
            if (!ValidateDungeonForRun(roomManager, null, out error))
            {
                return false;
            }

            isStartingRun = true;
            try
            {
                activeModifierState.Reset();
                if (roomManager == null) roomManager = RoomManager.Instance ?? FindAnyObjectByType<RoomManager>();
                CurrencyManager currency = roomManager.Currency ?? CurrencyManager.Instance;
                int gold = currency != null ? currency.Gold : roomManager.Gold;
                int soul = currency != null ? currency.Soul : 0;
                int crystal = currency != null ? currency.Crystal : 0;

                BattleSession.BeginEndless(roomManager.LayoutToJson(), "DungeonEditor", gold, soul, crystal);

                if (!SceneLoader.Load("Battle"))
                {
                    error = "Battle scene could not be loaded.";
                    return false;
                }

                error = string.Empty;
                return true;
            }
            finally
            {
                isStartingRun = false;
            }
        }

        public bool TryStartChallengeRun(string challengeId, RoomManager roomManager, out string error)
        {
            if (isStartingRun)
            {
                error = "A Challenge Run is already starting.";
                return false;
            }

            EnsureInitialized();
            if (!TrySelectChallenge(challengeId, out error))
            {
                return false;
            }

            ChallengeData challenge = GetChallenge(challengeId);
            if (!ValidateDungeonForRun(roomManager, challenge, out error))
            {
                return false;
            }

            isStartingRun = true;
            try
            {
                // Apply challenge modifiers for this run only (resets previous modifier state first)
                activeModifierState.ApplyModifiers(challenge.modifiers);

                if (roomManager == null) roomManager = RoomManager.Instance ?? FindAnyObjectByType<RoomManager>();
                CurrencyManager currency = roomManager.Currency ?? CurrencyManager.Instance;
                int gold = currency != null ? currency.Gold : roomManager.Gold;
                int soul = currency != null ? currency.Soul : 0;
                int crystal = currency != null ? currency.Crystal : 0;

                BattleSession.BeginChallenge(roomManager.LayoutToJson(), "DungeonEditor", gold, soul, crystal, challenge.challengeId);

                if (!SceneLoader.Load("Battle"))
                {
                    activeModifierState.Reset();
                    error = "Battle scene could not be loaded.";
                    return false;
                }

                error = string.Empty;
                return true;
            }
            finally
            {
                isStartingRun = false;
            }
        }

        public void CleanUpTemporaryRunState()
        {
            activeModifierState.Reset();
            isStartingRun = false;
        }

        public EndlessRunOutcome RecordEndlessRunOutcome(
            string runResultId,
            int finalWaveReached,
            int wavesCompleted,
            int normalEnemiesDefeated,
            int eliteEnemiesDefeated,
            int bossesDefeated,
            int coreHpRemaining,
            int coreMaxHp,
            float runDurationSeconds,
            bool grantImmediateCurrency = false)
        {
            EnsureInitialized();
            string resultKey = string.IsNullOrWhiteSpace(runResultId)
                ? $"ENDLESS_RES_{Guid.NewGuid():N}"
                : $"ENDLESS_RES_{runResultId.Trim()}";

            int safeWaveReached = Mathf.Max(1, finalWaveReached);
            int safeWavesCleared = Mathf.Max(0, wavesCompleted);
            int safeNormal = Mathf.Max(0, normalEnemiesDefeated);
            int safeElites = Mathf.Max(0, eliteEnemiesDefeated);
            int safeBosses = Mathf.Max(0, bossesDefeated);

            RunScoreBreakdown score = ScoreCalculator.Calculate(
                scoreConfig, safeWavesCleared, safeWaveReached,
                safeNormal, safeElites, safeBosses,
                coreHpRemaining, coreMaxHp, runDurationSeconds, challengeCompleted: false);

            endlessConfig.EvaluateWaveMultipliers(safeWaveReached, out _, out _, out _, out _, out float rewardMult);
            float facGoldMult = DungeonFacilityManager.Instance != null ? DungeonFacilityManager.Instance.GetGoldMultiplier() : 1f;
            float facSoulMult = DungeonFacilityManager.Instance != null ? DungeonFacilityManager.Instance.GetSoulMultiplier() : 1f;

            int rawGold = safeWavesCleared * endlessConfig.goldPerWaveCleared
                          + safeNormal * endlessConfig.goldPerEnemyDefeated
                          + safeElites * endlessConfig.goldPerEliteDefeated
                          + safeBosses * 150;
            int rawSoul = safeWavesCleared * endlessConfig.soulPerWaveCleared
                          + safeNormal * endlessConfig.soulPerEnemyDefeated
                          + safeElites * endlessConfig.soulPerEliteDefeated
                          + safeBosses * 35;
            int rawExp = safeWavesCleared * endlessConfig.expPerWaveCleared
                         + safeElites * 15
                         + safeBosses * 80;

            int finalGold = Mathf.Max(0, Mathf.RoundToInt(rawGold * rewardMult * facGoldMult));
            int finalSoul = Mathf.Max(0, Mathf.RoundToInt(rawSoul * rewardMult * facSoulMult));
            int finalExp = Mathf.Max(0, Mathf.RoundToInt(rawExp * rewardMult));

            // Anti-duplicate check
            if (!claimedRunResultIds.Add(resultKey))
            {
                return new EndlessRunOutcome
                {
                    RunResultId = resultKey,
                    FinalWaveReached = safeWaveReached,
                    WavesCompleted = safeWavesCleared,
                    NormalEnemiesDefeated = safeNormal,
                    EliteEnemiesDefeated = safeElites,
                    TotalEnemiesDefeated = safeNormal + safeElites,
                    BossesDefeated = safeBosses,
                    CoreHpRemaining = coreHpRemaining,
                    CoreMaxHp = coreMaxHp,
                    RunDurationSeconds = runDurationSeconds,
                    ScoreBreakdown = score,
                    RewardGold = 0,
                    RewardSoul = 0,
                    RewardExp = 0,
                    IsNewWaveRecord = false,
                    IsNewScoreRecord = false
                };
            }

            bool isNewWaveRec = safeWaveReached > highestEndlessWave;
            bool isNewScoreRec = score.TotalScore > highestEndlessScore;

            // Never overwrite better records with worse results
            if (isNewWaveRec) highestEndlessWave = safeWaveReached;
            if (isNewScoreRec) highestEndlessScore = score.TotalScore;

            totalEndlessRuns++;
            totalEndlessEnemiesDefeated += (safeNormal + safeElites);
            totalEndlessElitesDefeated += safeElites;
            totalEndlessBossesDefeated += safeBosses;

            if (grantImmediateCurrency)
            {
                GrantCurrencyAndExp(resultKey, finalGold, finalSoul, finalExp);
            }

            // Report Milestone 7 & 9 Quest and Achievement events (once per runResultId)
            ReportEndlessQuestsAndAchievements(resultKey, safeWaveReached, safeWavesCleared, safeNormal, safeElites, safeBosses, score.TotalScore);

            var outcome = new EndlessRunOutcome
            {
                RunResultId = resultKey,
                FinalWaveReached = safeWaveReached,
                WavesCompleted = safeWavesCleared,
                NormalEnemiesDefeated = safeNormal,
                EliteEnemiesDefeated = safeElites,
                TotalEnemiesDefeated = safeNormal + safeElites,
                BossesDefeated = safeBosses,
                CoreHpRemaining = coreHpRemaining,
                CoreMaxHp = coreMaxHp,
                RunDurationSeconds = runDurationSeconds,
                ScoreBreakdown = score,
                RewardGold = finalGold,
                RewardSoul = finalSoul,
                RewardExp = finalExp,
                IsNewWaveRecord = isNewWaveRec,
                IsNewScoreRecord = isNewScoreRec
            };

            CleanUpTemporaryRunState();
            OnLeaderboardScoreSubmitted?.Invoke("endless_dungeon", score.TotalScore, safeWaveReached);
            OnEndlessRunCompleted?.Invoke(outcome);
            OnRecordsUpdated?.Invoke();
            SaveManager.Instance?.SaveGame();
            return outcome;
        }

        public ChallengeRunOutcome RecordChallengeRunOutcome(
            string runResultId,
            string challengeId,
            bool victory,
            int wavesCleared,
            int totalWaves,
            int normalEnemiesDefeated,
            int eliteEnemiesDefeated,
            int bossesDefeated,
            int coreHpRemaining,
            int coreMaxHp,
            float runDurationSeconds,
            bool grantImmediateCurrency = false)
        {
            EnsureInitialized();
            ChallengeData challenge = GetChallenge(challengeId);
            if (challenge == null) return default;

            string resultKey = string.IsNullOrWhiteSpace(runResultId)
                ? $"CHAL_RES_{challenge.challengeId}_{Guid.NewGuid():N}"
                : $"CHAL_RES_{runResultId.Trim()}";

            int safeWavesCleared = Mathf.Max(0, wavesCleared);
            int safeTotalWaves = Mathf.Max(1, totalWaves);
            int safeNormal = Mathf.Max(0, normalEnemiesDefeated);
            int safeElites = Mathf.Max(0, eliteEnemiesDefeated);
            int safeBosses = Mathf.Max(0, bossesDefeated);

            // Evaluate completion conditions (including optional time limit and min Core HP percentage)
            bool objectivesMet = victory && EvaluateChallengeCompletionRule(challenge, safeWavesCleared, safeTotalWaves, safeBosses, coreHpRemaining, coreMaxHp, runDurationSeconds);

            RunScoreBreakdown score = ScoreCalculator.Calculate(
                scoreConfig, safeWavesCleared, safeWavesCleared,
                safeNormal, safeElites, safeBosses,
                coreHpRemaining, coreMaxHp, runDurationSeconds, challengeCompleted: objectivesMet);

            ChallengeRecordEntry rec = GetOrCreateChallengeRecord(challenge.challengeId);
            string modifiersSummary = challenge.GetModifiersSummaryText();
            string objectivesSummary = challenge.GetObjectivesText();

            // Anti-duplicate guard
            if (!claimedRunResultIds.Add(resultKey))
            {
                return new ChallengeRunOutcome
                {
                    RunResultId = resultKey,
                    ChallengeId = challenge.challengeId,
                    ChallengeName = challenge.challengeName,
                    Completed = objectivesMet,
                    IsFirstClear = false,
                    IsNewScoreRecord = false,
                    IsNewTimeRecord = false,
                    WavesCleared = safeWavesCleared,
                    TotalWaves = safeTotalWaves,
                    NormalEnemiesDefeated = safeNormal,
                    EliteEnemiesDefeated = safeElites,
                    BossesDefeated = safeBosses,
                    CoreHpRemaining = coreHpRemaining,
                    CoreMaxHp = coreMaxHp,
                    RunDurationSeconds = runDurationSeconds,
                    ScoreBreakdown = score,
                    RewardGold = 0,
                    RewardSoul = 0,
                    RewardExp = 0,
                    ObjectivesSummary = objectivesSummary,
                    ActiveModifiersSummary = modifiersSummary
                };
            }

            rec.totalAttempts++;
            bool isFirstClear = false;
            bool isNewScore = false;
            bool isNewTime = false;
            int finalGold = 0;
            int finalSoul = 0;
            int finalExp = 0;

            float modRewardMult = activeModifierState.IsActive ? activeModifierState.RewardMultiplier : 1.0f;

            if (objectivesMet)
            {
                rec.totalClears++;
                isFirstClear = !rec.firstClearClaimed;
                rec.isCompleted = true;
                rec.firstClearClaimed = true;

                if (score.TotalScore > rec.bestScore)
                {
                    rec.bestScore = score.TotalScore;
                    isNewScore = true;
                }

                if (rec.bestClearTimeSeconds <= 0f || (runDurationSeconds > 0f && runDurationSeconds < rec.bestClearTimeSeconds))
                {
                    rec.bestClearTimeSeconds = Mathf.Max(0.1f, runDurationSeconds);
                    isNewTime = true;
                }

                int baseGold = isFirstClear ? challenge.firstClearGold : challenge.replayGold;
                int baseSoul = isFirstClear ? challenge.firstClearSoul : challenge.replaySoul;
                int baseExp = isFirstClear ? challenge.firstClearExp : challenge.replayExp;

                finalGold = Mathf.Max(0, Mathf.RoundToInt(baseGold * modRewardMult));
                finalSoul = Mathf.Max(0, Mathf.RoundToInt(baseSoul * modRewardMult));
                finalExp = Mathf.Max(0, Mathf.RoundToInt(baseExp * modRewardMult));

                if (grantImmediateCurrency)
                {
                    GrantCurrencyAndExp(resultKey, finalGold, finalSoul, finalExp);
                }

                ReportChallengeQuestsAndAchievements(resultKey, challenge, safeWavesCleared, safeNormal, safeElites, safeBosses, score.TotalScore);
                OnLeaderboardScoreSubmitted?.Invoke(challenge.challengeId, score.TotalScore, safeWavesCleared);
            }

            // Always restore runtime modifiers when run ends
            CleanUpTemporaryRunState();

            var outcome = new ChallengeRunOutcome
            {
                RunResultId = resultKey,
                ChallengeId = challenge.challengeId,
                ChallengeName = challenge.challengeName,
                Completed = objectivesMet,
                IsFirstClear = isFirstClear,
                IsNewScoreRecord = isNewScore,
                IsNewTimeRecord = isNewTime,
                WavesCleared = safeWavesCleared,
                TotalWaves = safeTotalWaves,
                NormalEnemiesDefeated = safeNormal,
                EliteEnemiesDefeated = safeElites,
                BossesDefeated = safeBosses,
                CoreHpRemaining = coreHpRemaining,
                CoreMaxHp = coreMaxHp,
                RunDurationSeconds = runDurationSeconds,
                ScoreBreakdown = score,
                RewardGold = finalGold,
                RewardSoul = finalSoul,
                RewardExp = finalExp,
                ObjectivesSummary = objectivesSummary,
                ActiveModifiersSummary = modifiersSummary
            };

            OnChallengeRunCompleted?.Invoke(outcome);
            OnRecordsUpdated?.Invoke();
            SaveManager.Instance?.SaveGame();
            return outcome;
        }

        private static bool EvaluateChallengeCompletionRule(
            ChallengeData challenge, int wavesCleared, int totalWaves, int bossesDefeated, int coreHp, int coreMaxHp, float durationSeconds)
        {
            if (challenge == null) return false;
            if (coreHp <= 0) return false;
            if (challenge.optionalTimeLimitSeconds > 0f && durationSeconds > challenge.optionalTimeLimitSeconds)
                return false;

            switch (challenge.completionRule)
            {
                case ChallengeCompletionRule.DefeatAllBosses:
                    return wavesCleared >= totalWaves && bossesDefeated >= totalWaves;
                case ChallengeCompletionRule.MaintainCoreHpPercentage:
                    float ratio = coreMaxHp > 0 ? (float)coreHp / coreMaxHp : 0f;
                    return wavesCleared >= totalWaves && ratio >= challenge.minCoreHpRatioRequired;
                default:
                    return wavesCleared >= totalWaves;
            }
        }

        private static void GrantCurrencyAndExp(string eventId, int gold, int soul, int exp)
        {
            CurrencyManager currency = CurrencyManager.Instance ?? FindAnyObjectByType<CurrencyManager>();
            if (currency != null && (gold > 0 || soul > 0))
            {
                currency.TryAdd(gold, soul, 0, out _);
            }

            if (exp > 0)
            {
                if (GameManager.Instance != null)
                {
                    GameManager.Instance.GrantExperience(eventId, exp);
                }
                else if (DungeonProgressionManager.Instance != null)
                {
                    DungeonProgressionManager.Instance.AddExp(exp);
                }
            }
        }

        private static void ReportEndlessQuestsAndAchievements(
            string resultKey, int waveReached, int wavesCompleted, int normalDefeated, int eliteDefeated, int bossesDefeated, int totalScore)
        {
            int totalEnemies = normalDefeated + eliteDefeated;

            if (QuestManager.Instance != null)
            {
                QuestManager.Instance.NotifyBattleResult(resultKey, wavesCompleted > 0, wavesCompleted, totalEnemies, bossesDefeated);
                QuestManager.Instance.ReportObjectiveProgress(QuestObjectiveType.CompleteEndlessRun, 1, eventId: resultKey + "_Q_RUN");
                QuestManager.Instance.ReportObjectiveProgress(QuestObjectiveType.ReachEndlessWave, waveReached, eventId: resultKey + "_Q_WAVE", isAbsoluteValue: true);
                if (eliteDefeated > 0)
                    QuestManager.Instance.ReportObjectiveProgress(QuestObjectiveType.DefeatEndlessElite, eliteDefeated, eventId: resultKey + "_Q_ELITE");
                if (bossesDefeated > 0)
                    QuestManager.Instance.ReportObjectiveProgress(QuestObjectiveType.DefeatEndlessBoss, bossesDefeated, eventId: resultKey + "_Q_BOSS");
                if (totalScore > 0)
                    QuestManager.Instance.ReportObjectiveProgress(QuestObjectiveType.EarnEndlessScore, totalScore, eventId: resultKey + "_Q_SCORE", isAbsoluteValue: true);
            }

            if (AchievementManager.Instance != null)
            {
                AchievementManager.Instance.NotifyBattleResult(resultKey, wavesCompleted > 0, wavesCompleted, totalEnemies, bossesDefeated);
                AchievementManager.Instance.ReportProgress(QuestObjectiveType.CompleteEndlessRun, 1, eventId: resultKey + "_A_RUN");
                AchievementManager.Instance.ReportProgress(QuestObjectiveType.ReachEndlessWave, waveReached, eventId: resultKey + "_A_WAVE", isAbsoluteValue: true);
                if (eliteDefeated > 0)
                    AchievementManager.Instance.ReportProgress(QuestObjectiveType.DefeatEndlessElite, eliteDefeated, eventId: resultKey + "_A_ELITE");
                if (bossesDefeated > 0)
                    AchievementManager.Instance.ReportProgress(QuestObjectiveType.DefeatEndlessBoss, bossesDefeated, eventId: resultKey + "_A_BOSS");
                if (totalScore > 0)
                    AchievementManager.Instance.ReportProgress(QuestObjectiveType.EarnEndlessScore, totalScore, eventId: resultKey + "_A_SCORE", isAbsoluteValue: true);
            }
        }

        private static void ReportChallengeQuestsAndAchievements(
            string resultKey, ChallengeData challenge, int wavesCleared, int normalDefeated, int eliteDefeated, int bossesDefeated, int totalScore)
        {
            int totalEnemies = normalDefeated + eliteDefeated;

            if (QuestManager.Instance != null)
            {
                QuestManager.Instance.NotifyBattleResult(resultKey, true, wavesCleared, totalEnemies, bossesDefeated);
                QuestManager.Instance.ReportObjectiveProgress(QuestObjectiveType.CompleteChallenge, 1, eventId: resultKey + "_Q_CHAL", targetFilterId: challenge.challengeId);
                if (eliteDefeated > 0)
                    QuestManager.Instance.ReportObjectiveProgress(QuestObjectiveType.DefeatEndlessElite, eliteDefeated, eventId: resultKey + "_Q_CELITE");
                if (challenge.isBossRush)
                    QuestManager.Instance.ReportObjectiveProgress(QuestObjectiveType.CompleteBossRush, 1, eventId: resultKey + "_Q_BOSSRUSH");
            }

            if (AchievementManager.Instance != null)
            {
                AchievementManager.Instance.NotifyBattleResult(resultKey, true, wavesCleared, totalEnemies, bossesDefeated);
                AchievementManager.Instance.ReportProgress(QuestObjectiveType.CompleteChallenge, 1, eventId: resultKey + "_A_CHAL");
                if (eliteDefeated > 0)
                    AchievementManager.Instance.ReportProgress(QuestObjectiveType.DefeatEndlessElite, eliteDefeated, eventId: resultKey + "_A_CELITE");
                if (challenge.isBossRush)
                    AchievementManager.Instance.ReportProgress(QuestObjectiveType.CompleteBossRush, 1, eventId: resultKey + "_A_BOSSRUSH");
                if (totalScore > 0)
                    AchievementManager.Instance.ReportProgress(QuestObjectiveType.EarnEndlessScore, totalScore, eventId: resultKey + "_A_CSCORE", isAbsoluteValue: true);
            }
        }

        public EndlessChallengeSaveState ExportSaveState()
        {
            EnsureInitialized();
            var save = new EndlessChallengeSaveState
            {
                saveVersion = 1,
                highestEndlessWave = highestEndlessWave,
                highestEndlessScore = highestEndlessScore,
                totalEndlessRuns = totalEndlessRuns,
                totalEndlessEnemiesDefeated = totalEndlessEnemiesDefeated,
                totalEndlessElitesDefeated = totalEndlessElitesDefeated,
                totalEndlessBossesDefeated = totalEndlessBossesDefeated
            };

            foreach (var kvp in challengeRecords)
            {
                if (kvp.Value == null) continue;
                save.challengeRecords.Add(new ChallengeRecordEntry
                {
                    challengeId = kvp.Value.challengeId,
                    isCompleted = kvp.Value.isCompleted,
                    firstClearClaimed = kvp.Value.firstClearClaimed,
                    bestScore = kvp.Value.bestScore,
                    bestClearTimeSeconds = kvp.Value.bestClearTimeSeconds,
                    totalAttempts = kvp.Value.totalAttempts,
                    totalClears = kvp.Value.totalClears
                });
            }

            save.claimedRunResultIds.AddRange(claimedRunResultIds);
            return save;
        }

        public void ImportSaveState(EndlessChallengeSaveState save)
        {
            EnsureInitialized();
            if (save == null) return;

            // Never overwrite better records with worse results
            highestEndlessWave = Mathf.Max(highestEndlessWave, save.highestEndlessWave);
            highestEndlessScore = Mathf.Max(highestEndlessScore, save.highestEndlessScore);
            totalEndlessRuns = Mathf.Max(totalEndlessRuns, save.totalEndlessRuns);
            totalEndlessEnemiesDefeated = Mathf.Max(totalEndlessEnemiesDefeated, save.totalEndlessEnemiesDefeated);
            totalEndlessElitesDefeated = Mathf.Max(totalEndlessElitesDefeated, save.totalEndlessElitesDefeated);
            totalEndlessBossesDefeated = Mathf.Max(totalEndlessBossesDefeated, save.totalEndlessBossesDefeated);

            if (save.claimedRunResultIds != null)
            {
                foreach (string id in save.claimedRunResultIds)
                {
                    if (!string.IsNullOrWhiteSpace(id))
                        claimedRunResultIds.Add(id.Trim());
                }
            }

            if (save.challengeRecords != null)
            {
                foreach (ChallengeRecordEntry incoming in save.challengeRecords)
                {
                    if (incoming == null || string.IsNullOrWhiteSpace(incoming.challengeId)) continue;
                    ChallengeRecordEntry existing = GetOrCreateChallengeRecord(incoming.challengeId);
                    existing.isCompleted = existing.isCompleted || incoming.isCompleted;
                    existing.firstClearClaimed = existing.firstClearClaimed || incoming.firstClearClaimed;
                    existing.bestScore = Mathf.Max(existing.bestScore, incoming.bestScore);
                    if (incoming.bestClearTimeSeconds > 0f &&
                        (existing.bestClearTimeSeconds <= 0f || incoming.bestClearTimeSeconds < existing.bestClearTimeSeconds))
                    {
                        existing.bestClearTimeSeconds = incoming.bestClearTimeSeconds;
                    }
                    existing.totalAttempts = Mathf.Max(existing.totalAttempts, incoming.totalAttempts);
                    existing.totalClears = Mathf.Max(existing.totalClears, incoming.totalClears);
                }
            }

            OnRecordsUpdated?.Invoke();
        }
    }
}
