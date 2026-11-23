using System;
using System.Collections.Generic;
using UnityEngine;

namespace DreamDungeon
{
    public sealed class WaveManager : MonoBehaviour
    {
        private BattleConfiguration configuration;
        private AdventurerDatabase adventurerDatabase;
        private List<Vector2Int> route;
        private Func<Vector2Int, Vector3> worldConverter;
        private Sprite whiteSprite;
        private float cellSize = 0.62f;
        private Vector3 entranceWorldPos;

        private readonly List<AdventurerActor> allAdventurers = new List<AdventurerActor>();
        private int currentWaveIndex;
        private int entryIndex;
        private int entrySpawnedCount;
        private float spawnTimer;
        private int defeatedCount;
        private int eliteDefeatedCount;
        private int bossUnitDefeatedCount;
        private bool isSpawningActive;
        private bool isEndlessMode;
        private EndlessModeConfig endlessConfig;
        private AdventurerActor activeBossWaveActor;
        private string activeBossWaveTitle = string.Empty;
        private readonly HashSet<string> countedDeadActorIds = new HashSet<string>();
        private readonly HashSet<int> spawnedEndlessBossWaves = new HashSet<int>();

        public int CurrentWave => Mathf.Clamp(currentWaveIndex + 1, 1, isEndlessMode ? int.MaxValue : (TotalWaves > 0 ? TotalWaves : 1));
        public int TotalWaves => isEndlessMode ? 0 : (configuration != null && configuration.waves != null ? configuration.waves.Count : 0);
        public int LivingCount => CountLiving();
        public int DefeatedCount => defeatedCount;
        public int EliteDefeatedCount => eliteDefeatedCount;
        public int BossUnitDefeatedCount => bossUnitDefeatedCount;
        public bool IsEndlessMode => isEndlessMode;
        public AdventurerActor ActiveBossWaveActor => activeBossWaveActor != null && activeBossWaveActor.IsAlive ? activeBossWaveActor : null;
        public string ActiveBossWaveTitle => activeBossWaveTitle;
        public IReadOnlyList<AdventurerActor> AllAdventurers => allAdventurers;
        public bool IsRunning => isSpawningActive;

        public event Action<AdventurerActor> AdventurerSpawned;
        public event Action<AdventurerActor> AdventurerDied;
        public event Action<int> WaveStarted;
        public event Action<int> WaveCleared;
        public event Action<int, string> BossWaveTriggered;
        public event Action AllWavesCleared;

        public void Configure(BattleConfiguration config, AdventurerDatabase database, List<Vector2Int> pathRoute,
                              Func<Vector2Int, Vector3> converter, Vector3 entrancePos, float size, Sprite sprite)
        {
            configuration = config;
            adventurerDatabase = database;
            route = pathRoute;
            worldConverter = converter;
            entranceWorldPos = entrancePos;
            cellSize = Mathf.Max(0.1f, size);
            whiteSprite = sprite;

            currentWaveIndex = 0;
            entryIndex = 0;
            entrySpawnedCount = 0;
            spawnTimer = 0.2f;
            defeatedCount = 0;
            eliteDefeatedCount = 0;
            bossUnitDefeatedCount = 0;
            isEndlessMode = false;
            endlessConfig = null;
            activeBossWaveActor = null;
            activeBossWaveTitle = string.Empty;
            countedDeadActorIds.Clear();
            spawnedEndlessBossWaves.Clear();
            allAdventurers.Clear();
            isSpawningActive = false;
        }

        public void EnableEndlessMode(EndlessModeConfig cfg)
        {
            endlessConfig = cfg != null ? cfg : EndlessModeConfig.CreateRuntimeDefaultConfig();
            endlessConfig.EnsureDefaultsIfEmpty();
            isEndlessMode = true;
            if (configuration != null)
            {
                if (configuration.waves == null)
                    configuration.waves = new List<AdventurerWave>();
                else
                    configuration.waves.Clear();

                int startW = Mathf.Max(1, endlessConfig.startingWave);
                for (int w = 1; w <= startW; w++)
                {
                    EnsureEndlessWaveGenerated(w - 1);
                }
                currentWaveIndex = startW - 1;
            }
        }

