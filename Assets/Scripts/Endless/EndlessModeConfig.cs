using System;
using System.Collections.Generic;
using UnityEngine;

namespace DreamDungeon
{
    public enum EndlessBossTier
    {
        MiniBoss,
        MajorBoss,
        EliteBoss,
        EndlessBoss
    }

    [Serializable]
    public sealed class EndlessWaveScalingCheckpoint
    {
        [Min(1)] public int waveNumber = 1;
        [Min(0.1f)] public float enemyHpMultiplier = 1.0f;
        [Min(0.1f)] public float enemyAttackMultiplier = 1.0f;
        [Min(0.1f)] public float enemyDefenseMultiplier = 1.0f;
        [Min(0.1f)] public float enemyCountMultiplier = 1.0f;
        [Min(0.1f)] public float rewardMultiplier = 1.0f;
    }

    [Serializable]
    public sealed class EndlessBossScheduleEntry
    {
        [Min(1)] public int waveInterval = 5;
        public EndlessBossTier bossTier = EndlessBossTier.MiniBoss;
        public string adventurerId = "paladin";
        public string referencedBossDataId = "cave_dragon";
        public string bossTitle = "Abyssal Mini-Boss";
        [Min(1f)] public float bossHpMultiplier = 2.5f;
        [Min(1f)] public float bossAttackMultiplier = 1.6f;
        [Min(1f)] public float bossDefenseMultiplier = 1.5f;
        public Color bossAuraColor = new Color(1.0f, 0.35f, 0.25f, 1f);
        [Min(0)] public int bonusGoldReward = 150;
        [Min(0)] public int bonusSoulReward = 35;
        [Min(0)] public int bonusExpReward = 80;
    }

    [CreateAssetMenu(fileName = "EndlessModeConfig", menuName = "Dream Dungeon/Endless/Endless Mode Config")]
    public sealed class EndlessModeConfig : ScriptableObject
    {
        [Header("Core Endless Settings")]
        [Min(1)] public int startingWave = 1;
        [Min(1)] public int baseEnemiesPerWave = 4;
        [Min(1)] public int maxEnemiesPerWaveCap = 36;
        [Min(0.2f)] public float baseSpawnInterval = 0.85f;
        [Min(0.15f)] public float minSpawnInterval = 0.30f;

        [Header("Base Multipliers")]
        [Min(0.1f)] public float baseEnemyHpMultiplier = 1.0f;
        [Min(0.1f)] public float baseEnemyAttackMultiplier = 1.0f;
        [Min(0.1f)] public float baseEnemyDefenseMultiplier = 1.0f;
        [Min(0.1f)] public float baseEnemyCountMultiplier = 1.0f;
        [Min(0.1f)] public float baseRewardMultiplier = 1.0f;

        [Header("Intervals")]
        [Min(1)] public int eliteWaveInterval = 3;
        [Min(1)] public int bossWaveInterval = 5;

        [Header("Base Rewards Per Wave")]
        [Min(0)] public int goldPerWaveCleared = 45;
        [Min(0)] public int soulPerWaveCleared = 10;
        [Min(0)] public int expPerWaveCleared = 25;
        [Min(0)] public int goldPerEnemyDefeated = 8;
        [Min(0)] public int soulPerEnemyDefeated = 2;
        [Min(0)] public int goldPerEliteDefeated = 25;
        [Min(0)] public int soulPerEliteDefeated = 6;

        [Header("Wave Scaling Checkpoints (Piecewise Linear)")]
        public List<EndlessWaveScalingCheckpoint> scalingCheckpoints = new List<EndlessWaveScalingCheckpoint>();

        [Header("Normal Raider Pool")]
        public List<string> normalAdventurerPool = new List<string>
        {
            "swordsman", "archer", "mage", "knight", "bandit", "footman", "dark_archer", "paladin", "necromancer"
        };

        [Header("Elite Enemy Variants")]
        public List<EliteEnemyData> eliteVariants = new List<EliteEnemyData>();

        [Header("Boss Wave Schedule")]
        public List<EndlessBossScheduleEntry> bossSchedule = new List<EndlessBossScheduleEntry>();

