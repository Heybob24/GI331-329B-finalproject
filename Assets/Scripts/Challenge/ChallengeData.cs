using System;
using System.Collections.Generic;
using UnityEngine;

namespace DreamDungeon
{
    public enum ChallengeCompletionRule
    {
        SurviveAllWaves,
        DefeatAllBosses,
        MaintainCoreHpPercentage
    }

    [CreateAssetMenu(fileName = "ChallengeData", menuName = "Dream Dungeon/Challenge/Challenge Data")]
    public sealed class ChallengeData : ScriptableObject
    {
        [Header("Identification")]
        public string challengeId = "challenge_monster_survival";
        public string challengeName = "Monster Survival";
        [TextArea(2, 4)]
        public string description = "Defend the Dungeon Core for 5 waves with limited monster capacity.";
        public StageDifficulty difficulty = StageDifficulty.Normal;
        public bool isBossRush = false;

        [Header("Unlock Requirements")]
        [Min(1)] public int requiredDungeonLevel = 1;
        public string requiredPreviousChallengeId = string.Empty;
        public string requiredCampaignStageId = string.Empty;

        [Header("Wave & Enemy Configuration")]
        public List<WaveConfiguration> waveConfiguration = new List<WaveConfiguration>();
        public List<string> allowedEnemyIds = new List<string>();
        public List<string> bossWaveIds = new List<string>();
        [Range(0f, 1f)] public float eliteSpawnChance = 0.25f;

        [Header("Special Modifiers")]
        public List<ChallengeModifierConfig> modifiers = new List<ChallengeModifierConfig>();

        [Header("Completion Conditions")]
        public ChallengeCompletionRule completionRule = ChallengeCompletionRule.SurviveAllWaves;
        [Range(0f, 1f)] public float minCoreHpRatioRequired = 0f;
        [Min(0f)] public float optionalTimeLimitSeconds = 0f;

        [Header("Rewards (First Clear & Replay)")]
        [Min(0)] public int firstClearGold = 500;
        [Min(0)] public int firstClearSoul = 120;
        [Min(0)] public int firstClearExp = 200;
        [Min(0)] public int replayGold = 200;
        [Min(0)] public int replaySoul = 50;
        [Min(0)] public int replayExp = 80;

        public string GetModifiersSummaryText()
        {
            if (modifiers == null || modifiers.Count == 0) return "None";
            var parts = new List<string>(modifiers.Count);
            for (int i = 0; i < modifiers.Count; i++)
            {
                if (modifiers[i] != null) parts.Add(modifiers[i].GetFormattedSummary());
            }
            return string.Join("  •  ", parts);
        }

        public string GetObjectivesText()
        {
            int waveCount = waveConfiguration != null ? waveConfiguration.Count : 0;
            string baseObj = completionRule switch
            {
                ChallengeCompletionRule.DefeatAllBosses => $"Defeat all {waveCount} consecutive Boss encounters.",
                ChallengeCompletionRule.MaintainCoreHpPercentage => $"Clear {waveCount} waves with Core HP >= {Mathf.RoundToInt(minCoreHpRatioRequired * 100f)}%.",
                _ => $"Survive all {waveCount} waves and protect the Dungeon Core."
            };

            if (optionalTimeLimitSeconds > 0f)
            {
                baseObj += $" (Time Limit: {Mathf.RoundToInt(optionalTimeLimitSeconds)}s)";
            }
            return baseObj;
        }
    }

    [CreateAssetMenu(fileName = "ChallengeDatabase", menuName = "Dream Dungeon/Challenge/Challenge Database")]
    public sealed class ChallengeDatabase : ScriptableObject
    {
        [SerializeField] private List<ChallengeData> challenges = new List<ChallengeData>();

        public IReadOnlyList<ChallengeData> AllChallenges
        {
            get
            {
                if (challenges == null || challenges.Count == 0)
                {
                    challenges = BuildDefaultChallengesList();
                }
                return challenges;
            }
        }

        public void SetChallenges(List<ChallengeData> list)
        {
            challenges = list ?? new List<ChallengeData>();
        }

        public ChallengeData FindById(string challengeId)
        {
            if (string.IsNullOrWhiteSpace(challengeId)) return null;
            var list = AllChallenges;
            for (int i = 0; i < list.Count; i++)
            {
                if (list[i] != null && string.Equals(list[i].challengeId, challengeId.Trim(), StringComparison.OrdinalIgnoreCase))
                {
                    return list[i];
                }
            }
            return null;
        }

