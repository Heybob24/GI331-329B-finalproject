namespace DreamDungeon
{
    public enum DungeonRoomType
    {
        Entrance,
        DungeonCore,
        MonsterRoom,
        TreasureRoom,
        TrapRoom,
        MagicRoom,
        BossRoom,
        Path
    }

    public enum DungeonCellType
    {
        Empty,
        Entrance,
        DungeonCore,
        MonsterRoom,
        TreasureRoom,
        TrapRoom,
        MagicRoom,
        BossRoom,
        Path
    }

    public enum DungeonBuildTool
    {
        Room,
        Path,
        Move,
        Remove
    }
}