        public void StartWaves()
        {
            currentWaveIndex = isEndlessMode && endlessConfig != null ? Mathf.Max(0, endlessConfig.startingWave - 1) : 0;
            entryIndex = 0;
            entrySpawnedCount = 0;
            spawnTimer = 0.25f;
            defeatedCount = 0;
            eliteDefeatedCount = 0;
            bossUnitDefeatedCount = 0;
            countedDeadActorIds.Clear();
            spawnedEndlessBossWaves.Clear();
            if (isEndlessMode)
            {
                EnsureEndlessWaveGenerated(currentWaveIndex);
                AnnounceEndlessBossIfPresent(CurrentWave);
            }
            isSpawningActive = true;
            WaveStarted?.Invoke(CurrentWave);
        }

        public void Tick(float deltaTime, IReadOnlyList<ICombatTarget> monsterTargets, ICombatTarget coreTarget)
        {
            if (!isSpawningActive || configuration == null || configuration.waves == null) return;

            // 1. Spawning
            spawnTimer -= deltaTime;
            if (spawnTimer <= 0f && currentWaveIndex < configuration.waves.Count)
            {
                SpawnNextAdventurer();
            }

            // 2. Tick living adventurers
            for (int i = 0; i < allAdventurers.Count; i++)
            {
                AdventurerActor actor = allAdventurers[i];
                if (actor != null && actor.IsAlive)
                {
                    actor.Tick(deltaTime, monsterTargets, coreTarget);
                }
            }

            // 3. Wave completion check
            if (IsCurrentWaveSpawnComplete())
            {
                if (CountLivingInCurrentWave() == 0)
                {
                    WaveCleared?.Invoke(CurrentWave);
                    currentWaveIndex++;
                    entryIndex = 0;
                    entrySpawnedCount = 0;
                    spawnTimer = 0.8f;

                    if (isEndlessMode)
                    {
                        EnsureEndlessWaveGenerated(currentWaveIndex);
                        AnnounceEndlessBossIfPresent(CurrentWave);
                        WaveStarted?.Invoke(CurrentWave);
                    }
                    else if (currentWaveIndex >= configuration.waves.Count)
                    {
                        isSpawningActive = false;
                        AllWavesCleared?.Invoke();
                    }
                    else
                    {
                        WaveStarted?.Invoke(CurrentWave);
                    }
                }
            }
        }

