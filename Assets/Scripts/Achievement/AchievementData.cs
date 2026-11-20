using UnityEngine;

namespace DreamDungeon
{
    public enum AchievementCategory
    {
        Combat,
        Dungeon,
        Progression,
        Economy
    }

    [CreateAssetMenu(menuName = "Dream Dungeon/Achievement Data", fileName = "AchievementData")]
    public sealed class AchievementData : ScriptableObject
    {
        [Header("Identity")]
        public string achievementId = string.Empty;
        public string achievementName = string.Empty;
        [TextArea] public string description = string.Empty;
        public Sprite icon;

        [Header("Classification & Objective")]
        public AchievementCategory category = AchievementCategory.Combat;
        public QuestObjectiveType objectiveType = QuestObjectiveType.CompleteRaid;
        [Min(1)] public int targetAmount = 1;

        [Header("Runtime / Inspector Defaults")]
        [Min(0)] public int currentProgress = 0;
        public bool isUnlocked = false;
        public bool isRewardClaimed = false;

        [Header("Rewards")]
        [Min(0)] public int rewardGold = 250;
        [Min(0)] public int rewardSoul = 50;
        [Min(0)] public int rewardDungeonEXP = 100;

        private void OnValidate()
        {
            targetAmount = Mathf.Max(1, targetAmount);
            currentProgress = Mathf.Clamp(currentProgress, 0, targetAmount);
            rewardGold = Mathf.Max(0, rewardGold);
            rewardSoul = Mathf.Max(0, rewardSoul);
            rewardDungeonEXP = Mathf.Max(0, rewardDungeonEXP);
        }
    }
}
