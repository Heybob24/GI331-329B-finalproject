using System;
using System.Collections.Generic;
using UnityEngine;

namespace DreamDungeon
{
    [Serializable]
    public sealed class RegionUnlockRequirement
    {
        public string requiredPreviousRegionId = string.Empty;
        public string requiredCampaignId = string.Empty;
        [Min(1)] public int requiredDungeonLevel = 1;
        public string requiredBossId = string.Empty;
        public string requiredQuestId = string.Empty;
    }

    [CreateAssetMenu(menuName = "Dream Dungeon/World Region Data", fileName = "WorldRegionData")]
    public sealed class WorldRegionData : ScriptableObject
    {
        [Header("Region Identity")]
        public string regionId = "forgotten_forest";
        public string regionName = "Forgotten Forest";
        [TextArea] public string description = "An ancient woodland at the dungeon frontier.";
        public Sprite regionIcon;
        public Sprite regionBackground;

        [Header("Ordering & Requirements")]
        [Min(1)] public int regionOrder = 1;
        [Min(1)] public int recommendedDungeonLevel = 1;
        [Min(0)] public int requiredCampaignLevel = 0;
        public StageDifficulty baseDifficulty = StageDifficulty.Easy;
        public string primaryFactionId = "adventurer_guild";
        public List<string> availableStageIds = new List<string>();
        public RegionUnlockRequirement unlockRequirements = new RegionUnlockRequirement();

        [Header("Completion Rewards & Flags")]
        [Min(0)] public int regionRewardGold = 400;
        [Min(0)] public int regionRewardSoul = 100;
        public bool isStartingRegion = false;

        private void OnValidate()
        {
            regionOrder = Mathf.Max(1, regionOrder);
            recommendedDungeonLevel = Mathf.Max(1, recommendedDungeonLevel);
            requiredCampaignLevel = Mathf.Max(0, requiredCampaignLevel);
            regionRewardGold = Mathf.Max(0, regionRewardGold);
            regionRewardSoul = Mathf.Max(0, regionRewardSoul);
        }
    }
}