        public void EvaluateWaveMultipliers(int waveNumber, out float hpMult, out float atkMult, out float defMult, out float countMult, out float rewardMult)
        {
            int wave = Mathf.Max(1, waveNumber);
            EnsureDefaultsIfEmpty();

            EndlessWaveScalingCheckpoint prev = scalingCheckpoints[0];
            EndlessWaveScalingCheckpoint next = scalingCheckpoints[scalingCheckpoints.Count - 1];

            if (wave <= prev.waveNumber)
            {
                hpMult = ClampMultiplier(prev.enemyHpMultiplier * baseEnemyHpMultiplier);
                atkMult = ClampMultiplier(prev.enemyAttackMultiplier * baseEnemyAttackMultiplier);
                defMult = ClampMultiplier(prev.enemyDefenseMultiplier * baseEnemyDefenseMultiplier);
                countMult = ClampCountMultiplier(prev.enemyCountMultiplier * baseEnemyCountMultiplier);
                rewardMult = ClampMultiplier(prev.rewardMultiplier * baseRewardMultiplier);
                return;
            }

            if (wave >= next.waveNumber)
            {
                int extraWaves = wave - next.waveNumber;
                hpMult = ClampMultiplier((next.enemyHpMultiplier + extraWaves * 0.10f) * baseEnemyHpMultiplier);
                atkMult = ClampMultiplier((next.enemyAttackMultiplier + extraWaves * 0.06f) * baseEnemyAttackMultiplier);
                defMult = ClampMultiplier((next.enemyDefenseMultiplier + extraWaves * 0.05f) * baseEnemyDefenseMultiplier);
                countMult = ClampCountMultiplier((next.enemyCountMultiplier + extraWaves * 0.04f) * baseEnemyCountMultiplier);
                rewardMult = ClampMultiplier((next.rewardMultiplier + extraWaves * 0.08f) * baseRewardMultiplier);
                return;
            }

            for (int i = 0; i < scalingCheckpoints.Count - 1; i++)
            {
                var a = scalingCheckpoints[i];
                var b = scalingCheckpoints[i + 1];
                if (wave >= a.waveNumber && wave <= b.waveNumber)
                {
                    prev = a;
                    next = b;
                    break;
                }
            }

            float t = next.waveNumber > prev.waveNumber
                ? (float)(wave - prev.waveNumber) / (next.waveNumber - prev.waveNumber)
                : 0f;

            hpMult = ClampMultiplier(Mathf.Lerp(prev.enemyHpMultiplier, next.enemyHpMultiplier, t) * baseEnemyHpMultiplier);
            atkMult = ClampMultiplier(Mathf.Lerp(prev.enemyAttackMultiplier, next.enemyAttackMultiplier, t) * baseEnemyAttackMultiplier);
            defMult = ClampMultiplier(Mathf.Lerp(prev.enemyDefenseMultiplier, next.enemyDefenseMultiplier, t) * baseEnemyDefenseMultiplier);
            countMult = ClampCountMultiplier(Mathf.Lerp(prev.enemyCountMultiplier, next.enemyCountMultiplier, t) * baseEnemyCountMultiplier);
            rewardMult = ClampMultiplier(Mathf.Lerp(prev.rewardMultiplier, next.rewardMultiplier, t) * baseRewardMultiplier);
        }

        public int GetEnemyCountForWave(int waveNumber)
        {
            EvaluateWaveMultipliers(waveNumber, out _, out _, out _, out float countMult, out _);
            int count = Mathf.RoundToInt(Mathf.Max(1, baseEnemiesPerWave) * countMult);
            return Mathf.Clamp(count, 1, Mathf.Max(4, maxEnemiesPerWaveCap));
        }

        public float GetSpawnIntervalForWave(int waveNumber)
        {
            int wave = Mathf.Max(1, waveNumber);
            float interval = baseSpawnInterval - (wave - 1) * 0.015f;
            return Mathf.Clamp(interval, minSpawnInterval, baseSpawnInterval);
        }

        public bool IsEliteWave(int waveNumber)
        {
            int wave = Mathf.Max(1, waveNumber);
            return eliteWaveInterval > 0 && (wave % eliteWaveInterval == 0 || wave >= 6);
        }

