using System;

namespace DreamDungeon
{
    public enum QuestType
    {
        Main,
        Daily,
        Weekly,
        Special
    }

    public enum QuestObjectiveType
    {
        CompleteRaid,
        WinRaid,
        DefeatAdventurer,
        DefeatBoss,
        CompleteWave,
        ReachDungeonLevel,
        UpgradeCore,
        ExpandDungeon,
        UpgradeFacility,
        UnlockMonster,
        UnlockTrap,
        PlaceMonster,
        PlaceTrap,
        EarnGold,
        EarnSoul,
        PlaceRoom,
        UnlockRoom,
        MaximizeFacility
    }

    public enum QuestStatus
    {
        Locked,
        InProgress,
        Completed,
        Claimed
    }

    public enum QuestItemType
    {
        Room,
        Monster,
        Trap,
        UnlockContent
    }

    [Serializable]
    public sealed class QuestRewardItem
    {
        public QuestItemType itemType = QuestItemType.Room;
        public string itemId = string.Empty;
        public int quantity = 1;
    }

    public interface ITimeProvider
    {
        DateTime UtcNow { get; }
    }

    public sealed class SystemTimeProvider : ITimeProvider
    {
        public DateTime UtcNow => DateTime.UtcNow;
    }
}
