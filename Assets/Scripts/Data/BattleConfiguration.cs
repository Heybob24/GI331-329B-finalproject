using System.Collections.Generic;
using UnityEngine;

namespace DreamDungeon
{
    [System.Serializable]
    public sealed class AdventurerWaveEntry
    {
        public string adventurerId;
        [Min(1)] public int count = 1;
    }

    [System.Serializable]
    public sealed class AdventurerWave
    {
        public string waveName = "Wave";
        public List<AdventurerWaveEntry> entries = new List<AdventurerWaveEntry>();
    }

    [CreateAssetMenu(menuName = "Dream Dungeon/Battle Configuration", fileName = "BattleConfiguration")]
    public sealed class BattleConfiguration : ScriptableObject
    {
        public List<AdventurerWave> waves = new List<AdventurerWave>();
        [Min(.1f)] public float spawnInterval = 1f;
        [Min(1)] public int coreMaxHP = 500;
        [Min(1)] public int coreDefense;
        [Min(0)] public int goldPerDefeatedAdventurer = 10;
        [Min(0)] public int soulPerDefeatedAdventurer = 2;
        [Min(0)] public int victoryBonusGold = 100;
        [Min(0)] public int victoryBonusSoul = 20;
        [Min(0)] public int victoryBonusExp = 50;
        [Min(0)] public int defeatGoldPerDefeated = 5;
        [Min(0)] public int defeatSoulPerDefeated = 1;

        public bool Validate(AdventurerDatabase adventurers, out string error)
        {
            if (adventurers == null) { error = "Adventurer Database is missing."; return false; }
            if (!adventurers.Validate(out error)) return false;
            if (waves == null || waves.Count == 0) { error = "Battle Configuration has no waves."; return false; }
            foreach (AdventurerWave wave in waves)
            {
                if (wave == null || wave.entries == null || wave.entries.Count == 0) { error = "A battle wave has no adventurers."; return false; }
                foreach (AdventurerWaveEntry entry in wave.entries)
                    if (entry == null || entry.count < 1 || adventurers.Find(entry.adventurerId) == null) { error = $"Wave '{wave.waveName}' contains an invalid Adventurer entry."; return false; }
            }
            if (coreMaxHP < 1 || spawnInterval <= 0) { error = "Battle Configuration has an invalid Core HP or spawn interval."; return false; }
            error = string.Empty; return true;
        }
    }
}
