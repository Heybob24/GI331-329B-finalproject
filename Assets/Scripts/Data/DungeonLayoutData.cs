using System;
using System.Collections.Generic;
using UnityEngine;

namespace DreamDungeon
{
    [Serializable]
    public sealed class DungeonLayoutData
    {
        public int gridWidth = 9;
        public int gridHeight = 9;
        public List<DungeonRoomInstanceData> rooms = new List<DungeonRoomInstanceData>();
        public List<MonsterInstanceData> monsters = new List<MonsterInstanceData>();
        public List<TrapInstanceData> traps = new List<TrapInstanceData>();
        public List<BossInstanceData> bosses = new List<BossInstanceData>();

        public string ToJson() => JsonUtility.ToJson(this);

        public static DungeonLayoutData FromJson(string json)
        {
            DungeonLayoutData data = JsonUtility.FromJson<DungeonLayoutData>(json);
            if (data == null) throw new ArgumentException("Dungeon layout JSON could not be read.", nameof(json));
            if (data.rooms == null) data.rooms = new List<DungeonRoomInstanceData>();
            if (data.monsters == null) data.monsters = new List<MonsterInstanceData>();
            if (data.traps == null) data.traps = new List<TrapInstanceData>();
            if (data.bosses == null) data.bosses = new List<BossInstanceData>();
            return data;
        }
    }

    [Serializable]
    public sealed class DungeonRoomInstanceData
    {
        public string instanceId;
        public string roomId;
        public DungeonRoomType roomType;
        public Vector2Int position;
        public int rotation;
        public int width;
        public int height;
        public int upgradeLevel = 1;
        public bool isPlaced = true;
    }
}
