using System;
using System.Collections.Generic;
using UnityEngine;

namespace DreamDungeon
{
    [CreateAssetMenu(menuName = "Dream Dungeon/Campaign Data", fileName = "CampaignData")]
    public sealed class CampaignData : ScriptableObject
    {
        [Header("Campaign Identity")]
        public string campaignId = "campaign_forgotten_forest";
        public string campaignName = "Forgotten Forest Campaign";
        [TextArea] public string description = "Defend the outer forest dungeon from the Adventurer Guild.";
        public string regionId = "forgotten_forest";

        [Header("Stages & Ordering")]
        public List<string> stageIds = new List<string>();
        [Min(1)] public int campaignOrder = 1;
        public string unlockRequirements = string.Empty;

        [Header("Completion Reward")]
        public StageRewardInfo completionReward = new StageRewardInfo { gold = 400, soul = 80, exp = 150 };

        private void OnValidate()
        {
            campaignOrder = Mathf.Max(1, campaignOrder);
        }
    }
}
