using System;
using System.Collections.Generic;
using UnityEngine;

namespace DreamDungeon
{
    [Serializable]
    public sealed class QuestProgressEntry
    {
        public string questId = string.Empty;
        public int currentProgress = 0;
        public QuestStatus status = QuestStatus.InProgress;
        public bool isRewardClaimed = false;
        public string rewardClaimId = string.Empty;
        public long lastUpdatedTimestamp = 0;
    }

    [Serializable]
    public sealed class QuestSaveState
    {
        public List<QuestProgressEntry> entries = new List<QuestProgressEntry>();
        public List<string> unlockedMainQuestIds = new List<string>();
        public List<string> claimedRewardIds = new List<string>();
        public List<string> activeDailyQuestIds = new List<string>();
        public List<string> activeWeeklyQuestIds = new List<string>();
        public string lastDailyResetDate = string.Empty;
        public string lastWeeklyResetDate = string.Empty;
        public List<string> processedEventIds = new List<string>();

        public QuestSaveState()
        {
            entries = new List<QuestProgressEntry>();
            unlockedMainQuestIds = new List<string>();
            claimedRewardIds = new List<string>();
            activeDailyQuestIds = new List<string>();
            activeWeeklyQuestIds = new List<string>();
            lastDailyResetDate = string.Empty;
            lastWeeklyResetDate = string.Empty;
            processedEventIds = new List<string>();
        }
    }
}
