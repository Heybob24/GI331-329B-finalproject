using System;
using System.Collections.Generic;
using UnityEngine;

namespace DreamDungeon
{
    [Serializable]
    public sealed class DungeonProgressionData
    {
        [Header("Level & Experience")]
        public int dungeonLevel = 1;
        public int currentExp = 0;
        public int totalExpEarned = 0;

        [Header("Statistics")]
        public int totalRaids = 0;
        public int totalVictories = 0;
        public int totalDefeats = 0;
        public int totalBossesDefeated = 0;
        public int highestWaveCleared = 0;

        [Header("Claimed Level Rewards")]
        public List<int> claimedLevelRewards = new List<int>();

        [Header("Unlocked Features & Content")]
        public List<string> unlockedRoomIds = new List<string>();
        public List<string> unlockedMonsterIds = new List<string>();
        public List<string> unlockedTrapIds = new List<string>();
        public List<string> unlockedBossIds = new List<string>();

        public DungeonProgressionData()
        {
            dungeonLevel = 1;
            currentExp = 0;
            totalExpEarned = 0;
            claimedLevelRewards = new List<int>();
            unlockedRoomIds = new List<string>();
            unlockedMonsterIds = new List<string>();
            unlockedTrapIds = new List<string>();
            unlockedBossIds = new List<string>();
        }
    }
}