        public static ChallengeDatabase CreateRuntimeDefaultDatabase()
        {
            ChallengeDatabase db = CreateInstance<ChallengeDatabase>();
            db.name = "DefaultChallengeDatabase";
            db.SetChallenges(BuildDefaultChallengesList());
            return db;
        }

        public static List<ChallengeData> BuildDefaultChallengesList()
        {
            var list = new List<ChallengeData>
            {
                // Challenge 1: Monster Survival
                MakeChallenge(
                    "challenge_monster_survival",
                    "Challenge 1: Monster Survival",
                    "Defend the Dungeon Core for 4 waves with a strict limit of at most 3 placed monsters.",
                    StageDifficulty.Normal,
                    reqLevel: 1,
                    reqPrevChallenge: "",
                    isBossRush: false,
                    eliteChance: 0.15f,
                    rule: ChallengeCompletionRule.SurviveAllWaves,
                    minCoreRatio: 0f,
                    timeLimit: 0f,
                    fcGold: 450, fcSoul: 100, fcExp: 180,
                    repGold: 180, repSoul: 40, repExp: 70,
                    waves: MakeWaves("c1", 4, new[] { "swordsman", "bandit", "archer" }, 3, false, ""),
                    modifiers: new List<ChallengeModifierConfig>
                    {
                        new ChallengeModifierConfig
                        {
                            modifierType = ChallengeModifierType.ReducedMonsterCapacity,
                            displayName = "Limited Monster Capacity",
                            description = "At most 3 Monsters may be deployed in the dungeon.",
                            flatLimitValue = 3,
                            multiplierValue = 1f
                        },
                        new ChallengeModifierConfig
                        {
                            modifierType = ChallengeModifierType.IncreasedReward,
                            displayName = "Survival Bonus",
                            description = "+20% Reward Multiplier.",
                            multiplierValue = 1.2f
                        }
                    }),

                // Challenge 2: Trap Master
                MakeChallenge(
                    "challenge_trap_master",
                    "Challenge 2: Trap Master",
                    "Defeat invading raiders primarily using traps! Monster placement is limited to at most 1 monster.",
                    StageDifficulty.Normal,
                    reqLevel: 1,
                    reqPrevChallenge: "challenge_monster_survival",
                    isBossRush: false,
                    eliteChance: 0.20f,
                    rule: ChallengeCompletionRule.SurviveAllWaves,
                    minCoreRatio: 0f,
                    timeLimit: 0f,
                    fcGold: 550, fcSoul: 130, fcExp: 220,
                    repGold: 220, repSoul: 50, repExp: 90,
                    waves: MakeWaves("c2", 4, new[] { "bandit", "swordsman", "archer" }, 3, false, ""),
                    modifiers: new List<ChallengeModifierConfig>
                    {
                        new ChallengeModifierConfig
                        {
                            modifierType = ChallengeModifierType.ReducedMonsterCapacity,
                            displayName = "Minimal Monsters",
                            description = "At most 1 Monster allowed; rely on Traps!",
                            flatLimitValue = 1,
                            multiplierValue = 1f
                        },
                        new ChallengeModifierConfig
                        {
                            modifierType = ChallengeModifierType.LimitedTrapPlacement,
                            displayName = "Trap Network Cap",
                            description = "Up to 8 Traps can be placed.",
                            flatLimitValue = 8,
                            multiplierValue = 1f
                        }
                    }),

                // Challenge 3: Core Protection
                MakeChallenge(
                    "challenge_core_protection",
                    "Challenge 3: Core Protection",
                    "The Dungeon Core starts at 50% HP while enemies deal +35% increased damage.",
                    StageDifficulty.Hard,
                    reqLevel: 2,
                    reqPrevChallenge: "challenge_trap_master",
                    isBossRush: false,
                    eliteChance: 0.25f,
                    rule: ChallengeCompletionRule.MaintainCoreHpPercentage,
                    minCoreRatio: 0.25f,
                    timeLimit: 180f,
                    fcGold: 700, fcSoul: 170, fcExp: 280,
                    repGold: 280, repSoul: 65, repExp: 110,
                    waves: MakeWaves("c3", 4, new[] { "swordsman", "archer", "mage" }, 3, false, ""),
                    modifiers: new List<ChallengeModifierConfig>
                    {
                        new ChallengeModifierConfig
                        {
                            modifierType = ChallengeModifierType.ReducedCoreHP,
                            displayName = "Fragile Core",
                            description = "Dungeon Core Max HP is reduced to 50%.",
                            multiplierValue = 0.50f
                        },
                        new ChallengeModifierConfig
                        {
                            modifierType = ChallengeModifierType.IncreasedEnemyDamage,
                            displayName = "Fierce Raiders",
                            description = "Enemies deal +35% increased damage.",
                            multiplierValue = 1.35f
                        }
                    }),

                // Challenge 4: Elite Invasion
                MakeChallenge(
                    "challenge_elite_invasion",
                    "Challenge 4: Elite Invasion",
                    "Elite champions appear in every wave and all invaders have +40% Max HP and no healing is allowed.",
                    StageDifficulty.Hard,
                    reqLevel: 3,
                    reqPrevChallenge: "challenge_core_protection",
                    isBossRush: false,
                    eliteChance: 0.75f,
                    rule: ChallengeCompletionRule.SurviveAllWaves,
                    minCoreRatio: 0f,
                    timeLimit: 0f,
                    fcGold: 900, fcSoul: 220, fcExp: 350,
                    repGold: 360, repSoul: 85, repExp: 140,
                    waves: MakeWaves("c4", 5, new[] { "elite_knight", "dark_archer", "paladin", "necromancer" }, 3, true, "cave_dragon"),
                    modifiers: new List<ChallengeModifierConfig>
                    {
                        new ChallengeModifierConfig
                        {
                            modifierType = ChallengeModifierType.IncreasedEnemyHP,
                            displayName = "Fortified Invaders",
                            description = "Enemies have +40% Max HP.",
                            multiplierValue = 1.40f
                        },
                        new ChallengeModifierConfig
                        {
                            modifierType = ChallengeModifierType.NoHealing,
                            displayName = "No Healing",
                            description = "Healing effects are disabled during the challenge.",
                            multiplierValue = 1f
                        }
                    }),

                // Challenge 5: Boss Rush
                MakeChallenge(
                    "challenge_boss_rush",
                    "Challenge 5: Boss Rush",
                    "Fight 4 consecutive Boss waves with no normal waves in between!",
                    StageDifficulty.VeryHard,
                    reqLevel: 4,
                    reqPrevChallenge: "challenge_elite_invasion",
                    isBossRush: true,
                    eliteChance: 0.50f,
                    rule: ChallengeCompletionRule.DefeatAllBosses,
                    minCoreRatio: 0f,
                    timeLimit: 240f,
                    fcGold: 1300, fcSoul: 320, fcExp: 500,
                    repGold: 520, repSoul: 125, repExp: 200,
                    waves: MakeBossRushWaves(),
                    modifiers: new List<ChallengeModifierConfig>
                    {
                        new ChallengeModifierConfig
                        {
                            modifierType = ChallengeModifierType.IncreasedBossDamage,
                            displayName = "Wrathful Bosses",
                            description = "Bosses deal +30% increased damage.",
                            multiplierValue = 1.30f
                        },
                        new ChallengeModifierConfig
                        {
                            modifierType = ChallengeModifierType.IncreasedReward,
                            displayName = "Boss Bounty",
                            description = "+50% Reward Multiplier.",
                            multiplierValue = 1.50f
                        }
                    }),

                // Challenge 6: Limited Resources
                MakeChallenge(
                    "challenge_limited_resources",
                    "Challenge 6: Limited Resources",
                    "Complete 5 intense double-sized waves with a strict deployment budget (max 4 monsters, max 4 traps).",
                    StageDifficulty.VeryHard,
                    reqLevel: 5,
                    reqPrevChallenge: "challenge_boss_rush",
                    isBossRush: false,
                    eliteChance: 0.35f,
                    rule: ChallengeCompletionRule.SurviveAllWaves,
                    minCoreRatio: 0f,
                    timeLimit: 0f,
                    fcGold: 1500, fcSoul: 380, fcExp: 600,
                    repGold: 600, repSoul: 150, repExp: 240,
                    waves: MakeWaves("c6", 5, new[] { "footman", "archer", "mage", "paladin" }, 3, true, "demon_lord"),
                    modifiers: new List<ChallengeModifierConfig>
                    {
                        new ChallengeModifierConfig
                        {
                            modifierType = ChallengeModifierType.ReducedStartingGold,
                            displayName = "Limited Budget",
                            description = "Starting resource budget cap: 1,500 Gold.",
                            flatLimitValue = 1500,
                            multiplierValue = 1f
                        },
                        new ChallengeModifierConfig
                        {
                            modifierType = ChallengeModifierType.ReducedMonsterCapacity,
                            displayName = "Squad Cap (4 Monsters)",
                            description = "At most 4 Monsters may be deployed.",
                            flatLimitValue = 4,
                            multiplierValue = 1f
                        },
                        new ChallengeModifierConfig
                        {
                            modifierType = ChallengeModifierType.LimitedTrapPlacement,
                            displayName = "Trap Cap (4 Traps)",
                            description = "At most 4 Traps may be deployed.",
                            flatLimitValue = 4,
                            multiplierValue = 1f
                        },
                        new ChallengeModifierConfig
                        {
                            modifierType = ChallengeModifierType.DoubleEnemyCount,
                            displayName = "Swarm Assault",
                            description = "Enemy count per wave is increased by 50%.",
                            multiplierValue = 1.50f
                        }
                    })
            };

            return list;
        }

