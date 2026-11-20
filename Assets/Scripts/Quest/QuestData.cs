using System.Collections.Generic;
using UnityEngine;

namespace DreamDungeon
{
    [CreateAssetMenu(menuName = "Dream Dungeon/Quest Data", fileName = "QuestData")]
    public sealed class QuestData : ScriptableObject
    {
        [Header("Identity")]
        public string questId = string.Empty;
        public string questName = string.Empty;
        [TextArea] public string description = string.Empty;
        public Sprite icon;

        [Header("Classification & Objective")]
        public QuestType questType = QuestType.Main;
        public QuestObjectiveType objectiveType = QuestObjectiveType.CompleteRaid;
        [Min(1)] public int targetAmount = 1;
        public string targetIdFilter = string.Empty;

        [Header("Main Quest Sequence")]
        [Min(1)] public int chapterIndex = 1;
        public string chapterTitle = "Chapter 1: Awaken the Dungeon";
        [Min(0)] public int stepOrder = 0;

        [Header("Rewards")]
        [Min(0)] public int rewardGold = 100;
        [Min(0)] public int rewardSoul = 20;
        [Min(0)] public int rewardDungeonEXP = 50;
        public List<QuestRewardItem> rewardItems = new List<QuestRewardItem>();

        [Header("Flags & Requirements")]
        public bool isRepeatable = false;
        public bool isActive = true;
        public string unlockRequirement = string.Empty;
        [Min(1)] public int requiredDungeonLevel = 1;

        private void OnValidate()
        {
            targetAmount = Mathf.Max(1, targetAmount);
            rewardGold = Mathf.Max(0, rewardGold);
            rewardSoul = Mathf.Max(0, rewardSoul);
            rewardDungeonEXP = Mathf.Max(0, rewardDungeonEXP);
            chapterIndex = Mathf.Max(1, chapterIndex);
            requiredDungeonLevel = Mathf.Max(1, requiredDungeonLevel);
        }
    }
}
