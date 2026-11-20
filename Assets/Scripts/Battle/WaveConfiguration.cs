using System;
using System.Collections.Generic;
using UnityEngine;

namespace DreamDungeon
{
    [Serializable]
    public sealed class WaveSpawnGroup
    {
        public string adventurerType = "swordsman";
        [Min(1)] public int count = 2;
        [Min(0.1f)] public float spawnInterval = 0.8f;
        [Min(0)] public int spawnOrder = 0;
    }

    [Serializable]
    public sealed class WaveRewardData
    {
        [Min(0)] public int bonusGold = 30;
        [Min(0)] public int bonusSoul = 5;
        [Min(0)] public int bonusEXP = 20;
    }

    [CreateAssetMenu(menuName = "Dream Dungeon/Wave Configuration", fileName = "WaveConfiguration")]
    public sealed class WaveConfiguration : ScriptableObject
    {
        [Header("Wave Identity")]
        public string waveId = "wave_1";
        [Min(1)] public int waveNumber = 1;

        [Header("Spawn Groups & Timing")]
        public List<WaveSpawnGroup> spawnGroups = new List<WaveSpawnGroup>();
        [Min(0.1f)] public float spawnInterval = 0.85f;
        [Min(0f)] public float spawnDelay = 0.25f;
        [Min(1)] public int maxAdventurers = 25;

        [Header("Boss Wave Settings")]
        public bool bossWave = false;
        public string bossId = string.Empty;

        [Header("Wave Completion Reward")]
        public WaveRewardData waveReward = new WaveRewardData();

        private void OnValidate()
        {
            waveNumber = Mathf.Max(1, waveNumber);
            spawnInterval = Mathf.Max(0.1f, spawnInterval);
            spawnDelay = Mathf.Max(0f, spawnDelay);
            maxAdventurers = Mathf.Max(1, maxAdventurers);
        }

        public AdventurerWave ToAdventurerWave(float enemyCountMultiplier = 1f)
        {
            var wave = new AdventurerWave
            {
                waveName = bossWave ? $"Wave {waveNumber} (BOSS)" : $"Wave {waveNumber}",
                entries = new List<AdventurerWaveEntry>()
            };

            if (spawnGroups != null)
            {
                var sorted = new List<WaveSpawnGroup>(spawnGroups);
                sorted.Sort((a, b) => (a != null ? a.spawnOrder : 0).CompareTo(b != null ? b.spawnOrder : 0));

                int totalScheduled = 0;
                foreach (WaveSpawnGroup g in sorted)
                {
                    if (g == null || string.IsNullOrWhiteSpace(g.adventurerType) || g.count <= 0) continue;
                    int scaledCount = Mathf.Max(1, Mathf.RoundToInt(g.count * Mathf.Max(0.5f, enemyCountMultiplier)));
                    if (totalScheduled + scaledCount > maxAdventurers)
                    {
                        scaledCount = Mathf.Max(0, maxAdventurers - totalScheduled);
                    }
                    if (scaledCount <= 0) break;

                    wave.entries.Add(new AdventurerWaveEntry
                    {
                        adventurerId = g.adventurerType.Trim(),
                        count = scaledCount
                    });
                    totalScheduled += scaledCount;
                }
            }

            if (bossWave && !string.IsNullOrWhiteSpace(bossId) && wave.entries.Count < maxAdventurers)
            {
                // Add an elite champion invader to accompany the boss wave
                wave.entries.Add(new AdventurerWaveEntry
                {
                    adventurerId = "knight",
                    count = 1
                });
            }

            return wave;
        }

        public static BattleConfiguration BuildRuntimeBattleConfiguration(
            IReadOnlyList<WaveConfiguration> stageWaves,
            DifficultyData difficulty,
            AdventurerFactionData faction,
            BattleConfiguration baseTemplate = null)
        {
            BattleConfiguration runtime = CreateInstance<BattleConfiguration>();
            runtime.name = "RuntimeCampaignBattleConfig";

            float countMult = difficulty != null ? difficulty.enemyCountMultiplier : 1.0f;
            float intervalMult = faction?.factionSpawnConfiguration != null
                ? Mathf.Max(0.25f, faction.factionSpawnConfiguration.spawnIntervalMultiplier)
                : 1.0f;

            runtime.coreMaxHP = baseTemplate != null ? baseTemplate.coreMaxHP : 500;
            runtime.coreDefense = baseTemplate != null ? baseTemplate.coreDefense : 1;
            runtime.goldPerDefeatedAdventurer = baseTemplate != null ? baseTemplate.goldPerDefeatedAdventurer : 10;
            runtime.soulPerDefeatedAdventurer = baseTemplate != null ? baseTemplate.soulPerDefeatedAdventurer : 2;
            runtime.victoryBonusGold = baseTemplate != null ? baseTemplate.victoryBonusGold : 100;
            runtime.victoryBonusSoul = baseTemplate != null ? baseTemplate.victoryBonusSoul : 20;
            runtime.victoryBonusExp = baseTemplate != null ? baseTemplate.victoryBonusExp : 50;
            runtime.defeatGoldPerDefeated = baseTemplate != null ? baseTemplate.defeatGoldPerDefeated : 5;
            runtime.defeatSoulPerDefeated = baseTemplate != null ? baseTemplate.defeatSoulPerDefeated : 1;

            runtime.waves = new List<AdventurerWave>();
            float avgInterval = 0.85f;

            if (stageWaves != null && stageWaves.Count > 0)
            {
                float sumInterval = 0f;
                int validWaves = 0;
                foreach (WaveConfiguration wc in stageWaves)
                {
                    if (wc == null) continue;
                    AdventurerWave converted = wc.ToAdventurerWave(countMult);
                    if (converted.entries.Count > 0)
                    {
                        runtime.waves.Add(converted);
                        sumInterval += wc.spawnInterval;
                        validWaves++;
                    }
                }
                if (validWaves > 0)
                {
                    avgInterval = sumInterval / validWaves;
                }
            }

            if (runtime.waves.Count == 0 && baseTemplate != null && baseTemplate.waves != null)
            {
                foreach (AdventurerWave w in baseTemplate.waves)
                {
                    if (w == null || w.entries == null) continue;
                    var copyWave = new AdventurerWave { waveName = w.waveName, entries = new List<AdventurerWaveEntry>() };
                    foreach (AdventurerWaveEntry e in w.entries)
                    {
                        if (e == null) continue;
                        copyWave.entries.Add(new AdventurerWaveEntry
                        {
                            adventurerId = e.adventurerId,
                            count = Mathf.Max(1, Mathf.RoundToInt(e.count * countMult))
                        });
                    }
                    runtime.waves.Add(copyWave);
                }
                avgInterval = baseTemplate.spawnInterval;
            }

            runtime.spawnInterval = Mathf.Max(0.2f, avgInterval * intervalMult);
            return runtime;
        }
    }
}