        private static ChallengeData MakeChallenge(
            string id, string title, string desc, StageDifficulty diff,
            int reqLevel, string reqPrevChallenge, bool isBossRush, float eliteChance,
            ChallengeCompletionRule rule, float minCoreRatio, float timeLimit,
            int fcGold, int fcSoul, int fcExp, int repGold, int repSoul, int repExp,
            List<WaveConfiguration> waves, List<ChallengeModifierConfig> modifiers)
        {
            ChallengeData data = CreateInstance<ChallengeData>();
            data.name = id;
            data.challengeId = id;
            data.challengeName = title;
            data.description = desc;
            data.difficulty = diff;
            data.requiredDungeonLevel = reqLevel;
            data.requiredPreviousChallengeId = reqPrevChallenge;
            data.isBossRush = isBossRush;
            data.eliteSpawnChance = eliteChance;
            data.completionRule = rule;
            data.minCoreHpRatioRequired = minCoreRatio;
            data.optionalTimeLimitSeconds = timeLimit;
            data.firstClearGold = fcGold;
            data.firstClearSoul = fcSoul;
            data.firstClearExp = fcExp;
            data.replayGold = repGold;
            data.replaySoul = repSoul;
            data.replayExp = repExp;
            data.waveConfiguration = waves;
            data.modifiers = modifiers;
            return data;
        }

