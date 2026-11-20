using UnityEngine;

namespace DreamDungeon
{
    public enum MonsterRarity { Common, Rare, Epic, Legendary }
    public enum MonsterRole { Tank, Melee, Ranged, Magic, Boss }

    [CreateAssetMenu(menuName = "Dream Dungeon/Monster Data", fileName = "MonsterData")]
    public sealed class MonsterData : ScriptableObject
    {
        public string monsterId;
        public string monsterName;
        [TextArea] public string description;
        public Sprite icon;
        public GameObject monsterPrefab;
        public MonsterRarity rarity;
        public MonsterRole role;
        [Min(1)] public int maxHP = 100;
        [Min(0)] public int attack = 10;
        [Min(0)] public int defense = 5;
        [Min(0.1f)] public float attackRange = 1f;
        [Min(0.01f)] public float attackSpeed = 1f;
        [Min(0f)] public float moveSpeed = 1f;
        [Min(0)] public int goldCost;
        [Min(1)] public int unlockDungeonLevel = 1;
        [Min(1)] public int maxLevel = 5;
        [Min(0)] public int baseUpgradeGoldCost = 100;
        [Min(0)] public int baseUpgradeSoulCost;

        private void OnValidate()
        {
            if (string.IsNullOrWhiteSpace(monsterId)) Debug.LogError($"MonsterData '{name}' needs a stable monsterId.", this);
            if (string.IsNullOrWhiteSpace(monsterName)) Debug.LogError($"MonsterData '{name}' needs a name.", this);
            maxHP = Mathf.Max(1, maxHP); attack = Mathf.Max(0, attack); defense = Mathf.Max(0, defense);
            attackRange = Mathf.Max(.1f, attackRange); attackSpeed = Mathf.Max(.01f, attackSpeed); moveSpeed = Mathf.Max(0f, moveSpeed);
            goldCost = Mathf.Max(0, goldCost); unlockDungeonLevel = Mathf.Max(1, unlockDungeonLevel); maxLevel = Mathf.Max(1, maxLevel);
            baseUpgradeGoldCost = Mathf.Max(0, baseUpgradeGoldCost); baseUpgradeSoulCost = Mathf.Max(0, baseUpgradeSoulCost);
        }

        public int MaxHPAtLevel(int level) => Mathf.Max(1, Mathf.RoundToInt(maxHP * (1f + .20f * Mathf.Max(0, level - 1))));
        public int AttackAtLevel(int level) => Mathf.Max(0, Mathf.RoundToInt(attack * (1f + .10f * Mathf.Max(0, level - 1))));
        public int DefenseAtLevel(int level) => Mathf.Max(0, defense + 2 * Mathf.Max(0, level - 1));
    }
}