        private void SpawnNextAdventurer()
        {
            if (isEndlessMode)
            {
                EnsureEndlessWaveGenerated(currentWaveIndex);
            }
            if (currentWaveIndex >= configuration.waves.Count) return;
            AdventurerWave wave = configuration.waves[currentWaveIndex];
            if (wave == null || wave.entries == null || wave.entries.Count == 0) return;

            while (entryIndex < wave.entries.Count && entrySpawnedCount >= wave.entries[entryIndex].count)
            {
                entryIndex++;
                entrySpawnedCount = 0;
            }

            if (entryIndex >= wave.entries.Count) return;

            AdventurerWaveEntry entry = wave.entries[entryIndex];
            int waveNum = currentWaveIndex + 1;
            bool isBossEntry = entry.adventurerId != null && entry.adventurerId.StartsWith("BOSS:", StringComparison.OrdinalIgnoreCase);
            bool isEliteEntry = entry.adventurerId != null && entry.adventurerId.StartsWith("ELITE:", StringComparison.OrdinalIgnoreCase);

            string lookupAdvId = entry.adventurerId;
            EliteEnemyData eliteSpec = null;
            EndlessBossScheduleEntry bossSpec = null;

            if (isBossEntry && endlessConfig != null)
            {
                endlessConfig.IsBossWave(waveNum, out bossSpec);
                lookupAdvId = bossSpec != null && !string.IsNullOrWhiteSpace(bossSpec.adventurerId) ? bossSpec.adventurerId : "paladin";
            }
            else if (isEliteEntry && endlessConfig != null)
            {
                eliteSpec = endlessConfig.GetEliteForWave(waveNum, entryIndex);
                lookupAdvId = eliteSpec != null && !string.IsNullOrWhiteSpace(eliteSpec.baseAdventurerId) ? eliteSpec.baseAdventurerId : "elite_knight";
            }

            AdventurerData baseData = adventurerDatabase.Find(lookupAdvId) ?? adventurerDatabase.Find("swordsman");
            if (baseData == null) return;

            AdventurerData runtimeData = BuildScaledAdventurerDataForCurrentWave(baseData, waveNum, eliteSpec, bossSpec);

            // Compute lane offset perpendicular to entrance direction
            int spawnSlot = allAdventurers.Count % 3;
            float lateralOffset = (spawnSlot - 1) * (cellSize * 0.22f);
            Vector3 laneOffset = new Vector3(0f, lateralOffset, 0f);
            Vector3 spawnPos = entranceWorldPos + new Vector3(-cellSize * 0.4f, lateralOffset, -0.1f);

            GameObject go = runtimeData.prefab != null ? Instantiate(runtimeData.prefab, spawnPos, Quaternion.identity) : new GameObject($"Adventurer_{runtimeData.adventurerId}");
            go.transform.position = spawnPos;
            go.transform.localScale = Vector3.one * (cellSize * 0.45f);

            AdventurerActor actor = go.GetComponent<AdventurerActor>() ?? go.AddComponent<AdventurerActor>();
            string uniqueId = Guid.NewGuid().ToString("N");
            actor.Initialize(runtimeData, currentWaveIndex, route, worldConverter, laneOffset, whiteSprite, uniqueId);

            if (isBossEntry)
            {
                string bTitle = bossSpec != null ? bossSpec.bossTitle : $"Wave {waveNum} Boss";
                Color bColor = bossSpec != null ? bossSpec.bossAuraColor : new Color(1f, 0.3f, 0.25f);
                actor.ApplyBossWaveVariant(bTitle, bColor, 1.45f, 0.25f);
                activeBossWaveActor = actor;
                activeBossWaveTitle = bTitle;
            }
            else if (isEliteEntry && eliteSpec != null)
            {
                actor.ApplyEliteVariant(eliteSpec);
            }
            else if (!isEndlessMode && BattleSession.ActiveGameMode == GameModeType.Challenge)
            {
                ChallengeData ch = EndlessChallengeManager.Instance?.GetChallenge(BattleSession.ChallengeId);
                if (ch != null)
                {
                    if (wave.waveName != null && wave.waveName.IndexOf("BOSS", StringComparison.OrdinalIgnoreCase) >= 0 && entryIndex == wave.entries.Count - 1 && entrySpawnedCount == 0)
                    {
                        string bTitle = $"{ch.challengeName} Boss";
                        actor.ApplyBossWaveVariant(bTitle, new Color(1f, 0.35f, 0.3f), 1.42f, 0.20f);
                        activeBossWaveActor = actor;
                        activeBossWaveTitle = bTitle;
                    }
                    else if (ch.eliteSpawnChance > 0f && ((entryIndex + entrySpawnedCount + currentWaveIndex) % 3 == 0))
                    {
                        EliteEnemyData chElite = EndlessChallengeManager.Instance?.EndlessConfig?.GetEliteForWave(waveNum, entryIndex);
                        if (chElite != null) actor.ApplyEliteVariant(chElite);
                    }
                }
            }

            if (EndlessChallengeManager.Instance != null && EndlessChallengeManager.Instance.ActiveModifiers.IsActive &&
                EndlessChallengeManager.Instance.ActiveModifiers.DisableHealing)
            {
                actor.DisablePassiveHealing();
            }

            actor.Died += OnActorDied;

            allAdventurers.Add(actor);
            entrySpawnedCount++;
            spawnTimer = isEndlessMode && endlessConfig != null ? endlessConfig.GetSpawnIntervalForWave(waveNum) : configuration.spawnInterval;

            if (entrySpawnedCount >= entry.count)
            {
                entryIndex++;
                entrySpawnedCount = 0;
            }

            AdventurerSpawned?.Invoke(actor);
        }