        private static List<WaveConfiguration> MakeWaves(string prefix, int count, string[] unitPool, int baseCount, bool finalBoss, string bossId)
        {
            var list = new List<WaveConfiguration>(count);
            for (int w = 1; w <= count; w++)
            {
                bool isLast = (w == count) && finalBoss;
                WaveConfiguration wave = CreateInstance<WaveConfiguration>();
                wave.name = $"{prefix}_wave_{w}";
                wave.waveId = $"{prefix}_wave_{w}";
                wave.waveNumber = w;
                wave.spawnInterval = 0.85f;
                wave.spawnDelay = 0.5f;
                wave.bossWave = isLast;
                wave.bossId = isLast ? bossId : string.Empty;
                wave.spawnGroups = new List<WaveSpawnGroup>();

                for (int i = 0; i < unitPool.Length; i++)
                {
                    wave.spawnGroups.Add(new WaveSpawnGroup
                    {
                        adventurerType = unitPool[i],
                        count = Mathf.Max(1, baseCount + (w - 1) - i),
                        spawnInterval = 0.85f,
                        spawnOrder = i
                    });
                }
                list.Add(wave);
            }
            return list;
        }

        private static List<WaveConfiguration> MakeBossRushWaves()
        {
            string[] bossIds = { "cave_dragon", "ice_queen", "lich_king", "demon_lord" };
            string[] bossUnits = { "paladin", "necromancer", "elite_knight", "elite_knight" };
            var list = new List<WaveConfiguration>(bossIds.Length);

            for (int i = 0; i < bossIds.Length; i++)
            {
                WaveConfiguration wave = CreateInstance<WaveConfiguration>();
                wave.name = $"boss_rush_w{i + 1}";
                wave.waveId = $"boss_rush_w{i + 1}";
                wave.waveNumber = i + 1;
                wave.spawnInterval = 0.9f;
                wave.spawnDelay = 0.5f;
                wave.bossWave = true;
                wave.bossId = bossIds[i];
                wave.spawnGroups = new List<WaveSpawnGroup>
                {
                    new WaveSpawnGroup
                    {
                        adventurerType = bossUnits[i],
                        count = 1,
                        spawnInterval = 0.9f,
                        spawnOrder = 0
                    }
                };
                list.Add(wave);
            }
            return list;
        }
    }
}