        public bool IsBossWave(int waveNumber, out EndlessBossScheduleEntry bossEntry)
        {
            int wave = Mathf.Max(1, waveNumber);
            EnsureDefaultsIfEmpty();

            if (bossWaveInterval <= 0 || wave % bossWaveInterval != 0)
            {
                bossEntry = null;
                return false;
            }

            // Pick the matching milestone entry (e.g., 30 -> EndlessBoss, 15 -> EliteBoss, 10 -> MajorBoss, 5 -> MiniBoss)
            EndlessBossScheduleEntry best = bossSchedule[0];
            for (int i = 0; i < bossSchedule.Count; i++)
            {
                var candidate = bossSchedule[i];
                if (candidate != null && candidate.waveInterval > 0 && wave % candidate.waveInterval == 0)
                {
                    if (candidate.waveInterval >= best.waveInterval)
                    {
                        best = candidate;
                    }
                }
            }

            bossEntry = best;
            return bossEntry != null;
        }

        public EliteEnemyData GetEliteForWave(int waveNumber, int indexOffset = 0)
        {
            EnsureDefaultsIfEmpty();
            if (eliteVariants == null || eliteVariants.Count == 0) return null;
            int idx = Mathf.Abs(waveNumber + indexOffset) % eliteVariants.Count;
            return eliteVariants[idx];
        }

        private static float ClampMultiplier(float value)
        {
            if (float.IsNaN(value) || float.IsInfinity(value)) return 1f;
            return Mathf.Clamp(value, 0.1f, 250f);
        }

        private static float ClampCountMultiplier(float value)
        {
            if (float.IsNaN(value) || float.IsInfinity(value)) return 1f;
            return Mathf.Clamp(value, 0.25f, 10f);
        }