        private AdventurerData BuildScaledAdventurerDataForCurrentWave(
            AdventurerData baseData, int waveNum, EliteEnemyData eliteSpec, EndlessBossScheduleEntry bossSpec)
        {
            if (baseData == null) return null;

            float hpMult = 1f;
            float atkMult = 1f;
            float defMult = 1f;

            if (isEndlessMode && endlessConfig != null)
            {
                endlessConfig.EvaluateWaveMultipliers(waveNum, out hpMult, out atkMult, out defMult, out _, out _);
            }
            else if (BattleSession.IsCampaignBattle)
            {
                switch (BattleSession.Difficulty)
                {
                    case StageDifficulty.Easy:
                        hpMult *= 0.65f;
                        atkMult *= 0.65f;
                        defMult *= 0.60f;
                        break;
                    case StageDifficulty.Normal:
                        hpMult *= 0.85f;
                        atkMult *= 0.85f;
                        defMult *= 0.80f;
                        break;
                    case StageDifficulty.Hard:
                        hpMult *= 1.00f;
                        atkMult *= 1.00f;
                        defMult *= 1.00f;
                        break;
                    case StageDifficulty.VeryHard:
                        hpMult *= 1.20f;
                        atkMult *= 1.15f;
                        defMult *= 1.10f;
                        break;
                    case StageDifficulty.Nightmare:
                        hpMult *= 1.45f;
                        atkMult *= 1.30f;
                        defMult *= 1.25f;
                        break;
                }
            }

            if (EndlessChallengeManager.Instance != null && EndlessChallengeManager.Instance.ActiveModifiers.IsActive)
            {
                var mods = EndlessChallengeManager.Instance.ActiveModifiers;
                hpMult *= mods.EnemyHpMultiplier;
                atkMult *= mods.EnemyDamageMultiplier;
                if (bossSpec != null)
                {
                    atkMult *= mods.BossDamageMultiplier;
                }
            }

            if (bossSpec != null)
            {
                AdventurerData bossCopy = ScriptableObject.CreateInstance<AdventurerData>();
                bossCopy.name = $"{baseData.adventurerId}_EndlessBoss_W{waveNum}";
                bossCopy.adventurerId = baseData.adventurerId;
                bossCopy.adventurerName = bossSpec.bossTitle;
                bossCopy.description = baseData.description;
                bossCopy.icon = baseData.icon;
                bossCopy.prefab = baseData.prefab;
                bossCopy.attackType = baseData.attackType;
                bossCopy.targetPriority = baseData.targetPriority;
                bossCopy.attackRange = baseData.attackRange;
                bossCopy.moveSpeed = Mathf.Clamp(baseData.moveSpeed * 0.90f, 0.3f, 4f);
                bossCopy.attackSpeed = Mathf.Clamp(baseData.attackSpeed * 1.15f, 0.3f, 5f);

                long rawHp = (long)Mathf.RoundToInt(baseData.maxHP * hpMult * Mathf.Max(1.5f, bossSpec.bossHpMultiplier));
                long rawAtk = (long)Mathf.RoundToInt(baseData.attack * atkMult * Mathf.Max(1.2f, bossSpec.bossAttackMultiplier));
                long rawDef = (long)Mathf.RoundToInt(Mathf.Max(2, baseData.defense) * defMult * Mathf.Max(1.2f, bossSpec.bossDefenseMultiplier));

                bossCopy.maxHP = (int)Mathf.Clamp(rawHp, 50, 5000000);
                bossCopy.attack = (int)Mathf.Clamp(rawAtk, 5, 500000);
                bossCopy.defense = (int)Mathf.Clamp(rawDef, 0, 500000);
                return bossCopy;
            }

            if (eliteSpec != null)
            {
                return eliteSpec.CreateRuntimeEliteAdventurerData(baseData, hpMult, atkMult, defMult);
            }

            if (Mathf.Approximately(hpMult, 1f) && Mathf.Approximately(atkMult, 1f) && Mathf.Approximately(defMult, 1f))
            {
                return baseData;
            }

            AdventurerData copy = ScriptableObject.CreateInstance<AdventurerData>();
            copy.name = $"{baseData.adventurerId}_Scaled_W{waveNum}";
            copy.adventurerId = baseData.adventurerId;
            copy.adventurerName = baseData.adventurerName;
            copy.description = baseData.description;
            copy.icon = baseData.icon;
            copy.prefab = baseData.prefab;
            copy.attackType = baseData.attackType;
            copy.targetPriority = baseData.targetPriority;
            copy.attackRange = baseData.attackRange;
            copy.moveSpeed = baseData.moveSpeed;
            copy.attackSpeed = baseData.attackSpeed;

            copy.maxHP = (int)Mathf.Clamp((long)Mathf.RoundToInt(baseData.maxHP * hpMult), 1, 2000000);
            copy.attack = (int)Mathf.Clamp((long)Mathf.RoundToInt(baseData.attack * atkMult), 1, 500000);
            copy.defense = (int)Mathf.Clamp((long)Mathf.RoundToInt(baseData.defense * defMult), 0, 500000);
            return copy;
        }

