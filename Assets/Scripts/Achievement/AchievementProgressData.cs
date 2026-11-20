using System;
using System.Collections.Generic;

namespace DreamDungeon
{
    [Serializable]
    public sealed class AchievementProgressEntry
    {
        public string achievementId = string.Empty;
        public int currentProgress = 0;
        public bool isUnlocked = false;
        public bool isRewardClaimed = false;
        public string rewardClaimId = string.Empty;
        public long unlockedTimestamp = 0;
    }

    [Serializable]
    public sealed class AchievementSaveState
    {
        public int saveVersion = 1;
        public List<AchievementProgressEntry> entries = new List<AchievementProgressEntry>();
        public List<string> claimedRewardIds = new List<string>();
        public List<string> processedEventIds = new List<string>();

        public AchievementSaveState()
        {
            saveVersion = 1;
            entries = new List<AchievementProgressEntry>();
            claimedRewardIds = new List<string>();
            processedEventIds = new List<string>();
        }
    }
}