        public void EnsureDefaultsIfEmpty()
        {
            if (scalingCheckpoints == null || scalingCheckpoints.Count == 0)
            {
                scalingCheckpoints = new List<EndlessWaveScalingCheckpoint>
                {
                    new EndlessWaveScalingCheckpoint { waveNumber = 1,  enemyHpMultiplier = 1.00f, enemyAttackMultiplier = 1.00f, enemyDefenseMultiplier = 1.00f, enemyCountMultiplier = 1.00f, rewardMultiplier = 1.00f },
                    new EndlessWaveScalingCheckpoint { waveNumber = 5,  enemyHpMultiplier = 1.25f, enemyAttackMultiplier = 1.15f, enemyDefenseMultiplier = 1.10f, enemyCountMultiplier = 1.20f, rewardMultiplier = 1.25f },
                    new EndlessWaveScalingCheckpoint { waveNumber = 10, enemyHpMultiplier = 1.75f, enemyAttackMultiplier = 1.40f, enemyDefenseMultiplier = 1.25f, enemyCountMultiplier = 1.50f, rewardMultiplier = 1.60f },
                    new EndlessWaveScalingCheckpoint { waveNumber = 20, enemyHpMultiplier = 2.50f, enemyAttackMultiplier = 1.90f, enemyDefenseMultiplier = 1.60f, enemyCountMultiplier = 2.00f, rewardMultiplier = 2.25f },
                    new EndlessWaveScalingCheckpoint { waveNumber = 30, enemyHpMultiplier = 3.50f, enemyAttackMultiplier = 2.50f, enemyDefenseMultiplier = 2.00f, enemyCountMultiplier = 2.50f, rewardMultiplier = 3.20f }
                };
            }

            if (eliteVariants == null || eliteVariants.Count == 0)
            {
                eliteVariants = new List<EliteEnemyData>
                {
                    EliteEnemyData.CreateRuntimePreset("elite_knight", "Elite Knight", "elite_knight",
                        "Heavy plate champion with 25% damage resistance.",
                        1.85f, 1.35f, 1.60f, 1.05f, 1.10f, 0.25f, ElitePassiveType.DamageResistance, 0, new Color(1.0f, 0.84f, 0.30f)),
                    EliteEnemyData.CreateRuntimePreset("elite_archer", "Elite Archer", "dark_archer",
                        "Deadly sharpshooter with rapid volley speed.",
                        1.50f, 1.55f, 1.20f, 1.15f, 1.35f, 0.10f, ElitePassiveType.FrenzySpeed, 0, new Color(0.40f, 1.0f, 0.72f)),
                    EliteEnemyData.CreateRuntimePreset("elite_mage", "Elite Mage", "necromancer",
                        "Arcane arch-warlock dealing devastating spell damage.",
                        1.45f, 1.70f, 1.20f, 1.05f, 1.20f, 0.15f, ElitePassiveType.CoreBreaker, 0, new Color(0.88f, 0.45f, 1.0f)),
                    EliteEnemyData.CreateRuntimePreset("elite_assassin", "Elite Assassin", "bandit",
                        "Lightning-fast shadow blade that rushes dungeon defenses.",
                        1.40f, 1.60f, 1.15f, 1.40f, 1.40f, 0.15f, ElitePassiveType.FrenzySpeed, 0, new Color(1.0f, 0.38f, 0.52f)),
                    EliteEnemyData.CreateRuntimePreset("elite_paladin", "Elite Paladin", "paladin",
                        "Blessed crusader with continuous holy regeneration.",
                        2.00f, 1.40f, 1.55f, 1.05f, 1.10f, 0.20f, ElitePassiveType.Regeneration, 12, new Color(1.0f, 0.92f, 0.55f))
                };
            }

            if (bossSchedule == null || bossSchedule.Count == 0)
            {
                bossSchedule = new List<EndlessBossScheduleEntry>
                {
                    new EndlessBossScheduleEntry
                    {
                        waveInterval = 5,
                        bossTier = EndlessBossTier.MiniBoss,
                        adventurerId = "paladin",
                        referencedBossDataId = "cave_dragon",
                        bossTitle = "Mini-Boss: Crimson Vanguard",
                        bossHpMultiplier = 2.8f,
                        bossAttackMultiplier = 1.6f,
                        bossDefenseMultiplier = 1.4f,
                        bossAuraColor = new Color(1.0f, 0.55f, 0.22f),
                        bonusGoldReward = 150,
                        bonusSoulReward = 35,
                        bonusExpReward = 80
                    },
                    new EndlessBossScheduleEntry
                    {
                        waveInterval = 10,
                        bossTier = EndlessBossTier.MajorBoss,
                        adventurerId = "elite_knight",
                        referencedBossDataId = "dark_golem",
                        bossTitle = "Major Boss: Dread Monolith",
                        bossHpMultiplier = 4.2f,
                        bossAttackMultiplier = 2.0f,
                        bossDefenseMultiplier = 1.75f,
                        bossAuraColor = new Color(0.95f, 0.25f, 0.25f),
                        bonusGoldReward = 320,
                        bonusSoulReward = 75,
                        bonusExpReward = 160
                    },
                    new EndlessBossScheduleEntry
                    {
                        waveInterval = 15,
                        bossTier = EndlessBossTier.EliteBoss,
                        adventurerId = "necromancer",
                        referencedBossDataId = "lich_king",
                        bossTitle = "Elite Boss: Sovereign of Bone",
                        bossHpMultiplier = 5.5f,
                        bossAttackMultiplier = 2.35f,
                        bossDefenseMultiplier = 2.0f,
                        bossAuraColor = new Color(0.75f, 0.30f, 1.0f),
                        bonusGoldReward = 500,
                        bonusSoulReward = 120,
                        bonusExpReward = 260
                    },
                    new EndlessBossScheduleEntry
                    {
                        waveInterval = 30,
                        bossTier = EndlessBossTier.EndlessBoss,
                        adventurerId = "elite_knight",
                        referencedBossDataId = "demon_lord",
                        bossTitle = "Endless Boss: Abyssal Overlord",
                        bossHpMultiplier = 7.5f,
                        bossAttackMultiplier = 2.85f,
                        bossDefenseMultiplier = 2.4f,
                        bossAuraColor = new Color(1.0f, 0.15f, 0.35f),
                        bonusGoldReward = 900,
                        bonusSoulReward = 220,
                        bonusExpReward = 450
                    }
                };
            }
        }

        public static EndlessModeConfig CreateRuntimeDefaultConfig()
        {
            EndlessModeConfig cfg = CreateInstance<EndlessModeConfig>();
            cfg.name = "DefaultEndlessModeConfig";
            cfg.EnsureDefaultsIfEmpty();
            return cfg;
        }
    }
}
