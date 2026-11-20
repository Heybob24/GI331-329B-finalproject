using System;
using System.Collections.Generic;
using UnityEngine;

namespace DreamDungeon
{
    [CreateAssetMenu(fileName = "DungeonLevel", menuName = "Dream Dungeon/Progression/Dungeon Level Data")]
    public sealed class DungeonLevelData : ScriptableObject
    {
        [Header("Level Information")]
        public int level = 1;
        public int requiredExp = 100;

        [Header("Dungeon Grid & Capacity")]
        public int dungeonGridWidth = 9;
        public int dungeonGridHeight = 9;
        public int maxMonsterCapacity = 10;
        public int maxTrapCapacity = 10;

        [Header("Unlocked Content IDs")]
        public List<string> unlockedRoomIds = new List<string>();
        public List<string> unlockedMonsterIds = new List<string>();
        public List<string> unlockedTrapIds = new List<string>();
        public List<string> unlockedBossIds = new List<string>();

        [Header("Level-Up Rewards")]
        public int rewardGold = 200;
        public int rewardSoul = 50;
    }
}
