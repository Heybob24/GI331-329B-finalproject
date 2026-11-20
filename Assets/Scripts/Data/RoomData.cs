using UnityEngine;

namespace DreamDungeon
{
    [CreateAssetMenu(menuName = "Dream Dungeon/Room Data", fileName = "RoomData")]
    public sealed class RoomData : ScriptableObject
    {
        public string roomId;
        public string roomName;
        [TextArea] public string description;
        public Sprite roomIcon;
        public GameObject roomPrefab;
        public DungeonRoomType roomType;
        [Min(1)] public int width = 1;
        [Min(1)] public int height = 1;
        [Min(0)] public int goldCost;
        [Min(0)] public int soulCost;
        [Min(1)] public int unlockDungeonLevel = 1;
        [Min(1)] public int maxLevel = 5;
        [Min(0)] public int baseUpgradeGoldCost;
        [Min(0)] public int baseUpgradeSoulCost;
        [Min(0)] public int maxCapacity = 99;

        public bool IsPurchasable => roomType != DungeonRoomType.Entrance && roomType != DungeonRoomType.DungeonCore && roomType != DungeonRoomType.Path;

        private void OnValidate()
        {
            if (string.IsNullOrWhiteSpace(roomId)) Debug.LogError($"RoomData '{name}' needs a stable roomId.", this);
            width = Mathf.Max(1, width);
            height = Mathf.Max(1, height);
            goldCost = Mathf.Max(0, goldCost);
            soulCost = Mathf.Max(0, soulCost);
            unlockDungeonLevel = Mathf.Max(1, unlockDungeonLevel);
            maxLevel = Mathf.Max(1, maxLevel);
            baseUpgradeGoldCost = Mathf.Max(0, baseUpgradeGoldCost);
            baseUpgradeSoulCost = Mathf.Max(0, baseUpgradeSoulCost);
            maxCapacity = Mathf.Max(0, maxCapacity);
        }
    }
}