        private void EnsureEndlessWaveGenerated(int waveIdx)
        {
            if (configuration == null) return;
            if (configuration.waves == null) configuration.waves = new List<AdventurerWave>();
            if (endlessConfig == null) endlessConfig = EndlessModeConfig.CreateRuntimeDefaultConfig();

            while (configuration.waves.Count <= waveIdx)
            {
                int waveNum = configuration.waves.Count + 1;
                int totalUnits = endlessConfig.GetEnemyCountForWave(waveNum);
                bool hasBoss = endlessConfig.IsBossWave(waveNum, out EndlessBossScheduleEntry bossSpec);
                bool hasElite = endlessConfig.IsEliteWave(waveNum);

                var wave = new AdventurerWave
                {
                    waveName = hasBoss ? $"Endless Wave {waveNum} [BOSS: {bossSpec.bossTitle}]" : $"Endless Wave {waveNum}",
                    entries = new List<AdventurerWaveEntry>()
                };

                var pool = endlessConfig.normalAdventurerPool;
                if (pool == null || pool.Count == 0)
                {
                    pool = new List<string> { "swordsman", "archer", "mage", "knight" };
                }

                int normalRemaining = hasBoss ? Mathf.Max(1, totalUnits - 1) : totalUnits;
                int eliteCount = hasElite ? Mathf.Clamp(1 + (waveNum / 8), 1, Mathf.Max(1, normalRemaining / 2)) : 0;
                normalRemaining = Mathf.Max(1, normalRemaining - eliteCount);

                // Distribute normal enemies across 2-3 unit types
                int typesCount = Mathf.Clamp(1 + (waveNum / 3), 1, 3);
                int perType = Mathf.Max(1, normalRemaining / typesCount);
                for (int t = 0; t < typesCount; t++)
                {
                    string unitId = pool[(waveNum + t) % pool.Count];
                    int c = (t == typesCount - 1) ? Mathf.Max(1, normalRemaining - perType * (typesCount - 1)) : perType;
                    wave.entries.Add(new AdventurerWaveEntry { adventurerId = unitId, count = c });
                }

                // Add Elite entry if applicable
                if (eliteCount > 0)
                {
                    wave.entries.Add(new AdventurerWaveEntry { adventurerId = $"ELITE:{waveNum}", count = eliteCount });
                }

                // Add single Boss entry on Boss wave (prevents duplicate boss spawning)
                if (hasBoss && spawnedEndlessBossWaves.Add(waveNum))
                {
                    wave.entries.Add(new AdventurerWaveEntry { adventurerId = $"BOSS:{waveNum}", count = 1 });
                }

                configuration.waves.Add(wave);
            }
        }

        private void AnnounceEndlessBossIfPresent(int waveNum)
        {
            if (isEndlessMode && endlessConfig != null && endlessConfig.IsBossWave(waveNum, out var bossEntry) && bossEntry != null)
            {
                BossWaveTriggered?.Invoke(waveNum, bossEntry.bossTitle);
            }
        }

        private void OnActorDied(AdventurerActor actor)
        {
            if (actor != null && !string.IsNullOrEmpty(actor.InstanceId))
            {
                if (!countedDeadActorIds.Add(actor.InstanceId))
                {
                    return;
                }
            }

            defeatedCount++;
            if (actor != null && actor.IsBossWaveUnit)
            {
                bossUnitDefeatedCount++;
                eliteDefeatedCount++;
            }
            else if (actor != null && actor.IsElite)
            {
                eliteDefeatedCount++;
            }
            AdventurerDied?.Invoke(actor);
        }

        private bool IsCurrentWaveSpawnComplete()
        {
            if (currentWaveIndex >= configuration.waves.Count) return true;
            AdventurerWave wave = configuration.waves[currentWaveIndex];
            if (wave == null || wave.entries == null) return true;
            return entryIndex >= wave.entries.Count;
        }

        private int CountLivingInCurrentWave()
        {
            int count = 0;
            for (int i = 0; i < allAdventurers.Count; i++)
            {
                AdventurerActor actor = allAdventurers[i];
                if (actor != null && actor.IsAlive && actor.WaveIndex == currentWaveIndex)
                {
                    count++;
                }
            }
            return count;
        }

        private int CountLiving()
        {
            int count = 0;
            for (int i = 0; i < allAdventurers.Count; i++)
            {
                AdventurerActor actor = allAdventurers[i];
                if (actor != null && actor.IsAlive)
                {
                    count++;
                }
            }
            return count;
        }

        public void CleanUp()
        {
            for (int i = 0; i < allAdventurers.Count; i++)
            {
                if (allAdventurers[i] != null)
                {
                    allAdventurers[i].Died -= OnActorDied;
                    Destroy(allAdventurers[i].gameObject);
                }
            }
            allAdventurers.Clear();
            isSpawningActive = false;
        }

        private void OnDestroy()
        {
            CleanUp();
        }
    }
}
