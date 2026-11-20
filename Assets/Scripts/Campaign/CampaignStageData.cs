using System;
using System.Collections.Generic;
using UnityEngine;

namespace DreamDungeon
{
    public enum CampaignStageStatus
    {
        Locked = 0,
        Available = 1,
        InProgress = 2,
        Completed = 3,
        PerfectClear = 4
    }

    [Serializable]
    public sealed class StageRewardInfo
    {
        [Min(0)] public int gold = 150;
        [Min(0)] public int soul = 30;
        [Min(0)] public int exp = 60;
    }

    [CreateAssetMenu(menuName = "Dream Dungeon/Campaign Stage Data", fileName = "CampaignStageData")]
    public sealed class CampaignStageData : ScriptableObject
    {
        [Header("Stage Identity")]
        public string stageId = "forest_stage_1";
        public string stageName = "Forest Outskirts";
        [TextArea] public string description = "Repel the initial scouting party entering the forest border.";
        public string regionId = "forgotten_forest";
        [Min(1)] public int stageNumber = 1;

        [Header("Difficulty & Level")]
        public StageDifficulty difficulty = StageDifficulty.Easy;
        [Min(1)] public int recommendedDungeonLevel = 1;

        [Header("Waves, Factions & Boss")]
        public List<WaveConfiguration> waveConfiguration = new List<WaveConfiguration>();
        public List<string> adventurerGroupIds = new List<string>();
        public string bossId = string.Empty;

        [Header("Rewards")]
        [Min(0)] public int rewardGold = 200;
        [Min(0)] public int rewardSoul = 40;
        [Min(0)] public int rewardDungeonEXP = 80;
        public StageRewardInfo firstClearReward = new StageRewardInfo { gold = 150, soul = 35, exp = 60 };

        [Header("Unlock & Replay")]
        public string unlockRequirements = string.Empty;
        public bool replayable = true;
        [Range(0.1f, 1.0f)] public float perfectClearCoreHpRatio = 0.70f;

        private void OnValidate()
        {
            stageNumber = Mathf.Max(1, stageNumber);
            recommendedDungeonLevel = Mathf.Max(1, recommendedDungeonLevel);
            rewardGold = Mathf.Max(0, rewardGold);
            rewardSoul = Mathf.Max(0, rewardSoul);
            rewardDungeonEXP = Mathf.Max(0, rewardDungeonEXP);
        }
    }
}
