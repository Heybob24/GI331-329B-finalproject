using System;
using System.Collections.Generic;

namespace DreamDungeon
{
    [Serializable]
    public sealed class StageProgressRecord
    {
        public string stageId = string.Empty;
        public CampaignStageStatus status = CampaignStageStatus.Locked;
        public StageDifficulty bestDifficulty = StageDifficulty.Easy;
        public int bestWave = 0;
        public int totalAttempts = 0;
        public int totalVictories = 0;
        public int totalDefeats = 0;
        public bool firstClearClaimed = false;
        public bool perfectClearAchieved = false;
    }

    [Serializable]
    public sealed class CampaignSaveState
    {
        public int saveVersion = 1;

        // World Map
        public List<string> unlockedRegionIds = new List<string>();
        public List<string> completedRegionIds = new List<string>();
        public List<string> claimedRegionRewardIds = new List<string>();

        // Campaign & Stages
        public List<string> unlockedStageIds = new List<string>();
        public List<string> completedStageIds = new List<string>();
        public List<string> firstClearStageIds = new List<string>();
        public List<string> perfectClearStageIds = new List<string>();
        public List<string> defeatedBossIds = new List<string>();
        public List<string> claimedBattleResultIds = new List<string>();
        public List<StageProgressRecord> stageRecords = new List<StageProgressRecord>();

        // Global Campaign Metrics
        public int totalAttempts = 0;
        public int totalVictories = 0;
        public int totalDefeats = 0;

        public CampaignSaveState()
        {
            saveVersion = 1;
            unlockedRegionIds = new List<string>();
            completedRegionIds = new List<string>();
            claimedRegionRewardIds = new List<string>();
            unlockedStageIds = new List<string>();
            completedStageIds = new List<string>();
            firstClearStageIds = new List<string>();
            perfectClearStageIds = new List<string>();
            defeatedBossIds = new List<string>();
            claimedBattleResultIds = new List<string>();
            stageRecords = new List<StageProgressRecord>();
        }
    }
}
